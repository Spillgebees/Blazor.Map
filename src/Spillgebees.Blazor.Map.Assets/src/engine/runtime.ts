// Engine runtime: owns per-map scene state,
// dispatches ops in array order, and replays the scene after a style change. The op
// store kept here is the single copy of scene state — there is no C#-side mirror.

import type { FeatureCollection } from "geojson";
import {
  animationTick,
  applyMotion,
  applySelection,
  applyUpsert,
  collectFlush,
  createEntityLayerStore,
  decorationSourceId,
  type EntityLayerStore,
  easeInOut,
  entityIndexOfFeatureId,
  type FlushPlan,
  featureIdsOf,
  setHoverBoost,
  sourceForFeatureId,
} from "./entities";
import { createFollowController, type FollowClearReason } from "./follow";
import { decodeMotionFrame } from "./motion";
import type {
  ControlContentEvents,
  ControlData,
  EventHandlers,
  LatLng,
  MapConfigData,
  MarkerData,
  MarkerPopupData,
  Op,
  PopupData,
} from "./ops";
import { createScheduler, type Scheduler } from "./scheduler";
import { createVisibilityController, styleLayerInfo } from "./visibility";

const SLOT_LAYER_PREFIX = "sgb-slot:";

/** The MapLibre surface the engine touches — structural, so tests can stub it. */
export interface EngineMap {
  addSource(id: string, spec: Record<string, unknown>): void;
  removeSource(id: string): void;
  getSource(id: string): unknown;
  addLayer(spec: Record<string, unknown>, beforeId?: string): void;
  removeLayer(id: string): void;
  getLayer(id: string): unknown;
  moveLayer(id: string, beforeId?: string): void;
  setPaintProperty(id: string, name: string, value: unknown): void;
  setLayoutProperty(id: string, name: string, value: unknown): void;
  setFilter(id: string, filter: unknown): void;
  setLayerZoomRange(id: string, min: number, max: number): void;
  setFeatureState(target: { source: string; id: number | string }, state: Record<string, unknown>): void;
  removeFeatureState(target: { source: string; id: number | string }): void;
  easeTo(options: Record<string, unknown>): void;
  getZoom(): number;
  getBearing(): number;
  getPitch(): number;
  /** Disables a gesture group's interaction handlers and returns a thunk that restores their prior state. */
  lockInteraction(group: "zoom" | "orientation"): () => void;
  addImage(id: string, image: unknown, options?: Record<string, unknown>): void;
  removeImage(id: string): void;
  hasImage(id: string): boolean;
  /** Renders an image URL (including SVG data URIs) to pixel data plus addImage options. */
  loadImageData(
    url: string,
    options: Record<string, unknown> | null,
  ): Promise<{ data: unknown; options?: Record<string, unknown> }>;
  /** DOM marker upsert/removal (engine/markers.ts owns the element lifecycle). */
  setMarker(marker: MarkerData): void;
  removeMarker(id: string): void;
  /** Control upsert/removal + custom content binding (engine/controls.ts). */
  setControl(control: ControlData): void;
  removeControl(id: string): void;
  setControlContent(id: string, events?: ControlContentEvents | null): void;
  removeControlContent(id: string): void;
  /** Component popup upsert/removal (engine/popups.ts). */
  setPopup(popup: PopupData): void;
  removePopup(id: string): void;
  /** Transient text/html popup; showing a new one replaces the previous (one active per map). */
  showPopup(position: LatLng, options: MarkerPopupData): void;
  closeActivePopup(): void;
  /** Map-level configuration (camera limits, projection, pixel ratio). */
  configure(config: MapConfigData): void;
  resize(): void;
  /** Per-origin referrer policy for tile requests (transformRequest). */
  setRequestPolicy(origin: string, policy: string | null): void;
  flyTo(options: Record<string, unknown>): void;
  fitBounds(bounds: [[number, number], [number, number]], options: Record<string, unknown>): void;
  /** Live DOM marker position lookup (camera.fitFeatures). */
  markerPosition(id: string): { lng: number; lat: number } | null;
  on(event: string, handler: (event: unknown) => void): unknown;
  on(event: string, layerId: string, handler: (event: unknown) => void): unknown;
  off(event: string, handler: (event: unknown) => void): unknown;
  off(event: string, layerId: string, handler: (event: unknown) => void): unknown;
  /** All layers of the current style (including engine-managed ones; the runtime filters). */
  listStyleLayers(): { id: string; layout?: { visibility?: string }; filter?: unknown; metadata?: unknown }[];
  /** Composed overlay-style layer lookups (styles/composition.ts registry). */
  resolveComposedLayer(styleId: string, layerId: string): { layerId: string; visible: boolean } | null;
  listComposedLayers(styleId: string): { layerId: string; visible: boolean }[];
}

