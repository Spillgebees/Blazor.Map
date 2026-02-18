import { Control, type Map as LeafletMap } from "leaflet";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMockDotNetHelper } from "../test/dotNetHelperMock";
import type { MockMap, MockMarker, MockTileLayer } from "../test/leafletMock";
import { resetWindowGlobals } from "../test/windowSetup";

vi.mock("leaflet", async () => {
  const { createLeafletMock } = await import("../test/leafletMock");
  return createLeafletMock();
});

import { CenterControl } from "./controls";
import type {
  ISpillgebeesCircleMarker,
  ISpillgebeesMapControlOptions,
  ISpillgebeesMapOptions,
  ISpillgebeesMarker,
  ISpillgebeesPolyline,
  ISpillgebeesTileLayer,
} from "./interfaces/map";
import { MapTheme } from "./interfaces/map";
import { bootstrap } from "./map";

describe("bootstrap", () => {
  beforeEach(() => {
    resetWindowGlobals();
  });

  it("should create window.Spillgebees.Map namespace", () => {
    // act
    bootstrap();

    // assert
    expect(window.Spillgebees).toBeDefined();
    expect(window.Spillgebees.Map).toBeDefined();
  });

  it("should register all 8 mapFunctions", () => {
    // act
    bootstrap();

    // assert
    const fns = window.Spillgebees.Map.mapFunctions;
    expect(fns.createMap).toBeTypeOf("function");
    expect(fns.setLayers).toBeTypeOf("function");
    expect(fns.setTileLayers).toBeTypeOf("function");
    expect(fns.setMapControls).toBeTypeOf("function");
    expect(fns.setMapOptions).toBeTypeOf("function");
    expect(fns.invalidateSize).toBeTypeOf("function");
    expect(fns.fitBounds).toBeTypeOf("function");
    expect(fns.disposeMap).toBeTypeOf("function");
  });

  it("should create empty Maps for state stores", () => {
    // act
    bootstrap();

    // assert
    expect(window.Spillgebees.Map.maps).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.maps.size).toBe(0);
    expect(window.Spillgebees.Map.layers).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.layers.size).toBe(0);
    expect(window.Spillgebees.Map.tileLayers).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.tileLayers.size).toBe(0);
    expect(window.Spillgebees.Map.controls).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.controls.size).toBe(0);
  });

  it("should NOT overwrite if already initialized", () => {
    // arrange
    bootstrap();
    const originalMaps = window.Spillgebees.Map.maps;
    const originalFunctions = window.Spillgebees.Map.mapFunctions;

    // act
    bootstrap();

    // assert
    expect(window.Spillgebees.Map.maps).toBe(originalMaps);
    expect(window.Spillgebees.Map.mapFunctions).toBe(originalFunctions);
  });
});

