import { describe, expect, it, vi } from "vitest";
import { encodeMotionFrame } from "./motion";
import type { Op } from "./ops";
import { createEngine, type Engine, type EngineEvent, type EngineMap } from "./runtime";
import { createScheduler } from "./scheduler";

interface SourceStub {
  spec: Record<string, unknown>;
  setData: ReturnType<typeof vi.fn>;
  updateData?: ReturnType<typeof vi.fn>;
}

interface Harness {
  engine: Engine;
  map: EngineMap;
  log: string[];
  sources: Map<string, SourceStub>;
  layers: Map<string, { spec: Record<string, unknown>; beforeId: string | undefined }>;
  featureStates: { source: string; id: number | string; state: Record<string, unknown> }[];
  events: EngineEvent[];
  errors: unknown[];
  addedImages: string[];
  markerPositions: Map<string, { lng: number; lat: number }>;
  fire(event: string, layerId: string, payload: unknown): void;
  step(now?: number): void;
  resetLog(): void;
}

function createHarness(options: { sourcesSupportUpdateData?: boolean } = {}): Harness {
  const log: string[] = [];
  const sources = new Map<string, SourceStub>();
  const layers = new Map<string, { spec: Record<string, unknown>; beforeId: string | undefined }>();
  const featureStates: Harness["featureStates"] = [];
  const events: EngineEvent[] = [];
  const errors: unknown[] = [];
  const addedImages: string[] = [];
  const handlers = new Map<string, (event: unknown) => void>();
  const markerPositions = new Map<string, { lng: number; lat: number }>();

  let nextFrame = 1;
  const pendingFrames = new Map<number, (now: number) => void>();
  const scheduler = createScheduler(
    (callback) => {
      const handle = nextFrame++;
      pendingFrames.set(handle, callback);
      return handle;
    },
    (handle) => pendingFrames.delete(handle),
  );

  const map: EngineMap = {
    addSource(id, spec) {
      log.push(`addSource:${id}`);
      const stub: SourceStub = { spec, setData: vi.fn() };
      if (options.sourcesSupportUpdateData !== false) {
        stub.updateData = vi.fn();
      }
      sources.set(id, stub);
    },
    removeSource(id) {
      log.push(`removeSource:${id}`);
      sources.delete(id);
    },
    getSource: (id) => sources.get(id),
    addLayer(spec, beforeId) {
      const id = spec.id as string;
      log.push(`addLayer:${id}${beforeId ? `@${beforeId}` : ""}`);
      layers.set(id, { spec, beforeId });
    },
    removeLayer(id) {
      log.push(`removeLayer:${id}`);
      layers.delete(id);
    },
    getLayer: (id) => layers.get(id),
    moveLayer(id, beforeId) {
      log.push(`moveLayer:${id}${beforeId ? `@${beforeId}` : ""}`);
    },
    setPaintProperty(id, name) {
      log.push(`setPaint:${id}:${name}`);
    },
    setLayoutProperty(id, name, value) {
      log.push(`setLayout:${id}:${name}=${value}`);
    },
    setFilter(id, filter) {
      log.push(`setFilter:${id}:${JSON.stringify(filter)}`);
    },
    setLayerZoomRange(id, min, max) {
      log.push(`setZoom:${id}:${min}-${max}`);
    },
    setFeatureState(target, state) {
      featureStates.push({ source: target.source, id: target.id, state });
    },
    removeFeatureState(target) {
      log.push(`removeFeatureState:${target.source}:${target.id}`);
    },
    easeTo(options) {
      log.push(`easeTo:${JSON.stringify(options)}`);
    },
    getZoom: () => 0,
    getBearing: () => 0,
    getPitch: () => 0,
    lockInteraction: () => () => {},
    addImage(id) {
      addedImages.push(id);
    },
    removeImage(id) {
      log.push(`removeImage:${id}`);
    },
    hasImage: (id) => addedImages.includes(id),
    loadImageData: (url) => Promise.resolve({ data: `image:${url}` }),
    on(event: string, layerIdOrHandler: string | ((event: unknown) => void), handler?: (event: unknown) => void) {
      if (typeof layerIdOrHandler === "function") {
        handlers.set(event, layerIdOrHandler);
      } else if (handler) {
        handlers.set(`${event}:${layerIdOrHandler}`, handler);
      }
    },
    off(event: string, layerIdOrHandler: string | ((event: unknown) => void)) {
      handlers.delete(typeof layerIdOrHandler === "function" ? event : `${event}:${layerIdOrHandler}`);
    },
    listStyleLayers: () => [...layers.values()].map((layer) => layer.spec as { id: string }),
    resolveComposedLayer: () => null,
    listComposedLayers: () => [],
    setMarker(marker) {
      log.push(`setMarker:${marker.id}`);
    },
    removeMarker(id) {
      log.push(`removeMarker:${id}`);
    },
    setControl(control) {
      log.push(`setControl:${control.controlId}`);
    },
    setFullscreen(state) {
      log.push(`setFullscreen:${state}`);
    },
    removeControl(id) {
      log.push(`removeControl:${id}`);
    },
    setControlContent(id, events) {
      log.push(`setControlContent:${id}${events?.openChanged != null ? `@${events.openChanged}` : ""}`);
    },
    removeControlContent(id) {
      log.push(`removeControlContent:${id}`);
    },
    setPopup(popup) {
      log.push(`setPopup:${popup.id}`);
    },
    removePopup(id) {
      log.push(`removePopup:${id}`);
    },
    showPopup(position) {
      log.push(`showPopup:${position.longitude},${position.latitude}`);
    },
    closeActivePopup() {
      log.push("closeActivePopup");
    },
    configure(config) {
      log.push(`configure:${config.projection}@${config.pitch}/${config.bearing}`);
    },
    resize() {
      log.push("resize");
    },
    setRequestPolicy(origin, policy) {
      log.push(`requestPolicy:${origin}=${policy}`);
    },
    flyTo(options) {
      log.push(`flyTo:${JSON.stringify(options)}`);
    },
    fitBounds(bounds, options) {
      log.push(`fitBounds:${JSON.stringify(bounds)}:${JSON.stringify(options)}`);
    },
    markerPosition: (id) => markerPositions.get(id) ?? null,
  };

  const engine = createEngine(map, {
    scheduler,
    now: () => 0,
    onEvent: (handlerId, event) => events.push({ ...event, handlerId } as EngineEvent & { handlerId: number }),
    onError: (error) => errors.push(error),
  });

  return {
    engine,
    map,
    log,
    sources,
    layers,
    featureStates,
    events,
    markerPositions,
    errors,
    addedImages,
    fire(event, layerId, payload) {
      handlers.get(`${event}:${layerId}`)?.(payload);
    },
    step(now = 0) {
      const callbacks = [...pendingFrames.values()];
      pendingFrames.clear();
      for (const callback of callbacks) {
        callback(now);
      }
    },
    resetLog() {
      log.length = 0;
    },
  };
}

