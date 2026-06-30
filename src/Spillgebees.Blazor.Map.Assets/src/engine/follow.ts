import type { AnimationConfig, FollowCameraConfig, FollowGestureMode, FollowInteractionConfig } from "./ops";
import type { Scheduler } from "./scheduler";

// Camera follow controller. Drives the camera to track one tracked entity by reading its
// already-interpolated position from the entity store each frame, riding the engine's existing
// scheduler loop rather than starting its own.
//
// Each camera gesture has a mode. "free" leaves it to the user; "anchored" and "locked" hold a target
// (anchored lets the user nudge it and springs back, locked disables the gesture); "clear" ends the
// follow when the user uses it. Zoom is one gesture; pitch and bearing share one (MapLibre's
// drag-to-rotate tilts and turns together), so they share the orientation mode.

const ANIMATION_KEY = "camera:follow";
const DEFAULT_ANIMATION_MS = 500;
const POSITION_EPSILON = 1e-9;
const ZOOM_EPSILON = 1e-3;
const PITCH_EPSILON = 0.01;
const BEARING_EPSILON = 0.05;
// Time constant for easing the camera bearing toward a feature heading. Larger is gentler; this gives a
// smooth ~0.3s settle that takes the jarring snap out of sharp turns without feeling laggy.
const BEARING_SMOOTHING_TAU_MS = 150;
// While the followed entity is absent the cache misses, so resolveRecord would otherwise re-scan the
// whole store every frame. Throttle those failed scans so a large layer pays the O(n) cost at most this
// often (a present entity is still resolved every frame via the O(1) index cache).
const MISSING_RESCAN_MS = 250;
// How long tracking stays paused after a wheel tick. Wheel zoom has no end event, so we hold off the
// recentre for a short window after the last notch (refreshed on each one) and resume once it settles.
const WHEEL_PAUSE_MS = 400;
const CONTINUOUS_TRACKING_FRAME_MS = 50;

/** Why the engine cleared a follow, mirrored by the lowercase names of the .NET MapFollowChangeReason. */
export type FollowClearReason = "userinteraction" | "featuremissing";

/** The gesture groups the controller can lock, each mapping to a set of MapLibre interaction handlers. */
export type FollowGestureGroup = "zoom" | "orientation";

const DEFAULT_INTERACTION: FollowInteractionConfig = {
  clearOnUserPan: true,
  clearWhenFeatureMissing: false,
};

/** The followed record's live (already-interpolated) position + rotation, read each frame. */
export interface FollowRecord {
  id: string;
  primary: { geometry: { coordinates: number[] }; properties: Record<string, unknown> };
}

/** The subset of the entity store the controller reads. */
export interface FollowEntityStore {
  records: Map<number, FollowRecord>;
}

/** The subset of the engine map the controller needs. */
export interface FollowMap {
  easeTo(options: Record<string, unknown>): void;
  getZoom(): number;
  getPitch(): number;
  getBearing(): number;
  /**
   * Disables a gesture group's MapLibre handlers and returns a thunk that restores each handler to the
   * exact enabled state it had, so locking never re-enables a handler the host had disabled itself.
   */
  lockInteraction(group: FollowGestureGroup): () => void;
  on(event: string, handler: (event: unknown) => void): unknown;
  off(event: string, handler: (event: unknown) => void): unknown;
}

export interface FollowControllerDeps {
  map: FollowMap;
  scheduler: Pick<Scheduler, "setAnimation">;
  getStore: (layerId: string) => FollowEntityStore | undefined;
  /** Notifies .NET of a follow cleared by the engine (user interaction or missing entity). */
  onCleared: (reason: FollowClearReason) => void;
}

export interface FollowOpData {
  layerId: string;
  entityId: string;
  camera?: FollowCameraConfig | null;
  animation?: AnimationConfig | null;
  interaction?: FollowInteractionConfig | null;
}

export interface FollowController {
  apply(op: FollowOpData): void;
  /** Clears without notifying .NET (the clear originated there). */
  clear(): void;
  dispose(): void;
}

interface FollowTarget {
  layerId: string;
  entityId: string;
  camera: FollowCameraConfig | null;
  animationMs: number;
  // The engage move honours the requested easing. easeTo's native curve is already ease-in-out,
  // so we only override it for a linear request; the follow default (no animation) stays ease-in-out.
  linearEngage: boolean;
  trackingAnimationMs: number;
  linearTracking: boolean;
  interaction: FollowInteractionConfig;
}

const LINEAR_EASING = (t: number): number => t;

function holdsTarget(mode: FollowGestureMode): boolean {
  return mode === "anchored" || mode === "locked";
}

