import { beforeEach, describe, expect, it } from "vitest";
import { createMockDotNetHelper } from "../../test/dotNetHelperMock";
import { getLatestMockMapInstance, getMockMapSources, resetMockMapState } from "../../test/maplibreMock";
import { resetWindowGlobals } from "../../test/windowSetup";
import type { IMapControl } from "../interfaces/controls";
import type { ICircle, IPolyline, IPopupOptions } from "../interfaces/features";
import type { IMapOptions } from "../interfaces/map";
import { bootstrap, createMap } from "../map";
import type { FeatureStorage } from "../types/feature-storage";
import {
  addCircles,
  addPolylines,
  removeCircles,
  removePolylines,
  setupShapePopupHandlers,
  updateCircles,
  updatePolylines,
} from "./shapes";

function createDefaultMapOptions(): IMapOptions {
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
  };
}

function createDefaultControls(): IMapControl[] {
  return [];
}

function createEmptyFeatureStorage(): FeatureStorage {
  return {
    markers: new Map(),
    circles: new Map(),
    polylines: new Map(),
    circleData: new Map(),
    polylineData: new Map(),
  };
}

function createDefaultCircle(overrides?: Partial<ICircle>): ICircle {
  return {
    id: "circle-1",
    position: { latitude: 51.505, longitude: -0.09 },
    radius: 8,
    color: null,
    opacity: null,
    strokeColor: null,
    strokeWidth: null,
    strokeOpacity: null,
    popup: null,
    ...overrides,
  };
}

function createDefaultPolyline(overrides?: Partial<IPolyline>): IPolyline {
  return {
    id: "polyline-1",
    coordinates: [
      { latitude: 51.505, longitude: -0.09 },
      { latitude: 51.51, longitude: -0.1 },
      { latitude: 51.515, longitude: -0.08 },
    ],
    color: null,
    width: null,
    opacity: null,
    popup: null,
    ...overrides,
  };
}

function setupMapAndGetMockMap() {
  const mapElement = document.createElement("div");
  const dotNetHelper = createMockDotNetHelper();
  createMap(
    dotNetHelper,
    "OnMapInitialized",
    mapElement,
    createDefaultMapOptions(),
    createDefaultControls(),
    "light",
    [],
    [],
    [],
    [],
  );
  const mockMap = getLatestMockMapInstance()!;
  return mockMap as unknown as Parameters<typeof addCircles>[0];
}

