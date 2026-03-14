import { beforeEach, describe, expect, it } from "vitest";
import { createMockDotNetHelper } from "../test/dotNetHelperMock";
import {
  fireLoadEvent,
  fireMapEvent,
  getLatestMockMapInstance,
  getMockMapConstructor,
  getMockMarkerConstructor,
  resetMockMapState,
} from "../test/maplibreMock";
import { resetWindowGlobals } from "../test/windowSetup";
import type { IMapControlOptions } from "./interfaces/controls";
import type { IMarker, IPolyline } from "./interfaces/features";
import type { IFitBoundsOptions, IMapOptions, IMapStyle, ITileOverlay } from "./interfaces/map";
import {
  bootstrap,
  buildStyleFromOptions,
  createMap,
  disposeMap,
  fitBounds,
  flyTo,
  PROTOCOL_VERSION,
  resize,
  setControls,
  setMapOptions,
  setOverlays,
  setTheme,
  syncFeatures,
} from "./map";

function createDefaultMapOptions(overrides?: Partial<IMapOptions>): IMapOptions {
  return {
    center: { latitude: 51.505, longitude: -0.09 },
    zoom: 13,
    style: null,
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

describe("bootstrap", () => {
  beforeEach(() => {
    resetWindowGlobals();
  });

  it("should initialize the namespace when none exists", () => {
    // arrange & act
    bootstrap();

    // assert
    expect(window.Spillgebees).toBeDefined();
    expect(window.Spillgebees.Map).toBeDefined();
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
  });

  it("should register all map functions", () => {
    // arrange & act
    bootstrap();

    // assert
    const { mapFunctions } = window.Spillgebees.Map;
    expect(mapFunctions.createMap).toBeTypeOf("function");
    expect(mapFunctions.syncFeatures).toBeTypeOf("function");
    expect(mapFunctions.setOverlays).toBeTypeOf("function");
    expect(mapFunctions.setControls).toBeTypeOf("function");
    expect(mapFunctions.setMapOptions).toBeTypeOf("function");
    expect(mapFunctions.setTheme).toBeTypeOf("function");
    expect(mapFunctions.fitBounds).toBeTypeOf("function");
    expect(mapFunctions.flyTo).toBeTypeOf("function");
    expect(mapFunctions.resize).toBeTypeOf("function");
    expect(mapFunctions.disposeMap).toBeTypeOf("function");
  });

  it("should initialize empty stores", () => {
    // arrange & act
    bootstrap();

    // assert
    expect(window.Spillgebees.Map.maps).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.maps.size).toBe(0);
    expect(window.Spillgebees.Map.features).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.features.size).toBe(0);
    expect(window.Spillgebees.Map.overlays).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.overlays.size).toBe(0);
    expect(window.Spillgebees.Map.controls).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.controls.size).toBe(0);
  });

  it("should be a no-op when the protocol version already matches", () => {
    // arrange
    bootstrap();
    const originalMapFunctions = window.Spillgebees.Map.mapFunctions;
    const originalMaps = window.Spillgebees.Map.maps;

    // act
    bootstrap();

    // assert — same object references, not replaced
    expect(window.Spillgebees.Map.mapFunctions).toBe(originalMapFunctions);
    expect(window.Spillgebees.Map.maps).toBe(originalMaps);
  });

  it("should force-reinitialize when the protocol version mismatches", () => {
    // arrange — simulate a stale namespace from an older version
    window.Spillgebees = {
      Map: {
        getProtocolVersion: () => PROTOCOL_VERSION - 1,
        mapFunctions: { staleFunction: () => {} } as never,
        maps: new Map(),
        features: new Map(),
        overlays: new Map(),
        controls: new Map(),
      },
    };
    const staleMapFunctions = window.Spillgebees.Map.mapFunctions;

    // act
    bootstrap();

    // assert — namespace was replaced, not preserved
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
    expect(window.Spillgebees.Map.mapFunctions).not.toBe(staleMapFunctions);
    expect(window.Spillgebees.Map.mapFunctions.createMap).toBeTypeOf("function");
    expect((window.Spillgebees.Map.mapFunctions as Record<string, unknown>).staleFunction).toBeUndefined();
  });

  it("should reinitialize when getProtocolVersion is missing", () => {
    // arrange — simulate a corrupted namespace with no version function
    window.Spillgebees = {
      Map: {
        mapFunctions: {},
        maps: new Map(),
        features: new Map(),
        overlays: new Map(),
        controls: new Map(),
      } as never,
    };

    // act
    bootstrap();

    // assert
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
    expect(window.Spillgebees.Map.mapFunctions.createMap).toBeTypeOf("function");
  });

  it("should reinitialize when getProtocolVersion throws", () => {
    // arrange — simulate a namespace where getProtocolVersion is corrupted
    window.Spillgebees = {
      Map: {
        getProtocolVersion: () => {
          throw new Error("corrupted");
        },
        mapFunctions: {},
        maps: new Map(),
        features: new Map(),
        overlays: new Map(),
        controls: new Map(),
      } as never,
    };

    // act
    bootstrap();

    // assert
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
  });

  it("should preserve other Spillgebees namespace properties", () => {
    // arrange — simulate other libraries using the Spillgebees namespace
    window.Spillgebees = {
      OtherLibrary: { foo: "bar" },
    } as never;

    // act
    bootstrap();

    // assert — Map is initialized but OtherLibrary is preserved
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
    expect((window.Spillgebees as Record<string, unknown>).OtherLibrary).toEqual({ foo: "bar" });
  });
});

