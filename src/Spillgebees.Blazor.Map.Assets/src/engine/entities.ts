// Entity layer store (docs/plans/map-engine-protocol.md §3).
//
// Holds live GeoJSON features keyed by the .NET-assigned entity index and mutates them
// in place: structural upserts replace whole entities, motion frames touch only
// position/rotation/sortKey, and the flush plan decides between MapLibre setData and
// updateData. Feature ids are numeric — index * 64 for the primary feature,
// index * 64 + 1 + slot for decorations — so feature-state works natively.

import type { Feature, FeatureCollection, Point } from "geojson";
import type { MotionFrame } from "./motion";
import type { EntityDecorationUpsert, EntityLayerConfig, EntityUpsert } from "./ops";

export const FEATURES_PER_ENTITY = 64;
export const MAX_DECORATION_SLOTS = FEATURES_PER_ENTITY - 1;

/** Fraction of entities that may move per flush before a full setData is cheaper than a diff. */
const UPDATE_DATA_MAX_MOVED_RATIO = 0.2;

export function primaryFeatureId(index: number): number {
  return index * FEATURES_PER_ENTITY;
}

export function decorationFeatureId(index: number, slot: number): number {
  return index * FEATURES_PER_ENTITY + 1 + slot;
}

/**
 * Decorations live in a sibling source so MapLibre's cluster aggregates
 * (point_count, custom cluster properties) keep counting entities — user-supplied
 * cluster styling expressions depend on that.
 */
export function decorationSourceId(layerId: string): string {
  return `${layerId}-decorations`;
}

/** Routes a feature id to the source that owns it (primary vs decoration). */
export function sourceForFeatureId(layerId: string, featureId: number): string {
  return featureId % FEATURES_PER_ENTITY === 0 ? layerId : decorationSourceId(layerId);
}

type PointFeature = Feature<Point> & { id: number; properties: Record<string, unknown> };

interface AnimationState {
  fromLng: number;
  fromLat: number;
  toLng: number;
  toLat: number;
  startMs: number;
  durationMs: number;
}

interface EntityRecord {
  index: number;
  id: string;
  primary: PointFeature;
  decorations: PointFeature[];
  animation: AnimationState | null;
}

/** Subset of MapLibre's GeoJSONSourceDiff that the flush plan emits. */
export interface SourceDiff {
  update: {
    id: number;
    newGeometry: Point;
    addOrUpdateProperties: { key: string; value: unknown }[];
  }[];
}

export type FlushPlan =
  | { mode: "none" }
  | { mode: "setData"; data: FeatureCollection }
  | { mode: "updateData"; diff: SourceDiff };

export interface FlushPlans {
  primary: FlushPlan;
  decorations: FlushPlan;
}

export interface EntityLayerStore {
  readonly id: string;
  config: EntityLayerConfig;
  epoch: number;
  readonly records: Map<number, EntityRecord>;
  readonly collection: FeatureCollection;
  readonly decorationCollection: FeatureCollection;
  readonly selected: Set<number>;
  readonly animating: Set<number>;
  structuralDirty: boolean;
  featuresArrayStale: boolean;
  readonly movedIndices: Set<number>;
}

export function createEntityLayerStore(id: string, config: EntityLayerConfig): EntityLayerStore {
  return {
    id,
    config,
    epoch: 0,
    records: new Map(),
    collection: { type: "FeatureCollection", features: [] },
    decorationCollection: { type: "FeatureCollection", features: [] },
    selected: new Set(),
    animating: new Set(),
    structuralDirty: false,
    featuresArrayStale: false,
    movedIndices: new Set(),
  };
}

export function applyUpsert(store: EntityLayerStore, epoch: number, upserts: EntityUpsert[], removes: number[]): void {
  store.epoch = epoch;

  for (const index of removes) {
    if (store.records.delete(index)) {
      store.animating.delete(index);
      store.selected.delete(index);
      store.movedIndices.delete(index);
      store.structuralDirty = true;
      store.featuresArrayStale = true;
    }
  }

  for (const upsert of upserts) {
    store.records.set(upsert.idx, buildRecord(upsert));
    store.animating.delete(upsert.idx);
    store.movedIndices.delete(upsert.idx);
    store.structuralDirty = true;
    store.featuresArrayStale = true;
  }
}

export interface MotionResult {
  applied: boolean;
  matched: number;
}