function vehiclesSetup(harness: Harness, extraConfig: Record<string, unknown> = {}): void {
  harness.engine.applyOps([
    { op: "entities.create", id: "vehicles", config: extraConfig },
    {
      op: "entities.upsert",
      id: "vehicles",
      epoch: 1,
      removes: [],
      upserts: Array.from({ length: 10 }, (_, idx) => ({ idx, id: `v-${idx}`, lng: idx, lat: 0 })),
    },
  ]);
  harness.step();
}

describe("op dispatch", () => {
  it("applies ops in array order and keeps going past a failing op", () => {
    const harness = createHarness();
    const failing = {
      op: "layer.remove",
      id: "missing",
    } as Op;
    harness.map.removeLayer = () => {
      throw new Error("missing layer");
    };

    harness.engine.applyOps([
      { op: "source.add", id: "s1", spec: { type: "geojson", data: null } },
      failing,
      { op: "layer.add", id: "l1", spec: { id: "l1", type: "circle", source: "s1" } },
    ]);

    expect(harness.errors).toHaveLength(1);
    expect(harness.log).toEqual(["addSource:s1", "addLayer:l1"]);
  });

  it("schedules source.setData through the rAF queue, latest wins", () => {
    const harness = createHarness();
    harness.engine.applyOps([{ op: "source.add", id: "s1", spec: { type: "geojson", data: null } }]);

    harness.engine.applyOps([{ op: "source.setData", id: "s1", data: "first" }]);
    harness.engine.applyOps([{ op: "source.setData", id: "s1", data: "second" }]);
    harness.step();

    const source = harness.sources.get("s1");
    expect(source?.setData).toHaveBeenCalledExactlyOnceWith("second");
  });
});

