import { describe, expect, it, vi } from "vitest";
import {
  animationTick,
  applyMotion,
  applySelection,
  applyUpsert,
  collectFlush,
  createEntityLayerStore,
  decorationFeatureId,
  type EntityLayerStore,
  entityIndexOfFeatureId,
  featureIdsOf,
  primaryFeatureId,
  setHoverBoost,
} from "./entities";
import type { MotionFrame } from "./motion";
import type { EntityUpsert } from "./ops";

function upsertOf(idx: number, overrides: Partial<EntityUpsert> = {}): EntityUpsert {
  return { idx, id: `entity-${idx}`, lng: idx, lat: idx * 2, ...overrides };
}

function motionOf(entries: [index: number, lng: number, lat: number][], epoch = 1): MotionFrame {
  return {
    epoch,
    count: entries.length,
    indices: new Uint32Array(entries.map(([index]) => index)),
    coords: new Float64Array(entries.flatMap(([, lng, lat]) => [lng, lat])),
    rotations: null,
    sortKeys: null,
  };
}

function storeWithEntities(count: number, epoch = 1): EntityLayerStore {
  const store = createEntityLayerStore("vehicles", {});
  applyUpsert(
    store,
    epoch,
    Array.from({ length: count }, (_, idx) => upsertOf(idx)),
    [],
  );
  collectFlush(store); // settle the initial structural flush
  return store;
}

describe("entity feature identity", () => {
  it("derives numeric feature ids from the entity index", () => {
    expect(primaryFeatureId(5)).toBe(320);
    expect(decorationFeatureId(5, 0)).toBe(321);
    expect(decorationFeatureId(5, 62)).toBe(383);
    expect(entityIndexOfFeatureId(320)).toBe(5);
    expect(entityIndexOfFeatureId(383)).toBe(5);
  });

  it("rejects decoration slots outside the 6-bit range", () => {
    const store = createEntityLayerStore("vehicles", {});

    expect(() => applyUpsert(store, 1, [upsertOf(0, { decorations: [{ slot: 63, id: "label" }] })], [])).toThrow(
      /slot 63/,
    );
  });
});

describe("structural upserts", () => {
  it("builds primary and decoration features with pruned null properties", () => {
    const store = createEntityLayerStore("vehicles", {});
    applyUpsert(
      store,
      1,
      [
        upsertOf(2, {
          icon: "bus",
          rot: 90,
          color: null,
          sortKey: 7,
          hover: { scale: 1.3, raise: true },
          props: { line: "12" },
          decorations: [{ slot: 0, id: "label", text: "Line 12", textSize: 12 }],
        }),
      ],
      [],
    );

    const plans = collectFlush(store);
    expect(plans.primary.mode).toBe("setData");
    expect(plans.decorations.mode).toBe("setData");
    const features = plans.primary.mode === "setData" ? plans.primary.data.features : [];
    const decorationFeatures = plans.decorations.mode === "setData" ? plans.decorations.data.features : [];
    expect(features).toHaveLength(1);
    expect(decorationFeatures).toHaveLength(1);

    const primary = features[0] as GeoJSON.Feature & { properties: Record<string, unknown> };
    expect(primary.id).toBe(primaryFeatureId(2));
    expect(primary.properties).toEqual({
      kind: "primary",
      entityId: "entity-2",
      icon: "bus",
      rot: 90,
      sortKey: 7,
      hoverScale: 1.3,
      hoverRaise: true,
      line: "12",
    });

    const decoration = decorationFeatures[0] as GeoJSON.Feature & { properties: Record<string, unknown> };
    expect(decoration.id).toBe(decorationFeatureId(2, 0));
    expect(decoration.properties.kind).toBe("decoration");
    expect(decoration.properties.text).toBe("Line 12");
    expect(decoration.properties.sortKey).toBe(7);
  });

  it("removes entities and forgets their selection and animation state", () => {
    const store = storeWithEntities(3);
    store.selected.add(1);
    applyUpsert(store, 2, [], [1]);

    expect(store.records.has(1)).toBe(false);
    expect(store.selected.has(1)).toBe(false);
    expect(collectFlush(store).primary.mode).toBe("setData");
  });
});