describe("index lifecycle hooks", () => {
  beforeEach(() => {
    resetWindowGlobals();
  });

  it("should initialize on first beforeStart call", async () => {
    // arrange
    const { beforeStart } = await import("./index");

    // act
    beforeStart(undefined);

    // assert
    expect(window.hasBeforeStartBeenCalledForSpillgebeesMap).toBe(true);
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
  });

  it("should not re-bootstrap on duplicate lifecycle hook calls", async () => {
    // arrange
    const { beforeStart, beforeWebStart } = await import("./index");
    beforeStart(undefined);
    const originalMaps = window.Spillgebees.Map.maps;

    // act — simulate duplicate hook call from a different render mode
    beforeWebStart(undefined);

    // assert — same store reference, no re-initialization
    expect(window.Spillgebees.Map.maps).toBe(originalMaps);
  });
});

// biome-ignore lint/security/noSecrets: false positive on function name
describe("buildStyleFromOptions", () => {
  it("should return the default style URL when style is null", () => {
    // arrange & act
    const result = buildStyleFromOptions(null);

    // assert
    expect(result).toBe("https://tiles.openfreemap.org/styles/liberty");
  });

  it("should return the style URL directly when url is set", () => {
    // arrange
    const style: IMapStyle = {
      url: "https://example.com/style.json",
      rasterSource: null,
      wmsSource: null,
    };

    // act
    const result = buildStyleFromOptions(style);

    // assert
    expect(result).toBe("https://example.com/style.json");
  });

  it("should build a raster style from rasterSource", () => {
    // arrange
    const style: IMapStyle = {
      url: null,
      rasterSource: {
        urlTemplate: "https://tiles.example.com/{z}/{x}/{y}.png",
        attribution: "© Example",
        tileSize: 256,
      },
      wmsSource: null,
    };

    // act
    const result = buildStyleFromOptions(style);

    // assert
    expect(result).toEqual({
      version: 8,
      sources: {
        "raster-tiles": {
          type: "raster",
          tiles: ["https://tiles.example.com/{z}/{x}/{y}.png"],
          tileSize: 256,
          attribution: "© Example",
        },
      },
      layers: [{ id: "raster-layer", type: "raster", source: "raster-tiles" }],
    });
  });

  it("should build a WMS-compatible raster style from wmsSource", () => {
    // arrange
    const style: IMapStyle = {
      url: null,
      rasterSource: null,
      wmsSource: {
        baseUrl: "https://wms.example.com/wms",
        layers: "streets",
        attribution: "© WMS Provider",
        format: "image/png",
        transparent: true,
        version: "1.1.1",
        tileSize: 256,
      },
    };

    // act
    const result = buildStyleFromOptions(style);

    // assert
    expect(result).toEqual({
      version: 8,
      sources: {
        "raster-tiles": {
          type: "raster",
          tiles: [
            // biome-ignore lint/security/noSecrets: WMS URL for testing, not a secret
            "https://wms.example.com/wms?SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&LAYERS=streets&FORMAT=image/png&TRANSPARENT=true&SRS=EPSG:3857&STYLES=&WIDTH=256&HEIGHT=256&BBOX={bbox-epsg-3857}",
          ],
          tileSize: 256,
          attribution: "© WMS Provider",
        },
      },
      layers: [{ id: "raster-layer", type: "raster", source: "raster-tiles" }],
    });
  });

  it("should prefer url over rasterSource when both are set", () => {
    // arrange
    const style: IMapStyle = {
      url: "https://example.com/style.json",
      rasterSource: {
        urlTemplate: "https://tiles.example.com/{z}/{x}/{y}.png",
        attribution: "© Example",
        tileSize: 256,
      },
      wmsSource: null,
    };

    // act
    const result = buildStyleFromOptions(style);

    // assert
    expect(result).toBe("https://example.com/style.json");
  });
});