describe("slots", () => {
  it("anchors layers to their slot and resolves slot references in before", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "slot.define", id: "overlay" },
      { op: "layer.add", id: "tracks", spec: { id: "tracks", type: "line" }, slot: "overlay" },
      { op: "layer.add", id: "halo", spec: { id: "halo", type: "circle" }, before: "overlay" },
      { op: "layer.move", id: "tracks", before: "overlay" },
    ]);

    expect(harness.log).toEqual([
      "addLayer:sgb-slot:overlay",
      "addLayer:tracks@sgb-slot:overlay",
      "addLayer:halo@sgb-slot:overlay",
      "moveLayer:tracks@sgb-slot:overlay",
    ]);
  });

  it("inserts late-added convenience polylines below circles", () => {
    const harness = createHarness();
    harness.engine.applyOps([{ op: "layer.add", id: "sgb-circles-layer", spec: { id: "sgb-circles-layer", type: "circle" } }]);
    harness.resetLog();

    harness.engine.applyOps([
      { op: "layer.add", id: "sgb-polylines-layer", spec: { id: "sgb-polylines-layer", type: "line" } },
    ]);

    expect(harness.log).toEqual(["addLayer:sgb-polylines-layer@sgb-circles-layer"]);
    expect(harness.layers.get("sgb-polylines-layer")?.beforeId).toBe("sgb-circles-layer");
  });
});

describe("entity layers", () => {
  it("creates a geojson source carrying cluster options and the live collection", () => {
    const harness = createHarness();

    harness.engine.applyOps([
      { op: "entities.create", id: "vehicles", config: { cluster: { cluster: true, clusterRadius: 40 } } },
    ]);

    const spec = harness.sources.get("vehicles")?.spec;
    expect(spec?.type).toBe("geojson");
    expect(spec?.cluster).toBe(true);
    expect(spec?.clusterRadius).toBe(40);
    expect(spec?.data).toMatchObject({ type: "FeatureCollection" });
  });

  it("flushes structural upserts as a single setData per frame", () => {
    const harness = createHarness();
    vehiclesSetup(harness);

    const source = harness.sources.get("vehicles");
    expect(source?.setData).toHaveBeenCalledOnce();
    const collection = source?.setData.mock.calls[0][0] as GeoJSON.FeatureCollection;
    expect(collection.features).toHaveLength(10);
  });

  it("applies small motion frames via updateData", () => {
    const harness = createHarness();
    vehiclesSetup(harness);

    harness.engine.pushMotion(
      "vehicles",
      encodeMotionFrame({
        epoch: 1,
        count: 1,
        indices: new Uint32Array([4]),
        coords: new Float64Array([99, 98]),
        rotations: null,
        sortKeys: null,
      }),
    );
    harness.step();

    const source = harness.sources.get("vehicles");
    expect(source?.setData).toHaveBeenCalledOnce(); // only the structural flush
    expect(source?.updateData).toHaveBeenCalledOnce();
  });

  it("falls back to setData when the source lacks updateData", () => {
    const harness = createHarness({ sourcesSupportUpdateData: false });
    vehiclesSetup(harness);

    harness.engine.pushMotion(
      "vehicles",
      encodeMotionFrame({
        epoch: 1,
        count: 1,
        indices: new Uint32Array([4]),
        coords: new Float64Array([99, 98]),
        rotations: null,
        sortKeys: null,
      }),
    );
    harness.step();

    expect(harness.sources.get("vehicles")?.setData).toHaveBeenCalledTimes(2);
  });

  it("drops stale-epoch motion frames without flushing", () => {
    const harness = createHarness();
    vehiclesSetup(harness);

    harness.engine.pushMotion(
      "vehicles",
      encodeMotionFrame({
        epoch: 99,
        count: 1,
        indices: new Uint32Array([4]),
        coords: new Float64Array([99, 98]),
        rotations: null,
        sortKeys: null,
      }),
    );
    harness.step();

    const source = harness.sources.get("vehicles");
    expect(source?.updateData).not.toHaveBeenCalled();
    expect(source?.setData).toHaveBeenCalledOnce();
  });

  it("diffs selection into feature-state calls on the entity source", () => {
    const harness = createHarness();
    vehiclesSetup(harness);

    harness.engine.applyOps([{ op: "entities.select", id: "vehicles", selected: [2] }]);

    expect(harness.featureStates).toEqual([{ source: "vehicles", id: 128, state: { selected: true } }]);
  });

  it("clears feature-state of removed entities before their index can be recycled", () => {
    const harness = createHarness();
    vehiclesSetup(harness);

    harness.engine.applyOps([{ op: "entities.upsert", id: "vehicles", epoch: 2, upserts: [], removes: [4] }]);

    expect(harness.log).toContain("removeFeatureState:vehicles:256");
  });

  it("zooms to the cluster expansion zoom on cluster-layer clicks", async () => {
    const harness = createHarness();
    harness.engine.applyOps([
      {
        op: "entities.create",
        id: "vehicles",
        config: { cluster: { cluster: true }, clusterZoomLayerIds: ["vehicles-clusters"] },
      },
    ]);
    const source = harness.sources.get("vehicles") as SourceStub & {
      getClusterExpansionZoom?: (clusterId: number) => Promise<number>;
    };
    source.getClusterExpansionZoom = vi.fn().mockResolvedValue(14);

    harness.fire("click", "vehicles-clusters", {
      features: [
        { properties: { cluster: true, cluster_id: 77 }, geometry: { type: "Point", coordinates: [6.1, 49.6] } },
      ],
    });
    await Promise.resolve();
    await Promise.resolve();

    expect(source.getClusterExpansionZoom).toHaveBeenCalledExactlyOnceWith(77);
    expect(harness.log).toContain('easeTo:{"center":[6.1,49.6],"zoom":14}');
  });

  it("handles hover locally via feature-state on all features of the entity", () => {
    const harness = createHarness();
    vehiclesSetup(harness, { hoverLayerIds: ["vehicle-symbols"] });

    harness.fire("mousemove", "vehicle-symbols", { features: [{ id: 256 }] });
    expect(harness.featureStates).toEqual([{ source: "vehicles", id: 256, state: { hover: true } }]);

    harness.featureStates.length = 0;
    harness.fire("mousemove", "vehicle-symbols", { features: [{ id: 320 }] });
    expect(harness.featureStates).toEqual([
      { source: "vehicles", id: 256, state: { hover: false } },
      { source: "vehicles", id: 320, state: { hover: true } },
    ]);

    harness.featureStates.length = 0;
    harness.fire("mouseleave", "vehicle-symbols", {});
    expect(harness.featureStates).toEqual([{ source: "vehicles", id: 320, state: { hover: false } }]);
  });
});