interface GeoJsonSourceLike {
  setData(data: FeatureCollection | unknown): void;
  updateData?(diff: unknown): void;
  getClusterExpansionZoom?(clusterId: number): Promise<number>;
}

export interface EngineEvent {
  layerId: string;
  lng: number;
  lat: number;
  featureId: number | string | null;
  entityId: string | null;
  properties: Record<string, unknown> | null;
}

export interface EngineOptions {
  /** Receives UI events for handlers registered via `events.set`. */
  onEvent?: (handlerId: number, event: EngineEvent) => void;
  /** Receives async failures (image loads, flush errors). */
  onError?: (error: unknown) => void;
  /** Receives a camera-follow cleared by the engine (user interaction or missing entity). */
  onFollowCleared?: (reason: FollowClearReason) => void;
  scheduler?: Scheduler;
  now?: () => number;
}

export interface Engine {
  applyOps(ops: Op[]): void;
  pushMotion(layerId: string, bytes: Uint8Array): void;
  /** Re-applies the full scene in canonical order (after map.setStyle). */
  replay(): void;
  dispose(): void;
}

interface LayerRecord {
  id: string;
  spec: Record<string, unknown>;
  slot: string | null;
  before: string | null;
}

interface EventRecord {
  handlers: EventHandlers;
  listeners: { event: string; handler: (event: unknown) => void }[];
}

interface MapEventLike {
  lngLat?: { lng: number; lat: number };
  features?: {
    id?: number | string;
    properties?: Record<string, unknown>;
    geometry?: { type?: string; coordinates?: unknown };
  }[];
}

function updateSpecSection(record: LayerRecord | undefined, section: string, name: string, value: unknown): void {
  if (!record) {
    return;
  }

  if (typeof record.spec[section] !== "object" || record.spec[section] === null) {
    record.spec[section] = {};
  }

  (record.spec[section] as Record<string, unknown>)[name] = value;
}