describe("createMap", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should create a MapLibre map and store it", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // assert
    expect(window.Spillgebees.Map.maps.has(mapElement)).toBe(true);
    expect(getLatestMockMapInstance()).not.toBeNull();
  });

  it("should convert coordinates from (lat, lng) to [lng, lat]", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions({
      center: { latitude: 51.505, longitude: -0.09 },
    });
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // assert — MapLibre expects [lng, lat]
    expect(getMockMapConstructor()).toHaveBeenCalledWith(
      expect.objectContaining({
        center: [-0.09, 51.505],
      }),
    );
  });

  it("should use default style when style is null", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions({ style: null });
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // assert
    expect(getMockMapConstructor()).toHaveBeenCalledWith(
      expect.objectContaining({
        style: "https://tiles.openfreemap.org/styles/liberty",
      }),
    );
  });

  it("should build raster style from rasterSource", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions({
      style: {
        url: null,
        rasterSource: {
          urlTemplate: "https://tiles.example.com/{z}/{x}/{y}.png",
          attribution: "© Example",
          tileSize: 256,
        },
        wmsSource: null,
      },
    });
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // assert
    expect(getMockMapConstructor()).toHaveBeenCalledWith(
      expect.objectContaining({
        style: {
          version: 8,
          sources: {
            "raster-tiles": {
              type: "raster",
              tiles: ["https://tiles.example.com/{z}/{x}/{y}.png"],
              tileSize: 256,
              attribution: "© Example",
            },
          },
          layers: [{ id: "raster-layer", type: "raster", source: "raster-tiles" }],
        },
      }),
    );
  });

  it("should pass map options to MapLibre constructor", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions({
      zoom: 10,
      pitch: 45,
      bearing: 90,
      minZoom: 3,
      maxZoom: 18,
      interactive: false,
      cooperativeGestures: true,
    });
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // assert
    expect(getMockMapConstructor()).toHaveBeenCalledWith(
      expect.objectContaining({
        container: mapElement,
        zoom: 10,
        pitch: 45,
        bearing: 90,
        minZoom: 3,
        maxZoom: 18,
        interactive: false,
        cooperativeGestures: true,
        attributionControl: true,
      }),
    );
  });

  it("should pass undefined for minZoom/maxZoom when null", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions({ minZoom: null, maxZoom: null });
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // assert
    expect(getMockMapConstructor()).toHaveBeenCalledWith(
      expect.objectContaining({
        minZoom: undefined,
        maxZoom: undefined,
      }),
    );
  });

  it("should apply the dark theme class when theme is 'dark'", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "dark", [], [], [], []);

    // assert
    expect(mapElement.classList.contains("sgb-map-dark")).toBe(true);
  });

  it("should not apply the dark theme class when theme is 'light'", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // assert
    expect(mapElement.classList.contains("sgb-map-dark")).toBe(false);
  });

  it("should initialize empty feature storage", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // assert
    const map = window.Spillgebees.Map.maps.get(mapElement);
    expect(map).toBeDefined();
    const featureStorage = window.Spillgebees.Map.features.get(map!);
    expect(featureStorage).toBeDefined();
    expect(featureStorage!.markers.size).toBe(0);
    expect(featureStorage!.circleData.size).toBe(0);
    expect(featureStorage!.polylineData.size).toBe(0);
  });

  it("should call the dotNetHelper callback after load", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    fireLoadEvent();

    // assert
    expect(dotNetHelper.invokeMethodAsync).toHaveBeenCalledWith("OnMapInitialized");
  });

  it("should register a load event handler on the map", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // assert
    const mockMap = getLatestMockMapInstance()!;
    expect(mockMap.on).toHaveBeenCalledWith("load", expect.any(Function));
  });

  it("should set globe projection on load when projection is globe", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions({ projection: "globe" });
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    fireLoadEvent();

    // assert
    const mockMap = getLatestMockMapInstance()!;
    expect(mockMap.setProjection).toHaveBeenCalledWith("globe");
  });

  it("should not set projection when projection is mercator", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions({ projection: "mercator" });
    const controlOptions = createDefaultControlOptions();

    // act
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    fireLoadEvent();

    // assert
    const mockMap = getLatestMockMapInstance()!;
    expect(mockMap.setProjection).not.toHaveBeenCalled();
  });
});

describe("disposeMap", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should remove the map and clean up all stores", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    // act
    disposeMap(mapElement);

    // assert
    expect(mockMap.remove).toHaveBeenCalled();
    expect(window.Spillgebees.Map.maps.has(mapElement)).toBe(false);
    expect(window.Spillgebees.Map.maps.size).toBe(0);
  });

  it("should clean up feature, overlay, and control stores", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);

    // act
    disposeMap(mapElement);

    // assert
    expect(window.Spillgebees.Map.features.size).toBe(0);
    expect(window.Spillgebees.Map.overlays.size).toBe(0);
    expect(window.Spillgebees.Map.controls.size).toBe(0);
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");

    // act & assert — should not throw
    expect(() => disposeMap(unknownElement)).not.toThrow();
  });

  it("should not affect other maps when disposing one", () => {
    // arrange
    const mapElement1 = document.createElement("div");
    const mapElement2 = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement1, mapOptions, controlOptions, "light", [], [], [], []);
    createMap(dotNetHelper, "OnMapInitialized", mapElement2, mapOptions, controlOptions, "light", [], [], [], []);

    // act
    disposeMap(mapElement1);

    // assert
    expect(window.Spillgebees.Map.maps.has(mapElement1)).toBe(false);
    expect(window.Spillgebees.Map.maps.has(mapElement2)).toBe(true);
  });
});