describe("events", () => {
  it("routes layer events to handler ids with entity resolution", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "layer.add", id: "l1", spec: { id: "l1", type: "circle", source: "s1" } },
      { op: "events.set", layerId: "l1", handlers: { click: 7 } },
    ]);

    harness.fire("click", "l1", {
      lngLat: { lng: 6.1, lat: 49.6 },
      features: [{ id: 128, properties: { entityId: "v-2", kind: "primary" } }],
    });

    expect(harness.events).toHaveLength(1);
    expect(harness.events[0]).toMatchObject({
      layerId: "l1",
      lng: 6.1,
      lat: 49.6,
      featureId: 128,
      entityId: "v-2",
    });
  });

  it("unwires handlers on events.clear", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "events.set", layerId: "l1", handlers: { click: 7 } },
      { op: "events.clear", layerId: "l1" },
    ]);

    harness.fire("click", "l1", { lngLat: { lng: 0, lat: 0 } });

    expect(harness.events).toHaveLength(0);
  });
});

describe("visibility", () => {
  it("hides and restores runtime layers through groups", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "source.add", id: "s1", spec: { type: "geojson", data: null } },
      { op: "layer.add", id: "l1", spec: { id: "l1", type: "circle", source: "s1" } },
      { op: "layer.add", id: "l2", spec: { id: "l2", type: "circle", source: "s1" } },
      { op: "visibility.set", id: "g1", visible: false, targets: [{ kind: "runtimeLayer", layerIds: ["l1"] }] },
    ]);

    expect(harness.log).toContain("setLayout:l1:visibility=none");
    expect(harness.log).not.toContain("setLayout:l2:visibility=none");

    harness.engine.applyOps([
      { op: "visibility.set", id: "g1", visible: true, targets: [{ kind: "runtimeLayer", layerIds: ["l1"] }] },
    ]);
    expect(harness.log).toContain("setLayout:l1:visibility=visible");
  });

  it("composes overlay and part visibility", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "source.add", id: "s1", spec: { type: "geojson", data: null } },
      { op: "layer.add", id: "l1", spec: { id: "l1", type: "circle", source: "s1" } },
      {
        op: "overlay.set",
        id: "o1",
        visible: true,
        targets: [],
        parts: [{ id: "p1", visible: true, targets: [{ kind: "runtimeLayer", layerIds: ["l1"] }] }],
      },
    ]);
    expect(harness.log).toContain("setLayout:l1:visibility=visible");

    // hiding the part hides the layer even while the overlay stays visible
    harness.engine.applyOps([
      {
        op: "overlay.set",
        id: "o1",
        visible: true,
        targets: [],
        parts: [{ id: "p1", visible: false, targets: [{ kind: "runtimeLayer", layerIds: ["l1"] }] }],
      },
    ]);
    expect(harness.log).toContain("setLayout:l1:visibility=none");
  });

  it("composes hidden feature filters onto the baseline and restores it", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "source.add", id: "s1", spec: { type: "geojson", data: null } },
      { op: "layer.add", id: "l1", spec: { id: "l1", type: "circle", source: "s1", filter: ["has", "kind"] } },
      {
        op: "visibility.set",
        id: "g1",
        visible: false,
        targets: [{ kind: "styleLayerFeatures", styleId: "base", layerIds: ["l1"], filter: ["==", "type", "x"] }],
      },
    ]);

    expect(harness.log).toContain('setFilter:l1:["all",["has","kind"],["!",["==","type","x"]]]');

    harness.engine.applyOps([
      {
        op: "visibility.set",
        id: "g1",
        visible: true,
        targets: [{ kind: "styleLayerFeatures", styleId: "base", layerIds: ["l1"], filter: ["==", "type", "x"] }],
      },
    ]);
    expect(harness.log).toContain('setFilter:l1:["has","kind"]');
  });
});