describe("addCircles", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should create GeoJSON Point features with correct coordinates (lng, lat)", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const circle = createDefaultCircle({
      position: { latitude: 48.8566, longitude: 2.3522 },
    });

    // act
    addCircles(mockMap, [circle], storage);

    // assert — GeoJSON uses [lng, lat] order
    const feature = storage.circleData.get("circle-1")!;
    expect(feature.geometry.type).toBe("Point");
    expect((feature.geometry as GeoJSON.Point).coordinates).toEqual([2.3522, 48.8566]);
  });

  it("should store feature data in circleData map", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const circles = [createDefaultCircle({ id: "c1" }), createDefaultCircle({ id: "c2" })];

    // act
    addCircles(mockMap, circles, storage);

    // assert
    expect(storage.circleData.size).toBe(2);
    expect(storage.circleData.has("c1")).toBe(true);
    expect(storage.circleData.has("c2")).toBe(true);
  });

  it("should set up source and layer on first call", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const circle = createDefaultCircle();

    // act
    addCircles(mockMap, [circle], storage);

    // assert
    const mockMapInstance = getLatestMockMapInstance()!;
    expect(mockMapInstance.addSource).toHaveBeenCalledWith(
      "sgb-circles-source",
      expect.objectContaining({
        type: "geojson",
        data: expect.objectContaining({ type: "FeatureCollection" }),
      }),
    );
    expect(mockMapInstance.addLayer).toHaveBeenCalledWith(
      expect.objectContaining({
        id: "sgb-circles-layer",
        type: "circle",
        source: "sgb-circles-source",
      }),
    );
  });

  it("should not recreate source and layer on subsequent calls", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const circle1 = createDefaultCircle({ id: "c1" });
    const circle2 = createDefaultCircle({ id: "c2" });

    // act
    addCircles(mockMap, [circle1], storage);
    addCircles(mockMap, [circle2], storage);

    // assert — addSource and addLayer should only be called once
    const mockMapInstance = getLatestMockMapInstance()!;
    expect(mockMapInstance.addSource).toHaveBeenCalledTimes(1);
    expect(mockMapInstance.addLayer).toHaveBeenCalledTimes(1);
  });

  it("should include style properties in feature properties", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const circle = createDefaultCircle({
      radius: 12,
      color: "#ff0000",
      opacity: 0.8,
      strokeColor: "#000000",
      strokeWidth: 2,
      strokeOpacity: 0.5,
    });

    // act
    addCircles(mockMap, [circle], storage);

    // assert
    const feature = storage.circleData.get("circle-1")!;
    expect(feature.properties).toEqual(
      expect.objectContaining({
        id: "circle-1",
        radius: 12,
        color: "#ff0000",
        opacity: 0.8,
        strokeColor: "#000000",
        strokeWidth: 2,
        strokeOpacity: 0.5,
      }),
    );
  });

  it("should sync source data after adding circles", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const circle = createDefaultCircle();

    // act
    addCircles(mockMap, [circle], storage);

    // assert — the source setData should have been called
    const sources = getMockMapSources();
    const source = sources.get("sgb-circles-source");
    expect(source).toBeDefined();
    expect(source.setData).toHaveBeenCalledWith(
      expect.objectContaining({
        type: "FeatureCollection",
        features: expect.arrayContaining([
          expect.objectContaining({
            type: "Feature",
            geometry: expect.objectContaining({ type: "Point" }),
          }),
        ]),
      }),
    );
  });

  it("should store popup data as JSON string in properties", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Hello</p>",
      contentMode: "rawHtml",
      trigger: "click",
      anchor: "auto",
      offset: null,
      closeButton: true,
      maxWidth: "300px",
      className: null,
    };
    const circle = createDefaultCircle({ popup });

    // act
    addCircles(mockMap, [circle], storage);

    // assert
    const feature = storage.circleData.get("circle-1")!;
    expect(feature.properties!.popup).toBe(JSON.stringify(popup));
  });

  it("should store null popup when no popup is provided", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const circle = createDefaultCircle({ popup: null });

    // act
    addCircles(mockMap, [circle], storage);

    // assert
    const feature = storage.circleData.get("circle-1")!;
    expect(feature.properties!.popup).toBeNull();
  });

  it("should set feature id to circle id", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const circle = createDefaultCircle({ id: "my-circle" });

    // act
    addCircles(mockMap, [circle], storage);

    // assert
    const feature = storage.circleData.get("my-circle")!;
    expect(feature.id).toBe("my-circle");
  });
});

describe("setupShapePopupHandlers", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should replace existing shape popup handlers instead of accumulating them", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const mockMapInstance = getLatestMockMapInstance()!;

    // act
    setupShapePopupHandlers(mockMap);
    setupShapePopupHandlers(mockMap);

    // assert
    expect(mockMapInstance.off).toHaveBeenCalledWith("click", "sgb-circles-layer", expect.any(Function));
    expect(mockMapInstance.off).toHaveBeenCalledWith("mouseenter", "sgb-circles-layer", expect.any(Function));
    expect(mockMapInstance.off).toHaveBeenCalledWith("mouseleave", "sgb-circles-layer", expect.any(Function));
    expect(mockMapInstance.off).toHaveBeenCalledWith("click", "sgb-polylines-layer", expect.any(Function));
    expect(mockMapInstance.off).toHaveBeenCalledWith("mouseenter", "sgb-polylines-layer", expect.any(Function));
    expect(mockMapInstance.off).toHaveBeenCalledWith("mouseleave", "sgb-polylines-layer", expect.any(Function));
  });
});