describe("setTheme", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should add the dark class when theme is 'dark'", () => {
    // arrange
    const mapElement = document.createElement("div");

    // act
    setTheme(mapElement, "dark");

    // assert
    expect(mapElement.classList.contains("sgb-map-dark")).toBe(true);
  });

  it("should remove the dark class when theme is 'light'", () => {
    // arrange
    const mapElement = document.createElement("div");
    mapElement.classList.add("sgb-map-dark");

    // act
    setTheme(mapElement, "light");

    // assert
    expect(mapElement.classList.contains("sgb-map-dark")).toBe(false);
  });

  it("should not add duplicate dark classes", () => {
    // arrange
    const mapElement = document.createElement("div");
    mapElement.classList.add("sgb-map-dark");

    // act
    setTheme(mapElement, "dark");

    // assert — classList.add is naturally idempotent, but verify
    expect(mapElement.className).toBe("sgb-map-dark");
  });
});

describe("resize", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should call map.resize() on the stored map", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    // act
    resize(mapElement);

    // assert
    expect(mockMap.resize).toHaveBeenCalled();
  });

  it("should not throw for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");

    // act & assert
    expect(() => resize(unknownElement)).not.toThrow();
  });
});

describe("setMapOptions", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should call jumpTo with correct center and zoom", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const initialOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, initialOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const newOptions = createDefaultMapOptions({
      center: { latitude: 48.8566, longitude: 2.3522 },
      zoom: 15,
    });

    // act
    setMapOptions(mapElement, newOptions);

    // assert — center should be [lng, lat]
    expect(mockMap.jumpTo).toHaveBeenCalledWith(
      expect.objectContaining({
        center: [2.3522, 48.8566],
        zoom: 15,
      }),
    );
  });

  it("should update pitch and bearing", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const initialOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, initialOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const newOptions = createDefaultMapOptions({
      pitch: 60,
      bearing: 180,
    });

    // act
    setMapOptions(mapElement, newOptions);

    // assert
    expect(mockMap.setPitch).toHaveBeenCalledWith(60);
    expect(mockMap.setBearing).toHaveBeenCalledWith(180);
  });

  it("should not throw for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");
    const options = createDefaultMapOptions();

    // act & assert
    expect(() => setMapOptions(unknownElement, options)).not.toThrow();
  });
});

describe("setControls", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should add navigation control when enabled", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const navControlOptions: IMapControlOptions = {
      ...createDefaultControlOptions(),
      navigation: {
        enable: true,
        position: "top-right",
        showCompass: true,
        showZoom: true,
      },
    };

    // act
    setControls(mapElement, navControlOptions);

    // assert
    expect(mockMap.addControl).toHaveBeenCalledTimes(1);
    expect(mockMap.addControl).toHaveBeenCalledWith(expect.anything(), "top-right");
  });

  it("should add scale control when enabled", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const scaleControlOptions: IMapControlOptions = {
      ...createDefaultControlOptions(),
      scale: {
        enable: true,
        position: "bottom-left",
        unit: "metric",
      },
    };

    // act
    setControls(mapElement, scaleControlOptions);

    // assert
    expect(mockMap.addControl).toHaveBeenCalledTimes(1);
    expect(mockMap.addControl).toHaveBeenCalledWith(expect.anything(), "bottom-left");
  });

  it("should add multiple controls when multiple are enabled", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const multiControlOptions: IMapControlOptions = {
      navigation: {
        enable: true,
        position: "top-right",
        showCompass: true,
        showZoom: true,
      },
      scale: {
        enable: true,
        position: "bottom-left",
        unit: "metric",
      },
      fullscreen: {
        enable: true,
        position: "top-right",
      },
      geolocate: null,
      terrain: null,
      center: null,
    };

    // act
    setControls(mapElement, multiControlOptions);

    // assert
    expect(mockMap.addControl).toHaveBeenCalledTimes(3);
  });

  it("should remove existing controls before adding new ones", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const firstControlOptions: IMapControlOptions = {
      ...createDefaultControlOptions(),
      navigation: {
        enable: true,
        position: "top-right",
        showCompass: true,
        showZoom: true,
      },
    };

    // Add initial controls
    setControls(mapElement, firstControlOptions);
    expect(mockMap.addControl).toHaveBeenCalledTimes(1);

    const secondControlOptions: IMapControlOptions = {
      ...createDefaultControlOptions(),
      scale: {
        enable: true,
        position: "bottom-left",
        unit: "metric",
      },
    };

    // act — replace controls
    setControls(mapElement, secondControlOptions);

    // assert — the first control was removed, and the new one was added
    expect(mockMap.removeControl).toHaveBeenCalledTimes(1);
    expect(mockMap.addControl).toHaveBeenCalledTimes(2); // 1 from first + 1 from second
  });

  it("should handle all controls being null gracefully", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const emptyControlOptions = createDefaultControlOptions();

    // act
    setControls(mapElement, emptyControlOptions);

    // assert
    expect(mockMap.addControl).not.toHaveBeenCalled();
  });

  it("should add center control when enabled", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const centerControlOptions: IMapControlOptions = {
      ...createDefaultControlOptions(),
      center: {
        enable: true,
        position: "bottom-right",
        center: { latitude: 51.505, longitude: -0.09 },
        zoom: 13,
        fitBoundsOptions: null,
      },
    };

    // act
    setControls(mapElement, centerControlOptions);

    // assert
    expect(mockMap.addControl).toHaveBeenCalledTimes(1);
    expect(mockMap.addControl).toHaveBeenCalledWith(expect.anything(), "bottom-right");
  });

  it("should add geolocate control when enabled", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const geolocateControlOptions: IMapControlOptions = {
      ...createDefaultControlOptions(),
      geolocate: {
        enable: true,
        position: "top-left",
        trackUser: true,
      },
    };

    // act
    setControls(mapElement, geolocateControlOptions);

    // assert
    expect(mockMap.addControl).toHaveBeenCalledTimes(1);
    expect(mockMap.addControl).toHaveBeenCalledWith(expect.anything(), "top-left");
  });

  it("should add terrain control when enabled", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const terrainControlOptions: IMapControlOptions = {
      ...createDefaultControlOptions(),
      terrain: {
        enable: true,
        position: "top-right",
      },
    };

    // act
    setControls(mapElement, terrainControlOptions);

    // assert
    expect(mockMap.addControl).toHaveBeenCalledTimes(1);
    expect(mockMap.addControl).toHaveBeenCalledWith(expect.anything(), "top-right");
  });

  it("should not add controls that are disabled", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const mockMap = getLatestMockMapInstance()!;

    const disabledControlOptions: IMapControlOptions = {
      navigation: {
        enable: false,
        position: "top-right",
        showCompass: true,
        showZoom: true,
      },
      scale: {
        enable: false,
        position: "bottom-left",
        unit: "metric",
      },
      fullscreen: null,
      geolocate: null,
      terrain: null,
      center: null,
    };

    // act
    setControls(mapElement, disabledControlOptions);

    // assert
    expect(mockMap.addControl).not.toHaveBeenCalled();
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");
    const controlOptions = createDefaultControlOptions();

    // act & assert
    expect(() => setControls(unknownElement, controlOptions)).not.toThrow();
  });

  it("should track controls in the controls store", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    const map = window.Spillgebees.Map.maps.get(mapElement)!;

    const navControlOptions: IMapControlOptions = {
      ...createDefaultControlOptions(),
      navigation: {
        enable: true,
        position: "top-right",
        showCompass: true,
        showZoom: true,
      },
      scale: {
        enable: true,
        position: "bottom-left",
        unit: "metric",
      },
    };

    // act
    setControls(mapElement, navControlOptions);

    // assert
    const controls = window.Spillgebees.Map.controls.get(map);
    expect(controls).toBeDefined();
    expect(controls!.size).toBe(2);
  });
});