describe("animated source updates", () => {
  it("interpolates point features between documents and lands exactly on the target", () => {
    const harness = createHarness();
    const initial = {
      type: "FeatureCollection",
      features: [{ type: "Feature", id: "a", geometry: { type: "Point", coordinates: [0, 0] }, properties: {} }],
    };
    const next = {
      type: "FeatureCollection",
      features: [{ type: "Feature", id: "a", geometry: { type: "Point", coordinates: [10, 0] }, properties: {} }],
    };
    harness.engine.applyOps([{ op: "source.add", id: "s1", spec: { type: "geojson", data: initial } }]);

    harness.engine.applyOps([{ op: "source.setData", id: "s1", data: next, animate: { durationMs: 100 } }]);
    const setData = harness.sources.get("s1")?.setData;

    harness.step(50);
    const midway = setData?.mock.calls.at(-1)?.[0];
    expect(midway.features[0].geometry.coordinates[0]).toBeCloseTo(5, 5);

    harness.step(100);
    const final = setData?.mock.calls.at(-1)?.[0];
    expect(final.features[0].geometry.coordinates[0]).toBe(10);
  });

  it("falls back to a plain setData when nothing can be interpolated", () => {
    const harness = createHarness();
    harness.engine.applyOps([{ op: "source.add", id: "s1", spec: { type: "geojson", data: null } }]);
    harness.engine.applyOps([
      {
        op: "source.setData",
        id: "s1",
        data: { type: "FeatureCollection", features: [] },
        animate: { durationMs: 100 },
      },
    ]);

    harness.step();
    expect(harness.sources.get("s1")?.setData).toHaveBeenCalledTimes(1);
  });
});

describe("markers", () => {
  it("dispatches marker ops to the map surface in order", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "marker.set", marker: { id: "m1", position: { latitude: 51.5, longitude: -0.09 } } },
      { op: "marker.set", marker: { id: "m2", position: { latitude: 52, longitude: 1 }, draggable: true } },
      { op: "marker.remove", id: "m1" },
    ]);

    expect(harness.log).toEqual(["setMarker:m1", "setMarker:m2", "removeMarker:m1"]);
  });
});

