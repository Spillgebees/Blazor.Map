import type { Map as MapLibreMap } from "maplibre-gl";
import { describe, expect, it, vi } from "vitest";
import { createMockDotNetHelper } from "../../test/dotNetHelperMock";
import { fireLoadEvent, fireMapEvent, getLatestMockMapInstance, resetMockMapState } from "../../test/maplibreMock";
import { resetWindowGlobals } from "../../test/windowSetup";
import type { IMapControl } from "../interfaces/controls";
import type { IMapOptions } from "../interfaces/map";
import { bootstrap, createMap } from "../map";
import { addMapSource, wireLayerEvents } from "../sources/geojson";
import * as composition from "../styles/composition";
import { applySceneMutations } from "./sceneMutations";

describe.sequential("applySceneMutations", () => {
  function createDefaultMapOptions(overrides?: Partial<IMapOptions>): IMapOptions {
    return {
      center: { latitude: 51.505, longitude: -0.09 },
      zoom: 13,
      style: null,
      styles: null,
      composedGlyphsUrl: null,
      pitch: 0,
      bearing: 0,
      projection: "mercator",
      terrain: false,
      terrainExaggeration: 1,
      fitBoundsOptions: null,
      minZoom: null,
      maxZoom: null,
      maxBounds: null,
      interactive: true,
      cooperativeGestures: false,
      webFonts: null,
      pixelRatioMode: "browserDefault",
      pixelRatio: null,
      ...overrides,
    };
  }

  function createDefaultControls(): IMapControl[] {
    return [];
  }

  it("should register and rehydrate custom scene state from a batched mutation payload", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitialized",
      mapElement,
      createDefaultMapOptions({
        style: {
          id: "base-style",
          url: "https://example.com/style.json",
          referrerPolicy: null,
          rasterSource: null,
          wmsSource: null,
        },
      }),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "addSource",
          sourceId: "scene-source",
          sourceSpec: {
            type: "geojson",
            data: { type: "FeatureCollection", features: [] },
          },
        },
        {
          kind: "addLayer",
          layerId: "scene-layer",
          layerSpec: {
            id: "scene-layer",
            type: "symbol",
            source: "scene-source",
          },
          beforeLayerId: null,
          ordering: {
            declarationOrder: 1,
            layerGroup: null,
            beforeLayerGroup: null,
            afterLayerGroup: null,
          },
        },
        {
          kind: "wireLayerEvents",
          layerId: "scene-layer",
          dotNetRef: dotNetHelper,
          onClick: true,
          onMouseEnter: true,
          onMouseLeave: true,
        },
      ],
    });

    const mockMap = getLatestMockMapInstance()!;
    let layerExists = false;
    mockMap.getSource.mockImplementation((id: string) => (id === "scene-source" ? undefined : null));
    mockMap.addLayer.mockImplementation((layer: { id?: string }) => {
      if (layer.id === "scene-layer") {
        layerExists = true;
      }
    });
    mockMap.getLayer.mockImplementation((id: string) => (id === "scene-layer" && layerExists ? {} : undefined));
    mockMap.addSource.mockClear();
    mockMap.addLayer.mockClear();
    mockMap.on.mockClear();

    // act
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap as unknown as MapLibreMap);
    fireMapEvent("styledata");

    // assert
    expect(mockMap.addSource).toHaveBeenCalledWith("scene-source", {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    });
    expect(mockMap.addLayer).toHaveBeenCalledWith(
      {
        id: "scene-layer",
        type: "symbol",
        source: "scene-source",
      },
      undefined,
    );
    expect(mockMap.on).toHaveBeenCalledWith("click", "scene-layer", expect.any(Function));
    expect(mockMap.on).toHaveBeenCalledWith("mouseenter", "scene-layer", expect.any(Function));
    expect(mockMap.on).toHaveBeenCalledWith("mouseleave", "scene-layer", expect.any(Function));
  });

  it("should keep legacy source and event helpers compatible with registry replay", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitialized",
      mapElement,
      createDefaultMapOptions({
        style: {
          id: "base-style",
          url: "https://example.com/style.json",
          referrerPolicy: null,
          rasterSource: null,
          wmsSource: null,
        },
      }),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    addMapSource(mapElement, "legacy-source", {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    });
    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "addLayer",
          layerId: "legacy-layer",
          layerSpec: {
            id: "legacy-layer",
            type: "symbol",
            source: "legacy-source",
          },
          beforeLayerId: null,
          ordering: {
            declarationOrder: 1,
            layerGroup: null,
            beforeLayerGroup: null,
            afterLayerGroup: null,
          },
        },
      ],
    });
    wireLayerEvents(mapElement, "legacy-layer", dotNetHelper, true, false, false);

    const mockMap = getLatestMockMapInstance()!;
    let layerExists = false;
    mockMap.getSource.mockImplementation((id: string) => (id === "legacy-source" ? undefined : null));
    mockMap.addLayer.mockImplementation((layer: { id?: string }) => {
      if (layer.id === "legacy-layer") {
        layerExists = true;
      }
    });
    mockMap.getLayer.mockImplementation((id: string) => (id === "legacy-layer" && layerExists ? {} : undefined));
    mockMap.addSource.mockClear();
    mockMap.on.mockClear();

    // act
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap as unknown as MapLibreMap);
    fireMapEvent("styledata");

    // assert
    expect(mockMap.addSource).toHaveBeenCalledWith("legacy-source", {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    });
    expect(mockMap.on).toHaveBeenCalledWith("click", "legacy-layer", expect.any(Function));
  });

  it("should preserve ordering across batched mutations and style reload replay", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitialized",
      mapElement,
      createDefaultMapOptions({
        style: {
          id: "base-style",
          url: "https://example.com/style.json",
          referrerPolicy: null,
          rasterSource: null,
          wmsSource: null,
        },
      }),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "addSource",
          sourceId: "ordered-source",
          sourceSpec: {
            type: "geojson",
            data: { type: "FeatureCollection", features: [] },
          },
        },
        {
          kind: "addLayer",
          layerId: "layer-a",
          layerSpec: { id: "layer-a", type: "line", source: "ordered-source" },
          beforeLayerId: null,
          ordering: {
            declarationOrder: 1,
            layerGroup: "layerGroup-a",
            beforeLayerGroup: null,
            afterLayerGroup: null,
          },
        },
        {
          kind: "addLayer",
          layerId: "layer-b",
          layerSpec: { id: "layer-b", type: "line", source: "ordered-source" },
          beforeLayerId: null,
          ordering: {
            declarationOrder: 2,
            layerGroup: "layerGroup-b",
            beforeLayerGroup: null,
            afterLayerGroup: "layerGroup-a",
          },
        },
        {
          kind: "addLayer",
          layerId: "layer-c",
          layerSpec: { id: "layer-c", type: "line", source: "ordered-source" },
          beforeLayerId: null,
          ordering: {
            declarationOrder: 3,
            layerGroup: "layerGroup-c",
            beforeLayerGroup: null,
            afterLayerGroup: "layerGroup-b",
          },
        },
      ],
    });

    const mockMap = getLatestMockMapInstance()!;
    const existingLayers = new Set<string>();
    mockMap.getSource.mockImplementation((id: string) => (id === "ordered-source" ? undefined : null));
    mockMap.getStyle.mockReturnValue({ layers: [] });
    mockMap.getLayer.mockImplementation((id: string) => (existingLayers.has(id) ? {} : undefined));
    mockMap.addLayer.mockImplementation((layer: { id?: string }, beforeLayerId?: string) => {
      if (beforeLayerId && !existingLayers.has(beforeLayerId)) {
        throw new Error(`Unknown beforeLayerId: ${beforeLayerId}`);
      }

      if (layer.id) {
        existingLayers.add(layer.id);
      }
    });
    mockMap.moveLayer.mockImplementation((layerId: string, beforeLayerId?: string) => {
      if (!existingLayers.has(layerId)) {
        throw new Error(`Unknown layer: ${layerId}`);
      }

      if (beforeLayerId && !existingLayers.has(beforeLayerId)) {
        throw new Error(`Unknown beforeLayerId: ${beforeLayerId}`);
      }
    });
    mockMap.addSource.mockClear();
    mockMap.addLayer.mockClear();
    mockMap.moveLayer.mockClear();

    // act
    const act = () => {
      window.Spillgebees.Map.pendingStyleReloads.add(mockMap as unknown as MapLibreMap);
      fireMapEvent("styledata");
    };

    // assert
    expect(act).not.toThrow();
    expect(mockMap.addSource).toHaveBeenCalledWith("ordered-source", {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    });
    expect(mockMap.addLayer).toHaveBeenCalledWith({ id: "layer-b", type: "line", source: "ordered-source" }, "layer-c");
    expect(mockMap.addLayer).toHaveBeenCalledWith({ id: "layer-a", type: "line", source: "ordered-source" }, "layer-b");
  });

  it("should replay visibility groups after ordering and composed overlay replay", async () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitialized",
      mapElement,
      createDefaultMapOptions({
        style: {
          id: "base-style",
          url: "https://example.com/style.json",
          referrerPolicy: null,
          rasterSource: null,
          wmsSource: null,
        },
      }),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "addSource",
          sourceId: "visibility-source",
          sourceSpec: {
            type: "geojson",
            data: { type: "FeatureCollection", features: [] },
          },
        },
        {
          kind: "addLayer",
          layerId: "visibility-layer",
          layerSpec: { id: "visibility-layer", type: "line", source: "visibility-source" },
          beforeLayerId: null,
          ordering: {
            declarationOrder: 1,
            layerGroup: "visibility",
            beforeLayerGroup: null,
            afterLayerGroup: null,
          },
        },
        {
          kind: "setVisibilityGroup",
          groupId: "legend:stations",
          visible: false,
          targets: [{ kind: "styleLayer", styleId: "base-style", layerIds: ["visibility-layer"] }],
        },
      ],
    });

    const mockMap = getLatestMockMapInstance()!;
    let layerExists = false;
    mockMap.getSource.mockImplementation((id: string) => (id === "visibility-source" ? undefined : null));
    mockMap.addLayer.mockImplementation((layer: { id?: string }) => {
      if (layer.id === "visibility-layer") {
        layerExists = true;
      }
    });
    mockMap.getStyle.mockReturnValue({ layers: [] });
    mockMap.getLayer.mockImplementation((id: string) => {
      if (id === "visibility-layer") {
        return layerExists ? { id } : undefined;
      }

      if (id === "sgb-polylines-layer" || id === "sgb-circles-layer") {
        return undefined;
      }

      return { id };
    });
    mockMap.addSource.mockClear();
    mockMap.addLayer.mockClear();
    mockMap.moveLayer.mockClear();
    mockMap.setLayoutProperty.mockClear();

    // act
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap as unknown as MapLibreMap);
    fireMapEvent("styledata");
    await new Promise((resolve) => setTimeout(resolve, 0));

    // assert
    expect(
      mockMap.addLayer.mock.calls.some(
        ([layer, beforeLayerId]) =>
          beforeLayerId === undefined &&
          layer &&
          typeof layer === "object" &&
          "id" in layer &&
          "source" in layer &&
          "type" in layer &&
          layer.id === "visibility-layer" &&
          layer.source === "visibility-source" &&
          layer.type === "line",
      ),
    ).toBe(true);
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("visibility-layer", "visibility", "none");
  });

  it("should compose display feature filters with baseline filters", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitialized",
      mapElement,
      createDefaultMapOptions({
        style: {
          id: "base-style",
          url: "https://example.com/style.json",
          referrerPolicy: null,
          rasterSource: null,
          wmsSource: null,
        },
      }),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    const mockMap = getLatestMockMapInstance()!;
    mockMap.getStyle.mockReturnValue({
      layers: [{ id: "transit", type: "line", source: "base", filter: ["==", ["get", "class"], "rail"] }],
    });
    mockMap.getLayer.mockImplementation((id: string) => (id === "transit" ? { id } : undefined));

    // act
    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "setVisibilityGroup",
          groupId: "display:night-bus",
          visible: false,
          targets: [
            {
              kind: "styleLayerFeatures",
              styleId: "base-style",
              layerIds: ["transit"],
              filter: ["==", ["get", "service"], "night"],
            },
          ],
        },
      ],
    });

    // assert
    expect(mockMap.setFilter).toHaveBeenCalledWith("transit", [
      "all",
      ["==", ["get", "class"], "rail"],
      ["!", ["==", ["get", "service"], "night"]],
    ]);
  });

  it("should reconcile ordering once while replaying a style reload", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitialized",
      mapElement,
      createDefaultMapOptions({
        style: {
          id: "base-style",
          url: "https://example.com/style.json",
          referrerPolicy: null,
          rasterSource: null,
          wmsSource: null,
        },
      }),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "addSource",
          sourceId: "ordered-source",
          sourceSpec: {
            type: "geojson",
            data: { type: "FeatureCollection", features: [] },
          },
        },
        {
          kind: "addLayer",
          layerId: "layer-a",
          layerSpec: { id: "layer-a", type: "line", source: "ordered-source" },
          beforeLayerId: null,
          ordering: {
            declarationOrder: 1,
            layerGroup: "layerGroup-a",
            beforeLayerGroup: null,
            afterLayerGroup: null,
          },
        },
        {
          kind: "addLayer",
          layerId: "layer-b",
          layerSpec: { id: "layer-b", type: "line", source: "ordered-source" },
          beforeLayerId: null,
          ordering: {
            declarationOrder: 2,
            layerGroup: "layerGroup-b",
            beforeLayerGroup: null,
            afterLayerGroup: "layerGroup-a",
          },
        },
      ],
    });

    const mockMap = getLatestMockMapInstance()!;
    const existingLayers = new Set<string>();
    mockMap.getSource.mockImplementation((id: string) => (id === "ordered-source" ? undefined : null));
    mockMap.getStyle.mockReturnValue({ layers: [] });
    mockMap.getLayer.mockImplementation((id: string) => (existingLayers.has(id) ? {} : undefined));
    mockMap.addLayer.mockImplementation((layer: { id?: string }, beforeLayerId?: string) => {
      if (beforeLayerId && !existingLayers.has(beforeLayerId)) {
        throw new Error(`Unknown beforeLayerId: ${beforeLayerId}`);
      }

      if (layer.id) {
        existingLayers.add(layer.id);
      }
    });
    mockMap.moveLayer.mockImplementation((layerId: string, beforeLayerId?: string) => {
      if (!existingLayers.has(layerId)) {
        throw new Error(`Unknown layer: ${layerId}`);
      }

      if (beforeLayerId && !existingLayers.has(beforeLayerId)) {
        throw new Error(`Unknown beforeLayerId: ${beforeLayerId}`);
      }
    });
    mockMap.addSource.mockClear();
    mockMap.addLayer.mockClear();
    mockMap.moveLayer.mockClear();

    // act
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap as unknown as MapLibreMap);
    fireMapEvent("styledata");

    // assert
    expect(mockMap.moveLayer).toHaveBeenCalledTimes(0);
    expect(mockMap.addLayer.mock.calls).toContainEqual([
      { id: "layer-b", type: "line", source: "ordered-source" },
      undefined,
    ]);
    expect(mockMap.addLayer.mock.calls).toContainEqual([
      { id: "layer-a", type: "line", source: "ordered-source" },
      "layer-b",
    ]);
  });

  it("should notify .NET only after visibility replay runs after composed overlay replay", async () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitialized",
      mapElement,
      createDefaultMapOptions({
        styles: [
          {
            id: "base-style",
            url: "https://example.com/base-style.json",
            referrerPolicy: null,
            rasterSource: null,
            wmsSource: null,
          },
          {
            id: "overlay-style",
            url: "https://example.com/overlay-style.json",
            referrerPolicy: null,
            rasterSource: null,
            wmsSource: null,
          },
        ],
      }),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    const mockMap = getLatestMockMapInstance()!;
    const mapLibreMap = mockMap as unknown as MapLibreMap;
    mockMap.getStyle.mockReturnValue({ layers: [] });
    mockMap.getLayer.mockImplementation((id: string) => (id === "runtime-overlay-layer" ? { id } : undefined));
    window.Spillgebees.Map.composedStyleLayerIds.get(mapLibreMap)?.set("overlay-style\u0000overlay-layer", {
      styleId: "overlay-style",
      originalLayerId: "overlay-layer",
      runtimeLayerId: "runtime-overlay-layer",
    });

    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "setVisibilityGroup",
          groupId: "legend:overlay",
          visible: false,
          targets: [{ kind: "styleLayer", styleId: "overlay-style", layerIds: ["overlay-layer"] }],
        },
      ],
    });

    let overlayReplayResolved = false;
    const applyOverlayStylesSpy = vi.spyOn(composition, "applyOverlayStyles").mockImplementation(async () => {
      window.Spillgebees.Map.composedStyleLayerIds.get(mapLibreMap)?.set("overlay-style\u0000overlay-layer", {
        styleId: "overlay-style",
        originalLayerId: "overlay-layer",
        runtimeLayerId: "runtime-overlay-layer",
      });
      overlayReplayResolved = true;
    });
    const invokeMethodAsync = vi.mocked(dotNetHelper.invokeMethodAsync);
    invokeMethodAsync.mockClear();

    // act
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap as unknown as MapLibreMap);
    fireMapEvent("styledata");
    await new Promise((resolve) => setTimeout(resolve, 0));

    // assert
    expect(overlayReplayResolved).toBe(true);
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("runtime-overlay-layer", "visibility", "none");
    // biome-ignore lint/security/noSecrets: C# callback method name under test, not a secret
    expect(invokeMethodAsync).toHaveBeenCalledWith("OnMapStyleReloadedAsync");

    // cleanup
    applyOverlayStylesSpy.mockRestore();
  });

  it("should apply runtime layer visibility targets directly", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitializedAsync",
      mapElement,
      createDefaultMapOptions(),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    const mockMap = getLatestMockMapInstance()!;
    mockMap.getLayer.mockImplementation((id: string) => (id === "runtime-layer" ? { id } : undefined));

    // act
    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "setVisibilityGroup",
          groupId: "runtime-group",
          visible: false,
          targets: [{ kind: "runtimeLayer", layerIds: ["runtime-layer"] }],
        },
      ],
    });

    // assert
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("runtime-layer", "visibility", "none");
  });

  it("should apply whole composed style visibility targets", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitializedAsync",
      mapElement,
      createDefaultMapOptions(),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    const mockMap = getLatestMockMapInstance()!;
    const composedStyleLayerIds = new Map();
    window.Spillgebees.Map.composedStyleLayerIds.set(mockMap as unknown as MapLibreMap, composedStyleLayerIds);
    composedStyleLayerIds.set("overlay-style\u0000layer-a", {
      runtimeLayerId: "runtime-layer-a",
      styleId: "overlay-style",
      originalLayerId: "layer-a",
    });
    composedStyleLayerIds.set("overlay-style\u0000layer-b", {
      runtimeLayerId: "runtime-layer-b",
      styleId: "overlay-style",
      originalLayerId: "layer-b",
    });
    composedStyleLayerIds.set("other-style\u0000layer-c", {
      runtimeLayerId: "runtime-layer-c",
      styleId: "other-style",
      originalLayerId: "layer-c",
    });
    mockMap.getLayer.mockImplementation((id: string) =>
      id === "runtime-layer-a" || id === "runtime-layer-b" || id === "runtime-layer-c" ? { id } : undefined,
    );

    // act
    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "setVisibilityGroup",
          groupId: "overlay-group",
          visible: false,
          targets: [{ kind: "styleLayer", styleId: "overlay-style", layerIds: [] }],
        },
      ],
    });

    // assert
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("runtime-layer-a", "visibility", "none");
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("runtime-layer-b", "visibility", "none");
    expect(mockMap.setLayoutProperty).not.toHaveBeenCalledWith("runtime-layer-c", "visibility", "none");
  });

  it("should apply overlay parent and part visibility while preserving original style visibility", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitializedAsync",
      mapElement,
      createDefaultMapOptions(),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    const mockMap = getLatestMockMapInstance()!;
    window.Spillgebees.Map.composedStyleLayerIds.set(
      mockMap as unknown as MapLibreMap,
      new Map([
        [
          "lux-railway\u0000tracks",
          {
            runtimeLayerId: "runtime-tracks",
            styleId: "lux-railway",
            originalLayerId: "tracks",
            originalVisible: true,
          },
        ],
        [
          "lux-railway\u0000hidden-labels",
          {
            runtimeLayerId: "runtime-hidden-labels",
            styleId: "lux-railway",
            originalLayerId: "hidden-labels",
            originalVisible: false,
          },
        ],
        [
          "other-style\u0000tracks",
          {
            runtimeLayerId: "other-tracks",
            styleId: "other-style",
            originalLayerId: "tracks",
            originalVisible: true,
          },
        ],
      ]),
    );
    mockMap.getLayer.mockImplementation((id: string) =>
      ["runtime-tracks", "runtime-hidden-labels", "other-tracks"].includes(id) ? { id } : undefined,
    );

    // act
    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "setOverlay",
          overlayId: "lux-railway",
          visible: true,
          overlayTargets: [{ kind: "styleLayer", styleId: "lux-railway", layerIds: [] }],
          parts: [
            {
              partId: "tracks",
              visible: false,
              targets: [{ kind: "styleLayer", styleId: "lux-railway", layerIds: ["tracks"] }],
            },
          ],
        },
      ],
    });

    // assert
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("runtime-tracks", "visibility", "none");
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("runtime-hidden-labels", "visibility", "none");
    expect(mockMap.setLayoutProperty).not.toHaveBeenCalledWith("other-tracks", "visibility", expect.anything());
  });

  it("should restore effective visibility when an overlay is removed", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitializedAsync",
      mapElement,
      createDefaultMapOptions(),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    const mockMap = getLatestMockMapInstance()!;
    window.Spillgebees.Map.composedStyleLayerIds.set(
      mockMap as unknown as MapLibreMap,
      new Map([
        [
          "lux-railway\u0000tracks",
          {
            runtimeLayerId: "runtime-tracks",
            styleId: "lux-railway",
            originalLayerId: "tracks",
            originalVisible: true,
          },
        ],
        [
          "lux-railway\u0000stations",
          {
            runtimeLayerId: "runtime-stations",
            styleId: "lux-railway",
            originalLayerId: "stations",
            originalVisible: true,
          },
        ],
      ]),
    );
    mockMap.getLayer.mockImplementation((id: string) =>
      ["runtime-tracks", "runtime-stations"].includes(id) ? { id } : undefined,
    );

    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "setVisibilityGroup",
          groupId: "stations",
          visible: false,
          targets: [{ kind: "styleLayer", styleId: "lux-railway", layerIds: ["stations"] }],
        },
        {
          kind: "setOverlay",
          overlayId: "lux-railway",
          visible: false,
          overlayTargets: [{ kind: "styleLayer", styleId: "lux-railway", layerIds: [] }],
          parts: [],
        },
      ],
    });
    mockMap.setLayoutProperty.mockClear();

    // act
    applySceneMutations(mapElement, {
      mutations: [{ kind: "removeOverlay", overlayId: "lux-railway" }],
    });

    // assert
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("runtime-tracks", "visibility", "visible");
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("runtime-stations", "visibility", "none");
  });

  it("should use runtime layer original visibility instead of mutated layout visibility", () => {
    // arrange
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();

    const mapElement = document.createElement("div");
    const dotNetHelper = createMockDotNetHelper();
    createMap(
      dotNetHelper,
      "OnMapInitializedAsync",
      mapElement,
      createDefaultMapOptions(),
      createDefaultControls(),
      "light",
      [],
      [],
      [],
      [],
    );
    fireLoadEvent();

    const mockMap = getLatestMockMapInstance()!;
    mockMap.getLayer.mockImplementation((id: string) => (id === "runtime-layer" ? { id } : undefined));
    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "addLayer",
          layerId: "runtime-layer",
          layerSpec: {
            id: "runtime-layer",
            type: "line",
            source: "runtime-source",
            layout: { visibility: "visible" },
          },
          beforeLayerId: null,
          ordering: {
            declarationOrder: 0,
            layerGroup: null,
            beforeLayerGroup: null,
            afterLayerGroup: null,
          },
        },
      ],
    });

    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "setOverlay",
          overlayId: "runtime-overlay",
          visible: false,
          overlayTargets: [{ kind: "runtimeLayer", layerIds: ["runtime-layer"] }],
          parts: [],
        },
      ],
    });
    mockMap.setLayoutProperty.mockClear();

    // act
    applySceneMutations(mapElement, {
      mutations: [
        {
          kind: "setOverlay",
          overlayId: "runtime-overlay",
          visible: true,
          overlayTargets: [{ kind: "runtimeLayer", layerIds: ["runtime-layer"] }],
          parts: [],
        },
      ],
    });

    // assert
    expect(mockMap.setLayoutProperty).toHaveBeenCalledWith("runtime-layer", "visibility", "visible");
  });
});