export function createFollowController(deps: FollowControllerDeps): FollowController {
  let target: FollowTarget | null = null;
  let cachedIndex: number | null = null;
  // Frame time before which a failed full scan is not retried, throttling rescans while the entity is absent.
  let nextScanAt = 0;
  let hasEngaged = false;
  // Frame time until which the animated engage move owns the camera; per-frame tracking is held off
  // until then so a moving entity's recentres don't interrupt (and freeze) the engage animation.
  let engageUntil = 0;
  // True while the user holds the pointer on the map. The per-frame recentre calls easeTo, which cancels
  // any in-flight gesture, so a continuously-recentred camera never lets a drag register (its dragstart
  // never fires). Pausing tracking for the duration of the press lets the gesture through.
  let paused = false;
  // Frame time of the last tick, and a deadline after the last wheel tick during which tracking stays
  // paused (wheel has no press to hold, so it gets a grace window instead).
  let lastFrameNow = 0;
  let pausedUntil = 0;
  let lastMoveAt = Number.NEGATIVE_INFINITY;
  let lastLng = Number.NaN;
  let lastLat = Number.NaN;
  // Resolved targets, fixed at engage. null zoom/pitch means that gesture's mode does not hold a target.
  let holdZoom: number | null = null;
  let holdPitch: number | null = null;
  let bearingHeld = false;
  // The static bearing target (keep-current / fixed); match-heading recomputes from the entity instead.
  let staticBearing = 0;
  // The bearing actually applied, eased toward the target each frame in match-heading mode.
  let appliedBearing = 0;
  // Restore thunks for gesture groups this controller locked, called on clear or re-target.
  const lockReleases = new Map<FollowGestureGroup, () => void>();
  const listeners: { event: string; handler: (event: unknown) => void }[] = [];

  function modeFor(active: FollowTarget | null, group: FollowGestureGroup): FollowGestureMode {
    if (!active?.camera) {
      return "free";
    }
    return group === "zoom" ? active.camera.zoomMode : active.camera.orientationMode;
  }

  function apply(op: FollowOpData): void {
    detachListeners();
    restoreLocks();
    target = {
      layerId: op.layerId,
      entityId: op.entityId,
      camera: op.camera ?? null,
      animationMs: op.animation?.durationMs ?? DEFAULT_ANIMATION_MS,
      linearEngage: op.animation?.easing === "linear",
      trackingAnimationMs: op.camera?.trackingAnimation?.durationMs ?? 0,
      linearTracking: op.camera?.trackingAnimation?.easing === "linear",
      interaction: op.interaction ?? DEFAULT_INTERACTION,
    };
    cachedIndex = null;
    nextScanAt = 0;
    hasEngaged = false;
    engageUntil = 0;
    paused = false;
    lastFrameNow = 0;
    pausedUntil = 0;
    lastMoveAt = Number.NEGATIVE_INFINITY;
    lastLng = Number.NaN;
    lastLat = Number.NaN;
    holdZoom = null;
    holdPitch = null;
    bearingHeld = false;
    staticBearing = 0;
    appliedBearing = 0;
    attachListeners(target.interaction);
    applyLocks(target);
    deps.scheduler.setAnimation(ANIMATION_KEY, tick);
  }

  function clear(): void {
    stop();
  }

  function dispose(): void {
    stop();
  }

  function stop(): void {
    deps.scheduler.setAnimation(ANIMATION_KEY, null);
    detachListeners();
    restoreLocks();
    target = null;
    cachedIndex = null;
    hasEngaged = false;
    paused = false;
    pausedUntil = 0;
  }

  function clearAndNotify(reason: FollowClearReason): void {
    stop();
    deps.onCleared(reason);
  }

  function tick(now: number): boolean {
    if (!target) {
      return false;
    }

    const dt = Math.max(0, now - lastFrameNow);
    lastFrameNow = now;

    const record = resolveRecord(target);
    if (!record) {
      if (target.interaction.clearWhenFeatureMissing) {
        clearAndNotify("featuremissing");
        return false;
      }
      return true;
    }

    const [lng, lat] = record.primary.geometry.coordinates;
    const rot = typeof record.primary.properties.rot === "number" ? record.primary.properties.rot : undefined;

    if (!hasEngaged) {
      resolveTargets(target, rot);
      applyEngageMove(target, lng, lat);
      hasEngaged = true;
      engageUntil = now + target.animationMs;
      lastMoveAt = now;
      lastLng = lng;
      lastLat = lat;
      return true;
    }

    // hold tracking while the engage move animates, so it can complete its zoom/pitch/bearing ramp
    // instead of being cut short by a recentre on the very next frame.
    if (now < engageUntil) {
      return true;
    }

    // the user is interacting with the map; don't recentre over their gesture (a drag or wheel zoom
    // clears or adjusts the camera through its own start event, which only fires once we stop fighting).
    if (paused || now < pausedUntil) {
      return true;
    }

    // Resolve this frame's bearing: match-heading eases toward the entity heading; a static target is
    // pinned. The eased value is still converging until it is within an epsilon of its target.
    let trackBearing: number | undefined;
    let bearingConverging = false;
    if (bearingHeld) {
      if (target.camera?.bearingSource === "matchheading") {
        const headingTarget = typeof rot === "number" ? rot : appliedBearing;
        const factor = 1 - Math.exp(-dt / BEARING_SMOOTHING_TAU_MS);
        appliedBearing = normalizeBearing(appliedBearing + shortestAngleDelta(headingTarget, appliedBearing) * factor);
        bearingConverging = Math.abs(shortestAngleDelta(headingTarget, appliedBearing)) > BEARING_EPSILON;
      } else {
        appliedBearing = staticBearing;
      }
      trackBearing = appliedBearing;
    }

    // Re-assert the held targets so they stay put, not just at engage. Fire a track move when the entity
    // moved, the bearing is still easing, or a held value drifted (the user moved the camera off it);
    // otherwise stay idle so a parked camera does no work.
    const positionMoved = Math.abs(lng - lastLng) > POSITION_EPSILON || Math.abs(lat - lastLat) > POSITION_EPSILON;
    const zoomDrifted = holdZoom != null && Math.abs(deps.map.getZoom() - holdZoom) > ZOOM_EPSILON;
    const pitchDrifted = holdPitch != null && Math.abs(deps.map.getPitch() - holdPitch) > PITCH_EPSILON;
    const bearingDrifted =
      trackBearing != null && Math.abs(shortestAngleDelta(trackBearing, deps.map.getBearing())) > BEARING_EPSILON;

    if (positionMoved || bearingConverging || zoomDrifted || pitchDrifted || bearingDrifted) {
      applyTrackMove(target, lng, lat, trackBearing, shouldAnimateTrackMove(target, now, positionMoved));
      if (positionMoved) {
        lastMoveAt = now;
        lastLng = lng;
        lastLat = lat;
      }
    }

    return true;
  }

  // Resolve by numeric index, validating the cache each frame so it stays O(1); only re-scan when the
  // entity is new or its recycled index changed (a structural event).
  function resolveRecord(active: FollowTarget): FollowRecord | undefined {
    const store = deps.getStore(active.layerId);
    if (!store) {
      return undefined;
    }

    if (cachedIndex !== null) {
      const cached = store.records.get(cachedIndex);
      if (cached && cached.id === active.entityId) {
        return cached;
      }
    }

    // A cache miss needs a full O(n) scan. While the entity is absent this would repeat every frame, so
    // throttle the failed scans; an entity that only changed index still resolves on the next attempt.
    if (lastFrameNow < nextScanAt) {
      return undefined;
    }

    for (const [index, record] of store.records) {
      if (record.id === active.entityId) {
        cachedIndex = index;
        return record;
      }
    }

    cachedIndex = null;
    nextScanAt = lastFrameNow + MISSING_RESCAN_MS;
    return undefined;
  }

  // Fix the held targets at engage. A null zoom/pitch means "hold whatever the camera had at engage".
  function resolveTargets(active: FollowTarget, rot: number | undefined): void {
    const camera = active.camera;

    holdZoom = camera && holdsTarget(camera.zoomMode) ? (camera.zoom ?? deps.map.getZoom()) : null;

    if (camera && holdsTarget(camera.orientationMode)) {
      holdPitch = camera.pitch ?? deps.map.getPitch();
      bearingHeld = true;
      if (camera.bearingSource === "matchheading") {
        appliedBearing = typeof rot === "number" ? rot : deps.map.getBearing();
      } else {
        staticBearing =
          camera.bearingSource === "fixed" ? (camera.bearing ?? deps.map.getBearing()) : deps.map.getBearing();
        appliedBearing = staticBearing;
      }
    } else {
      holdPitch = null;
      bearingHeld = false;
    }
  }

  function applyEngageMove(active: FollowTarget, lng: number, lat: number): void {
    const options: Record<string, unknown> = { center: [lng, lat], duration: active.animationMs };

    if (active.linearEngage) {
      options.easing = LINEAR_EASING;
    }

    if (active.camera?.offset) {
      options.offset = [active.camera.offset.x, active.camera.offset.y];
    }

    if (holdZoom != null) {
      options.zoom = holdZoom;
    }

    if (holdPitch != null) {
      options.pitch = holdPitch;
    }

    if (bearingHeld) {
      options.bearing = appliedBearing;
    }

    deps.map.easeTo(options);
  }

  function shouldAnimateTrackMove(active: FollowTarget, now: number, positionMoved: boolean): boolean {
    // Interpolated entities update every frame. A non-zero easeTo duration would be cancelled and
    // restarted on each frame, so only apply tracking ease after a short gap that marks a discrete jump.
    return positionMoved && active.trackingAnimationMs > 0 && now - lastMoveAt >= CONTINUOUS_TRACKING_FRAME_MS;
  }

  function applyTrackMove(
    active: FollowTarget,
    lng: number,
    lat: number,
    bearing: number | undefined,
    animate: boolean,
  ): void {
    const options: Record<string, unknown> = { center: [lng, lat], duration: animate ? active.trackingAnimationMs : 0 };

    if (animate && active.linearTracking) {
      options.easing = LINEAR_EASING;
    }

    if (active.camera?.offset) {
      options.offset = [active.camera.offset.x, active.camera.offset.y];
    }

    if (holdZoom != null) {
      options.zoom = holdZoom;
    }

    if (holdPitch != null) {
      options.pitch = holdPitch;
    }

    if (bearing != null) {
      options.bearing = bearing;
    }

    deps.map.easeTo(options);
  }

  // Disable the MapLibre handlers for any locked gesture group, keeping the restore thunk so we can put
  // each handler back exactly as it was on clear or re-target.
  function applyLocks(active: FollowTarget): void {
    for (const group of ["zoom", "orientation"] as const) {
      if (modeFor(active, group) === "locked" && !lockReleases.has(group)) {
        lockReleases.set(group, deps.map.lockInteraction(group));
      }
    }
  }

  function restoreLocks(): void {
    for (const release of lockReleases.values()) {
      release();
    }
    lockReleases.clear();
  }

  function attachListeners(interaction: FollowInteractionConfig): void {
    // Pause tracking for the duration of a pointer press so the user's gesture is recognised instead of
    // being overwritten by the per-frame recentre. Resume on release if the follow is still active.
    addListener("mousedown", pause);
    addListener("touchstart", pause);
    addListener("mouseup", resume);
    addListener("touchend", resume);
    // Backstops so a lost pointer-up never freezes tracking: a cancelled touch, and moveend (MapLibre
    // ends a drag through its own window listeners and fires this even when the mouse-up lands off-canvas).
    addListener("touchcancel", resume);
    addListener("moveend", resume);
    addListener("wheel", onWheel);

    // A drag is always user-originated, so it clears without requiring an originalEvent. Zoom and the
    // rotate/pitch gestures also fire for programmatic moves (including the follow's own easeTo), so they
    // clear only on a real gesture (one carrying an originalEvent) and only when that gesture's mode is
    // "clear".
    addListener("dragstart", () => {
      if (interaction.clearOnUserPan) {
        clearAndNotify("userinteraction");
      }
    });
    addListener("zoomstart", (event) => {
      if (modeFor(target, "zoom") === "clear" && hasOriginalEvent(event)) {
        clearAndNotify("userinteraction");
      }
    });
    addListener("rotatestart", (event) => {
      if (modeFor(target, "orientation") === "clear" && hasOriginalEvent(event)) {
        clearAndNotify("userinteraction");
      }
    });
    addListener("pitchstart", (event) => {
      if (modeFor(target, "orientation") === "clear" && hasOriginalEvent(event)) {
        clearAndNotify("userinteraction");
      }
    });
  }

  function pause(): void {
    paused = true;
  }

  function resume(): void {
    paused = false;
  }

  function onWheel(): void {
    pausedUntil = lastFrameNow + WHEEL_PAUSE_MS;
  }

  function addListener(event: string, handler: (event: unknown) => void): void {
    deps.map.on(event, handler);
    listeners.push({ event, handler });
  }

  function detachListeners(): void {
    for (const { event, handler } of listeners) {
      deps.map.off(event, handler);
    }
    listeners.length = 0;
  }

  return { apply, clear, dispose };
}

function hasOriginalEvent(event: unknown): boolean {
  return (
    typeof event === "object" &&
    event !== null &&
    "originalEvent" in event &&
    Boolean((event as { originalEvent?: unknown }).originalEvent)
  );
}

// Smallest signed rotation (degrees, in (-180, 180]) from current to target, so easing always turns the
// short way around the compass.
function shortestAngleDelta(target: number, current: number): number {
  let delta = (target - current) % 360;
  if (delta > 180) {
    delta -= 360;
  }
  if (delta < -180) {
    delta += 360;
  }
  return delta;
}

function normalizeBearing(deg: number): number {
  return ((deg % 360) + 360) % 360;
}