describe("motion application", () => {
  it("mutates feature geometry in place and propagates to decorations", () => {
    const store = createEntityLayerStore("vehicles", {});
    applyUpsert(store, 1, [upsertOf(0, { decorations: [{ slot: 0, id: "label", text: "x" }] })], []);
    collectFlush(store);

    const result = applyMotion(store, motionOf([[0, 10, 20]]), 0);

    expect(result).toEqual({ applied: true, matched: 1 });
    const record = store.records.get(0);
    expect(record?.primary.geometry.coordinates).toEqual([10, 20]);
    expect(record?.decorations[0].geometry.coordinates).toEqual([10, 20]);
  });

  it("drops frames from a stale epoch", () => {
    const store = storeWithEntities(1, 2);

    const result = applyMotion(store, motionOf([[0, 10, 20]], 1), 0);

    expect(result.applied).toBe(false);
    expect(store.records.get(0)?.primary.geometry.coordinates).toEqual([0, 0]);
    expect(collectFlush(store).primary.mode).toBe("none");
  });

  it("applies rotation to the primary and sortKey to all features", () => {
    const store = createEntityLayerStore("vehicles", {});
    applyUpsert(store, 1, [upsertOf(0, { decorations: [{ slot: 0, id: "label", text: "x" }] })], []);
    collectFlush(store);

    const frame = motionOf([[0, 1, 1]]);
    frame.rotations = new Float32Array([45]);
    frame.sortKeys = new Float32Array([9]);
    applyMotion(store, frame, 0);

    const record = store.records.get(0);
    expect(record?.primary.properties.rot).toBe(45);
    expect(record?.primary.properties.sortKey).toBe(9);
    expect(record?.decorations[0].properties.sortKey).toBe(9);
    expect(record?.decorations[0].properties.rot).toBeUndefined();
  });
});

describe("flush planning", () => {
  it("emits an updateData diff when few entities moved", () => {
    const store = storeWithEntities(10);
    applyMotion(store, motionOf([[3, 30, 31]]), 0);

    const plans = collectFlush(store);

    expect(plans.primary.mode).toBe("updateData");
    expect(plans.decorations.mode).toBe("none");
    const diff = plans.primary.mode === "updateData" ? plans.primary.diff : { update: [] };
    expect(diff.update).toHaveLength(1);
    expect(diff.update[0].id).toBe(primaryFeatureId(3));
    expect(diff.update[0].newGeometry.coordinates).toEqual([30, 31]);
  });

  it("routes moved decoration features to the decoration source diff", () => {
    const store = createEntityLayerStore("vehicles", {});
    applyUpsert(
      store,
      1,
      Array.from({ length: 10 }, (_, idx) => upsertOf(idx, { decorations: [{ slot: 0, id: "label", text: "x" }] })),
      [],
    );
    collectFlush(store);
    applyMotion(store, motionOf([[3, 30, 31]]), 0);

    const plans = collectFlush(store);

    expect(plans.primary.mode).toBe("updateData");
    expect(plans.decorations.mode).toBe("updateData");
    const decorationDiff = plans.decorations.mode === "updateData" ? plans.decorations.diff : { update: [] };
    expect(decorationDiff.update).toHaveLength(1);
    expect(decorationDiff.update[0].id).toBe(decorationFeatureId(3, 0));
  });

  it("falls back to setData when more than 20% of entities moved", () => {
    const store = storeWithEntities(10);
    applyMotion(
      store,
      motionOf([
        [0, 1, 1],
        [1, 2, 2],
        [2, 3, 3],
      ]),
      0,
    );

    expect(collectFlush(store).primary.mode).toBe("setData");
  });

  it("always uses setData for structural changes and clears dirty state", () => {
    const store = storeWithEntities(10);
    applyUpsert(store, 2, [upsertOf(10)], []);

    expect(collectFlush(store).primary.mode).toBe("setData");
    expect(collectFlush(store).primary.mode).toBe("none");
  });
});

