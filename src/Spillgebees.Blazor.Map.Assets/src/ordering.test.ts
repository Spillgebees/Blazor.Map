import { describe, expect, it } from "vitest";
import type { RegisteredMapLayer } from "./interfaces/spillgebees";
import { buildLayerPlan, resolveLayerOrder } from "./ordering";

function createRegisteredLayer(
  layerId: string,
  declarationOrder: number,
  overrides?: Partial<RegisteredMapLayer>,
): RegisteredMapLayer {
  return {
    layerId,
    layerSpec: {
      id: layerId,
      type: "line",
      source: "test-source",
    },
    beforeLayerId: null,
    imperativeBeforeLayerId: undefined,
    ordering: {
      declarationOrder,
      layerGroup: null,
      beforeLayerGroup: null,
      afterLayerGroup: null,
    },
    ...overrides,
  };
}

describe("resolveLayerOrder", () => {
  it("should keep declaration order stable while honoring layerGroup relationships and native anchors", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("stations", 2, {
        ordering: {
          declarationOrder: 2,
          layerGroup: "stations",
          beforeLayerGroup: null,
          afterLayerGroup: null,
        },
      }),
      createRegisteredLayer("clusters", 3, {
        ordering: {
          declarationOrder: 3,
          layerGroup: "trains",
          beforeLayerGroup: null,
          afterLayerGroup: "stations",
        },
      }),
      createRegisteredLayer("labels", 4, {
        beforeLayerId: "poi-label",
        ordering: {
          declarationOrder: 4,
          layerGroup: "train-labels",
          beforeLayerGroup: null,
          afterLayerGroup: "trains",
        },
      }),
    ];
    const styleLayerIds = ["background", "road-label", "poi-label"];

    // act
    const resolved = resolveLayerOrder(layers, styleLayerIds);

    // assert
    expect(resolved).toEqual(["labels", "stations", "clusters"]);
  });

  it("should reject cyclic layerGroup declarations with a clear error", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("a", 1, {
        ordering: {
          declarationOrder: 1,
          layerGroup: "a",
          beforeLayerGroup: null,
          afterLayerGroup: "b",
        },
      }),
      createRegisteredLayer("b", 2, {
        ordering: {
          declarationOrder: 2,
          layerGroup: "b",
          beforeLayerGroup: null,
          afterLayerGroup: "a",
        },
      }),
    ];

    // act
    const act = () => resolveLayerOrder(layers, ["background"]);

    // assert
    expect(act).toThrowError(/cyclic/i);
  });

  it("should let beforeLayerId anchor take precedence over conflicting layerGroup placement", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("labels", 1, {
        beforeLayerId: "road-label",
        ordering: {
          declarationOrder: 1,
          layerGroup: "labels",
          beforeLayerGroup: null,
          afterLayerGroup: "trains",
        },
      }),
      createRegisteredLayer("trains", 2, {
        ordering: {
          declarationOrder: 2,
          layerGroup: "trains",
          beforeLayerGroup: null,
          afterLayerGroup: null,
        },
      }),
    ];

    // act
    const resolved = resolveLayerOrder(layers, ["background", "road-label", "poi-label"]);

    // assert
    expect(resolved).toEqual(["labels", "trains"]);
  });

  it("should support mixing native anchors with custom beforeLayerId dependencies", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("labels", 1, {
        beforeLayerId: "road-label",
        ordering: {
          declarationOrder: 1,
          layerGroup: "labels",
          beforeLayerGroup: null,
          afterLayerGroup: null,
        },
      }),
      createRegisteredLayer("halo", 2, {
        beforeLayerId: "labels",
        ordering: {
          declarationOrder: 2,
          layerGroup: "halo",
          beforeLayerGroup: null,
          afterLayerGroup: null,
        },
      }),
    ];

    // act
    const resolved = resolveLayerOrder(layers, ["background", "road-label", "poi-label"]);

    // assert
    expect(resolved).toEqual(["halo", "labels"]);
  });

  it("should build a stable successor chain for a train-like layerGroup fixture", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("station-labels", 1, {
        ordering: {
          declarationOrder: 1,
          layerGroup: "station-labels",
          beforeLayerGroup: null,
          afterLayerGroup: null,
        },
      }),
      createRegisteredLayer("train-cluster-hit-area", 2, {
        ordering: {
          declarationOrder: 2,
          layerGroup: "train-cluster-hit-area",
          beforeLayerGroup: null,
          afterLayerGroup: "station-labels",
        },
      }),
      createRegisteredLayer("train-clusters", 3, {
        ordering: {
          declarationOrder: 3,
          layerGroup: "train-clusters",
          beforeLayerGroup: null,
          afterLayerGroup: "train-cluster-hit-area",
        },
      }),
      createRegisteredLayer("train-cluster-count", 4, {
        ordering: {
          declarationOrder: 4,
          layerGroup: "train-cluster-count",
          beforeLayerGroup: null,
          afterLayerGroup: "train-clusters",
        },
      }),
      createRegisteredLayer("train-icons", 5, {
        ordering: {
          declarationOrder: 5,
          layerGroup: "train-icons",
          beforeLayerGroup: null,
          afterLayerGroup: "train-cluster-count",
        },
      }),
    ];

    // act
    const plan = buildLayerPlan(layers, []);

    // assert
    expect(plan).toEqual([
      { layerId: "station-labels", beforeLayerId: "train-cluster-hit-area" },
      { layerId: "train-cluster-hit-area", beforeLayerId: "train-clusters" },
      { layerId: "train-clusters", beforeLayerId: "train-cluster-count" },
      { layerId: "train-cluster-count", beforeLayerId: "train-icons" },
      { layerId: "train-icons", beforeLayerId: null },
    ]);
  });
});
