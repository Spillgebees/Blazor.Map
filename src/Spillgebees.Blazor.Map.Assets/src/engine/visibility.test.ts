import { describe, expect, it } from "vitest";
import { composeDisplayFilter, createVisibilityController, styleLayerInfo, type VisibilityHost } from "./visibility";

interface HostOptions {
  styleLayers?: ReturnType<typeof styleLayerInfo>[];
  composed?: Record<string, { layerId: string; visible: boolean }[]>;
}

const overlayStyleId = "railway";

function railwayLifecycleProposedLayer(layoutVisibility?: "visible" | "none") {
  return {
    id: "railway-lifecycle-proposed",
    type: "line",
    source: "railway",
    "source-layer": "railway_lines_lifecycle",
    minzoom: 10,
    metadata: { group: "lifecycle" },
    filter: [
      "all",
      ["==", ["get", "railway"], "proposed"],
      ["!", ["in", ["coalesce", ["get", "railway"], ""], ["literal", ["tram", "light_rail"]]]],
    ],
    layout: {
      "line-join": "round",
      "line-cap": "butt",
      ...(layoutVisibility ? { visibility: layoutVisibility } : {}),
    },
    paint: {
      "line-color": "#ca8a04",
      "line-width": 2,
      "line-opacity": 0.9,
    },
  };
}

function composedOverlayLayer(layer: ReturnType<typeof railwayLifecycleProposedLayer>) {
  return {
    layerId: `sgb-overlay-style-${overlayStyleId}-${layer.id}`,
    visible: layer.layout.visibility !== "none",
  };
}

function createHost(options: HostOptions = {}) {
  const visibilityCalls: [string, boolean][] = [];
  const filterCalls: [string, unknown][] = [];
  const styleLayers = options.styleLayers ?? [];
  const composed = options.composed ?? {};
  const knownLayers = new Set([
    ...styleLayers.map((layer) => layer.id),
    ...Object.values(composed).flatMap((layers) => layers.map((layer) => layer.layerId)),
  ]);

  const host: VisibilityHost = {
    getRuntimeLayer: () => null,
    getRuntimeBaselineFilter: () => null,
    listStyleLayers: () => styleLayers,
    resolveComposedLayer: (styleId, layerId) =>
      composed[styleId]?.find((layer) => layer.layerId.endsWith(layerId)) ?? null,
    listComposedLayers: (styleId) => composed[styleId] ?? [],
    setLayerVisibility: (layerId, visible) => visibilityCalls.push([layerId, visible]),
    setLayerFilter: (layerId, filter) => filterCalls.push([layerId, filter]),
    hasLayer: (layerId) => knownLayers.has(layerId),
  };

  return { host, visibilityCalls, filterCalls };
}

describe("composeDisplayFilter", () => {
  it("returns the baseline when nothing is hidden", () => {
    expect(composeDisplayFilter(["has", "x"], [])).toEqual(["has", "x"]);
    expect(composeDisplayFilter(null, [])).toBeNull();
  });

  it("negates hidden filters and ANDs them onto the baseline", () => {
    expect(composeDisplayFilter(["has", "x"], [["==", "t", "a"]])).toEqual([
      "all",
      ["has", "x"],
      ["!", ["==", "t", "a"]],
    ]);
    expect(composeDisplayFilter(null, [["==", "t", "a"]])).toEqual(["all", ["!", ["==", "t", "a"]]]);
  });
});

describe("styleLayerInfo", () => {
  it("reads visibility, filter, and tags from both metadata keys", () => {
    expect(
      styleLayerInfo({
        id: "parks",
        layout: { visibility: "none" },
        filter: ["has", "park"],
        metadata: { "sgb:tags": ["nature"] },
      }),
    ).toEqual({ id: "parks", visible: false, filter: ["has", "park"], tags: ["nature"] });

    expect(styleLayerInfo({ id: "rail", metadata: { tags: ["transit"] } }).tags).toEqual(["transit"]);
  });
});

