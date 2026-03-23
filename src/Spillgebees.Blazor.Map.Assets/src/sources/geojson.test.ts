import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMockDotNetHelper } from "../../test/dotNetHelperMock";
import {
  fireLoadEvent,
  fireMapEvent,
  getLatestMockMapInstance,
  getMockMapSources,
  resetMockMapState,
} from "../../test/maplibreMock";
import { resetWindowGlobals } from "../../test/windowSetup";
import type { IMapControlOptions } from "../interfaces/controls";
import type { IMapOptions } from "../interfaces/map";
import { bootstrap, createMap } from "../map";
import {
  addMapLayer,
  addMapSource,
  moveMapLayer,
  reconcileLayerOrdering,
  removeMapLayer,
  removeMapSource,
  setLayoutProperty,
  setSourceData,
  setSourceDataAnimated,
  unregisterLayerEvents,
  wireLayerEvents,
} from "./geojson";

function createDefaultMapOptions(overrides?: Partial<IMapOptions>): IMapOptions {
  return {
    center: { latitude: 51.505, longitude: -0.09 },
    zoom: 13,
    style: null,
    composedGlyphsUrl: null,
    pitch: 0,
    bearing: 0,
    projection: "mercator",
    terrain: false,
    terrainExaggeration: 1.0,
    fitBoundsOptions: null,
    minZoom: null,
    maxZoom: null,
    interactive: true,
    cooperativeGestures: false,
    ...overrides,
  };
}

function createDefaultControlOptions(): IMapControlOptions {
  return {
    navigation: null,
    scale: null,
    fullscreen: null,
    geolocate: null,
    terrain: null,
    center: null,
  };
}

function setupMapElement(): HTMLElement {
  const mapElement = document.createElement("div");
  const dotNetHelper = createMockDotNetHelper();
  createMap(
    dotNetHelper,
    "OnMapInitialized",
    mapElement,
    createDefaultMapOptions(),
    createDefaultControlOptions(),
    "light",
    [],
    [],
    [],
    [],
  );
  fireLoadEvent();
  return mapElement;
}

describe("addMapSource", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should add a geojson source to the map", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    const sourceSpec = {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    };

    // act
    addMapSource(mapElement, "test-source", sourceSpec);

    // assert
    expect(mockMap.addSource).toHaveBeenCalledWith("test-source", sourceSpec);
  });

  it("should not add source if it already exists", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    const sourceSpec = {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    };

    // Add source once
    addMapSource(mapElement, "test-source", sourceSpec);
    mockMap.addSource.mockClear();

    // act — add again (source already exists in mockSources)
    addMapSource(mapElement, "test-source", sourceSpec);

    // assert — should not call addSource again
    expect(mockMap.addSource).not.toHaveBeenCalled();
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");
    const sourceSpec = { type: "geojson", data: null };

    // act & assert — should not throw
    expect(() => addMapSource(unknownElement, "test-source", sourceSpec)).not.toThrow();
  });
});

describe("removeMapSource", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should remove source from the map", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    const sourceSpec = {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    };
    addMapSource(mapElement, "test-source", sourceSpec);

    // act
    removeMapSource(mapElement, "test-source");

    // assert
    expect(mockMap.removeSource).toHaveBeenCalledWith("test-source");
  });

  it("should remove all layers referencing the source before removing it", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;

    // Add source and layer
    addMapSource(mapElement, "test-source", { type: "geojson", data: null });
    addMapLayer(mapElement, { id: "test-layer", type: "line", source: "test-source" }, null);

    // Mock getStyle to return the layer
    mockMap.getStyle.mockReturnValue({
      layers: [{ id: "test-layer", type: "line", source: "test-source" }],
    });
    mockMap.getLayer.mockImplementation((id: string) => (id === "test-layer" ? {} : undefined));

    // act
    removeMapSource(mapElement, "test-source");

    // assert — layer should be removed before source
    expect(mockMap.removeLayer).toHaveBeenCalledWith("test-layer");
    expect(mockMap.removeSource).toHaveBeenCalledWith("test-source");
  });

  it("should not throw when source does not exist", () => {
    // arrange
    const mapElement = setupMapElement();

    // act & assert
    expect(() => removeMapSource(mapElement, "nonexistent")).not.toThrow();
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");

    // act & assert
    expect(() => removeMapSource(unknownElement, "test-source")).not.toThrow();
  });
});

