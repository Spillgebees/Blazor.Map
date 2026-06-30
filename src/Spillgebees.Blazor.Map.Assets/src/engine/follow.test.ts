import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createFollowController,
  type FollowController,
  type FollowEntityStore,
  type FollowGestureGroup,
  type FollowOpData,
} from "./follow";
import type { FollowCameraConfig, FollowInteractionConfig } from "./ops";

const LAYER = "vehicles";
const ENTITY = "bus-1";

function camera(overrides: Partial<FollowCameraConfig> = {}): FollowCameraConfig {
  return { zoomMode: "free", orientationMode: "free", bearingSource: "keepcurrent", ...overrides };
}

function interaction(overrides: Partial<FollowInteractionConfig> = {}): FollowInteractionConfig {
  return { clearOnUserPan: true, clearWhenFeatureMissing: false, ...overrides };
}

function follow(overrides: Partial<FollowOpData> = {}): FollowOpData {
  return { layerId: LAYER, entityId: ENTITY, ...overrides };
}

function createFake() {
  let tick: ((now: number) => boolean) | null = null;
  // Frames advance 1s apart so the default 500ms engage window closes after the first step,
  // matching how real frames progress in time.
  let frameNow = 0;
  // The fake camera state. easeTo writes to it (like the real map), and the set* helpers below let a
  // test simulate the user moving the camera away from a held value so the controller can correct it.
  let zoom = 10;
  let pitch = 0;
  let bearing = 0;
  // Whether each gesture group's handlers are enabled, toggled by setInteractionEnabled (the lock).
  let zoomEnabled = true;
  let orientationEnabled = true;
  const easeTo = vi.fn<(options: Record<string, unknown>) => void>((options) => {
    if (typeof options.zoom === "number") {
      zoom = options.zoom;
    }
    if (typeof options.pitch === "number") {
      pitch = options.pitch;
    }
    if (typeof options.bearing === "number") {
      bearing = options.bearing;
    }
  });
  const listeners = new Map<string, (event: unknown) => void>();
  const records: FollowEntityStore["records"] = new Map();
  const onCleared = vi.fn<(reason: string) => void>();
  const setAnimation = vi.fn<(key: string, tick: ((now: number) => boolean) | null) => void>((_key, next) => {
    tick = next;
  });

  const controller: FollowController = createFollowController({
    map: {
      easeTo,
      getZoom: () => zoom,
      getPitch: () => pitch,
      getBearing: () => bearing,
      lockInteraction: (group) => {
        const wasEnabled = group === "zoom" ? zoomEnabled : orientationEnabled;
        if (group === "zoom") {
          zoomEnabled = false;
        } else {
          orientationEnabled = false;
        }
        return () => {
          if (group === "zoom") {
            zoomEnabled = wasEnabled;
          } else {
            orientationEnabled = wasEnabled;
          }
        };
      },
      on: (event, handler) => listeners.set(event, handler),
      off: (event) => listeners.delete(event),
    },
    scheduler: { setAnimation },
    getStore: (layerId) => (layerId === LAYER ? { records } : undefined),
    onCleared,
  });

  return {
    controller,
    easeTo,
    listeners,
    records,
    onCleared,
    setAnimation,
    setZoom: (value: number) => {
      zoom = value;
    },
    setPitch: (value: number) => {
      pitch = value;
    },
    setBearing: (value: number) => {
      bearing = value;
    },
    isEnabled: (group: FollowGestureGroup) => (group === "zoom" ? zoomEnabled : orientationEnabled),
    // simulate the host having disabled a gesture before the follow starts
    presetEnabled: (group: FollowGestureGroup, enabled: boolean) => {
      if (group === "zoom") {
        zoomEnabled = enabled;
      } else {
        orientationEnabled = enabled;
      }
    },
    step: (now?: number) => {
      frameNow += 1000;
      return tick?.(now ?? frameNow);
    },
    setEntity: (lng: number, lat: number, rot?: number) =>
      records.set(0, {
        id: ENTITY,
        primary: { geometry: { coordinates: [lng, lat] }, properties: rot == null ? {} : { rot } },
      }),
    lastEaseTo: () => easeTo.mock.calls.at(-1)?.[0] as Record<string, unknown> | undefined,
  };
}