export function applyMotion(store: EntityLayerStore, frame: MotionFrame, nowMs: number): MotionResult {
  // Stale frames are dropped, never reordered: the upsert that bumped the epoch already
  // carried fresh positions for the entities it touched (protocol §3.1).
  if (frame.epoch !== store.epoch) {
    return { applied: false, matched: 0 };
  }

  const animation = store.config.animation;
  let matched = 0;

  for (let i = 0; i < frame.count; i++) {
    const index = frame.indices[i];
    const record = store.records.get(index);
    if (!record) {
      continue;
    }

    matched++;
    const lng = frame.coords[i * 2];
    const lat = frame.coords[i * 2 + 1];

    if (animation && animation.durationMs > 0) {
      const [currentLng, currentLat] = record.primary.geometry.coordinates;
      record.animation = {
        fromLng: currentLng,
        fromLat: currentLat,
        toLng: lng,
        toLat: lat,
        startMs: nowMs,
        durationMs: animation.durationMs,
      };
      store.animating.add(index);
    } else {
      setRecordPosition(record, lng, lat);
      store.movedIndices.add(index);
    }

    if (frame.rotations) {
      record.primary.properties.rot = frame.rotations[i];
      store.movedIndices.add(index);
    }

    if (frame.sortKeys) {
      const sortKey = frame.sortKeys[i];
      record.primary.properties.sortKey = sortKey;
      for (const decoration of record.decorations) {
        decoration.properties.sortKey = sortKey;
      }
      store.movedIndices.add(index);
    }
  }

  return { applied: true, matched };
}

/** Advances active position animations; returns true while any animation is still running. */
export function animationTick(store: EntityLayerStore, nowMs: number): boolean {
  const easing = store.config.animation?.easing ?? "linear";

  for (const index of store.animating) {
    const record = store.records.get(index);
    const animation = record?.animation;
    if (!record || !animation) {
      store.animating.delete(index);
      continue;
    }

    const rawProgress = Math.min(1, (nowMs - animation.startMs) / animation.durationMs);
    const progress = easing === "easeInOut" ? easeInOut(rawProgress) : rawProgress;
    setRecordPosition(
      record,
      animation.fromLng + (animation.toLng - animation.fromLng) * progress,
      animation.fromLat + (animation.toLat - animation.fromLat) * progress,
    );
    store.movedIndices.add(index);

    if (rawProgress >= 1) {
      record.animation = null;
      store.animating.delete(index);
    }
  }

  return store.animating.size > 0;
}

export function collectFlush(store: EntityLayerStore): FlushPlans {
  if (!store.structuralDirty && store.movedIndices.size === 0) {
    return { primary: { mode: "none" }, decorations: { mode: "none" } };
  }

  const movedRatio = store.records.size === 0 ? 1 : store.movedIndices.size / store.records.size;
  if (store.structuralDirty || movedRatio > UPDATE_DATA_MAX_MOVED_RATIO) {
    if (store.featuresArrayStale) {
      rebuildFeaturesArray(store);
    }
    clearDirty(store);
    return {
      primary: { mode: "setData", data: store.collection },
      decorations: { mode: "setData", data: store.decorationCollection },
    };
  }

  const primaryDiff: SourceDiff = { update: [] };
  const decorationDiff: SourceDiff = { update: [] };
  for (const index of store.movedIndices) {
    const record = store.records.get(index);
    if (!record) {
      continue;
    }

    primaryDiff.update.push({
      id: record.primary.id,
      newGeometry: record.primary.geometry,
      addOrUpdateProperties: diffProperties(record.primary, ["rot", "sortKey", "hoverBoost"]),
    });
    for (const decoration of record.decorations) {
      decorationDiff.update.push({
        id: decoration.id,
        newGeometry: decoration.geometry,
        addOrUpdateProperties: diffProperties(decoration, ["sortKey"]),
      });
    }
  }

  clearDirty(store);
  return {
    primary: { mode: "updateData", diff: primaryDiff },
    decorations: decorationDiff.update.length > 0 ? { mode: "updateData", diff: decorationDiff } : { mode: "none" },
  };
}

/**
 * Applies a new selected set; emits feature-state changes only for the difference.
 * Selection targets every feature of an entity (primary + decorations).
 */
export function applySelection(
  store: EntityLayerStore,
  selected: number[],
  setFeatureState: (featureId: number, state: Record<string, unknown>) => void,
): void {
  const next = new Set(selected);

  for (const index of store.selected) {
    if (!next.has(index)) {
      for (const featureId of featureIdsOf(store, index)) {
        setFeatureState(featureId, { selected: false });
      }
    }
  }

  for (const index of next) {
    if (!store.selected.has(index) && store.records.has(index)) {
      for (const featureId of featureIdsOf(store, index)) {
        setFeatureState(featureId, { selected: true });
      }
    }
  }

  store.selected.clear();
  for (const index of next) {
    if (store.records.has(index)) {
      store.selected.add(index);
    }
  }
}