describe("setSourceData", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should call setData on the source", () => {
    // arrange
    const mapElement = setupMapElement();
    const sourceSpec = {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    };
    addMapSource(mapElement, "test-source", sourceSpec);

    const newData = {
      type: "FeatureCollection",
      features: [
        {
          type: "Feature",
          geometry: { type: "Point", coordinates: [0, 0] },
          properties: {},
        },
      ],
    };

    // act
    setSourceData(mapElement, "test-source", newData);

    // assert
    const mockSource = getMockMapSources().get("test-source");
    expect(mockSource).toBeDefined();
    expect(mockSource!.setData).toHaveBeenCalledWith(newData);
  });

  it("should be a no-op when source does not exist", () => {
    // arrange
    const mapElement = setupMapElement();

    // act & assert — should not throw
    expect(() => setSourceData(mapElement, "nonexistent", { type: "FeatureCollection", features: [] })).not.toThrow();
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");

    // act & assert
    expect(() =>
      setSourceData(unknownElement, "test-source", { type: "FeatureCollection", features: [] }),
    ).not.toThrow();
  });
});

describe("addMapLayer", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should add a layer to the map", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    addMapSource(mapElement, "test-source", { type: "geojson", data: null });
    mockMap.getStyle.mockReturnValue({ layers: [] });

    const layerSpec = {
      id: "test-layer",
      type: "line",
      source: "test-source",
      paint: { "line-color": "#ff0000" },
    };

    // act
    addMapLayer(mapElement, layerSpec, null);
    reconcileLayerOrdering(mockMap);

    // assert
    expect(mockMap.addLayer).toHaveBeenCalledWith(layerSpec, undefined);
  });

  it("should pass beforeId when provided", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    addMapSource(mapElement, "test-source", { type: "geojson", data: null });

    const layerSpec = {
      id: "test-layer",
      type: "line",
      source: "test-source",
    };

    // act
    addMapLayer(mapElement, layerSpec, "other-layer");
    reconcileLayerOrdering(mockMap);

    // assert
    expect(mockMap.addLayer).toHaveBeenCalledWith(layerSpec, "other-layer");
  });

  it("should reconcile ordering against registered native anchors", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    addMapSource(mapElement, "test-source", { type: "geojson", data: null });
    mockMap.getStyle.mockReturnValue({
      layers: [{ id: "background" }, { id: "road-label" }, { id: "poi-label" }],
    });
    const existingLayers = new Set<string>(["train-labels", "train-icons"]);
    mockMap.getLayer.mockImplementation((id: string) => (existingLayers.has(id) ? {} : undefined));

    // act
    addMapLayer(mapElement, { id: "train-labels", type: "symbol", source: "test-source" }, "road-label", {
      declarationOrder: 1,
      stack: "labels",
      beforeStack: null,
      afterStack: null,
    });
    addMapLayer(mapElement, { id: "train-icons", type: "symbol", source: "test-source" }, null, {
      declarationOrder: 2,
      stack: "trains",
      beforeStack: null,
      afterStack: null,
    });
    reconcileLayerOrdering(mockMap);

    // assert
    expect(mockMap.moveLayer).toHaveBeenCalledWith("train-labels", "road-label");
    expect(mockMap.moveLayer).toHaveBeenCalledWith("train-icons", undefined);
  });

  it("should add a chained custom stack from an empty map without referencing missing successors", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    addMapSource(mapElement, "test-source", { type: "geojson", data: null });

    const existingLayers = new Set<string>();
    mockMap.getStyle.mockReturnValue({ layers: [] });
    mockMap.getLayer.mockImplementation((id: string) => (existingLayers.has(id) ? {} : undefined));
    mockMap.addLayer.mockImplementation((layer: { id?: string }, beforeId?: string) => {
      if (beforeId && !existingLayers.has(beforeId)) {
        throw new Error(`Unknown beforeId: ${beforeId}`);
      }

      if (layer.id) {
        existingLayers.add(layer.id);
      }
    });
    mockMap.moveLayer.mockImplementation((layerId: string, beforeId?: string) => {
      if (!existingLayers.has(layerId)) {
        throw new Error(`Unknown layer: ${layerId}`);
      }

      if (beforeId && !existingLayers.has(beforeId)) {
        throw new Error(`Unknown beforeId: ${beforeId}`);
      }
    });

    // act
    const act = () => {
      addMapLayer(mapElement, { id: "layer-a", type: "line", source: "test-source" }, null, {
        declarationOrder: 1,
        stack: "stack-a",
        beforeStack: null,
        afterStack: null,
      });
      addMapLayer(mapElement, { id: "layer-b", type: "line", source: "test-source" }, null, {
        declarationOrder: 2,
        stack: "stack-b",
        beforeStack: null,
        afterStack: "stack-a",
      });
      addMapLayer(mapElement, { id: "layer-c", type: "line", source: "test-source" }, null, {
        declarationOrder: 3,
        stack: "stack-c",
        beforeStack: null,
        afterStack: "stack-b",
      });
      reconcileLayerOrdering(mockMap);
    };

    // assert
    expect(act).not.toThrow();
    expect(mockMap.addLayer).toHaveBeenCalledWith({ id: "layer-c", type: "line", source: "test-source" }, undefined);
    expect(mockMap.addLayer).toHaveBeenCalledWith({ id: "layer-b", type: "line", source: "test-source" }, "layer-c");
    expect(mockMap.addLayer).toHaveBeenCalledWith({ id: "layer-a", type: "line", source: "test-source" }, "layer-b");
  });

  it("should reconcile mixed existing and missing custom layers by applying the final chain from the top down", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    addMapSource(mapElement, "test-source", { type: "geojson", data: null });

    const existingLayers = new Set<string>();
    mockMap.getStyle.mockReturnValue({ layers: [] });
    mockMap.getLayer.mockImplementation((id: string) => (existingLayers.has(id) ? {} : undefined));
    mockMap.addLayer.mockImplementation((layer: { id?: string }, beforeId?: string) => {
      if (beforeId && !existingLayers.has(beforeId)) {
        throw new Error(`Unknown beforeId: ${beforeId}`);
      }

      if (layer.id) {
        existingLayers.add(layer.id);
      }
    });
    mockMap.moveLayer.mockImplementation((layerId: string, beforeId?: string) => {
      if (!existingLayers.has(layerId)) {
        throw new Error(`Unknown layer: ${layerId}`);
      }

      if (beforeId && !existingLayers.has(beforeId)) {
        throw new Error(`Unknown beforeId: ${beforeId}`);
      }
    });

    addMapLayer(mapElement, { id: "layer-a", type: "line", source: "test-source" }, null, {
      declarationOrder: 1,
      stack: "stack-a",
      beforeStack: null,
      afterStack: null,
    });
    addMapLayer(mapElement, { id: "layer-c", type: "line", source: "test-source" }, null, {
      declarationOrder: 3,
      stack: "stack-c",
      beforeStack: null,
      afterStack: "stack-b",
    });
    mockMap.addLayer.mockClear();
    mockMap.moveLayer.mockClear();

    // act
    const act = () => {
      addMapLayer(mapElement, { id: "layer-b", type: "line", source: "test-source" }, null, {
        declarationOrder: 2,
        stack: "stack-b",
        beforeStack: null,
        afterStack: "stack-a",
      });
      reconcileLayerOrdering(mockMap);
    };

    // assert
    expect(act).not.toThrow();
    expect(mockMap.addLayer).toHaveBeenCalledWith({ id: "layer-c", type: "line", source: "test-source" }, undefined);
    expect(mockMap.addLayer).toHaveBeenCalledWith({ id: "layer-b", type: "line", source: "test-source" }, "layer-c");
    expect(mockMap.addLayer).toHaveBeenCalledWith({ id: "layer-a", type: "line", source: "test-source" }, "layer-b");
  });

  it("should not add layer if it already exists", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    addMapSource(mapElement, "test-source", { type: "geojson", data: null });

    const layerSpec = { id: "test-layer", type: "line", source: "test-source" };

    // Mock getLayer to return truthy for existing layer
    mockMap.getLayer.mockImplementation((id: string) => (id === "test-layer" ? {} : undefined));

    // act
    addMapLayer(mapElement, layerSpec, null);

    // assert — addLayer should not be called since the layer already "exists"
    expect(mockMap.addLayer).not.toHaveBeenCalledWith(layerSpec, expect.anything());
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");
    const layerSpec = { id: "test-layer", type: "line", source: "test-source" };

    // act & assert
    expect(() => addMapLayer(unknownElement, layerSpec, null)).not.toThrow();
  });
});