function createDefaultMarker(overrides?: Partial<IMarker>): IMarker {
  return {
    id: "marker-1",
    position: { latitude: 51.505, longitude: -0.09 },
    title: null,
    popup: null,
    icon: null,
    color: null,
    scale: null,
    rotation: null,
    draggable: false,
    opacity: null,
    className: null,
    ...overrides,
  };
}

describe("syncFeatures", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should delegate to marker add/update/remove", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    createMap(dotNetHelper, "OnMapInitialized", mapElement, mapOptions, controlOptions, "light", [], [], [], []);
    fireLoadEvent();

    const marker = createDefaultMarker({ id: "m1" });

    // act — add a marker
    syncFeatures(mapElement, {
      markers: { added: [marker], updated: [], removedIds: [] },
      circles: { added: [], updated: [], removedIds: [] },
      polylines: { added: [], updated: [], removedIds: [] },
    });

    // assert — marker was created
    const map = window.Spillgebees.Map.maps.get(mapElement)!;
    const storage = window.Spillgebees.Map.features.get(map)!;
    expect(storage.markers.size).toBe(1);
    expect(storage.markers.has("m1")).toBe(true);

    // act — update the marker
    const updatedMarker = createDefaultMarker({
      id: "m1",
      position: { latitude: 52.0, longitude: 0.0 },
    });
    syncFeatures(mapElement, {
      markers: { added: [], updated: [updatedMarker], removedIds: [] },
      circles: { added: [], updated: [], removedIds: [] },
      polylines: { added: [], updated: [], removedIds: [] },
    });

    // assert — still one marker in storage (updated, not added)
    expect(storage.markers.size).toBe(1);

    // act — remove the marker
    syncFeatures(mapElement, {
      markers: { added: [], updated: [], removedIds: ["m1"] },
      circles: { added: [], updated: [], removedIds: [] },
      polylines: { added: [], updated: [], removedIds: [] },
    });

    // assert
    expect(storage.markers.size).toBe(0);
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");

    // act & assert — should not throw
    expect(() =>
      syncFeatures(unknownElement, {
        markers: { added: [], updated: [], removedIds: [] },
        circles: { added: [], updated: [], removedIds: [] },
        polylines: { added: [], updated: [], removedIds: [] },
      }),
    ).not.toThrow();
  });

  it("should create markers with correct coordinates on initial sync in createMap", () => {
    // arrange
    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    const mapOptions = createDefaultMapOptions();
    const controlOptions = createDefaultControlOptions();
    const initialMarker = createDefaultMarker({
      id: "init-1",
      position: { latitude: 48.8566, longitude: 2.3522 },
    });

    // act
    createMap(
      dotNetHelper,
      "OnMapInitialized",
      mapElement,
      mapOptions,
      controlOptions,
      "light",
      [initialMarker],
      [],
      [],
      [],
    );
    fireLoadEvent();

    // assert — marker was created via syncFeatures during load
    const markerConstructor = getMockMarkerConstructor();
    const markerInstance = markerConstructor.mock.results[0]?.value;
    expect(markerInstance.setLngLat).toHaveBeenCalledWith([2.3522, 48.8566]);
    expect(markerInstance.addTo).toHaveBeenCalled();
  });
});

