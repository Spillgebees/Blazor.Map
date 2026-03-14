import { Control, type Map as LeafletMap } from "leaflet";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMockDotNetHelper } from "../test/dotNetHelperMock";
import type { MockCircleMarker, MockIcon, MockMap, MockMarker, MockPolyline, MockTileLayer } from "../test/leafletMock";
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

  it("should register all 10 mapFunctions", () => {
    // act
    bootstrap();

    // assert
    const fns = window.Spillgebees.Map.mapFunctions;
    expect(fns.createMap).toBeTypeOf("function");
    expect(fns.addLayers).toBeTypeOf("function");
    expect(fns.updateLayers).toBeTypeOf("function");
    expect(fns.removeLayers).toBeTypeOf("function");
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
      detectRetina: null,
      tileSize: null,
    },
  ];

  const defaultMapOptions: ISpillgebeesMapOptions = {
    center: { latitude: 49.6, longitude: 6.1 },
    zoom: 13,
    showLeafletPrefix: false,
    fitBoundsOptions: null,
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
      showMetric: null,
      showImperial: null,
    },
    centerControlOptions: {
      enable: false,
      position: "topleft",
      center: { latitude: 49.6, longitude: 6.1 },
      zoom: 13,
      fitBoundsOptions: null,
    },
  };

  const createMarker = (overrides: Partial<ISpillgebeesMarker> = {}): ISpillgebeesMarker => ({
    id: "marker-1",
    coordinate: { latitude: 49.6, longitude: 6.1 },
    title: null,
    icon: null,
    rotationAngle: null,
    rotationOrigin: null,
    zIndexOffset: null,
    riseOnHover: null,
    riseOffset: null,
    stroke: true,
    strokeColor: null,
    strokeWeight: null,
    strokeOpacity: null,
    fill: false,
    fillColor: null,
    fillOpacity: null,
    tooltip: null,
    ...overrides,
  });

  const createCircleMarker = (overrides: Partial<ISpillgebeesCircleMarker> = {}): ISpillgebeesCircleMarker => ({
    id: "cm-1",
    coordinate: { latitude: 49.6, longitude: 6.1 },
    radius: 10,
    stroke: true,
    strokeColor: null,
    strokeWeight: null,
    strokeOpacity: null,
    fill: false,
    fillColor: null,
    fillOpacity: null,
    tooltip: null,
    ...overrides,
  });

  const createPolyline = (overrides: Partial<ISpillgebeesPolyline> = {}): ISpillgebeesPolyline => ({
    id: "polyline-1",
    coordinates: [
      { latitude: 49.6, longitude: 6.1 },
      { latitude: 50.0, longitude: 6.5 },
    ],
    smoothFactor: null,
    noClip: false,
    stroke: true,
    strokeColor: null,
    strokeWeight: null,
    strokeOpacity: null,
    fill: false,
    fillColor: null,
    fillOpacity: null,
    tooltip: null,
    ...overrides,
  });

  beforeEach(() => {
    resetWindowGlobals();
    bootstrap();
    mapContainer = document.createElement("div");
  });

  const createMapWithLayers = async () => {
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
  };

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
          detectRetina: null,
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

    it("should not pass tileSize to Leaflet when C# sends null", async () => {
      // arrange
      const dotNetHelper = createMockDotNetHelper();
      const tileLayers: ISpillgebeesTileLayer[] = [
        {
          urlTemplate: "https://{s}.tile.example.com/{z}/{x}/{y}.png",
          attribution: "&copy; Test",
          detectRetina: null,
          tileSize: null,
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

      // assert — null tileSize should be omitted from options entirely,
      // letting Leaflet use its own default (256)
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const tileLayerSet = window.Spillgebees.Map.tileLayers.get(map)!;
      const tileLayer = [...tileLayerSet][0] as unknown as { _options: Record<string, unknown> };
      expect(tileLayer._options).not.toHaveProperty("tileSize");
      expect(tileLayer._options).not.toHaveProperty("detectRetina");
    });
  });

  describe("addLayers", () => {
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

      const markers: ISpillgebeesMarker[] = [createMarker({ title: "Test Marker" })];

      // act
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

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
        createPolyline({
          smoothFactor: 1.0,
          strokeColor: "#ff0000",
          strokeWeight: 3,
          strokeOpacity: 1,
        }),
      ];

      // act
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], [], polylines);

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
        createCircleMarker({
          radius: 15,
          strokeColor: "#00ff00",
          strokeWeight: 2,
          strokeOpacity: 1,
          fill: true,
          fillColor: "#00ff00",
          fillOpacity: 0.5,
        }),
      ];

      // act
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], circleMarkers, []);

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
        createMarker({
          id: "marker-tooltip",
          title: "Tooltip Marker",
          tooltip: {
            content: "Hello tooltip",
            offset: null,
            direction: null,
            permanent: false,
            sticky: false,
            interactive: false,
            opacity: null,
            className: null,
          },
        }),
      ];

      // act
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

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
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], [], []);

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
      window.Spillgebees.Map.mapFunctions.addLayers(unknownContainer, [], [], []);
    });

    it("should pass custom icon to Leaflet marker when icon is provided", async () => {
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
        createMarker({
          id: "marker-icon",
          icon: {
            iconUrl: "https://example.com/icon.png",
            iconSize: [25, 41],
            iconAnchor: [12, 41],
            popupAnchor: [1, -34],
            tooltipAnchor: null,
            shadowUrl: null,
            shadowSize: null,
            shadowAnchor: null,
            className: null,
          },
        }),
      ];

      // act
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      const layerTuple = layerStorage!.byId.get("marker-icon");
      expect(layerTuple).toBeDefined();

      const mockMarker = layerTuple!.leaflet as unknown as MockMarker;
      const iconOption = mockMarker._options.icon as MockIcon | undefined;
      expect(iconOption).toBeDefined();
      expect(iconOption!._options.iconUrl).toBe("https://example.com/icon.png");
      expect(iconOption!._options.iconSize).toEqual([25, 41]);
      expect(iconOption!._options.iconAnchor).toEqual([12, 41]);
      expect(iconOption!._options.popupAnchor).toEqual([1, -34]);
      expect(iconOption!._options).not.toHaveProperty("tooltipAnchor");
      expect(iconOption!._options).not.toHaveProperty("shadowUrl");
    });

    it("should pass rotation options to Leaflet marker when provided", async () => {
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
        createMarker({
          id: "marker-rotated",
          title: "Rotated",
          rotationAngle: 45,
          rotationOrigin: "bottom center",
        }),
      ];

      // act
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      const layerTuple = layerStorage!.byId.get("marker-rotated");
      expect(layerTuple).toBeDefined();

      const mockMarker = layerTuple!.leaflet as unknown as MockMarker;
      expect(mockMarker._options.rotationAngle).toBe(45);
      expect(mockMarker._options.rotationOrigin).toBe("bottom center");
    });

    it("should not pass icon or rotation options when they are null", async () => {
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

      const markers: ISpillgebeesMarker[] = [createMarker({ id: "marker-default", title: "Default" })];

      // act
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      const layerTuple = layerStorage!.byId.get("marker-default");
      expect(layerTuple).toBeDefined();

      const mockMarker = layerTuple!.leaflet as unknown as MockMarker;
      expect(mockMarker._options).not.toHaveProperty("icon");
      expect(mockMarker._options).not.toHaveProperty("rotationAngle");
      expect(mockMarker._options).not.toHaveProperty("rotationOrigin");
      expect(mockMarker._options).not.toHaveProperty("zIndexOffset");
      expect(mockMarker._options).not.toHaveProperty("riseOnHover");
      expect(mockMarker._options).not.toHaveProperty("riseOffset");
    });

    it("should pass zIndexOffset and riseOnHover options when specified", async () => {
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
        createMarker({
          id: "marker-rise",
          zIndexOffset: 100,
          riseOnHover: true,
          riseOffset: 500,
        }),
      ];

      // act
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      const layerTuple = layerStorage!.byId.get("marker-rise");
      expect(layerTuple).toBeDefined();

      const mockMarker = layerTuple!.leaflet as unknown as MockMarker;
      expect(mockMarker._options).toHaveProperty("zIndexOffset", 100);
      expect(mockMarker._options).toHaveProperty("riseOnHover", true);
      expect(mockMarker._options).toHaveProperty("riseOffset", 500);
    });

    it("should pass icon with all options when fully specified", async () => {
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
        createMarker({
          id: "marker-full-icon",
          icon: {
            iconUrl: "https://example.com/icon.png",
            iconSize: [25, 41],
            iconAnchor: [12, 41],
            popupAnchor: [1, -34],
            tooltipAnchor: [16, -28],
            shadowUrl: "https://example.com/shadow.png",
            shadowSize: [41, 41],
            shadowAnchor: [12, 41],
            className: "custom-icon",
          },
          rotationAngle: 90,
          rotationOrigin: "center center",
        }),
      ];

      // act
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map);
      const layerTuple = layerStorage!.byId.get("marker-full-icon");
      expect(layerTuple).toBeDefined();

      const mockMarker = layerTuple!.leaflet as unknown as MockMarker;
      const iconOption = mockMarker._options.icon as MockIcon | undefined;
      expect(iconOption).toBeDefined();
      expect(iconOption!._options.iconUrl).toBe("https://example.com/icon.png");
      expect(iconOption!._options.iconSize).toEqual([25, 41]);
      expect(iconOption!._options.iconAnchor).toEqual([12, 41]);
      expect(iconOption!._options.popupAnchor).toEqual([1, -34]);
      expect(iconOption!._options.tooltipAnchor).toEqual([16, -28]);
      expect(iconOption!._options.shadowUrl).toBe("https://example.com/shadow.png");
      expect(iconOption!._options.shadowSize).toEqual([41, 41]);
      expect(iconOption!._options.shadowAnchor).toEqual([12, 41]);
      expect(iconOption!._options.className).toBe("custom-icon");
      expect(mockMarker._options.rotationAngle).toBe(90);
      expect(mockMarker._options.rotationOrigin).toBe("center center");
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
          detectRetina: null,
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
          fitBoundsOptions: null,
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
      const markers: ISpillgebeesMarker[] = [createMarker({ id: "marker-disp", title: "Disposable" })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

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

  describe("updateLayers", () => {
    it("should update marker position via setLatLng", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [createMarker({ title: "Test" })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          coordinate: { latitude: 50.0, longitude: 7.0 },
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-1")!;
      const mockMarker = layerTuple.leaflet as unknown as MockMarker;
      expect(mockMarker.setLatLng).toHaveBeenCalled();
    });

    it("should update marker rotation via setRotationAngle", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [createMarker({ id: "marker-rot", title: "Rotated", rotationAngle: 45 })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          rotationAngle: 90,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-rot")!;
      const mockMarker = layerTuple.leaflet as unknown as MockMarker;
      expect(mockMarker.setRotationAngle).toHaveBeenCalledWith(90);
    });

    it("should clear rotation when rotationAngle is null", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [createMarker({ id: "marker-clear-rot", rotationAngle: 45 })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          rotationAngle: null,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-clear-rot")!;
      const mockMarker = layerTuple.leaflet as unknown as MockMarker;
      expect(mockMarker.setRotationAngle).toHaveBeenCalledWith(0);
    });

    it("should update marker icon when icon is provided", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [
        createMarker({
          id: "marker-icon-update",
          icon: {
            iconUrl: "https://example.com/icon.png",
            iconSize: [25, 41],
            iconAnchor: null,
            popupAnchor: null,
            tooltipAnchor: null,
            shadowUrl: null,
            shadowSize: null,
            shadowAnchor: null,
            className: null,
          },
        }),
      ];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          icon: {
            iconUrl: "https://example.com/new-icon.png",
            iconSize: [32, 32],
            iconAnchor: null,
            popupAnchor: null,
            tooltipAnchor: null,
            shadowUrl: null,
            shadowSize: null,
            shadowAnchor: null,
            className: null,
          },
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-icon-update")!;
      const mockMarker = layerTuple.leaflet as unknown as MockMarker;
      expect(mockMarker.setIcon).toHaveBeenCalled();
    });

    it("should update circleMarker position and radius", async () => {
      // arrange
      await createMapWithLayers();
      const circleMarkers: ISpillgebeesCircleMarker[] = [
        createCircleMarker({
          radius: 15,
          strokeColor: "#00ff00",
          strokeWeight: 2,
          strokeOpacity: 1,
          fill: true,
          fillColor: "#00ff00",
          fillOpacity: 0.5,
        }),
      ];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], circleMarkers, []);

      const updatedCircleMarkers: ISpillgebeesCircleMarker[] = [
        {
          ...circleMarkers[0],
          coordinate: { latitude: 50.0, longitude: 7.0 },
          radius: 25,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, [], updatedCircleMarkers, []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("cm-1")!;
      const mockCm = layerTuple.leaflet as unknown as MockCircleMarker;
      expect(mockCm.setLatLng).toHaveBeenCalled();
      expect(mockCm.setRadius).toHaveBeenCalledWith(25);
    });

    it("should update path styles on circleMarkers", async () => {
      // arrange
      await createMapWithLayers();
      const circleMarkers: ISpillgebeesCircleMarker[] = [
        createCircleMarker({
          id: "cm-style",
          strokeColor: "#ff0000",
          strokeWeight: 2,
          strokeOpacity: 1,
          fill: true,
          fillColor: "#ff0000",
          fillOpacity: 0.5,
        }),
      ];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], circleMarkers, []);

      const updatedCircleMarkers: ISpillgebeesCircleMarker[] = [
        {
          ...circleMarkers[0],
          strokeColor: "#0000ff",
          fillColor: "#0000ff",
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, [], updatedCircleMarkers, []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("cm-style")!;
      const mockCm = layerTuple.leaflet as unknown as MockCircleMarker;
      expect(mockCm.setStyle).toHaveBeenCalled();
    });

    it("should update polyline coordinates", async () => {
      // arrange
      await createMapWithLayers();
      const polylines: ISpillgebeesPolyline[] = [
        createPolyline({
          id: "poly-1",
          strokeColor: "#ff0000",
          strokeWeight: 3,
          strokeOpacity: 1,
        }),
      ];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], [], polylines);

      const updatedPolylines: ISpillgebeesPolyline[] = [
        {
          ...polylines[0],
          coordinates: [
            { latitude: 48.0, longitude: 5.0 },
            { latitude: 51.0, longitude: 7.0 },
            { latitude: 52.0, longitude: 8.0 },
          ],
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, [], [], updatedPolylines);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("poly-1")!;
      const mockPoly = layerTuple.leaflet as unknown as MockPolyline;
      expect(mockPoly.setLatLngs).toHaveBeenCalled();
    });

    it("should update path styles on polylines", async () => {
      // arrange
      await createMapWithLayers();
      const polylines: ISpillgebeesPolyline[] = [
        createPolyline({
          id: "poly-style",
          strokeColor: "#ff0000",
          strokeWeight: 3,
          strokeOpacity: 1,
        }),
      ];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], [], polylines);

      const updatedPolylines: ISpillgebeesPolyline[] = [
        {
          ...polylines[0],
          strokeColor: "#0000ff",
          strokeWeight: 5,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, [], [], updatedPolylines);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("poly-style")!;
      const mockPoly = layerTuple.leaflet as unknown as MockPolyline;
      expect(mockPoly.setStyle).toHaveBeenCalled();
    });

    it("should silently skip unknown IDs", async () => {
      // arrange
      await createMapWithLayers();
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], [], []);

      const unknownMarkers: ISpillgebeesMarker[] = [createMarker({ id: "nonexistent" })];

      // act & assert — should not throw
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, unknownMarkers, [], []);
    });

    it("should update tooltip when tooltip changes", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [
        createMarker({
          id: "marker-tooltip-update",
          tooltip: {
            content: "Original",
            offset: null,
            direction: null,
            permanent: false,
            sticky: false,
            interactive: false,
            opacity: null,
            className: null,
          },
        }),
      ];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          tooltip: {
            content: "Updated",
            offset: null,
            direction: null,
            permanent: false,
            sticky: false,
            interactive: false,
            opacity: null,
            className: null,
          },
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-tooltip-update")!;
      const mockMarker = layerTuple.leaflet as unknown as MockMarker;
      expect(mockMarker.unbindTooltip).toHaveBeenCalled();
      expect(mockMarker.bindTooltip).toHaveBeenCalledTimes(2); // once in addLayers, once in updateLayers
    });

    it("should early-return if map container is not found", () => {
      // arrange
      const unknownContainer = document.createElement("div");

      // act & assert — should not throw
      window.Spillgebees.Map.mapFunctions.updateLayers(unknownContainer, [], [], []);
    });

    it("should update the stored model in layerStorage", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [createMarker({ id: "marker-model-update", title: "Original" })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          coordinate: { latitude: 50.0, longitude: 7.0 },
          title: "Updated",
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-model-update")!;
      expect((layerTuple.model as ISpillgebeesMarker).title).toBe("Updated");
    });

    it("should remove tooltip when tooltip becomes null", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [
        createMarker({
          id: "marker-tooltip-remove",
          tooltip: {
            content: "Will be removed",
            offset: null,
            direction: null,
            permanent: false,
            sticky: false,
            interactive: false,
            opacity: null,
            className: null,
          },
        }),
      ];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          tooltip: null,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-tooltip-remove")!;
      const mockMarker = layerTuple.leaflet as unknown as MockMarker;
      // unbindTooltip called in updateLayers to clear the existing tooltip
      expect(mockMarker.unbindTooltip).toHaveBeenCalled();
      // bindTooltip should only have been called once (in addLayers), not again in updateLayers
      expect(mockMarker.bindTooltip).toHaveBeenCalledTimes(1);
    });

    it("should preserve existing icon when icon is null in update", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [
        createMarker({
          id: "marker-icon-preserve",
          icon: {
            iconUrl: "https://example.com/icon.png",
            iconSize: [25, 41],
            iconAnchor: null,
            popupAnchor: null,
            tooltipAnchor: null,
            shadowUrl: null,
            shadowSize: null,
            shadowAnchor: null,
            className: null,
          },
        }),
      ];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          icon: null,
          coordinate: { latitude: 50.0, longitude: 7.0 },
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-icon-preserve")!;
      const mockMarker = layerTuple.leaflet as unknown as MockMarker;
      // setIcon should NOT be called in updateLayers when icon is null (preserves existing icon)
      expect(mockMarker.setIcon).not.toHaveBeenCalled();
    });

    it("should update zIndexOffset via setZIndexOffset", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [createMarker({ id: "marker-zindex" })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          zIndexOffset: 250,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-zindex")!;
      const mockMarker = layerTuple.leaflet as unknown as MockMarker;
      expect(mockMarker.setZIndexOffset).toHaveBeenCalledWith(250);
    });

    it("should reset zIndexOffset to 0 when null", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [createMarker({ id: "marker-clear-zindex", zIndexOffset: 100 })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const updatedMarkers: ISpillgebeesMarker[] = [
        {
          ...markers[0],
          zIndexOffset: null,
        },
      ];

      // act
      window.Spillgebees.Map.mapFunctions.updateLayers(mapContainer, updatedMarkers, [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      const layerTuple = layerStorage.byId.get("marker-clear-zindex")!;
      const mockMarker = layerTuple.leaflet as unknown as MockMarker;
      expect(mockMarker.setZIndexOffset).toHaveBeenCalledWith(0);
    });
  });

  describe("removeLayers", () => {
    it("should remove marker by ID", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [createMarker({ id: "marker-rm", title: "Remove Me" })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      // act
      window.Spillgebees.Map.mapFunctions.removeLayers(mapContainer, ["marker-rm"], [], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      expect(map.removeLayer).toHaveBeenCalled();
      const layerStorage = window.Spillgebees.Map.layers.get(map as unknown as LeafletMap)!;
      expect(layerStorage.byId.has("marker-rm")).toBe(false);
    });

    it("should remove circleMarker by ID", async () => {
      // arrange
      await createMapWithLayers();
      const circleMarkers: ISpillgebeesCircleMarker[] = [createCircleMarker({ id: "cm-rm" })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], circleMarkers, []);

      // act
      window.Spillgebees.Map.mapFunctions.removeLayers(mapContainer, [], ["cm-rm"], []);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      expect(map.removeLayer).toHaveBeenCalled();
      const layerStorage = window.Spillgebees.Map.layers.get(map as unknown as LeafletMap)!;
      expect(layerStorage.byId.has("cm-rm")).toBe(false);
    });

    it("should remove polyline by ID", async () => {
      // arrange
      await createMapWithLayers();
      const polylines: ISpillgebeesPolyline[] = [createPolyline({ id: "poly-rm" })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], [], polylines);

      // act
      window.Spillgebees.Map.mapFunctions.removeLayers(mapContainer, [], [], ["poly-rm"]);

      // assert
      const map = window.Spillgebees.Map.maps.get(mapContainer)! as unknown as MockMap;
      expect(map.removeLayer).toHaveBeenCalled();
      const layerStorage = window.Spillgebees.Map.layers.get(map as unknown as LeafletMap)!;
      expect(layerStorage.byId.has("poly-rm")).toBe(false);
    });

    it("should silently skip unknown IDs", async () => {
      // arrange
      await createMapWithLayers();
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, [], [], []);

      // act & assert — should not throw
      window.Spillgebees.Map.mapFunctions.removeLayers(mapContainer, ["nonexistent"], ["unknown"], ["missing"]);
    });

    it("should clean up layer storage entries from both byId and byLeaflet", async () => {
      // arrange
      await createMapWithLayers();
      const markers: ISpillgebeesMarker[] = [createMarker({ id: "marker-cleanup", title: "Cleanup" })];
      window.Spillgebees.Map.mapFunctions.addLayers(mapContainer, markers, [], []);

      const map = window.Spillgebees.Map.maps.get(mapContainer)!;
      const layerStorage = window.Spillgebees.Map.layers.get(map)!;
      expect(layerStorage.byLeaflet.size).toBe(1);

      // act
      window.Spillgebees.Map.mapFunctions.removeLayers(mapContainer, ["marker-cleanup"], [], []);

      // assert
      expect(layerStorage.byId.size).toBe(0);
      expect(layerStorage.byLeaflet.size).toBe(0);
    });

    it("should early-return if map container is not found", () => {
      // arrange
      const unknownContainer = document.createElement("div");

      // act & assert — should not throw
      window.Spillgebees.Map.mapFunctions.removeLayers(unknownContainer, [], [], []);
    });
  });
});