describe("updateCircles", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should replace feature data and sync source", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const circle = createDefaultCircle({ id: "c1", radius: 8 });
    addCircles(mockMap, [circle], storage);

    const updatedCircle = createDefaultCircle({
      id: "c1",
      radius: 16,
      color: "#00ff00",
    });

    // act
    updateCircles(mockMap, [updatedCircle], storage);

    // assert
    const feature = storage.circleData.get("c1")!;
    expect(feature.properties!.radius).toBe(16);
    expect(feature.properties!.color).toBe("#00ff00");

    // Source setData should have been called again
    const sources = getMockMapSources();
    const source = sources.get("sgb-circles-source");
    // Called once during addCircles, once during updateCircles
    expect(source.setData).toHaveBeenCalledTimes(2);
  });

  it("should preserve other circles when updating one", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    addCircles(mockMap, [createDefaultCircle({ id: "c1" }), createDefaultCircle({ id: "c2" })], storage);

    const updatedCircle = createDefaultCircle({ id: "c1", radius: 20 });

    // act
    updateCircles(mockMap, [updatedCircle], storage);

    // assert
    expect(storage.circleData.size).toBe(2);
    expect(storage.circleData.get("c1")!.properties!.radius).toBe(20);
    expect(storage.circleData.has("c2")).toBe(true);
  });
});

describe("removeCircles", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should remove feature data and sync source", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    addCircles(mockMap, [createDefaultCircle({ id: "c1" })], storage);
    expect(storage.circleData.size).toBe(1);

    // act
    removeCircles(mockMap, ["c1"], storage);

    // assert
    expect(storage.circleData.size).toBe(0);

    // Source setData should have been called with empty features
    const sources = getMockMapSources();
    const source = sources.get("sgb-circles-source");
    expect(source.setData).toHaveBeenLastCalledWith(
      expect.objectContaining({
        type: "FeatureCollection",
        features: [],
      }),
    );
  });

  it("should preserve other circles when removing one", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    addCircles(mockMap, [createDefaultCircle({ id: "c1" }), createDefaultCircle({ id: "c2" })], storage);

    // act
    removeCircles(mockMap, ["c1"], storage);

    // assert
    expect(storage.circleData.size).toBe(1);
    expect(storage.circleData.has("c2")).toBe(true);
  });

  it("should not throw when removing nonexistent ids", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    addCircles(mockMap, [createDefaultCircle({ id: "c1" })], storage);

    // act & assert
    expect(() => removeCircles(mockMap, ["nonexistent"], storage)).not.toThrow();
    expect(storage.circleData.size).toBe(1);
  });
});

describe("addPolylines", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should create GeoJSON LineString features with correct coordinate arrays", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const polyline = createDefaultPolyline({
      coordinates: [
        { latitude: 48.8566, longitude: 2.3522 },
        { latitude: 48.8606, longitude: 2.3376 },
      ],
    });

    // act
    addPolylines(mockMap, [polyline], storage);

    // assert — GeoJSON uses [lng, lat] order
    const feature = storage.polylineData.get("polyline-1")!;
    expect(feature.geometry.type).toBe("LineString");
    expect((feature.geometry as GeoJSON.LineString).coordinates).toEqual([
      [2.3522, 48.8566],
      [2.3376, 48.8606],
    ]);
  });

  it("should store feature data in polylineData map", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const polylines = [createDefaultPolyline({ id: "p1" }), createDefaultPolyline({ id: "p2" })];

    // act
    addPolylines(mockMap, polylines, storage);

    // assert
    expect(storage.polylineData.size).toBe(2);
    expect(storage.polylineData.has("p1")).toBe(true);
    expect(storage.polylineData.has("p2")).toBe(true);
  });

  it("should set up source and layer on first call", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const polyline = createDefaultPolyline();

    // act
    addPolylines(mockMap, [polyline], storage);

    // assert
    const mockMapInstance = getLatestMockMapInstance()!;
    expect(mockMapInstance.addSource).toHaveBeenCalledWith(
      "sgb-polylines-source",
      expect.objectContaining({
        type: "geojson",
        data: expect.objectContaining({ type: "FeatureCollection" }),
      }),
    );
    expect(mockMapInstance.addLayer).toHaveBeenCalledWith(
      expect.objectContaining({
        id: "sgb-polylines-layer",
        type: "line",
        source: "sgb-polylines-source",
      }),
    );
  });

  it("should include style properties in feature properties", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const polyline = createDefaultPolyline({
      color: "#ff0000",
      width: 5,
      opacity: 0.7,
    });

    // act
    addPolylines(mockMap, [polyline], storage);

    // assert
    const feature = storage.polylineData.get("polyline-1")!;
    expect(feature.properties).toEqual(
      expect.objectContaining({
        id: "polyline-1",
        color: "#ff0000",
        width: 5,
        opacity: 0.7,
      }),
    );
  });

  it("should store popup data as JSON string in properties", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Route info</p>",
      contentMode: "rawHtml",
      trigger: "hover",
      anchor: "top",
      offset: { x: 0, y: -10 },
      closeButton: false,
      maxWidth: null,
      className: "route-popup",
    };
    const polyline = createDefaultPolyline({ popup });

    // act
    addPolylines(mockMap, [polyline], storage);

    // assert
    const feature = storage.polylineData.get("polyline-1")!;
    expect(feature.properties!.popup).toBe(JSON.stringify(popup));
  });

  it("should set feature id to polyline id", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const polyline = createDefaultPolyline({ id: "my-polyline" });

    // act
    addPolylines(mockMap, [polyline], storage);

    // assert
    const feature = storage.polylineData.get("my-polyline")!;
    expect(feature.id).toBe("my-polyline");
  });

  it("should sync source data after adding polylines", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const polyline = createDefaultPolyline();

    // act
    addPolylines(mockMap, [polyline], storage);

    // assert
    const sources = getMockMapSources();
    const source = sources.get("sgb-polylines-source");
    expect(source).toBeDefined();
    expect(source.setData).toHaveBeenCalledWith(
      expect.objectContaining({
        type: "FeatureCollection",
        features: expect.arrayContaining([
          expect.objectContaining({
            type: "Feature",
            geometry: expect.objectContaining({ type: "LineString" }),
          }),
        ]),
      }),
    );
  });
});