// --- Phase 6: Tile overlays ---

function createDefaultOverlay(overrides?: Partial<ITileOverlay>): ITileOverlay {
  return {
    id: "overlay-1",
    urlTemplate: "https://tiles.example.com/{z}/{x}/{y}.png",
    attribution: "© Example",
    tileSize: 256,
    opacity: 0.7,
    ...overrides,
  };
}

describe("setOverlays", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should add raster source and layer for each overlay", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    const overlay = createDefaultOverlay();

    // act
    setOverlays(mapElement, [overlay]);

    // assert
    expect(mockMap.addSource).toHaveBeenCalledWith("sgb-overlay-overlay-1", {
      type: "raster",
      tiles: ["https://tiles.example.com/{z}/{x}/{y}.png"],
      tileSize: 256,
      attribution: "© Example",
    });
    expect(mockMap.addLayer).toHaveBeenCalledWith({
      id: "sgb-overlay-overlay-1",
      type: "raster",
      source: "sgb-overlay-overlay-1",
      paint: {
        "raster-opacity": 0.7,
      },
    });
  });

  it("should remove overlays not in new list", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    // Add an overlay first
    setOverlays(mapElement, [createDefaultOverlay({ id: "to-remove" })]);

    // Mock getLayer/getSource to return truthy for existing overlays
    mockMap.getLayer.mockImplementation((id: string) => (id === "sgb-overlay-to-remove" ? {} : undefined));

    // act — set overlays with an empty list
    setOverlays(mapElement, []);

    // assert
    expect(mockMap.removeLayer).toHaveBeenCalledWith("sgb-overlay-to-remove");
    expect(mockMap.removeSource).toHaveBeenCalledWith("sgb-overlay-to-remove");
  });

  it("should not duplicate existing overlays", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    const overlay = createDefaultOverlay({ id: "existing" });
    setOverlays(mapElement, [overlay]);

    // Clear call counts
    mockMap.addSource.mockClear();
    mockMap.addLayer.mockClear();

    // act — call setOverlays again with the same overlay
    setOverlays(mapElement, [overlay]);

    // assert — should not add source/layer again
    expect(mockMap.addSource).not.toHaveBeenCalledWith("sgb-overlay-existing", expect.anything());
    expect(mockMap.addLayer).not.toHaveBeenCalledWith(expect.objectContaining({ id: "sgb-overlay-existing" }));
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");

    // act & assert — should not throw
    expect(() => setOverlays(unknownElement, [createDefaultOverlay()])).not.toThrow();
  });
});

// --- Phase 7: FitBounds + FlyTo ---

function createDefaultPolyline(overrides?: Partial<IPolyline>): IPolyline {
  return {
    id: "polyline-1",
    coordinates: [
      { latitude: 51.505, longitude: -0.09 },
      { latitude: 51.51, longitude: -0.1 },
    ],
    color: null,
    width: null,
    opacity: null,
    popup: null,
    ...overrides,
  };
}

