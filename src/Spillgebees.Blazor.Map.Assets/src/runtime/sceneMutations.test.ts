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
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap);
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
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap);
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
      window.Spillgebees.Map.pendingStyleReloads.add(mockMap);
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
          targets: [{ styleId: "base-style", layerIds: ["visibility-layer"] }],
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
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap);
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
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap);
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
    mockMap.getStyle.mockReturnValue({ layers: [] });
    mockMap.getLayer.mockImplementation((id: string) => (id === "runtime-overlay-layer" ? { id } : undefined));
    window.Spillgebees.Map.composedStyleLayerIds.get(mockMap)?.set("overlay-style\u0000overlay-layer", {
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
          targets: [{ styleId: "overlay-style", layerIds: ["overlay-layer"] }],
        },
      ],
    });

    let overlayReplayResolved = false;
    const applyOverlayStylesSpy = vi.spyOn(composition, "applyOverlayStyles").mockImplementation(async () => {
      window.Spillgebees.Map.composedStyleLayerIds.get(mockMap)?.set("overlay-style\u0000overlay-layer", {
        styleId: "overlay-style",
        originalLayerId: "overlay-layer",
        runtimeLayerId: "runtime-overlay-layer",
      });
      overlayReplayResolved = true;
    });
    const invokeMethodAsync = vi.mocked(dotNetHelper.invokeMethodAsync);
    invokeMethodAsync.mockClear();

    // act
    window.Spillgebees.Map.pendingStyleReloads.add(mockMap);
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
});
