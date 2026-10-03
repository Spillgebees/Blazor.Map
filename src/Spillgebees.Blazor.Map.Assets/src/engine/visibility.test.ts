import { describe, expect, it } from "vitest";
import type { VisibilityTarget } from "./ops";
import {
  type ComposedLayerInfo,
  composeDisplayFilter,
  createVisibilityController,
  layerTags,
  type RuntimeLayerInfo,
  type StyleLayerInfo,
  styleLayerInfo,
  type VisibilityHost,
} from "./visibility";

const overlayStyleId = "railway";

interface HostOptions {
  baseStyleId?: string | null;
  knownStyles?: string[];
  styleLayers?: StyleLayerInfo[];
  composed?: Record<string, ComposedLayerInfo[]>;
  runtime?: RuntimeLayerInfo[];
}

function composedLayer(
  originalLayerId: string,
  options: { visible?: boolean; filter?: unknown; tags?: string[] } = {},
): ComposedLayerInfo {
  return {
    layerId: `sgb-overlay-style-${overlayStyleId}-${originalLayerId}`,
    originalLayerId,
    visible: options.visible ?? true,
    filter: options.filter ?? null,
    tags: options.tags ?? [],
  };
}

function runtimeLayer(id: string, options: Partial<Omit<RuntimeLayerInfo, "id">> = {}): RuntimeLayerInfo {
  return { id, owner: options.owner ?? null, visible: options.visible ?? true, filter: options.filter ?? null };
}

/**
 * Host whose live map state reflects the controller's writes, so tests catch any
 * attempt to read the controller's own changes back as the style's originals.
 */
function createHost(options: HostOptions = {}) {
  const visibilityCalls: [string, boolean][] = [];
  const filterCalls: [string, unknown][] = [];
  const warnings: string[] = [];
  const live = new Map<string, boolean>();
  const state = {
    baseStyleId: options.baseStyleId ?? null,
    styleLayers: options.styleLayers ?? [],
    composed: options.composed ?? {},
    runtime: options.runtime ?? [],
  };
  const knownLayers = () =>
    new Set([
      ...state.styleLayers.map((layer) => layer.id),
      ...Object.values(state.composed).flatMap((layers) => layers.map((layer) => layer.layerId)),
      ...state.runtime.map((layer) => layer.id),
    ]);

  const host: VisibilityHost = {
    listRuntimeLayers: () => state.runtime,
    getRuntimeLayer: (layerId) => state.runtime.find((layer) => layer.id === layerId) ?? null,
    listBaseStyleLayers: () =>
      state.styleLayers.map((layer) => ({ ...layer, visible: live.get(layer.id) ?? layer.visible })),
    baseStyleId: () => state.baseStyleId,
    composedStyleLayers: (styleId) => state.composed[styleId] ?? null,
    isKnownStyle: (styleId) => options.knownStyles?.includes(styleId) ?? true,
    setLayerVisibility: (layerId, visible) => {
      live.set(layerId, visible);
      visibilityCalls.push([layerId, visible]);
    },
    setLayerFilter: (layerId, filter) => filterCalls.push([layerId, filter]),
    hasLayer: (layerId) => knownLayers().has(layerId),
    warn: (message) => warnings.push(message),
  };

  const visibilityOf = (layerId: string) => new Map(visibilityCalls).get(layerId);
  /** Simulates a base style swap: the new style's layers load with their own visibility. */
  const swapStyle = (styleLayers: StyleLayerInfo[]) => {
    state.styleLayers = styleLayers;
    live.clear();
  };

  return { host, state, visibilityCalls, filterCalls, warnings, visibilityOf, swapStyle };
}

const styleLayers = (styleId: string, ...layerIds: string[]): VisibilityTarget => ({
  kind: "styleLayers",
  styleId,
  layerIds,
});

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
  it("reads visibility, filter, and tags", () => {
    expect(
      styleLayerInfo({
        id: "parks",
        layout: { visibility: "none" },
        filter: ["has", "park"],
        metadata: { "sgb:tags": ["nature"] },
      }),
    ).toEqual({ id: "parks", visible: false, filter: ["has", "park"], tags: ["nature"] });
  });

  it("prefers sgb:tags and falls back to tags", () => {
    expect(layerTags({ tags: ["transit"] })).toEqual(["transit"]);
    expect(layerTags({ "sgb:tags": ["a"], tags: ["b"] })).toEqual(["a"]);
    expect(layerTags(undefined)).toEqual([]);
  });
});