describe("fitBounds", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should calculate correct bounds from marker coordinates", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    // Add markers
    const marker1 = createDefaultMarker({ id: "m1", position: { latitude: 48.0, longitude: 2.0 } });
    const marker2 = createDefaultMarker({ id: "m2", position: { latitude: 52.0, longitude: 4.0 } });
    syncFeatures(mapElement, {
      markers: { added: [marker1, marker2], updated: [], removedIds: [] },
      circles: { added: [], updated: [], removedIds: [] },
      polylines: { added: [], updated: [], removedIds: [] },
    });

    // Mock getLngLat to return the right coordinates for each marker
    const map = window.Spillgebees.Map.maps.get(mapElement)!;
    const storage = window.Spillgebees.Map.features.get(map)!;
    const markerEntry1 = storage.markers.get("m1")!;
    const markerEntry2 = storage.markers.get("m2")!;
    (markerEntry1.marker.getLngLat as ReturnType<typeof import("vitest").vi.fn>).mockReturnValue({
      lng: 2.0,
      lat: 48.0,
    });
    (markerEntry2.marker.getLngLat as ReturnType<typeof import("vitest").vi.fn>).mockReturnValue({
      lng: 4.0,
      lat: 52.0,
    });

    const options: IFitBoundsOptions = {
      featureIds: ["m1", "m2"],
      padding: null,
      topLeftPadding: null,
      bottomRightPadding: null,
    };

    // act
    fitBounds(mapElement, options);

    // assert — bounds should be [[minLng, minLat], [maxLng, maxLat]]
    expect(mockMap.fitBounds).toHaveBeenCalledWith(
      [
        [2.0, 48.0],
        [4.0, 52.0],
      ],
      {},
    );
  });

  it("should include padding options", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    // Add a marker
    const marker = createDefaultMarker({ id: "m1", position: { latitude: 48.0, longitude: 2.0 } });
    syncFeatures(mapElement, {
      markers: { added: [marker], updated: [], removedIds: [] },
      circles: { added: [], updated: [], removedIds: [] },
      polylines: { added: [], updated: [], removedIds: [] },
    });

    const map = window.Spillgebees.Map.maps.get(mapElement)!;
    const storage = window.Spillgebees.Map.features.get(map)!;
    const entry = storage.markers.get("m1")!;
    (entry.marker.getLngLat as ReturnType<typeof import("vitest").vi.fn>).mockReturnValue({
      lng: 2.0,
      lat: 48.0,
    });

    const options: IFitBoundsOptions = {
      featureIds: ["m1"],
      padding: { x: 50, y: 30 },
      topLeftPadding: null,
      bottomRightPadding: null,
    };

    // act
    fitBounds(mapElement, options);

    // assert
    expect(mockMap.fitBounds).toHaveBeenCalledWith(
      [
        [2.0, 48.0],
        [2.0, 48.0],
      ],
      { padding: { top: 30, bottom: 30, left: 50, right: 50 } },
    );
  });

  it("should be a no-op when no features match", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    const options: IFitBoundsOptions = {
      featureIds: ["nonexistent"],
      padding: null,
      topLeftPadding: null,
      bottomRightPadding: null,
    };

    // act
    fitBounds(mapElement, options);

    // assert
    expect(mockMap.fitBounds).not.toHaveBeenCalled();
  });

  it("should handle polyline coordinates with multiple points", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    // Add a polyline
    const polyline = createDefaultPolyline({
      id: "p1",
      coordinates: [
        { latitude: 48.0, longitude: 2.0 },
        { latitude: 50.0, longitude: 3.0 },
        { latitude: 52.0, longitude: 1.0 },
      ],
    });
    syncFeatures(mapElement, {
      markers: { added: [], updated: [], removedIds: [] },
      circles: { added: [], updated: [], removedIds: [] },
      polylines: { added: [polyline], updated: [], removedIds: [] },
    });

    const options: IFitBoundsOptions = {
      featureIds: ["p1"],
      padding: null,
      topLeftPadding: null,
      bottomRightPadding: null,
    };

    // act
    fitBounds(mapElement, options);

    // assert — bounds should encompass all polyline points
    expect(mockMap.fitBounds).toHaveBeenCalledWith(
      [
        [1.0, 48.0],
        [3.0, 52.0],
      ],
      {},
    );
  });

  it("should handle topLeftPadding and bottomRightPadding", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    const marker = createDefaultMarker({ id: "m1", position: { latitude: 48.0, longitude: 2.0 } });
    syncFeatures(mapElement, {
      markers: { added: [marker], updated: [], removedIds: [] },
      circles: { added: [], updated: [], removedIds: [] },
      polylines: { added: [], updated: [], removedIds: [] },
    });

    const map = window.Spillgebees.Map.maps.get(mapElement)!;
    const storage = window.Spillgebees.Map.features.get(map)!;
    const entry = storage.markers.get("m1")!;
    (entry.marker.getLngLat as ReturnType<typeof import("vitest").vi.fn>).mockReturnValue({
      lng: 2.0,
      lat: 48.0,
    });

    const options: IFitBoundsOptions = {
      featureIds: ["m1"],
      padding: null,
      topLeftPadding: { x: 10, y: 20 },
      bottomRightPadding: { x: 30, y: 40 },
    };

    // act
    fitBounds(mapElement, options);

    // assert
    expect(mockMap.fitBounds).toHaveBeenCalledWith(
      [
        [2.0, 48.0],
        [2.0, 48.0],
      ],
      { padding: { top: 20, left: 10, bottom: 40, right: 30 } },
    );
  });
});

describe("flyTo", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should call map.flyTo with correct coordinate swap", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    // act
    flyTo(mapElement, { latitude: 48.8566, longitude: 2.3522 }, null, null, null);

    // assert — center should be [lng, lat]
    expect(mockMap.flyTo).toHaveBeenCalledWith({
      center: [2.3522, 48.8566],
    });
  });

  it("should pass optional zoom, bearing, and pitch", () => {
    // arrange
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
    const mockMap = getLatestMockMapInstance()!;

    // act
    flyTo(mapElement, { latitude: 48.8566, longitude: 2.3522 }, 15, 90, 45);

    // assert
    expect(mockMap.flyTo).toHaveBeenCalledWith({
      center: [2.3522, 48.8566],
      zoom: 15,
      bearing: 90,
      pitch: 45,
    });
  });

  it("should be a no-op for unknown elements", () => {
    // arrange
    const unknownElement = document.createElement("div");

    // act & assert
    expect(() => flyTo(unknownElement, { latitude: 0, longitude: 0 }, null, null, null)).not.toThrow();
  });
});

// --- Phase 8: Map events ---