describe("visibility controller", () => {
  it("resolves tag targets against style layer metadata", () => {
    const { host, visibilityCalls } = createHost({
      styleLayers: [
        styleLayerInfo({ id: "parks", metadata: { tags: ["nature"] } }),
        styleLayerInfo({ id: "rail", metadata: { "sgb:tags": ["transit"] } }),
        styleLayerInfo({ id: "roads" }),
      ],
    });
    const controller = createVisibilityController(host);

    controller.setGroup("g", false, [{ kind: "styleLayerTag", styleId: "base", tags: ["nature", "transit"] }]);

    expect(visibilityCalls).toEqual([
      ["parks", false],
      ["rail", false],
    ]);
  });

  it("resolves an empty styleLayer target to every layer of the style", () => {
    const { host, visibilityCalls } = createHost({
      styleLayers: [styleLayerInfo({ id: "a" }), styleLayerInfo({ id: "b" })],
    });
    const controller = createVisibilityController(host);

    controller.setGroup("g", false, [{ kind: "styleLayer", styleId: "base", layerIds: [] }]);

    expect(visibilityCalls.map(([id]) => id).sort()).toEqual(["a", "b"]);
  });

  it("prefers composed overlay-style layers over base style layers", () => {
    const { host, visibilityCalls } = createHost({
      composed: { railway: [{ layerId: "sgb-overlay-style-railway-tracks", visible: true }] },
    });
    const controller = createVisibilityController(host);

    controller.setGroup("g", false, [{ kind: "styleLayer", styleId: "railway", layerIds: ["tracks"] }]);

    expect(visibilityCalls).toEqual([["sgb-overlay-style-railway-tracks", false]]);
  });

  it("composes whole-style and layer-specific visibility groups", () => {
    const wholeStyle = [{ kind: "styleLayer" as const, styleId: "railway", layerIds: [] }];
    const lifecycle = [
      {
        kind: "styleLayer" as const,
        styleId: "railway",
        layerIds: ["railway-lifecycle-construction"],
      },
    ];
    const cases = [
      { wholeOn: false, lifecycleOn: true, expectedLifecycle: false, expectedSwitches: false },
      { wholeOn: true, lifecycleOn: false, expectedLifecycle: false, expectedSwitches: true },
      { wholeOn: false, lifecycleOn: false, expectedLifecycle: false, expectedSwitches: false },
      { wholeOn: true, lifecycleOn: true, expectedLifecycle: true, expectedSwitches: true },
    ];

    for (const item of cases) {
      const { host, visibilityCalls } = createHost({
        composed: {
          railway: [
            { layerId: "sgb-overlay-style-railway-railway-lifecycle-construction", visible: true },
            { layerId: "sgb-overlay-style-railway-railway-switches", visible: true },
          ],
        },
      });
      const controller = createVisibilityController(host);

      controller.setGroup("whole", item.wholeOn, wholeStyle);
      controller.setGroup("lifecycle", item.lifecycleOn, lifecycle);

      const finalVisibility = new Map(visibilityCalls);
      expect(finalVisibility.get("sgb-overlay-style-railway-railway-lifecycle-construction")).toBe(
        item.expectedLifecycle,
      );
      expect(finalVisibility.get("sgb-overlay-style-railway-railway-switches")).toBe(item.expectedSwitches);
    }
  });

  it("never shows a layer the style originally hid", () => {
    const { host, visibilityCalls } = createHost({
      styleLayers: [styleLayerInfo({ id: "hidden-by-style", layout: { visibility: "none" } })],
    });
    const controller = createVisibilityController(host);

    controller.setGroup("g", false, [{ kind: "styleLayer", styleId: "base", layerIds: ["hidden-by-style"] }]);
    controller.setGroup("g", true, [{ kind: "styleLayer", styleId: "base", layerIds: ["hidden-by-style"] }]);

    expect(visibilityCalls).toEqual([
      ["hidden-by-style", false],
      ["hidden-by-style", false],
    ]);
  });

  it("keeps a composed overlay layer hidden when its original style visibility is none", () => {
    // arrange
    const lifecycleLayer = railwayLifecycleProposedLayer("none");
    const { host, visibilityCalls } = createHost({
      composed: { [overlayStyleId]: [composedOverlayLayer(lifecycleLayer)] },
    });
    const controller = createVisibilityController(host);

    // act
    controller.setGroup("whole-overlay", true, [{ kind: "styleLayer", styleId: overlayStyleId, layerIds: [] }]);

    // assert
    expect(visibilityCalls).toEqual([["sgb-overlay-style-railway-railway-lifecycle-proposed", false]]);
  });

  it("ANDs broad composed overlay visibility with a narrower hidden lifecycle layer target", () => {
    // arrange
    const lifecycleLayer = railwayLifecycleProposedLayer();
    const { host, visibilityCalls } = createHost({
      composed: { [overlayStyleId]: [composedOverlayLayer(lifecycleLayer)] },
    });
    const controller = createVisibilityController(host);

    // act
    controller.setGroup("lifecycle", false, [
      { kind: "styleLayer", styleId: overlayStyleId, layerIds: [lifecycleLayer.id] },
    ]);
    controller.setGroup("whole-overlay", true, [{ kind: "styleLayer", styleId: overlayStyleId, layerIds: [] }]);

    // assert
    expect(new Map(visibilityCalls).get("sgb-overlay-style-railway-railway-lifecycle-proposed")).toBe(false);
  });

  it("captures and restores style layer baseline filters", () => {
    const { host, filterCalls } = createHost({
      styleLayers: [styleLayerInfo({ id: "rail", filter: ["has", "rail"] })],
    });
    const controller = createVisibilityController(host);
    const target = {
      kind: "styleLayerFeatures" as const,
      styleId: "base",
      layerIds: ["rail"],
      filter: ["==", "type", "express"],
    };

    controller.setGroup("g", false, [target]);
    controller.setGroup("g", true, [target]);

    expect(filterCalls).toEqual([
      ["rail", ["all", ["has", "rail"], ["!", ["==", "type", "express"]]]],
      ["rail", ["has", "rail"]],
    ]);
  });

  it("recaptures originals on replay", () => {
    const styleLayers = [styleLayerInfo({ id: "a", filter: ["has", "old"] })];
    const { host, filterCalls } = createHost({ styleLayers });
    const controller = createVisibilityController(host);
    const target = {
      kind: "styleLayerFeatures" as const,
      styleId: "base",
      layerIds: ["a"],
      filter: ["==", "x", 1],
    };
    controller.setGroup("g", false, [target]);

    // a style switch replaces the layer's baseline filter
    styleLayers[0] = styleLayerInfo({ id: "a", filter: ["has", "new"] });
    controller.replay();

    expect(filterCalls.at(-1)).toEqual(["a", ["all", ["has", "new"], ["!", ["==", "x", 1]]]]);
  });
});