describe("selection", () => {
  it("emits feature-state changes only for the symmetric difference", () => {
    const store = createEntityLayerStore("vehicles", {});
    applyUpsert(store, 1, [upsertOf(0, { decorations: [{ slot: 0, id: "d", text: "x" }] }), upsertOf(1)], []);
    const setFeatureState = vi.fn();

    applySelection(store, [0], setFeatureState);
    expect(setFeatureState.mock.calls).toEqual([
      [primaryFeatureId(0), { selected: true }],
      [decorationFeatureId(0, 0), { selected: true }],
    ]);

    setFeatureState.mockClear();
    applySelection(store, [0, 1], setFeatureState);
    expect(setFeatureState.mock.calls).toEqual([[primaryFeatureId(1), { selected: true }]]);

    setFeatureState.mockClear();
    applySelection(store, [1], setFeatureState);
    expect(setFeatureState.mock.calls).toEqual([
      [primaryFeatureId(0), { selected: false }],
      [decorationFeatureId(0, 0), { selected: false }],
    ]);
  });

  it("ignores selections for unknown entities", () => {
    const store = storeWithEntities(1);
    const setFeatureState = vi.fn();

    applySelection(store, [99], setFeatureState);

    expect(setFeatureState).not.toHaveBeenCalled();
    expect(store.selected.size).toBe(0);
  });
});

describe("animation", () => {
  it("interpolates from the current position toward the motion target", () => {
    const store = createEntityLayerStore("vehicles", { animation: { durationMs: 100 } });
    applyUpsert(store, 1, [upsertOf(0)], []);
    collectFlush(store);

    applyMotion(store, motionOf([[0, 10, 0]]), 1000);
    expect(store.animating.has(0)).toBe(true);

    expect(animationTick(store, 1050)).toBe(true);
    expect(store.records.get(0)?.primary.geometry.coordinates[0]).toBeCloseTo(5);

    expect(animationTick(store, 1100)).toBe(false);
    expect(store.records.get(0)?.primary.geometry.coordinates[0]).toBe(10);
    expect(store.animating.size).toBe(0);
  });

  it("retargets mid-flight from the interpolated position", () => {
    const store = createEntityLayerStore("vehicles", { animation: { durationMs: 100 } });
    applyUpsert(store, 1, [upsertOf(0)], []);
    collectFlush(store);

    applyMotion(store, motionOf([[0, 10, 0]]), 1000);
    animationTick(store, 1050);
    applyMotion(store, motionOf([[0, 0, 0]]), 1050);
    animationTick(store, 1100);

    expect(store.records.get(0)?.primary.geometry.coordinates[0]).toBeCloseTo(2.5);
  });
});

describe("hover boost", () => {
  it("mirrors the hover scale into a data property and back", () => {
    const store = createEntityLayerStore("vehicles", {});
    applyUpsert(store, 1, [upsertOf(0, { hover: { scale: 1.5 } }), upsertOf(1)], []);
    collectFlush(store);

    expect(setHoverBoost(store, 0, true)).toBe(true);
    expect(store.records.get(0)?.primary.properties.hoverBoost).toBe(1.5);
    expect(store.movedIndices.has(0)).toBe(true);

    expect(setHoverBoost(store, 0, false)).toBe(true);
    expect(store.records.get(0)?.primary.properties.hoverBoost).toBe(1);

    // entities without a hover scale never need a flush
    expect(setHoverBoost(store, 1, true)).toBe(false);
  });
});

describe("feature id lookup", () => {
  it("returns all feature ids of an entity", () => {
    const store = createEntityLayerStore("vehicles", {});
    applyUpsert(store, 1, [upsertOf(4, { decorations: [{ slot: 2, id: "d", text: "x" }] })], []);

    expect(featureIdsOf(store, 4)).toEqual([primaryFeatureId(4), decorationFeatureId(4, 2)]);
    expect(featureIdsOf(store, 99)).toEqual([]);
  });
});