describe("map events", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should call dotNetHelper on map click with correct coordinate swap", () => {
    // arrange
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

    // act — simulate a map click event
    fireMapEvent("click", { lngLat: { lat: 48.8566, lng: 2.3522 } });

    // assert — should convert lng/lat back to latitude/longitude
    // biome-ignore lint/security/noSecrets: C# callback method name, not a secret
    expect(dotNetHelper.invokeMethodAsync).toHaveBeenCalledWith("OnMapClickCallbackAsync", {
      position: { latitude: 48.8566, longitude: 2.3522 },
    });
  });

  it("should call dotNetHelper on move end with map state", () => {
    // arrange
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

    const mockMap = getLatestMockMapInstance()!;
    mockMap.getCenter.mockReturnValue({ lng: 2.3522, lat: 48.8566 });
    mockMap.getZoom.mockReturnValue(12);
    mockMap.getBearing.mockReturnValue(45);
    mockMap.getPitch.mockReturnValue(30);

    // act
    fireMapEvent("moveend");

    // assert
    // biome-ignore lint/security/noSecrets: C# callback method name, not a secret
    expect(dotNetHelper.invokeMethodAsync).toHaveBeenCalledWith("OnMoveEndCallbackAsync", {
      center: { latitude: 48.8566, longitude: 2.3522 },
      zoom: 12,
      bearing: 45,
      pitch: 30,
    });
  });

  it("should call dotNetHelper on zoom end with map state", () => {
    // arrange
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

    const mockMap = getLatestMockMapInstance()!;
    mockMap.getCenter.mockReturnValue({ lng: -0.09, lat: 51.505 });
    mockMap.getZoom.mockReturnValue(15);
    mockMap.getBearing.mockReturnValue(0);
    mockMap.getPitch.mockReturnValue(0);

    // act
    fireMapEvent("zoomend");

    // assert
    // biome-ignore lint/security/noSecrets: C# callback method name, not a secret
    expect(dotNetHelper.invokeMethodAsync).toHaveBeenCalledWith("OnZoomEndCallbackAsync", {
      center: { latitude: 51.505, longitude: -0.09 },
      zoom: 15,
      bearing: 0,
      pitch: 0,
    });
  });

  it("should call dotNetHelper on marker click with marker ID and position", () => {
    // arrange
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

    // Add a marker
    const marker = createDefaultMarker({ id: "m1", position: { latitude: 48.0, longitude: 2.0 } });
    syncFeatures(mapElement, {
      markers: { added: [marker], updated: [], removedIds: [] },
      circles: { added: [], updated: [], removedIds: [] },
      polylines: { added: [], updated: [], removedIds: [] },
    });

    // Get the marker element and mock getLngLat
    const map = window.Spillgebees.Map.maps.get(mapElement)!;
    const storage = window.Spillgebees.Map.features.get(map)!;
    const markerEntry = storage.markers.get("m1")!;
    (markerEntry.marker.getLngLat as ReturnType<typeof import("vitest").vi.fn>).mockReturnValue({
      lng: 2.0,
      lat: 48.0,
    });

    // act — simulate click on the marker element
    const markerElement = markerEntry.marker.getElement();
    const clickEvent = new Event("click", { bubbles: true });
    markerElement.dispatchEvent(clickEvent);

    // assert
    expect(dotNetHelper.invokeMethodAsync).toHaveBeenCalledWith("OnMarkerClickCallbackAsync", {
      markerId: "m1",
      position: { latitude: 48.0, longitude: 2.0 },
    });
  });

  it("should call dotNetHelper on marker drag end with new position", () => {
    // arrange
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

    // Add a draggable marker
    const marker = createDefaultMarker({
      id: "drag-marker",
      position: { latitude: 48.0, longitude: 2.0 },
      draggable: true,
    });
    syncFeatures(mapElement, {
      markers: { added: [marker], updated: [], removedIds: [] },
      circles: { added: [], updated: [], removedIds: [] },
      polylines: { added: [], updated: [], removedIds: [] },
    });

    // Get the marker instance from the mock constructor and mock getLngLat on it
    const markerConstructor = getMockMarkerConstructor();
    const mockMarkerInstance = markerConstructor.mock.results[markerConstructor.mock.results.length - 1]?.value;
    mockMarkerInstance.getLngLat.mockReturnValue({ lng: 3.0, lat: 49.0 });

    // Find the "dragend" callback registered via marker.on("dragend", ...)
    const onCalls = mockMarkerInstance.on.mock.calls;
    const dragEndCall = onCalls.find((call: unknown[]) => call[0] === "dragend");
    expect(dragEndCall).toBeDefined();

    // act — invoke the dragend callback
    dragEndCall![1]();

    // assert
    // biome-ignore lint/security/noSecrets: C# callback method name, not a secret
    expect(dotNetHelper.invokeMethodAsync).toHaveBeenCalledWith("OnMarkerDragEndCallbackAsync", {
      markerId: "drag-marker",
      position: { latitude: 49.0, longitude: 3.0 },
    });
  });

  it("should store and clean up dotNetHelpers on dispose", () => {
    // arrange
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

    const map = window.Spillgebees.Map.maps.get(mapElement)!;
    expect(window.Spillgebees.Map.dotNetHelpers.get(map)).toBe(dotNetHelper);

    // act
    disposeMap(mapElement);

    // assert — dotNetHelper should be cleaned up
    expect(window.Spillgebees.Map.dotNetHelpers.size).toBe(0);
  });
});