describe("updatePolylines", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should replace feature data and sync source", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    const polyline = createDefaultPolyline({ id: "p1", color: "#0000ff" });
    addPolylines(mockMap, [polyline], storage);

    const updatedPolyline = createDefaultPolyline({
      id: "p1",
      color: "#ff0000",
      width: 10,
    });

    // act
    updatePolylines(mockMap, [updatedPolyline], storage);

    // assert
    const feature = storage.polylineData.get("p1")!;
    expect(feature.properties!.color).toBe("#ff0000");
    expect(feature.properties!.width).toBe(10);

    const sources = getMockMapSources();
    const source = sources.get("sgb-polylines-source");
    expect(source.setData).toHaveBeenCalledTimes(2);
  });

  it("should preserve other polylines when updating one", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    addPolylines(mockMap, [createDefaultPolyline({ id: "p1" }), createDefaultPolyline({ id: "p2" })], storage);

    const updatedPolyline = createDefaultPolyline({ id: "p1", color: "#00ff00" });

    // act
    updatePolylines(mockMap, [updatedPolyline], storage);

    // assert
    expect(storage.polylineData.size).toBe(2);
    expect(storage.polylineData.get("p1")!.properties!.color).toBe("#00ff00");
    expect(storage.polylineData.has("p2")).toBe(true);
  });
});

describe("removePolylines", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should remove feature data and sync source", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    addPolylines(mockMap, [createDefaultPolyline({ id: "p1" })], storage);
    expect(storage.polylineData.size).toBe(1);

    // act
    removePolylines(mockMap, ["p1"], storage);

    // assert
    expect(storage.polylineData.size).toBe(0);

    const sources = getMockMapSources();
    const source = sources.get("sgb-polylines-source");
    expect(source.setData).toHaveBeenLastCalledWith(
      expect.objectContaining({
        type: "FeatureCollection",
        features: [],
      }),
    );
  });

  it("should preserve other polylines when removing one", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    addPolylines(mockMap, [createDefaultPolyline({ id: "p1" }), createDefaultPolyline({ id: "p2" })], storage);

    // act
    removePolylines(mockMap, ["p1"], storage);

    // assert
    expect(storage.polylineData.size).toBe(1);
    expect(storage.polylineData.has("p2")).toBe(true);
  });

  it("should not throw when removing nonexistent ids", () => {
    // arrange
    const mockMap = setupMapAndGetMockMap();
    const storage = createEmptyFeatureStorage();
    addPolylines(mockMap, [createDefaultPolyline({ id: "p1" })], storage);

    // act & assert
    expect(() => removePolylines(mockMap, ["nonexistent"], storage)).not.toThrow();
    expect(storage.polylineData.size).toBe(1);
  });
});