/**
 * Applies the hover scale boost as a data property (layout properties cannot read
 * feature-state, so icon-size reads `hoverBoost` instead). Returns true when the
 * feature changed and a flush is needed.
 */
export function setHoverBoost(store: EntityLayerStore, index: number, hovered: boolean): boolean {
  const record = store.records.get(index);
  const scale = record?.primary.properties.hoverScale;
  if (!record || typeof scale !== "number") {
    return false;
  }

  const boost = hovered ? scale : 1;
  if (record.primary.properties.hoverBoost === boost) {
    return false;
  }

  record.primary.properties.hoverBoost = boost;
  store.movedIndices.add(index);
  return true;
}

export function featureIdsOf(store: EntityLayerStore, index: number): number[] {
  const record = store.records.get(index);
  if (!record) {
    return [];
  }

  return [record.primary.id, ...record.decorations.map((decoration) => decoration.id)];
}

/** Resolves a feature id (primary or decoration) back to its entity index. */
export function entityIndexOfFeatureId(featureId: number): number {
  return Math.floor(featureId / FEATURES_PER_ENTITY);
}

function buildRecord(upsert: EntityUpsert): EntityRecord {
  const primary = buildPointFeature(primaryFeatureId(upsert.idx), upsert.lng, upsert.lat, {
    kind: "primary",
    entityId: upsert.id,
    icon: upsert.icon,
    size: upsert.size,
    rot: upsert.rot,
    anchor: upsert.anchor,
    offset: upsert.offset,
    color: upsert.color,
    sortKey: upsert.sortKey,
    hoverScale: upsert.hover?.scale,
    hoverRaise: upsert.hover?.raise,
    ...upsert.props,
  });

  const decorations = (upsert.decorations ?? []).map((decoration) => buildDecorationFeature(upsert, decoration));

  return { index: upsert.idx, id: upsert.id, primary, decorations, animation: null };
}

function buildDecorationFeature(upsert: EntityUpsert, decoration: EntityDecorationUpsert): PointFeature {
  if (decoration.slot < 0 || decoration.slot > MAX_DECORATION_SLOTS - 1) {
    throw new Error(`Decoration slot ${decoration.slot} out of range (0-${MAX_DECORATION_SLOTS - 1})`);
  }

  return buildPointFeature(decorationFeatureId(upsert.idx, decoration.slot), upsert.lng, upsert.lat, {
    kind: "decoration",
    entityId: upsert.id,
    decorationId: decoration.id,
    text: decoration.text,
    icon: decoration.icon,
    anchor: decoration.anchor,
    offset: decoration.offset,
    displayMode: decoration.displayMode,
    color: decoration.color ?? upsert.color,
    textSize: decoration.textSize,
    iconSize: decoration.iconSize,
    rot: decoration.rot,
    sortKey: decoration.sortKey ?? upsert.sortKey,
    haloColor: decoration.haloColor,
    haloWidth: decoration.haloWidth,
    iconColor: decoration.iconColor,
    hoverScale: upsert.hover?.scale,
    hoverRaise: upsert.hover?.raise,
  });
}

function buildPointFeature(id: number, lng: number, lat: number, properties: Record<string, unknown>): PointFeature {
  const pruned: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(properties)) {
    if (value !== null && value !== undefined) {
      pruned[key] = value;
    }
  }

  return {
    type: "Feature",
    id,
    geometry: { type: "Point", coordinates: [lng, lat] },
    properties: pruned,
  };
}

function setRecordPosition(record: EntityRecord, lng: number, lat: number): void {
  record.primary.geometry.coordinates[0] = lng;
  record.primary.geometry.coordinates[1] = lat;
  for (const decoration of record.decorations) {
    decoration.geometry.coordinates[0] = lng;
    decoration.geometry.coordinates[1] = lat;
  }
}

function rebuildFeaturesArray(store: EntityLayerStore): void {
  store.collection.features.length = 0;
  store.decorationCollection.features.length = 0;
  for (const record of store.records.values()) {
    store.collection.features.push(record.primary);
    store.decorationCollection.features.push(...record.decorations);
  }
  store.featuresArrayStale = false;
}

function clearDirty(store: EntityLayerStore): void {
  store.structuralDirty = false;
  store.movedIndices.clear();
}

function diffProperties(feature: PointFeature, keys: string[]): { key: string; value: unknown }[] {
  const updates: { key: string; value: unknown }[] = [];
  for (const key of keys) {
    const value = feature.properties[key];
    if (value !== undefined) {
      updates.push({ key, value });
    }
  }

  return updates;
}

export function easeInOut(t: number): number {
  return t < 0.5 ? 4 * t * t * t : 1 - (-2 * t + 2) ** 3 / 2;
}