describe("removeMapLayer", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should remove a layer from the map", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;

    // Mock getLayer to return truthy
    mockMap.getLayer.mockImplementation((id: string) => (id === "test-layer" ? {} : undefined));

    // act
    removeMapLayer(mapElement, "test-layer");

    // assert
    expect(mockMap.removeLayer).toHaveBeenCalledWith("test-layer");
  });

  it("should not throw when layer does not exist", () => {
    // arrange
    const mapElement = setupMapElement();

    // act & assert
    expect(() => removeMapLayer(mapElement, "nonexistent")).not.toThrow();
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");

    // act & assert
    expect(() => removeMapLayer(unknownElement, "test-layer")).not.toThrow();
  });
});

describe("moveMapLayer", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should move a layer and update the stored ordering hint", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    addMapSource(mapElement, "test-source", { type: "geojson", data: null });
    addMapLayer(mapElement, { id: "background-layer", type: "line", source: "test-source" }, null);
    addMapLayer(mapElement, { id: "tracked-layer", type: "line", source: "test-source" }, null);
    mockMap.getLayer.mockImplementation((id: string) => (id === "tracked-layer" ? {} : undefined));

    // act
    moveMapLayer(mapElement, "tracked-layer", "background-layer");

    // assert
    expect(mockMap.moveLayer).toHaveBeenCalledWith("tracked-layer", "background-layer");

    const map = window.Spillgebees.Map.maps.get(mapElement)!;
    const registration = window.Spillgebees.Map.layerSpecs.get(map)?.get("tracked-layer");
    expect(registration?.imperativeBeforeId).toBe("background-layer");
  });

  it("should fall back to direct map movement for unregistered native layers", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    mockMap.getLayer.mockImplementation((id: string) => (id === "native-layer" ? {} : undefined));

    // act
    moveMapLayer(mapElement, "native-layer", "road-label");

    // assert
    expect(mockMap.moveLayer).toHaveBeenCalledWith("native-layer", "road-label");
  });
});