describe("visibility controller", () => {
  describe("explicit items", () => {
    it("hides the layers of an item that is off and restores them when it turns on", () => {
      const { host, visibilityCalls } = createHost({ styleLayers: [styleLayerInfo({ id: "roads" })] });
      const controller = createVisibilityController(host);

      controller.setGroup("g", false, [styleLayers("base", "roads")]);
      controller.setGroup("g", true, [styleLayers("base", "roads")]);

      expect(visibilityCalls).toEqual([
        ["roads", false],
        ["roads", true],
      ]);
    });

    it("shows a layer the style hides when an item naming it is on", () => {
      const { host, visibilityOf } = createHost({
        styleLayers: [styleLayerInfo({ id: "tram", layout: { visibility: "none" } })],
      });
      const controller = createVisibilityController(host);

      controller.setGroup("tram", true, [styleLayers("base", "tram")]);
      expect(visibilityOf("tram")).toBe(true);

      controller.setGroup("tram", false, [styleLayers("base", "tram")]);
      expect(visibilityOf("tram")).toBe(false);
    });

    it("shows a composed overlay layer its style hides when an item naming it is on", () => {
      const { host, visibilityOf } = createHost({
        composed: { [overlayStyleId]: [composedLayer("tram-line-fill", { visible: false })] },
      });
      const controller = createVisibilityController(host);

      controller.setGroup("tram", true, [styleLayers(overlayStyleId, "tram-line-fill")]);

      expect(visibilityOf("sgb-overlay-style-railway-tram-line-fill")).toBe(true);
    });

    it("lets an item that is off win over one that is on", () => {
      const { host, visibilityOf } = createHost({
        styleLayers: [styleLayerInfo({ id: "tram", layout: { visibility: "none" } })],
      });
      const controller = createVisibilityController(host);

      controller.setGroup("show", true, [styleLayers("base", "tram")]);
      controller.setGroup("hide", false, [styleLayers("base", "tram")]);
      expect(visibilityOf("tram")).toBe(false);

      controller.setGroup("hide", true, [styleLayers("base", "tram")]);
      expect(visibilityOf("tram")).toBe(true);
    });

    it("returns a layer to the style's visibility when its item is removed", () => {
      const { host, visibilityOf } = createHost({
        styleLayers: [styleLayerInfo({ id: "tram", layout: { visibility: "none" } }), styleLayerInfo({ id: "roads" })],
      });
      const controller = createVisibilityController(host);

      controller.setGroup("tram", true, [styleLayers("base", "tram")]);
      controller.setGroup("roads", false, [styleLayers("base", "roads")]);
      controller.removeGroup("tram");
      controller.removeGroup("roads");

      expect(visibilityOf("tram")).toBe(false);
      expect(visibilityOf("roads")).toBe(true);
    });
  });

  describe("targets", () => {
    it("shows every layer carrying a tag, including ones the style hides", () => {
      const { host, visibilityOf } = createHost({
        composed: {
          [overlayStyleId]: [
            composedLayer("tram-line-fill", { visible: false, tags: ["tram", "active"] }),
            composedLayer("tram-lifecycle-fill", { visible: false, tags: ["tram", "construction"] }),
            composedLayer("railway-line-main", { tags: ["heavy_rail", "active"] }),
          ],
        },
      });
      const controller = createVisibilityController(host);

      controller.setGroup("tram", true, [{ kind: "styleTags", styleId: overlayStyleId, tags: ["tram"] }]);

      expect(visibilityOf("sgb-overlay-style-railway-tram-line-fill")).toBe(true);
      expect(visibilityOf("sgb-overlay-style-railway-tram-lifecycle-fill")).toBe(true);
      expect(visibilityOf("sgb-overlay-style-railway-railway-line-main")).toBeUndefined();
    });

    it("lets a layer item that is off override a tag item that is on", () => {
      const { host, visibilityOf } = createHost({
        composed: {
          [overlayStyleId]: [
            composedLayer("tram-line-fill", { visible: false, tags: ["tram"] }),
            composedLayer("tram-debug-labels", { visible: false, tags: ["tram"] }),
          ],
        },
      });
      const controller = createVisibilityController(host);

      controller.setGroup("tram", true, [{ kind: "styleTags", styleId: overlayStyleId, tags: ["tram"] }]);
      controller.setGroup("debug", false, [styleLayers(overlayStyleId, "tram-debug-labels")]);

      expect(visibilityOf("sgb-overlay-style-railway-tram-line-fill")).toBe(true);
      expect(visibilityOf("sgb-overlay-style-railway-tram-debug-labels")).toBe(false);
    });

    it("hides a whole style but never shows the layers it hides", () => {
      const { host, visibilityOf } = createHost({
        composed: {
          [overlayStyleId]: [composedLayer("tracks"), composedLayer("debug", { visible: false })],
        },
      });
      const controller = createVisibilityController(host);

      controller.setGroup("overlay", true, [{ kind: "style", styleId: overlayStyleId }]);
      expect(visibilityOf("sgb-overlay-style-railway-tracks")).toBe(true);
      expect(visibilityOf("sgb-overlay-style-railway-debug")).toBe(false);

      controller.setGroup("overlay", false, [{ kind: "style", styleId: overlayStyleId }]);
      expect(visibilityOf("sgb-overlay-style-railway-tracks")).toBe(false);
    });

    it("composes a whole-style switch with layer items", () => {
      const cases = [
        { wholeOn: false, lifecycleOn: true, expectedLifecycle: false, expectedSwitches: false },
        { wholeOn: true, lifecycleOn: false, expectedLifecycle: false, expectedSwitches: true },
        { wholeOn: false, lifecycleOn: false, expectedLifecycle: false, expectedSwitches: false },
        { wholeOn: true, lifecycleOn: true, expectedLifecycle: true, expectedSwitches: true },
      ];

      for (const item of cases) {
        const { host, visibilityOf } = createHost({
          composed: { [overlayStyleId]: [composedLayer("lifecycle"), composedLayer("switches")] },
        });
        const controller = createVisibilityController(host);

        controller.setGroup("whole", item.wholeOn, [{ kind: "style", styleId: overlayStyleId }]);
        controller.setGroup("lifecycle", item.lifecycleOn, [styleLayers(overlayStyleId, "lifecycle")]);

        expect(visibilityOf("sgb-overlay-style-railway-lifecycle")).toBe(item.expectedLifecycle);
        expect(visibilityOf("sgb-overlay-style-railway-switches")).toBe(item.expectedSwitches);
      }
    });

    it("resolves runtime layers by their own id or by the component that created them", () => {
      const { host, visibilityOf } = createHost({
        runtime: [
          runtimeLayer("trains-symbols", { owner: "trains" }),
          runtimeLayer("trains-decoration-route", { owner: "trains" }),
          runtimeLayer("buildings"),
        ],
      });
      const controller = createVisibilityController(host);

      controller.setGroup("trains", false, [{ kind: "layers", ids: ["trains", "buildings"] }]);

      expect(visibilityOf("trains-symbols")).toBe(false);
      expect(visibilityOf("trains-decoration-route")).toBe(false);
      expect(visibilityOf("buildings")).toBe(false);
    });

    it("resolves style targets against the named base style only", () => {
      const { host, visibilityOf } = createHost({
        baseStyleId: "base",
        styleLayers: [styleLayerInfo({ id: "roads" })],
      });
      const controller = createVisibilityController(host);

      controller.setGroup("other", false, [styleLayers("elsewhere", "roads")]);
      expect(visibilityOf("roads")).toBeUndefined();

      controller.setGroup("base", false, [styleLayers("base", "roads")]);
      expect(visibilityOf("roads")).toBe(false);
    });

    it("warns once about layer ids a loaded style does not contain", () => {
      const { host, warnings } = createHost({ composed: { [overlayStyleId]: [composedLayer("tracks")] } });
      const controller = createVisibilityController(host);

      controller.setGroup("g", false, [styleLayers(overlayStyleId, "tracks", "missing")]);
      controller.setGroup("g", true, [styleLayers(overlayStyleId, "tracks", "missing")]);

      expect(warnings).toEqual([
        "[Spillgebees.Map] Display item 'g' targets layer 'missing', which style 'railway' does not contain.",
      ]);
    });

    it("warns once about style ids the map was not configured with", () => {
      const { host, warnings } = createHost({ baseStyleId: "sgb-positron", knownStyles: ["sgb-positron"] });
      const controller = createVisibilityController(host);

      controller.setGroup("g", false, [styleLayers("positron", "roads")]);
      controller.setGroup("g", true, [styleLayers("positron", "roads")]);

      expect(warnings).toEqual([
        "[Spillgebees.Map] Display item 'g' targets style 'positron', which is not one of the map's styles.",
      ]);
    });

    it("does not warn about styles that are not composed yet", () => {
      const { host, warnings } = createHost({ baseStyleId: "base" });
      const controller = createVisibilityController(host);

      controller.setGroup("g", false, [styleLayers(overlayStyleId, "tracks")]);

      expect(warnings).toEqual([]);
    });
  });

  describe("unset items", () => {
    it("leave the style's visibility alone", () => {
      const { host, visibilityCalls } = createHost({
        styleLayers: [styleLayerInfo({ id: "tram", layout: { visibility: "none" } }), styleLayerInfo({ id: "roads" })],
      });
      const controller = createVisibilityController(host);

      controller.setGroup("tram", null, [styleLayers("base", "tram")]);
      controller.setGroup("roads", null, [styleLayers("base", "roads")]);

      expect(new Map(visibilityCalls)).toEqual(
        new Map([
          ["tram", false],
          ["roads", true],
        ]),
      );
    });

    it("report the style default: on when any targeted layer is visible", () => {
      const { host } = createHost({
        styleLayers: [
          styleLayerInfo({ id: "tram", layout: { visibility: "none" } }),
          styleLayerInfo({ id: "service", layout: { visibility: "none" } }),
          styleLayerInfo({ id: "main" }),
        ],
      });
      const controller = createVisibilityController(host);

      controller.setGroup("tram", null, [styleLayers("base", "tram")]);
      controller.setGroup("tracks", null, [styleLayers("base", "main", "service")]);
      controller.setGroup("explicit", true, [styleLayers("base", "main")]);
      controller.setGroup("unresolved", null, [styleLayers("base", "nope")]);

      expect(controller.takeDefaults()).toEqual({ tram: false, tracks: true });
    });

    it("report defaults only when they change", () => {
      const { host } = createHost({ styleLayers: [styleLayerInfo({ id: "tram", layout: { visibility: "none" } })] });
      const controller = createVisibilityController(host);

      expect(controller.takeDefaults()).toBeNull();

      controller.setGroup("tram", null, [styleLayers("base", "tram")]);
      expect(controller.takeDefaults()).toEqual({ tram: false });

      controller.setGroup("tram", null, [styleLayers("base", "tram")]);
      expect(controller.takeDefaults()).toBeNull();

      controller.setGroup("tram", true, [styleLayers("base", "tram")]);
      expect(controller.takeDefaults()).toEqual({});
    });

    it("keep reporting the style default after the item's layers were shown", () => {
      const { host } = createHost({ styleLayers: [styleLayerInfo({ id: "tram", layout: { visibility: "none" } })] });
      const controller = createVisibilityController(host);

      controller.setGroup("show", true, [styleLayers("base", "tram")]);
      controller.setGroup("tram", null, [styleLayers("base", "tram")]);

      expect(controller.takeDefaults()).toEqual({ tram: false });
    });
  });

  describe("feature filters", () => {
    it("captures and restores style layer baseline filters", () => {
      const { host, filterCalls, visibilityCalls } = createHost({
        styleLayers: [styleLayerInfo({ id: "rail", filter: ["has", "rail"] })],
      });
      const controller = createVisibilityController(host);
      const target: VisibilityTarget = { ...styleLayers("base", "rail"), filter: ["==", "type", "express"] };

      controller.setGroup("g", false, [target]);
      controller.setGroup("g", true, [target]);

      expect(filterCalls).toEqual([
        ["rail", ["all", ["has", "rail"], ["!", ["==", "type", "express"]]]],
        ["rail", ["has", "rail"]],
      ]);
      expect(visibilityCalls).toEqual([]);
    });

    it("never shows a layer the style hides", () => {
      const { host, visibilityCalls } = createHost({
        styleLayers: [styleLayerInfo({ id: "rail", layout: { visibility: "none" } })],
      });
      const controller = createVisibilityController(host);

      controller.setGroup("g", true, [{ ...styleLayers("base", "rail"), filter: ["==", "type", "x"] }]);

      expect(visibilityCalls).toEqual([]);
    });

    it("composes onto composed overlay and runtime layer baselines", () => {
      const filter = ["==", ["get", "status"], "planned"];
      const { host, filterCalls } = createHost({
        composed: { [overlayStyleId]: [composedLayer("lifecycle", { filter: ["has", "railway"] })] },
        runtime: [runtimeLayer("trains-symbols", { owner: "trains", filter: ["has", "id"] })],
      });
      const controller = createVisibilityController(host);

      controller.setGroup("planned", false, [
        { ...styleLayers(overlayStyleId, "lifecycle"), filter },
        { kind: "layers", ids: ["trains"], filter },
      ]);

      expect(filterCalls).toEqual([
        ["sgb-overlay-style-railway-lifecycle", ["all", ["has", "railway"], ["!", filter]]],
        ["trains-symbols", ["all", ["has", "id"], ["!", filter]]],
      ]);
    });
  });

  describe("replay", () => {
    it("keeps base-style originals when only the overlay styles changed", () => {
      const { host, visibilityOf } = createHost({ styleLayers: [styleLayerInfo({ id: "roads" })] });
      const controller = createVisibilityController(host);

      controller.setGroup("g", false, [styleLayers("base", "roads")]);
      controller.replay(false);
      controller.setGroup("g", true, [styleLayers("base", "roads")]);

      expect(visibilityOf("roads")).toBe(true);
    });

    it("re-snapshots originals after a base style change", () => {
      const { host, swapStyle, filterCalls, visibilityOf } = createHost({
        styleLayers: [styleLayerInfo({ id: "a", filter: ["has", "old"] })],
      });
      const controller = createVisibilityController(host);
      const target: VisibilityTarget = { ...styleLayers("base", "a"), filter: ["==", "x", 1] };
      controller.setGroup("g", false, [target]);
      controller.setGroup("h", null, [styleLayers("base", "a")]);

      swapStyle([styleLayerInfo({ id: "a", filter: ["has", "new"], layout: { visibility: "none" } })]);
      controller.replay(true);

      expect(filterCalls.at(-1)).toEqual(["a", ["all", ["has", "new"], ["!", ["==", "x", 1]]]]);
      expect(visibilityOf("a")).toBe(false);
      controller.takeDefaults();
      controller.setGroup("h", true, [styleLayers("base", "a")]);
      expect(visibilityOf("a")).toBe(true);
    });

    it("applies items to composed layers that appear later", () => {
      const { host, state, visibilityOf } = createHost();
      const controller = createVisibilityController(host);
      controller.setGroup("tram", true, [styleLayers(overlayStyleId, "tram-line-fill")]);

      state.composed[overlayStyleId] = [composedLayer("tram-line-fill", { visible: false })];
      controller.replay(false);

      expect(visibilityOf("sgb-overlay-style-railway-tram-line-fill")).toBe(true);
    });

    it("applies items to runtime layers added later", () => {
      const { host, state, visibilityOf } = createHost();
      const controller = createVisibilityController(host);
      controller.setGroup("trains", false, [{ kind: "layers", ids: ["trains"] }]);

      state.runtime.push(runtimeLayer("trains-symbols", { owner: "trains" }));
      controller.onLayerAdded("trains-symbols");

      expect(visibilityOf("trains-symbols")).toBe(false);
    });
  });
});
