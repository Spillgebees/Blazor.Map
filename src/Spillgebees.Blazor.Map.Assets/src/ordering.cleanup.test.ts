import { describe, expect, it } from "vitest";
import type { RegisteredMapLayer } from "./interfaces/spillgebees";

describe("RegisteredMapLayer cleanup", () => {
  it("should expose renamed wire fields and omit legacy names", () => {
    // arrange
    const layer: RegisteredMapLayer = {
      layerId: "labels",
      layerSpec: {},
      beforeLayerId: "roads",
      ordering: {
        declarationOrder: 1,
        layerGroup: "labels",
        beforeLayerGroup: "roads",
        afterLayerGroup: "water",
      },
    };

    // act
    const payload = JSON.parse(JSON.stringify(layer)) as Record<string, unknown>;
    const ordering = payload.ordering as Record<string, unknown>;

    // assert
    expect(payload.beforeLayerId).toBe("roads");
    expect(payload.beforeId).toBeUndefined();
    expect(ordering.layerGroup).toBe("labels");
    expect(ordering.beforeLayerGroup).toBe("roads");
    expect(ordering.afterLayerGroup).toBe("water");
    expect(ordering.stack).toBeUndefined();
    expect(ordering.beforeStack).toBeUndefined();
    expect(ordering.afterStack).toBeUndefined();
  });
});