describe("wireLayerEvents", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should replace existing subscriptions before wiring new ones", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    const dotNetRef = createMockDotNetHelper();

    // act
    wireLayerEvents(mapElement, "test-layer", dotNetRef, true, true, true);
    wireLayerEvents(mapElement, "test-layer", dotNetRef, true, false, false);

    // assert
    expect(mockMap.off).toHaveBeenCalledWith("click", "test-layer", expect.any(Function));
    expect(mockMap.off).toHaveBeenCalledWith("mouseenter", "test-layer", expect.any(Function));
    expect(mockMap.off).toHaveBeenCalledWith("mouseleave", "test-layer", expect.any(Function));
  });

  it("should rebind layer-scoped handlers when styledata fires after layer recreation", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    const dotNetRef = createMockDotNetHelper();
    mockMap.getLayer.mockImplementation((id: string) => (id === "test-layer" ? {} : undefined));

    wireLayerEvents(mapElement, "test-layer", dotNetRef, true, true, true);
    mockMap.on.mockClear();

    // act
    fireMapEvent("styledata");

    // assert
    expect(mockMap.on).toHaveBeenCalledWith("click", "test-layer", expect.any(Function));
    expect(mockMap.on).toHaveBeenCalledWith("mouseenter", "test-layer", expect.any(Function));
    expect(mockMap.on).toHaveBeenCalledWith("mouseleave", "test-layer", expect.any(Function));
  });

  it("should unregister layer event handlers explicitly", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    const dotNetRef = createMockDotNetHelper();
    wireLayerEvents(mapElement, "test-layer", dotNetRef, true, false, true);

    // act
    unregisterLayerEvents(mapElement, "test-layer");

    // assert
    expect(mockMap.off).toHaveBeenCalledWith("click", "test-layer", expect.any(Function));
    expect(mockMap.off).toHaveBeenCalledWith("mouseleave", "test-layer", expect.any(Function));
  });
});

describe("setLayoutProperty", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should update layout values for existing layers", () => {
    // arrange
    const mapElement = setupMapElement();
    const mockMap = getLatestMockMapInstance()!;
    addMapSource(mapElement, "test-source", { type: "geojson", data: null });
    addMapLayer(
      mapElement,
      {
        id: "test-layer",
        type: "symbol",
        source: "test-source",
        layout: { "text-rotate": ["coalesce", ["get", "rotation"], 0] },
      },
      null,
    );
    mockMap.getLayer.mockImplementation((id: string) => (id === "test-layer" ? {} : undefined));

    // act
    setLayoutProperty(mapElement, "test-layer", "text-rotate", ["coalesce", ["get", "rotation"], 45]);

    // assert
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("test-layer", "text-rotate", [
      "coalesce",
      ["get", "rotation"],
      45,
    ]);
  });
});

