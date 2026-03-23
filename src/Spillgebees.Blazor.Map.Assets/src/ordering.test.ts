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
    beforeId: null,
    imperativeBeforeId: undefined,
    ordering: {
      declarationOrder,
      stack: null,
      beforeStack: null,
      afterStack: null,
    },
    ...overrides,
  };
}

describe("resolveLayerOrder", () => {
  it("should keep declaration order stable while honoring stack relationships and native anchors", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("stations", 2, {
        ordering: {
          declarationOrder: 2,
          stack: "stations",
          beforeStack: null,
          afterStack: null,
        },
      }),
      createRegisteredLayer("clusters", 3, {
        ordering: {
          declarationOrder: 3,
          stack: "trains",
          beforeStack: null,
          afterStack: "stations",
        },
      }),
      createRegisteredLayer("labels", 4, {
        beforeId: "poi-label",
        ordering: {
          declarationOrder: 4,
          stack: "train-labels",
          beforeStack: null,
          afterStack: "trains",
        },
      }),
    ];
    const styleLayerIds = ["background", "road-label", "poi-label"];

    // act
    const resolved = resolveLayerOrder(layers, styleLayerIds);

    // assert
    expect(resolved).toEqual(["labels", "stations", "clusters"]);
  });

  it("should reject cyclic stack declarations with a clear error", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("a", 1, {
        ordering: {
          declarationOrder: 1,
          stack: "a",
          beforeStack: null,
          afterStack: "b",
        },
      }),
      createRegisteredLayer("b", 2, {
        ordering: {
          declarationOrder: 2,
          stack: "b",
          beforeStack: null,
          afterStack: "a",
        },
      }),
    ];

    // act
    const act = () => resolveLayerOrder(layers, ["background"]);

    // assert
    expect(act).toThrowError(/cyclic/i);
  });

  it("should let beforeId anchor take precedence over conflicting stack placement", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("labels", 1, {
        beforeId: "road-label",
        ordering: {
          declarationOrder: 1,
          stack: "labels",
          beforeStack: null,
          afterStack: "trains",
        },
      }),
      createRegisteredLayer("trains", 2, {
        ordering: {
          declarationOrder: 2,
          stack: "trains",
          beforeStack: null,
          afterStack: null,
        },
      }),
    ];

    // act
    const resolved = resolveLayerOrder(layers, ["background", "road-label", "poi-label"]);

    // assert
    expect(resolved).toEqual(["labels", "trains"]);
  });

  it("should support mixing native anchors with custom beforeId dependencies", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("labels", 1, {
        beforeId: "road-label",
        ordering: {
          declarationOrder: 1,
          stack: "labels",
          beforeStack: null,
          afterStack: null,
        },
      }),
      createRegisteredLayer("halo", 2, {
        beforeId: "labels",
        ordering: {
          declarationOrder: 2,
          stack: "halo",
          beforeStack: null,
          afterStack: null,
        },
      }),
    ];

    // act
    const resolved = resolveLayerOrder(layers, ["background", "road-label", "poi-label"]);

    // assert
    expect(resolved).toEqual(["halo", "labels"]);
  });

  it("should build a stable successor chain for a train-like stack fixture", () => {
    // arrange
    const layers: RegisteredMapLayer[] = [
      createRegisteredLayer("station-labels", 1, {
        ordering: {
          declarationOrder: 1,
          stack: "station-labels",
          beforeStack: null,
          afterStack: null,
        },
      }),
      createRegisteredLayer("train-cluster-hit-area", 2, {
        ordering: {
          declarationOrder: 2,
          stack: "train-cluster-hit-area",
          beforeStack: null,
          afterStack: "station-labels",
        },
      }),
      createRegisteredLayer("train-clusters", 3, {
        ordering: {
          declarationOrder: 3,
          stack: "train-clusters",
          beforeStack: null,
          afterStack: "train-cluster-hit-area",
        },
      }),
      createRegisteredLayer("train-cluster-count", 4, {
        ordering: {
          declarationOrder: 4,
          stack: "train-cluster-count",
          beforeStack: null,
          afterStack: "train-clusters",
        },
      }),
      createRegisteredLayer("train-icons", 5, {
        ordering: {
          declarationOrder: 5,
          stack: "train-icons",
          beforeStack: null,
          afterStack: "train-cluster-count",
        },
      }),
    ];

    // act
    const plan = buildLayerPlan(layers, []);

    // assert
    expect(plan).toEqual([
      { layerId: "station-labels", beforeId: "train-cluster-hit-area" },
      { layerId: "train-cluster-hit-area", beforeId: "train-clusters" },
      { layerId: "train-clusters", beforeId: "train-cluster-count" },
      { layerId: "train-cluster-count", beforeId: "train-icons" },
      { layerId: "train-icons", beforeId: null },
    ]);
  });
});