export function createEngine(map: EngineMap, options: EngineOptions = {}): Engine {
  const scheduler = options.scheduler ?? createScheduler();
  const now = options.now ?? (() => performance.now());
  const onEvent = options.onEvent ?? (() => {});
  const onError = options.onError ?? (() => {});
  const onFollowCleared = options.onFollowCleared ?? (() => {});

  const slots = new Map<string, { before: string | null }>();
  const sources = new Map<string, Record<string, unknown>>();
  const layers = new Map<string, LayerRecord>();
  const entityLayers = new Map<string, EntityLayerStore>();
  const follow = createFollowController({
    map,
    scheduler,
    getStore: (layerId) => entityLayers.get(layerId),
    onCleared: onFollowCleared,
  });
  const visibilityController = createVisibilityController({
    getRuntimeLayer(layerId) {
      const record = layers.get(layerId);
      if (!record) {
        return null;
      }

      const layout = record.spec.layout as Record<string, unknown> | undefined;
      return { visible: layout?.visibility !== "none" };
    },
    getRuntimeBaselineFilter: (layerId) => layers.get(layerId)?.spec.filter ?? null,
    listStyleLayers: () =>
      map
        .listStyleLayers()
        .filter((layer) => !layers.has(layer.id) && !layer.id.startsWith(SLOT_LAYER_PREFIX))
        .map(styleLayerInfo),
    resolveComposedLayer: (styleId, layerId) => map.resolveComposedLayer(styleId, layerId),
    listComposedLayers: (styleId) => map.listComposedLayers(styleId),
    setLayerVisibility: (layerId, visible) =>
      map.setLayoutProperty(layerId, "visibility", visible ? "visible" : "none"),
    setLayerFilter: (layerId, filter) => map.setFilter(layerId, filter),
    hasLayer: (layerId) => Boolean(map.getLayer(layerId)),
  });
  const images = new Map<string, { url: string; options: Record<string, unknown> | null }>();
  const events = new Map<string, EventRecord>();
  const hovered = new Map<string, number | null>();
  // hover/cluster-zoom listeners per entity layer, so entities.remove can unwire them
  // (a recreated layer with the same ids must not stack handlers on a stale store)
  const entityListeners = new Map<string, { event: string; layerId: string; handler: (event: unknown) => void }[]>();

  function slotAnchorId(slotId: string): string {
    return `${SLOT_LAYER_PREFIX}${slotId}`;
  }

  function resolveBeforeId(slot: string | null | undefined, before: string | null | undefined): string | undefined {
    if (before) {
      return slots.has(before) ? slotAnchorId(before) : before;
    }

    if (slot && slots.has(slot)) {
      return slotAnchorId(slot);
    }

    return undefined;
  }

  function addSlotAnchor(slotId: string, before: string | null): void {
    map.addLayer(
      {
        id: slotAnchorId(slotId),
        type: "background",
        layout: { visibility: "none" },
      },
      resolveBeforeId(null, before),
    );
  }

  function entitySourceSpec(store: EntityLayerStore): Record<string, unknown> {
    return {
      type: "geojson",
      data: store.collection,
      ...(store.config.cluster ?? {}),
    };
  }

  function entityDecorationSourceSpec(store: EntityLayerStore): Record<string, unknown> {
    return {
      type: "geojson",
      data: store.decorationCollection,
      ...(store.config.decorationCluster ?? {}),
    };
  }

  function applyFlushPlan(sourceId: string, plan: FlushPlan, fallback: FeatureCollection): void {
    if (plan.mode === "none") {
      return;
    }

    const source = map.getSource(sourceId) as GeoJsonSourceLike | undefined;
    if (!source) {
      return;
    }

    try {
      if (plan.mode === "updateData" && typeof source.updateData === "function") {
        source.updateData(plan.diff);
      } else if (plan.mode === "updateData") {
        // updateData unavailable: features are mutated in place, so the shared
        // collection is already current and a full setData is a safe fallback.
        source.setData(fallback);
      } else {
        source.setData(plan.data);
      }
    } catch (error) {
      onError(error);
    }
  }

  function flushEntityLayer(store: EntityLayerStore): void {
    const plans = collectFlush(store);
    applyFlushPlan(store.id, plans.primary, store.collection);
    if (store.config.decorations && plans.decorations.mode !== "none") {
      // Decorations trail the primaries by one frame: each source update triggers a
      // symbol re-layout + buffer upload, and paying both in the same frame is what
      // produces multi-frame hitches at high feature counts. One frame (~17 ms) of
      // label lag at 10 Hz data is imperceptible; markDirty during a flush lands in
      // the next frame by scheduler design.
      const decorationPlan = plans.decorations;
      scheduler.markDirty(`entities:${store.id}:decorations`, () =>
        applyFlushPlan(decorationSourceId(store.id), decorationPlan, store.decorationCollection),
      );
    }
  }

  /**
   * Interpolates Point features (matched by feature id) from the previous document to
   * the new one, writing per-frame setData. Returns
   * false when there is nothing to interpolate (caller falls back to a plain setData).
   */
  function startSourceAnimation(
    sourceId: string,
    previousData: unknown,
    nextData: unknown,
    animate: { durationMs: number; easing?: string },
  ): boolean {
    const pointIndex = (data: unknown) => {
      const index = new Map<string | number, { lng: number; lat: number; bearing?: number }>();
      const features = (data as FeatureCollection | undefined)?.features;
      if (!Array.isArray(features)) {
        return index;
      }

      for (const feature of features) {
        if (feature.geometry?.type === "Point" && feature.id != null) {
          const [lng, lat] = feature.geometry.coordinates as [number, number];
          index.set(feature.id, { lng, lat, bearing: feature.properties?.bearing as number | undefined });
        }
      }

      return index;
    };

    const fromFeatures = pointIndex(previousData);
    const collection = nextData as FeatureCollection | undefined;
    if (fromFeatures.size === 0 || !Array.isArray(collection?.features)) {
      return false;
    }

    const startMs = now();
    scheduler.setAnimation(`sourceAnim:${sourceId}`, (frameNow) => {
      const source = map.getSource(sourceId) as GeoJsonSourceLike | undefined;
      if (!source) {
        return false;
      }

      const t = Math.min(1, (frameNow - startMs) / animate.durationMs);
      if (t >= 1) {
        source.setData(collection);
        return false;
      }

      const eased = animate.easing === "easeInOut" ? easeInOut(t) : t;
      const features = collection.features.map((feature) => {
        if (feature.geometry?.type !== "Point" || feature.id == null) {
          return feature;
        }

        const from = fromFeatures.get(feature.id);
        if (!from) {
          return feature;
        }

        const [toLng, toLat] = feature.geometry.coordinates as [number, number];
        const interpolated: typeof feature = {
          ...feature,
          geometry: {
            type: "Point",
            coordinates: [from.lng + (toLng - from.lng) * eased, from.lat + (toLat - from.lat) * eased],
          },
        };

        const toBearing = feature.properties?.bearing as number | undefined;
        if (from.bearing != null && toBearing != null && interpolated.properties) {
          // bearing wraparound: 350° → 10° goes through 0°, not backwards
          let diff = toBearing - from.bearing;
          if (diff > 180) {
            diff -= 360;
          }
          if (diff < -180) {
            diff += 360;
          }

          interpolated.properties = { ...interpolated.properties, bearing: from.bearing + diff * eased };
        }

        return interpolated;
      });

      source.setData({ type: "FeatureCollection", features });
      return true;
    });

    return true;
  }

  function markEntityLayerDirty(store: EntityLayerStore): void {
    scheduler.markDirty(`entities:${store.id}`, () => flushEntityLayer(store));
  }

  function startAnimationLoop(store: EntityLayerStore): void {
    if (store.animating.size === 0) {
      return;
    }

    scheduler.setAnimation(`entities:${store.id}`, (frameNow) => {
      const active = animationTick(store, frameNow);
      if (store.movedIndices.size > 0) {
        markEntityLayerDirty(store);
      }

      return active;
    });
  }

  function setEntityHover(store: EntityLayerStore, index: number | null): void {
    const previous = hovered.get(store.id) ?? null;
    if (previous === index) {
      return;
    }

    let needsFlush = false;
    if (previous !== null) {
      for (const featureId of featureIdsOf(store, previous)) {
        map.setFeatureState({ source: sourceForFeatureId(store.id, featureId), id: featureId }, { hover: false });
      }

      needsFlush = setHoverBoost(store, previous, false) || needsFlush;
    }

    if (index !== null) {
      for (const featureId of featureIdsOf(store, index)) {
        map.setFeatureState({ source: sourceForFeatureId(store.id, featureId), id: featureId }, { hover: true });
      }

      needsFlush = setHoverBoost(store, index, true) || needsFlush;
    }

    hovered.set(store.id, index);
    if (needsFlush) {
      markEntityLayerDirty(store);
    }
  }

  function trackEntityListener(
    storeId: string,
    event: string,
    layerId: string,
    handler: (event: unknown) => void,
  ): void {
    map.on(event, layerId, handler);
    const listeners = entityListeners.get(storeId) ?? [];
    listeners.push({ event, layerId, handler });
    entityListeners.set(storeId, listeners);
  }

  function unwireEntityListeners(storeId: string): void {
    for (const { event, layerId, handler } of entityListeners.get(storeId) ?? []) {
      map.off(event, layerId, handler);
    }
    entityListeners.delete(storeId);
  }

  function wireEntityHover(store: EntityLayerStore): void {
    for (const layerId of store.config.hoverLayerIds ?? []) {
      trackEntityListener(store.id, "mousemove", layerId, (rawEvent) => {
        const event = rawEvent as MapEventLike;
        const featureId = event.features?.[0]?.id;
        setEntityHover(store, typeof featureId === "number" ? entityIndexOfFeatureId(featureId) : null);
      });
      trackEntityListener(store.id, "mouseleave", layerId, () => setEntityHover(store, null));
    }
  }

  function clusterZoomHandler(sourceId: string): (rawEvent: unknown) => void {
    return (rawEvent) => {
      const feature = (rawEvent as MapEventLike).features?.[0];
      const clusterId = feature?.properties?.cluster_id;
      const coordinates = feature?.geometry?.type === "Point" ? feature.geometry.coordinates : null;
      const source = map.getSource(sourceId) as GeoJsonSourceLike | undefined;
      if (typeof clusterId !== "number" || !coordinates || !source?.getClusterExpansionZoom) {
        return;
      }

      source
        .getClusterExpansionZoom(clusterId)
        .then((zoom) => map.easeTo({ center: coordinates, zoom }))
        .catch(onError);
    };
  }

  function wireClusterZoomForSource(sourceId: string, layerIds: string[]): void {
    for (const layerId of layerIds) {
      map.on("click", layerId, clusterZoomHandler(sourceId));
    }
  }

  function wireClusterZoom(store: EntityLayerStore): void {
    for (const layerId of store.config.clusterZoomLayerIds ?? []) {
      trackEntityListener(store.id, "click", layerId, clusterZoomHandler(store.id));
    }
  }

  function toEngineEvent(layerId: string, rawEvent: unknown): EngineEvent {
    const event = rawEvent as MapEventLike;
    const feature = event.features?.[0];
    const properties = feature?.properties ?? null;

    return {
      layerId,
      lng: event.lngLat?.lng ?? 0,
      lat: event.lngLat?.lat ?? 0,
      featureId: feature?.id ?? null,
      entityId: typeof properties?.entityId === "string" ? properties.entityId : null,
      properties,
    };
  }

  function wireLayerEvents(layerId: string, handlers: EventHandlers): void {
    unwireLayerEvents(layerId);

    const listeners: EventRecord["listeners"] = [];
    const bindings: [string, number | undefined][] = [
      ["click", handlers.click],
      ["mouseenter", handlers.enter],
      ["mouseleave", handlers.leave],
    ];

    for (const [event, handlerId] of bindings) {
      if (handlerId === undefined) {
        continue;
      }

      const handler = (rawEvent: unknown) => onEvent(handlerId, toEngineEvent(layerId, rawEvent));
      map.on(event, layerId, handler);
      listeners.push({ event, handler });
    }

    events.set(layerId, { handlers, listeners });
  }

  function unwireLayerEvents(layerId: string): void {
    const record = events.get(layerId);
    if (!record) {
      return;
    }

    for (const { event, handler } of record.listeners) {
      map.off(event, layerId, handler);
    }
    events.delete(layerId);
  }

  function loadAndAddImage(id: string, url: string, imageOptions: Record<string, unknown> | null): void {
    map
      .loadImageData(url, imageOptions)
      .then((loaded) => {
        if (images.has(id) && !map.hasImage(id)) {
          map.addImage(id, loaded.data, loaded.options);
        }
      })
      .catch(onError);
  }

  function applyOp(op: Op): void {
    switch (op.op) {
      case "source.add":
        sources.set(op.id, op.spec);
        map.addSource(op.id, op.spec);
        break;
      case "source.remove":
        sources.delete(op.id);
        map.removeSource(op.id);
        break;
      case "source.setData": {
        const spec = sources.get(op.id);
        const previousData = spec?.data;
        if (spec) {
          spec.data = op.data;
        }

        if (op.animate && op.animate.durationMs > 0 && startSourceAnimation(op.id, previousData, op.data, op.animate)) {
          break;
        }

        scheduler.setAnimation(`sourceAnim:${op.id}`, null);
        scheduler.markDirty(`source:${op.id}`, () => {
          const source = map.getSource(op.id) as GeoJsonSourceLike | undefined;
          source?.setData(op.data);
        });
        break;
      }
      case "source.clusterZoom":
        wireClusterZoomForSource(op.id, op.layerIds);
        break;
      case "layer.add": {
        const record: LayerRecord = { id: op.id, spec: op.spec, slot: op.slot ?? null, before: op.before ?? null };
        layers.set(op.id, record);
        map.addLayer(op.spec, resolveBeforeId(record.slot, record.before));
        visibilityController.onLayerAdded(op.id);
        break;
      }
      case "layer.remove":
        layers.delete(op.id);
        unwireLayerEvents(op.id);
        map.removeLayer(op.id);
        break;
      case "layer.setPaint": {
        updateSpecSection(layers.get(op.id), "paint", op.name, op.value);
        map.setPaintProperty(op.id, op.name, op.value);
        break;
      }
      case "layer.setLayout": {
        updateSpecSection(layers.get(op.id), "layout", op.name, op.value);
        map.setLayoutProperty(op.id, op.name, op.value);
        break;
      }
      case "layer.setFilter": {
        const record = layers.get(op.id);
        if (record) {
          record.spec.filter = op.filter;
        }

        // the controller owns the applied filter: it re-composes display filters
        // on top of the new baseline (or applies the baseline directly).
        visibilityController.onBaselineFilterChanged(op.id);
        break;
      }
      case "layer.setZoom": {
        const record = layers.get(op.id);
        if (record) {
          record.spec.minzoom = op.min;
          record.spec.maxzoom = op.max;
        }

        map.setLayerZoomRange(op.id, op.min, op.max);
        break;
      }
      case "layer.move": {
        const record = layers.get(op.id);
        if (record) {
          record.slot = op.slot ?? null;
          record.before = op.before ?? null;
        }

        map.moveLayer(op.id, resolveBeforeId(op.slot, op.before));
        break;
      }
      case "slot.define":
        slots.set(op.id, { before: op.before ?? null });
        addSlotAnchor(op.id, op.before ?? null);
        break;
      case "entities.create": {
        const store = createEntityLayerStore(op.id, op.config);
        entityLayers.set(op.id, store);
        map.addSource(op.id, entitySourceSpec(store));
        if (op.config.decorations) {
          map.addSource(decorationSourceId(op.id), entityDecorationSourceSpec(store));
        }

        wireEntityHover(store);
        wireClusterZoom(store);
        break;
      }
      case "entities.configure": {
        const store = entityLayers.get(op.id);
        if (store) {
          store.config = { ...store.config, ...op.config };
        }

        break;
      }
      case "entities.remove": {
        scheduler.setAnimation(`entities:${op.id}`, null);
        unwireEntityListeners(op.id);
        const store = entityLayers.get(op.id);
        entityLayers.delete(op.id);
        hovered.delete(op.id);
        if (store?.config.decorations) {
          map.removeSource(decorationSourceId(op.id));
        }

        map.removeSource(op.id);
        break;
      }
      case "entities.upsert": {
        const store = entityLayers.get(op.id);
        if (store) {
          // clear feature-state before removal so a recycled index can't inherit
          // a previous entity's hover/selected state.
          for (const index of op.removes) {
            for (const featureId of featureIdsOf(store, index)) {
              map.removeFeatureState({ source: sourceForFeatureId(store.id, featureId), id: featureId });
            }

            if (hovered.get(store.id) === index) {
              hovered.set(store.id, null);
            }
          }

          applyUpsert(store, op.epoch, op.upserts, op.removes);
          markEntityLayerDirty(store);
        }

        break;
      }
      case "entities.select": {
        const store = entityLayers.get(op.id);
        if (store) {
          applySelection(store, op.selected, (featureId, state) =>
            map.setFeatureState({ source: sourceForFeatureId(store.id, featureId), id: featureId }, state),
          );
        }

        break;
      }
      case "visibility.set":
        visibilityController.setGroup(op.id, op.visible, op.targets);
        break;
      case "visibility.remove":
        visibilityController.removeGroup(op.id);
        break;
      case "overlay.set":
        visibilityController.setOverlay(op.id, op.visible, op.targets, op.parts);
        break;
      case "overlay.remove":
        visibilityController.removeOverlay(op.id);
        break;
      case "image.add":
        images.set(op.id, { url: op.url, options: op.options ?? null });
        loadAndAddImage(op.id, op.url, op.options ?? null);
        break;
      case "image.remove":
        images.delete(op.id);
        if (map.hasImage(op.id)) {
          map.removeImage(op.id);
        }

        break;
      case "events.set":
        wireLayerEvents(op.layerId, op.handlers);
        break;
      case "events.clear":
        unwireLayerEvents(op.layerId);
        break;
      // markers, controls, and popups are DOM constructs: they survive map.setStyle,
      // so unlike sources/layers they need no replay bookkeeping here — their
      // controllers are the only stores.
      case "marker.set":
        map.setMarker(op.marker);
        break;
      case "marker.remove":
        map.removeMarker(op.id);
        break;
      case "control.set":
        map.setControl(op.control);
        break;
      case "control.remove":
        map.removeControl(op.id);
        break;
      case "control.content":
        map.setControlContent(op.id, op.events);
        break;
      case "control.removeContent":
        map.removeControlContent(op.id);
        break;
      case "popup.set":
        map.setPopup(op.popup);
        break;
      case "popup.remove":
        map.removePopup(op.id);
        break;
      case "popup.show":
        map.showPopup(op.position, op.options);
        break;
      case "popup.close":
        map.closeActivePopup();
        break;
      case "map.configure":
        map.configure(op.config);
        break;
      case "map.resize":
        map.resize();
        break;
      case "map.requestPolicy":
        map.setRequestPolicy(op.origin, op.policy ?? null);
        break;
      case "camera.flyTo": {
        const flyOptions: Record<string, unknown> = {};
        if (op.center) {
          flyOptions.center = [op.center.longitude, op.center.latitude];
        }
        if (op.zoom != null) {
          flyOptions.zoom = op.zoom;
        }
        if (op.bearing != null) {
          flyOptions.bearing = op.bearing;
        }
        if (op.pitch != null) {
          flyOptions.pitch = op.pitch;
        }

        map.flyTo(flyOptions);
        break;
      }
      case "camera.fitFeatures":
        fitFeatures(op.featureIds, op.padding, op.topLeftPadding, op.bottomRightPadding);
        break;
      case "camera.follow":
        follow.apply(op);
        break;
      case "camera.clearFollow":
        follow.clear();
        break;
      case "source.featureState": {
        const target: { source: string; id: string | number; sourceLayer?: string } = {
          source: op.id,
          id: op.featureId,
        };
        if (op.sourceLayer) {
          target.sourceLayer = op.sourceLayer;
        }

        map.setFeatureState(target, op.state);
        break;
      }
    }
  }

  /**
   * Fits the viewport around markers/circles/polylines by feature id. Markers
   * resolve from the live DOM marker controller; shapes resolve
   * from the convenience shape sources' FeatureCollections in the op store.
   */
  function fitFeatures(
    featureIds: string[],
    padding?: { x: number; y: number } | null,
    topLeftPadding?: { x: number; y: number } | null,
    bottomRightPadding?: { x: number; y: number } | null,
  ): void {
    const coordinates: [number, number][] = [];
    const shapeFeatures = new Map<string, GeoJSON.Feature>();
    for (const sourceId of ["sgb-circles-source", "sgb-polylines-source"]) {
      const data = sources.get(sourceId)?.data as GeoJSON.FeatureCollection | undefined;
      for (const feature of data?.features ?? []) {
        if (typeof feature.id === "string") {
          shapeFeatures.set(feature.id, feature);
        }
      }
    }

    for (const id of featureIds) {
      const markerPosition = map.markerPosition(id);
      if (markerPosition) {
        coordinates.push([markerPosition.lng, markerPosition.lat]);
        continue;
      }

      const feature = shapeFeatures.get(id);
      if (feature?.geometry.type === "Point") {
        const [lng, lat] = (feature.geometry as GeoJSON.Point).coordinates;
        coordinates.push([lng, lat]);
      } else if (feature?.geometry.type === "LineString") {
        for (const coordinate of (feature.geometry as GeoJSON.LineString).coordinates) {
          coordinates.push([coordinate[0], coordinate[1]]);
        }
      }
    }

    if (coordinates.length === 0) {
      return;
    }

    let [minLng, minLat] = coordinates[0];
    let [maxLng, maxLat] = coordinates[0];
    for (const [lng, lat] of coordinates) {
      minLng = Math.min(minLng, lng);
      maxLng = Math.max(maxLng, lng);
      minLat = Math.min(minLat, lat);
      maxLat = Math.max(maxLat, lat);
    }

    const fitOptions: Record<string, unknown> = {};
    if (padding) {
      fitOptions.padding = { top: padding.y, bottom: padding.y, left: padding.x, right: padding.x };
    } else if (topLeftPadding || bottomRightPadding) {
      fitOptions.padding = {
        top: topLeftPadding?.y ?? 0,
        left: topLeftPadding?.x ?? 0,
        bottom: bottomRightPadding?.y ?? 0,
        right: bottomRightPadding?.x ?? 0,
      };
    }

    map.fitBounds(
      [
        [minLng, minLat],
        [maxLng, maxLat],
      ],
      fitOptions,
    );
  }

  return {
    applyOps(ops) {
      for (const op of ops) {
        try {
          applyOp(op);
        } catch (error) {
          onError(error);
        }
      }
    },
    pushMotion(layerId, bytes) {
      const store = entityLayers.get(layerId);
      if (!store) {
        return;
      }

      try {
        const frame = decodeMotionFrame(bytes);
        const result = applyMotion(store, frame, now());
        if (!result.applied) {
          return;
        }

        if (store.animating.size > 0) {
          startAnimationLoop(store);
        }

        if (store.movedIndices.size > 0) {
          markEntityLayerDirty(store);
        }
      } catch (error) {
        onError(error);
      }
    },
    replay() {
      // Canonical order: slots → sources → layers → visibility → images. Layer event
      // listeners are delegated on the map object and survive setStyle, so they are
      // not rewired here.
      for (const [slotId, slot] of slots) {
        addSlotAnchor(slotId, slot.before);
      }

      for (const [id, spec] of sources) {
        map.addSource(id, spec);
      }

      for (const [id, store] of entityLayers) {
        map.addSource(id, entitySourceSpec(store));
        if (store.config.decorations) {
          map.addSource(decorationSourceId(id), entityDecorationSourceSpec(store));
        }
      }

      for (const record of layers.values()) {
        map.addLayer(record.spec, resolveBeforeId(record.slot, record.before));
      }

      visibilityController.replay();

      for (const [id, image] of images) {
        if (!map.hasImage(id)) {
          loadAndAddImage(id, image.url, image.options);
        }
      }
    },
    dispose() {
      follow.dispose();
      for (const layerId of [...events.keys()]) {
        unwireLayerEvents(layerId);
      }
      scheduler.dispose();
    },
  };
}