describe("setSourceDataAnimated", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should scope active animations per map instance", () => {
    // arrange
    const originalRequestAnimationFrame = globalThis.requestAnimationFrame;
    const originalCancelAnimationFrame = globalThis.cancelAnimationFrame;
    const requestAnimationFrameMock = vi.fn<(callback: FrameRequestCallback) => number>().mockReturnValue(1);
    const cancelAnimationFrameMock = vi.fn<(handle: number) => void>();
    globalThis.requestAnimationFrame = requestAnimationFrameMock;
    globalThis.cancelAnimationFrame = cancelAnimationFrameMock;

    try {
      const firstMapElement = setupMapElement();
      const firstSourceSpec = {
        type: "geojson",
        data: {
          type: "FeatureCollection",
          features: [
            { type: "Feature", id: "train", geometry: { type: "Point", coordinates: [1, 1] }, properties: {} },
          ],
        },
      };
      addMapSource(firstMapElement, "shared-source", firstSourceSpec);
      const firstMockMap = getLatestMockMapInstance()!;
      firstMockMap.querySourceFeatures = vi
        .fn()
        .mockReturnValue([
          { id: "train", geometry: { type: "Point", coordinates: [1, 1] }, properties: { bearing: 0 } },
        ]);

      const secondMapElement = document.createElement("div");
      const dotNetHelper = createMockDotNetHelper();
      createMap(
        dotNetHelper,
        "OnMapInitialized",
        secondMapElement,
        createDefaultMapOptions(),
        createDefaultControlOptions(),
        "light",
        [],
        [],
        [],
        [],
      );
      fireLoadEvent();
      const secondMockMap = getLatestMockMapInstance()!;
      addMapSource(secondMapElement, "shared-source", firstSourceSpec);
      secondMockMap.querySourceFeatures = vi
        .fn()
        .mockReturnValue([
          { id: "train", geometry: { type: "Point", coordinates: [2, 2] }, properties: { bearing: 10 } },
        ]);

      const nextData = {
        type: "FeatureCollection",
        features: [{ type: "Feature", id: "train", geometry: { type: "Point", coordinates: [3, 3] }, properties: {} }],
      } as GeoJSON.FeatureCollection;

      // act
      setSourceDataAnimated(firstMapElement, "shared-source", nextData, 1000, "linear");
      setSourceDataAnimated(secondMapElement, "shared-source", nextData, 1000, "linear");

      // assert
      expect(cancelAnimationFrameMock).not.toHaveBeenCalled();
      expect(requestAnimationFrameMock).toHaveBeenCalledTimes(2);
    } finally {
      globalThis.requestAnimationFrame = originalRequestAnimationFrame;
      globalThis.cancelAnimationFrame = originalCancelAnimationFrame;
    }
  });

  it("should animate from the stored source snapshot when rendered features are unavailable", () => {
    // arrange
    const originalRequestAnimationFrame = globalThis.requestAnimationFrame;
    const originalCancelAnimationFrame = globalThis.cancelAnimationFrame;
    const requestAnimationFrameMock = vi.fn<(callback: FrameRequestCallback) => number>().mockReturnValue(1);
    const cancelAnimationFrameMock = vi.fn<(handle: number) => void>();
    globalThis.requestAnimationFrame = requestAnimationFrameMock;
    globalThis.cancelAnimationFrame = cancelAnimationFrameMock;

    try {
      const mapElement = setupMapElement();
      addMapSource(mapElement, "tracked-source", {
        type: "geojson",
        data: {
          type: "FeatureCollection",
          features: [
            {
              type: "Feature",
              id: "train-1",
              geometry: { type: "Point", coordinates: [1, 1] },
              properties: { bearing: 0 },
            },
          ],
        },
      });

      const mockMap = getLatestMockMapInstance()!;
      mockMap.querySourceFeatures.mockReturnValue([]);

      const source = getMockMapSources().get("tracked-source");
      expect(source).toBeDefined();
      source?.setData.mockClear();

      const nextData = {
        type: "FeatureCollection",
        features: [
          {
            type: "Feature",
            id: "train-1",
            geometry: { type: "Point", coordinates: [2, 2] },
            properties: { bearing: 45 },
          },
        ],
      } as GeoJSON.FeatureCollection;

      // act
      setSourceDataAnimated(mapElement, "tracked-source", nextData, 500, "linear");

      // assert
      expect(requestAnimationFrameMock).toHaveBeenCalledTimes(1);
      expect(source?.setData).not.toHaveBeenCalledWith(nextData);
      expect(cancelAnimationFrameMock).not.toHaveBeenCalled();
    } finally {
      globalThis.requestAnimationFrame = originalRequestAnimationFrame;
      globalThis.cancelAnimationFrame = originalCancelAnimationFrame;
    }
  });
});