describe("follow controller", () => {
  let fake: ReturnType<typeof createFake>;

  beforeEach(() => {
    fake = createFake();
  });

  describe("engage", () => {
    it("eases to the entity and applies a held zoom", () => {
      // arrange
      fake.setZoom(10);
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ camera: camera({ zoomMode: "anchored", zoom: 15 }) }));

      // act
      fake.step();

      // assert
      expect(fake.easeTo).toHaveBeenCalledTimes(1);
      expect(fake.lastEaseTo()?.center).toEqual([6, 49]);
      expect(fake.lastEaseTo()?.zoom).toBe(15);
    });

    it("holds the engage-time zoom when a mode holds but no zoom value is given", () => {
      // arrange
      fake.setZoom(12);
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ camera: camera({ zoomMode: "locked" }) }));

      // act
      fake.step();

      // assert: null zoom means "hold whatever the camera had at engage"
      expect(fake.lastEaseTo()?.zoom).toBe(12);
    });

    it("applies pitch, fixed bearing, and offset when orientation holds", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(
        follow({
          camera: camera({
            orientationMode: "anchored",
            pitch: 45,
            bearingSource: "fixed",
            bearing: 90,
            offset: { x: 10, y: 20 },
          }),
        }),
      );

      // act
      fake.step();

      // assert
      const options = fake.lastEaseTo();
      expect(options?.pitch).toBe(45);
      expect(options?.bearing).toBe(90);
      expect(options?.offset).toEqual([10, 20]);
    });

    it("leaves zoom/pitch/bearing untouched when every mode is free", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow());

      // act
      fake.step();

      // assert
      const options = fake.lastEaseTo();
      expect(options?.zoom).toBeUndefined();
      expect(options?.pitch).toBeUndefined();
      expect(options?.bearing).toBeUndefined();
    });

    it("honours a linear engage easing and leaves the native curve otherwise", () => {
      // arrange: linear request
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ animation: { durationMs: 400, easing: "linear" } }));

      // act
      fake.step();

      // assert: linear is passed as an explicit easing function
      expect(typeof fake.lastEaseTo()?.easing).toBe("function");
      expect((fake.lastEaseTo()?.easing as (t: number) => number)(0.5)).toBe(0.5);

      // arrange + act: default (no animation) keeps easeTo's native ease-in-out
      const other = createFake();
      other.setEntity(6, 49);
      other.controller.apply(follow());
      other.step();

      // assert: no override, so the map's default curve applies
      expect(other.lastEaseTo()?.easing).toBeUndefined();
    });

    it("does not let tracking interrupt an animated engage while the entity moves", () => {
      // arrange: an animated (600ms) engage on a moving entity
      fake.setZoom(10);
      fake.setEntity(6, 49);
      fake.controller.apply(
        follow({ camera: camera({ zoomMode: "anchored", zoom: 13 }), animation: { durationMs: 600 } }),
      );

      // act: engage at t=0, then a frame still inside the engage window with the entity having moved
      fake.step(0);
      expect(fake.easeTo).toHaveBeenCalledTimes(1);
      expect(fake.lastEaseTo()?.zoom).toBe(13);
      fake.setEntity(6.2, 49.2);
      fake.step(300); // 300ms < 600ms engage window

      // assert: no recentre fired, so the engage animation (zoom ramp) is left to finish
      expect(fake.easeTo).toHaveBeenCalledTimes(1);

      // act: once the window passes, tracking resumes
      fake.step(700);

      // assert: now it recentres on the moved entity
      expect(fake.easeTo).toHaveBeenCalledTimes(2);
      expect(fake.lastEaseTo()?.center).toEqual([6.2, 49.2]);
      expect(fake.lastEaseTo()?.duration).toBe(0);
    });

    it("engages when the entity only appears in a later frame", () => {
      // arrange: entity absent at apply time
      fake.setZoom(10);
      fake.controller.apply(follow({ camera: camera({ zoomMode: "anchored", zoom: 13 }) }));

      // act + assert: holds, no move
      expect(fake.step()).toBe(true);
      expect(fake.easeTo).not.toHaveBeenCalled();

      // act: entity appears
      fake.setEntity(6.2, 49.2);
      fake.step();

      // assert: first resolution engages (held zoom applied)
      expect(fake.lastEaseTo()?.center).toEqual([6.2, 49.2]);
      expect(fake.lastEaseTo()?.zoom).toBe(13);
    });
  });

  describe("tracking", () => {
    it("re-centres on movement and keeps a held pitch pinned", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ camera: camera({ orientationMode: "anchored", pitch: 30 }) }));
      fake.step(); // engage

      // act
      fake.setEntity(6.3, 49.3);
      fake.step();

      // assert
      expect(fake.easeTo).toHaveBeenCalledTimes(2);
      const options = fake.lastEaseTo();
      expect(options?.center).toEqual([6.3, 49.3]);
      expect(options?.duration).toBe(0);
      expect(options?.pitch).toBe(30);
    });

    it("uses instant tracking moves by default", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ animation: { durationMs: 0 } }));
      fake.step();

      // act
      fake.setEntity(6.3, 49.3);
      fake.step();

      // assert
      expect(fake.lastEaseTo()?.duration).toBe(0);
      expect(fake.lastEaseTo()?.easing).toBeUndefined();
    });

    it("applies configured ease-in-out tracking animation without overriding native easing", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(
        follow({
          camera: camera({ trackingAnimation: { durationMs: 250, easing: "easeInOut" } }),
          animation: { durationMs: 0 },
        }),
      );
      fake.step();

      // act
      fake.setEntity(6.3, 49.3);
      fake.step();

      // assert
      expect(fake.lastEaseTo()?.duration).toBe(250);
      expect(fake.lastEaseTo()?.easing).toBeUndefined();
    });

    it("keeps configured tracking animation for jumped updates after idle", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(
        follow({
          camera: camera({ trackingAnimation: { durationMs: 250, easing: "easeInOut" } }),
          animation: { durationMs: 0 },
        }),
      );
      fake.step(0);

      // act
      fake.setEntity(6.3, 49.3);
      fake.step(1000);

      // assert
      expect(fake.lastEaseTo()?.duration).toBe(250);
      expect(fake.lastEaseTo()?.easing).toBeUndefined();
    });

    it("does not restart configured tracking animation for continuous updates", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(
        follow({
          camera: camera({ trackingAnimation: { durationMs: 250, easing: "easeInOut" } }),
          animation: { durationMs: 0 },
        }),
      );
      fake.step(0);

      // act
      fake.setEntity(6.01, 49.01);
      fake.step(16);
      fake.setEntity(6.02, 49.02);
      fake.step(32);

      // assert
      expect(fake.easeTo).toHaveBeenCalledTimes(3);
      expect(fake.easeTo.mock.calls[1]?.[0].duration).toBe(0);
      expect(fake.easeTo.mock.calls[2]?.[0].duration).toBe(0);
    });

    it("applies configured linear tracking animation", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(
        follow({
          camera: camera({ trackingAnimation: { durationMs: 250, easing: "linear" } }),
          animation: { durationMs: 0 },
        }),
      );
      fake.step();

      // act
      fake.setEntity(6.3, 49.3);
      fake.step();

      // assert
      expect(fake.lastEaseTo()?.duration).toBe(250);
      expect(typeof fake.lastEaseTo()?.easing).toBe("function");
      expect((fake.lastEaseTo()?.easing as (t: number) => number)(0.5)).toBe(0.5);
    });

    it("keeps engage animation separate from tracking animation", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(
        follow({
          camera: camera({ trackingAnimation: { durationMs: 300, easing: "linear" } }),
          animation: { durationMs: 700, easing: "easeInOut" },
        }),
      );

      // act
      fake.step(0);
      const engageOptions = fake.lastEaseTo();
      fake.setEntity(6.3, 49.3);
      fake.step(800);

      // assert
      expect(engageOptions?.duration).toBe(700);
      expect(engageOptions?.easing).toBeUndefined();
      expect(fake.lastEaseTo()?.duration).toBe(300);
      expect(typeof fake.lastEaseTo()?.easing).toBe("function");
    });

    it("pauses tracking while the pointer is held, then resumes on release", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow());
      fake.step(); // engage
      expect(fake.easeTo).toHaveBeenCalledTimes(1);

      // act: pointer down, entity moves, frame ticks
      fake.listeners.get("mousedown")?.({});
      fake.setEntity(6.2, 49.2);
      fake.step();

      // assert: no recentre while the gesture is in progress
      expect(fake.easeTo).toHaveBeenCalledTimes(1);

      // act: release, then a frame
      fake.listeners.get("mouseup")?.({});
      fake.step();

      // assert: tracking resumes and recentres on the entity's current position
      expect(fake.easeTo).toHaveBeenCalledTimes(2);
      expect(fake.lastEaseTo()?.center).toEqual([6.2, 49.2]);
    });

    it("pauses tracking briefly after a wheel tick, then resumes", () => {
      // arrange: instant engage so the engage gate doesn't mask the wheel pause
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ animation: { durationMs: 0 } }));
      fake.step(1000); // engage; lastFrameNow = 1000
      expect(fake.easeTo).toHaveBeenCalledTimes(1);

      // act: wheel tick opens a ~400ms grace, entity moves, frame inside the window
      fake.listeners.get("wheel")?.({});
      fake.setEntity(6.2, 49.2);
      fake.step(1200);

      // assert: no recentre fights the zoom
      expect(fake.easeTo).toHaveBeenCalledTimes(1);

      // act: a frame past the grace window
      fake.step(1600);

      // assert: tracking resumes
      expect(fake.easeTo).toHaveBeenCalledTimes(2);
      expect(fake.lastEaseTo()?.center).toEqual([6.2, 49.2]);
    });

    it("skips the move when the position is unchanged and nothing is held", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow());
      fake.step(); // engage

      // act
      fake.step();
      fake.step();

      // assert: only the engage move
      expect(fake.easeTo).toHaveBeenCalledTimes(1);
    });

    it("does not recentre on rotation alone when bearing is not tracked", () => {
      // arrange: entity carries a rotation but every mode is free
      fake.setEntity(6, 49, 30);
      fake.controller.apply(follow());
      fake.step(); // engage

      // act: position unchanged, only a later frame
      fake.step();

      // assert: the spurious-bearing recentre is gone, only the engage move stands
      expect(fake.easeTo).toHaveBeenCalledTimes(1);
    });

    it("eases the bearing toward the heading instead of snapping in match-heading mode", () => {
      // arrange: instant engage at heading 0 so the eased frames are not masked by the engage window
      fake.setEntity(6, 49, 0);
      fake.controller.apply(
        follow({
          camera: camera({ orientationMode: "anchored", bearingSource: "matchheading" }),
          animation: { durationMs: 0 },
        }),
      );
      fake.step(0); // engage uses the heading
      expect(fake.lastEaseTo()?.bearing).toBe(0);

      // act: heading jumps to 90; a single short frame should only move part-way, not snap
      fake.setEntity(6, 49, 90);
      fake.step(16);

      // assert: eased a fraction of the turn, nowhere near the full 90
      const partial = fake.lastEaseTo()?.bearing as number;
      expect(partial).toBeGreaterThan(0);
      expect(partial).toBeLessThan(45);

      // act: keep ticking at the new heading
      for (let now = 32; now <= 1000; now += 16) {
        fake.step(now);
      }

      // assert: it converges on the heading
      expect(fake.lastEaseTo()?.bearing as number).toBeCloseTo(90, 0);
    });
  });

  describe("holding camera targets", () => {
    it("holds a fixed zoom, snapping back when the user zooms away", () => {
      // arrange
      fake.setZoom(10);
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ camera: camera({ zoomMode: "anchored", zoom: 15 }) }));
      fake.step(); // engage -> zoom 15
      expect(fake.lastEaseTo()?.zoom).toBe(15);

      // act: user zooms out while the entity is stationary
      fake.setZoom(11);
      fake.step();

      // assert: the held zoom is re-asserted even though the position did not change
      expect(fake.lastEaseTo()?.zoom).toBe(15);
    });

    it("holds a fixed pitch, re-applying it on each tracking frame", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ camera: camera({ orientationMode: "anchored", pitch: 45 }) }));
      fake.step(); // engage -> pitch 45
      expect(fake.lastEaseTo()?.pitch).toBe(45);

      // act
      fake.setEntity(6.1, 49.1);
      fake.step();

      // assert
      expect(fake.lastEaseTo()?.pitch).toBe(45);
    });

    it("holds a fixed bearing, restoring it after the user rotates away", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(
        follow({ camera: camera({ orientationMode: "anchored", bearingSource: "fixed", bearing: 90 }) }),
      );
      fake.step(); // engage -> bearing 90
      expect(fake.lastEaseTo()?.bearing).toBe(90);

      // act: user rotates away while stationary
      fake.setBearing(20);
      fake.step();

      // assert: snapped back to the fixed bearing
      expect(fake.lastEaseTo()?.bearing).toBe(90);
    });
  });

  describe("locking gestures", () => {
    it("locks the zoom gesture on engage and restores it on clear", () => {
      // arrange + act
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ camera: camera({ zoomMode: "locked", zoom: 15 }) }));

      // assert: zoom handlers disabled, orientation untouched
      expect(fake.isEnabled("zoom")).toBe(false);
      expect(fake.isEnabled("orientation")).toBe(true);

      // act: clearing restores the gesture
      fake.controller.clear();
      expect(fake.isEnabled("zoom")).toBe(true);
    });

    it("locks the rotate-and-tilt gesture as a unit and restores it on clear", () => {
      // arrange + act
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ camera: camera({ orientationMode: "locked", pitch: 30 }) }));

      // assert
      expect(fake.isEnabled("orientation")).toBe(false);
      expect(fake.isEnabled("zoom")).toBe(true);

      // act
      fake.controller.clear();
      expect(fake.isEnabled("orientation")).toBe(true);
    });

    it("leaves a gesture the host already disabled disabled after clearing", () => {
      // arrange: host disabled zoom before any follow
      fake.presetEnabled("zoom", false);
      fake.setEntity(6, 49);

      // act
      fake.controller.apply(follow({ camera: camera({ zoomMode: "locked", zoom: 15 }) }));
      expect(fake.isEnabled("zoom")).toBe(false);
      fake.controller.clear();

      // assert: we never disabled it, so we do not re-enable it
      expect(fake.isEnabled("zoom")).toBe(false);
    });
  });

  describe("interaction clearing", () => {
    it("clears on user drag and notifies once", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow());
      fake.step();

      // act
      fake.listeners.get("dragstart")?.({});

      // assert
      expect(fake.onCleared).toHaveBeenCalledExactlyOnceWith("userinteraction");
      // tracking stopped: a later frame does nothing
      fake.setEntity(7, 50);
      fake.step();
      expect(fake.easeTo).toHaveBeenCalledTimes(1);
    });

    it("clears on a real user zoom when zoom mode is clear, ignoring programmatic moves", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ camera: camera({ zoomMode: "clear" }) }));
      fake.step();

      // act: programmatic (no originalEvent) does not clear
      fake.listeners.get("zoomstart")?.({});
      expect(fake.onCleared).not.toHaveBeenCalled();

      // act: a real gesture clears
      fake.listeners.get("zoomstart")?.({ originalEvent: new Event("wheel") });

      // assert
      expect(fake.onCleared).toHaveBeenCalledExactlyOnceWith("userinteraction");
    });

    it("clears on a real rotate or pitch when orientation mode is clear", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ camera: camera({ orientationMode: "clear" }) }));
      fake.step();

      // act + assert: rotate gesture clears
      fake.listeners.get("rotatestart")?.({ originalEvent: new Event("pointermove") });
      expect(fake.onCleared).toHaveBeenCalledExactlyOnceWith("userinteraction");

      // arrange: a fresh follow, this time exercise the pitch gesture
      const other = createFake();
      other.setEntity(6, 49);
      other.controller.apply(follow({ camera: camera({ orientationMode: "clear" }) }));
      other.step();

      // act + assert: pitch gesture clears
      other.listeners.get("pitchstart")?.({ originalEvent: new Event("pointermove") });
      expect(other.onCleared).toHaveBeenCalledExactlyOnceWith("userinteraction");
    });

    it("does not clear on drag when opted out", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ interaction: interaction({ clearOnUserPan: false }) }));
      fake.step();

      // act
      fake.listeners.get("dragstart")?.({});

      // assert
      expect(fake.onCleared).not.toHaveBeenCalled();
    });
  });

  describe("missing entity", () => {
    it("holds by default when the entity disappears", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow());
      fake.step();
      fake.records.clear();

      // act + assert: holds, no notify
      expect(fake.step()).toBe(true);
      expect(fake.onCleared).not.toHaveBeenCalled();
    });

    it("clears and notifies when clearWhenFeatureMissing is set", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow({ interaction: interaction({ clearWhenFeatureMissing: true }) }));
      fake.step();
      fake.records.clear();

      // act
      const alive = fake.step();

      // assert
      expect(alive).toBe(false);
      expect(fake.onCleared).toHaveBeenCalledExactlyOnceWith("featuremissing");
    });

    it("throttles the full rescan while the entity is absent", () => {
      // arrange: follow a target that is not in the store yet
      fake.controller.apply(follow());

      // act + assert: the first frame scans, finds nothing, and arms the rescan throttle
      fake.step(0);
      expect(fake.easeTo).not.toHaveBeenCalled();

      // the entity appears, but a frame inside the throttle window does not rescan for it yet
      fake.setEntity(6, 49);
      fake.step(100);
      expect(fake.easeTo).not.toHaveBeenCalled();

      // a frame past the window rescans, finds it, and engages
      fake.step(300);
      expect(fake.easeTo).toHaveBeenCalledTimes(1);
      expect(fake.lastEaseTo()?.center).toEqual([6, 49]);
    });
  });

  describe("lifecycle", () => {
    it("clear() stops tracking without notifying", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow());
      fake.step();

      // act
      fake.controller.clear();

      // assert: scheduler stopped, no echo to .NET, listeners detached
      expect(fake.setAnimation).toHaveBeenLastCalledWith("camera:follow", null);
      expect(fake.onCleared).not.toHaveBeenCalled();
      expect(fake.listeners.size).toBe(0);
    });

    it("re-targeting detaches the previous interaction listeners", () => {
      // arrange
      fake.setEntity(6, 49);
      fake.controller.apply(follow());
      const firstDragStart = fake.listeners.get("dragstart");

      // act: re-target
      fake.controller.apply(follow({ entityId: "bus-2" }));

      // assert: the old listener was removed and replaced (4 clear + 7 pause/resume/wheel listeners)
      expect(fake.listeners.get("dragstart")).not.toBe(firstDragStart);
      expect(fake.listeners.size).toBe(11);
    });
  });
});