describe("mapFunctions", () => {
  let mapContainer: HTMLElement;

  const defaultTileLayers: ISpillgebeesTileLayer[] = [
    {
      urlTemplate: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
      attribution: "&copy; OSM",
      tileSize: undefined,
    },
  ];

  const defaultMapOptions: ISpillgebeesMapOptions = {
    center: { latitude: 49.6, longitude: 6.1 },
    zoom: 13,
    showLeafletPrefix: false,
    theme: MapTheme.Default,
  };

  const defaultControlOptions: ISpillgebeesMapControlOptions = {
    zoomControlOptions: {
      enable: false,
      position: "topleft",
      showZoomInButton: true,
      showZoomOutButton: true,
    },
    scaleControlOptions: {
      enable: false,
      position: "bottomleft",
    },
    centerControlOptions: {
      enable: false,
      position: "topleft",
      center: { latitude: 49.6, longitude: 6.1 },
      zoom: 13,
    },
  };

  beforeEach(() => {
    resetWindowGlobals();
    bootstrap();
    mapContainer = document.createElement("div");
  });

  describe("createMap", () => {
    it("should create map, apply tile layers, store in globals, and call dotNetHelper", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();

      // act
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      // assert
      expect(window.Spillgebees.Map.maps.size).toBe(1);
      expect(window.Spillgebees.Map.maps.has(mapContainer)).toBe(true);
      expect(dotNetHelper.invokeMethodAsync).toHaveBeenCalledWith("OnMapReady");
    });

    it("should apply dark theme class when theme is Dark", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      const darkOptions: ISpillgebeesMapOptions = {
        ...defaultMapOptions,
        theme: MapTheme.Dark,
      };

      // act
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        darkOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      // assert
      expect(mapContainer.classList.contains("sgb-map-dark")).toBe(true);
    });

    it("should store tile layers in global tileLayers store", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();

      // act
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer);
      expect(map).toBeDefined();
      const storedTileLayers = window.Spillgebees.Map.tileLayers.get(map!);
      expect(storedTileLayers).toBeDefined();
      expect(storedTileLayers!.size).toBe(1);
    });

    it("should forward tileSize to Leaflet TileLayer options", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      const tileLayers: ISpillgebeesTileLayer[] = [
        {
          urlTemplate: "https://{s}.tile.example.com/{z}/{x}/{y}.png",
          attribution: "&copy; Test",
          tileSize: 512,
        },
      ];

      // act
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        tileLayers,
        [],
        [],
        [],
      );

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const storedTileLayers = window.Spillgebees.Map.tileLayers.get(map)!;
      const tileLayer = storedTileLayers.values().next().value as unknown as MockTileLayer;
      expect(tileLayer._options.tileSize).toBe(512);
    });
  });

  describe("setLayers", () => {
    it("should create markers and store them in layer storage", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const markers: ISpillgebeesMarker[] = [
        {
          id: "marker-1",
          coordinate: { latitude: 49.6, longitude: 6.1 },
          title: "Test Marker",
          icon: undefined,
          stroke: undefined,
          strokeColor: undefined,
          strokeWeight: undefined,
          strokeOpacity: undefined,
          fill: undefined,
          fillColor: undefined,
          fillOpacity: undefined,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.setLayers(mapContainer, markers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      expect(layerStorage).toBeDefined();
      expect(layerStorage!.byId.has("marker-1")).toBe(true);
    });

    it("should create polylines and store them in layer storage", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const polylines: ISpillgebeesPolyline[] = [
        {
          id: "polyline-1",
          coordinates: [
            { latitude: 49.6, longitude: 6.1 },
            { latitude: 50.0, longitude: 6.5 },
          ],
          smoothFactor: 1.0,
          noClip: false,
          stroke: true,
          strokeColor: "#ff0000",
          strokeWeight: 3,
          strokeOpacity: 1,
          fill: false,
          fillColor: undefined,
          fillOpacity: undefined,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.setLayers(mapContainer, [], [], polylines);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      expect(layerStorage).toBeDefined();
      expect(layerStorage!.byId.has("polyline-1")).toBe(true);
    });

    it("should create circleMarkers and store them in layer storage", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const circleMarkers: ISpillgebeesCircleMarker[] = [
        {
          id: "cm-1",
          coordinate: { latitude: 49.6, longitude: 6.1 },
          radius: 15,
          stroke: true,
          strokeColor: "#00ff00",
          strokeWeight: 2,
          strokeOpacity: 1,
          fill: true,
          fillColor: "#00ff00",
          fillOpacity: 0.5,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.setLayers(mapContainer, [], circleMarkers, []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      expect(layerStorage).toBeDefined();
      expect(layerStorage!.byId.has("cm-1")).toBe(true);
    });

    it("should bind tooltip when marker has tooltip", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const markers: ISpillgebeesMarker[] = [
        {
          id: "marker-tooltip",
          coordinate: { latitude: 49.6, longitude: 6.1 },
          title: "Tooltip Marker",
          icon: undefined,
          stroke: undefined,
          strokeColor: undefined,
          strokeWeight: undefined,
          strokeOpacity: undefined,
          fill: undefined,
          fillColor: undefined,
          fillOpacity: undefined,
          tooltip: {
            content: "Hello tooltip",
          },
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.setLayers(mapContainer, markers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      const layerTuple = layerStorage!.byId.get("marker-tooltip");
      expect(layerTuple).toBeDefined();
      expect((layerTuple!.leaflet as unknown as MockMarker).bindTooltip).toHaveBeenCalled();
    });

    it("should handle empty arrays gracefully", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      // act
      window.Spillgebees.Map.mapFunctions.setLayers(mapContainer, [], [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      expect(layerStorage).toBeDefined();
      expect(layerStorage!.byId.size).toBe(0);
    });

    it("should early-return if map container is not found", () => {
      // arrange
      const unknownContainer = document.createElement("div");

      // act & assert — should not throw
      window.Spillgebees.Map.mapFunctions.setLayers(unknownContainer, [], [], []);
    });
  });

  describe("setTileLayers", () => {
    it("should remove old tile layers and add new ones", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      vi.clearAllMocks();

      const newTileLayers: ISpillgebeesTileLayer[] = [
        {
          urlTemplate: "https://new-tiles/{z}/{x}/{y}.png",
          attribution: "&copy; New",
          tileSize: 256,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.setTileLayers(mapContainer, newTileLayers);

      // assert
      // removeLayer should have been called for each old tile layer
      expect(map.removeLayer).toHaveBeenCalled();
      // addLayer should have been called for the new tile layer
      expect(map.addLayer).toHaveBeenCalled();
      // Global store should be updated
      const storedTileLayers = window.Spillgebees.Map.tileLayers.get(map as unknown as LeafletMap);
      expect(storedTileLayers).toBeDefined();
      expect(storedTileLayers!.size).toBe(1);
    });

    it("should early-return if map container is not found", () => {
      // arrange
      const unknownContainer = document.createElement("div");

      // act & assert — should not throw
      window.Spillgebees.Map.mapFunctions.setTileLayers(unknownContainer, []);
    });
  });

  describe("setMapControls", () => {
    it("should add zoom control when enabled", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      vi.clearAllMocks();

      const controlOptions: ISpillgebeesMapControlOptions = {
        ...defaultControlOptions,
        zoomControlOptions: {
          enable: true,
          position: "topright",
          showZoomInButton: true,
          showZoomOutButton: true,
        },
      };

      // act
      window.Spillgebees.Map.mapFunctions.setMapControls(mapContainer, controlOptions);

      // assert
      expect(map.addControl).toHaveBeenCalled();
      const storedControls = window.Spillgebees.Map.controls.get(map as unknown as LeafletMap);
      expect(storedControls).toBeDefined();
      expect(storedControls!.size).toBeGreaterThanOrEqual(1);
      expect(storedControls!.values().next().value).toBeInstanceOf(Control.Zoom);
    });

    it("should add scale control when enabled", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      vi.clearAllMocks();

      const controlOptions: ISpillgebeesMapControlOptions = {
        ...defaultControlOptions,
        scaleControlOptions: {
          enable: true,
          position: "bottomleft",
          showMetric: true,
          showImperial: false,
        },
      };

      // act
      window.Spillgebees.Map.mapFunctions.setMapControls(mapContainer, controlOptions);

      // assert
      expect(map.addControl).toHaveBeenCalled();
      const storedControls = window.Spillgebees.Map.controls.get(map as unknown as LeafletMap);
      expect(storedControls).toBeDefined();
      expect(storedControls!.size).toBeGreaterThanOrEqual(1);
      expect(storedControls!.values().next().value).toBeInstanceOf(Control.Scale);
    });

    it("should add center control when enabled", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      vi.clearAllMocks();

      const controlOptions: ISpillgebeesMapControlOptions = {
        ...defaultControlOptions,
        centerControlOptions: {
          enable: true,
          position: "topleft",
          center: { latitude: 49.6, longitude: 6.1 },
          zoom: 13,
        },
      };

      // act
      window.Spillgebees.Map.mapFunctions.setMapControls(mapContainer, controlOptions);

      // assert
      expect(map.addControl).toHaveBeenCalled();
      const storedControls = window.Spillgebees.Map.controls.get(map as unknown as LeafletMap);
      expect(storedControls).toBeDefined();
      expect(storedControls!.size).toBeGreaterThanOrEqual(1);

      expect(storedControls!.values().next().value).toBeInstanceOf(CenterControl);
    });

    it("should remove existing controls before adding new ones", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      // First, add controls
      const controlOptions: ISpillgebeesMapControlOptions = {
        ...defaultControlOptions,
        zoomControlOptions: {
          enable: true,
          position: "topright",
          showZoomInButton: true,
          showZoomOutButton: true,
        },
      };
      window.Spillgebees.Map.mapFunctions.setMapControls(mapContainer, controlOptions);

      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      vi.clearAllMocks();

      // act — set controls again, which should remove old ones first
      window.Spillgebees.Map.mapFunctions.setMapControls(mapContainer, controlOptions);

      // assert
      expect(map.removeControl).toHaveBeenCalled();
    });

    it("should early-return if map container is not found", () => {
      // arrange
      const unknownContainer = document.createElement("div");

      // act & assert — should not throw
      window.Spillgebees.Map.mapFunctions.setMapControls(unknownContainer, defaultControlOptions);
    });
  });

  describe("setMapOptions", () => {
    it("should add dark theme class when theme is Dark", () => {
      // act
      window.Spillgebees.Map.mapFunctions.setMapOptions(mapContainer, {
        ...defaultMapOptions,
        theme: MapTheme.Dark,
      });

      // assert
      expect(mapContainer.classList.contains("sgb-map-dark")).toBe(true);
    });

    it("should remove dark theme class when theme is Default", () => {
      // arrange
      mapContainer.classList.add("sgb-map-dark");

      // act
      window.Spillgebees.Map.mapFunctions.setMapOptions(mapContainer, {
        ...defaultMapOptions,
        theme: MapTheme.Default,
      });

      // assert
      expect(mapContainer.classList.contains("sgb-map-dark")).toBe(false);
    });
  });

  describe("invalidateSize", () => {
    it("should call map.invalidateSize()", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      vi.clearAllMocks();

      // act
      window.Spillgebees.Map.mapFunctions.invalidateSize(mapContainer);

      // assert
      expect(map.invalidateSize).toHaveBeenCalledOnce();
    });

    it("should early-return if map container is not found", () => {
      // arrange
      const unknownContainer = document.createElement("div");

      // act & assert — should not throw
      window.Spillgebees.Map.mapFunctions.invalidateSize(unknownContainer);
    });
  });

  describe("disposeMap", () => {
    it("should clean up all layers, tile layers, and remove from stores", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      // Add some layers
      const markers: ISpillgebeesMarker[] = [
        {
          id: "marker-disp",
          coordinate: { latitude: 49.6, longitude: 6.1 },
          title: "Disposable",
          icon: undefined,
          stroke: undefined,
          strokeColor: undefined,
          strokeWeight: undefined,
          strokeOpacity: undefined,
          fill: undefined,
          fillColor: undefined,
          fillOpacity: undefined,
        },
      ];
      window.Spillgebees.Map.mapFunctions.setLayers(mapContainer, markers, [], []);

      // act
      window.Spillgebees.Map.mapFunctions.disposeMap(mapContainer);

      // assert
      expect(window.Spillgebees.Map.maps.has(mapContainer)).toBe(false);
    });

    it("should remove controls, call map.remove(), and clean up controls store", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      await window.Spillgebees.Map.mapFunctions.createMap(
        dotNetHelper,
        "OnMapReady",
        mapContainer,
        defaultMapOptions,
        defaultControlOptions,
        defaultTileLayers,
        [],
        [],
        [],
      );

      const controlOptions: ISpillgebeesMapControlOptions = {
        ...defaultControlOptions,
        zoomControlOptions: {
          enable: true,
          position: "topright",
          showZoomInButton: true,
          showZoomOutButton: true,
        },
      };
      window.Spillgebees.Map.mapFunctions.setMapControls(mapContainer, controlOptions);

      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      vi.clearAllMocks();

      // act
      window.Spillgebees.Map.mapFunctions.disposeMap(mapContainer);

      // assert
      expect(map.removeControl).toHaveBeenCalled();
      expect(map.remove).toHaveBeenCalledOnce();
      expect(window.Spillgebees.Map.controls.has(map as unknown as LeafletMap)).toBe(false);
      expect(window.Spillgebees.Map.maps.has(mapContainer)).toBe(false);
    });

    it("should early-return if map container is not found", () => {
      // arrange
      const unknownContainer = document.createElement("div");

      // act & assert — should not throw
      window.Spillgebees.Map.mapFunctions.disposeMap(unknownContainer);
    });
  });
});