describe("map configuration and camera", () => {
  it("dispatches configure, resize, request policies, and transient popups", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "map.configure", config: { pitch: 30, bearing: 90, projection: "globe" } },
      { op: "map.resize" },
      { op: "map.requestPolicy", origin: "https://tiles.example", policy: "no-referrer" },
      {
        op: "popup.show",
        position: { latitude: 49.6, longitude: 6.13 },
        options: { content: "hi", contentMode: "text", trigger: "click", anchor: "auto", closeButton: true },
      },
      { op: "popup.close" },
      { op: "camera.flyTo", center: { latitude: 50, longitude: 7 }, zoom: 8 },
    ]);

    expect(harness.log).toEqual([
      "configure:globe@30/90",
      "resize",
      "requestPolicy:https://tiles.example=no-referrer",
      "showPopup:6.13,49.6",
      "closeActivePopup",
      'flyTo:{"center":[7,50],"zoom":8}',
    ]);
  });

  it("fits the viewport around markers and convenience shapes by feature id", () => {
    const harness = createHarness();
    harness.markerPositions.set("m1", { lng: 10, lat: 50 });
    harness.engine.applyOps([
      { op: "source.add", id: "sgb-circles-source", spec: { type: "geojson", data: null } },
      {
        op: "source.setData",
        id: "sgb-circles-source",
        data: {
          type: "FeatureCollection",
          features: [{ type: "Feature", id: "c1", geometry: { type: "Point", coordinates: [4, 46] }, properties: {} }],
        },
      },
      { op: "camera.fitFeatures", featureIds: ["m1", "c1", "missing"], padding: { x: 20, y: 10 } },
    ]);

    expect(harness.log.at(-1)).toBe(
      'fitBounds:[[4,46],[10,50]]:{"padding":{"top":10,"bottom":10,"left":20,"right":20}}',
    );
  });

  it("writes arbitrary feature state with optional source layers", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "source.featureState", id: "buildings", featureId: 42, state: { highlight: true }, sourceLayer: "b" },
    ]);

    expect(harness.featureStates).toEqual([{ source: "buildings", id: 42, state: { highlight: true } }]);
  });
});

describe("controls and popups", () => {
  it("dispatches control and popup ops to the map surface in order", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      {
        op: "control.set",
        control: { kind: "navigation", controlId: "nav", visible: true, position: "top-right", order: 100 },
      },
      { op: "control.content", id: "legend-1", events: { openChanged: 7 } },
      { op: "control.removeContent", id: "legend-1" },
      { op: "control.remove", id: "nav" },
      {
        op: "popup.set",
        popup: {
          id: "p1",
          position: { latitude: 49.6, longitude: 6.13 },
          options: { content: "", contentMode: "text", trigger: "click", anchor: "auto", closeButton: true },
          events: { closed: 9 },
        },
      },
      { op: "popup.remove", id: "p1" },
    ]);

    expect(harness.log).toEqual([
      "setControl:nav",
      "setControlContent:legend-1@7",
      "removeControlContent:legend-1",
      "removeControl:nav",
      "setPopup:p1",
      "removePopup:p1",
    ]);
  });
});

describe("images", () => {
  it("loads and registers images asynchronously", async () => {
    const harness = createHarness();
    harness.engine.applyOps([{ op: "image.add", id: "bus", url: "https://example.test/bus.png" }]);

    await Promise.resolve();
    await Promise.resolve();

    expect(harness.addedImages).toEqual(["bus"]);
  });
});

describe("replay", () => {
  it("re-applies the scene in canonical order after a style change", () => {
    const harness = createHarness();
    harness.engine.applyOps([
      { op: "slot.define", id: "overlay" },
      { op: "source.add", id: "s1", spec: { type: "geojson", data: null } },
      { op: "entities.create", id: "vehicles", config: {} },
      { op: "layer.add", id: "l1", spec: { id: "l1", type: "circle", source: "s1" }, slot: "overlay" },
      { op: "visibility.set", id: "g1", visible: false, targets: [{ kind: "runtimeLayer", layerIds: ["l1"] }] },
    ]);
    harness.resetLog();

    harness.engine.replay();

    expect(harness.log).toEqual([
      "addLayer:sgb-slot:overlay",
      "addSource:s1",
      "addSource:vehicles",
      "addLayer:l1@sgb-slot:overlay",
      "setLayout:l1:visibility=none",
    ]);
  });
});
