import { beforeEach, describe, expect, it, vi } from "vitest";
import { applyComposedStyles, resolveLayerSlot, validateComposedGlyphs } from "./composition";

function createMockMap(glyphs?: string) {
  return {
    getStyle: vi.fn().mockReturnValue({ layers: [], glyphs: glyphs ?? null }),
  } as unknown as Parameters<typeof validateComposedGlyphs>[0];
}

describe("validateComposedGlyphs", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it("should return proceed with no rewrite when overlay list is empty", async () => {
    // arrange
    const map = createMockMap("https://fonts.example.com/{fontstack}/{range}.pbf");

    // act
    const result = await validateComposedGlyphs(map, [], null);

    // assert
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should return effective glyph URL when composedGlyphsUrl differs from base style glyphs", async () => {
    // arrange
    const baseGlyphs = "https://fonts-a.example.com/{fontstack}/{range}.pbf";
    const composedGlyphsUrl = "https://fonts-shared.example.com/{fontstack}/{range}.pbf";
    const map = createMockMap(baseGlyphs);

    // act
    const result = await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: "https://example.com/overlay.json", referrerPolicy: null }],
      composedGlyphsUrl,
    );

    // assert
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: composedGlyphsUrl });
  });

  it("should return null effective glyph URL when composedGlyphsUrl matches base style glyphs", async () => {
    // arrange
    const sharedGlyphs = "https://fonts.example.com/{fontstack}/{range}.pbf";
    const map = createMockMap(sharedGlyphs);

    // act
    const result = await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: "https://example.com/overlay.json", referrerPolicy: null }],
      sharedGlyphs,
    );

    // assert
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should return proceed true when all styles share the same glyph URL", async () => {
    // arrange
    const sharedGlyphs = "https://fonts.example.com/{fontstack}/{range}.pbf";
    const map = createMockMap(sharedGlyphs);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        url: "https://example.com/overlay.json",
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [],
          glyphs: sharedGlyphs,
        }),
      }),
    );

    // act
    const result = await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: "https://example.com/overlay.json", referrerPolicy: null }],
      null,
    );

    // assert
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should return proceed false and warn when styles have conflicting glyph URLs", async () => {
    // arrange
    const baseGlyphs = "https://fonts-a.example.com/{fontstack}/{range}.pbf";
    const overlayGlyphs = "https://fonts-b.example.com/{fontstack}/{range}.pbf";
    const map = createMockMap(baseGlyphs);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        url: "https://example.com/overlay.json",
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [],
          glyphs: overlayGlyphs,
        }),
      }),
    );
    const warnSpy = vi.spyOn(console, "warn").mockImplementation(() => {});

    // act
    const result = await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: "https://example.com/overlay.json", referrerPolicy: null }],
      null,
    );

    // assert
    expect(result).toEqual({ proceed: false });
    expect(warnSpy).toHaveBeenCalledWith(
      expect.stringContaining("Composed map styles require a single shared glyph endpoint"),
    );
    expect(warnSpy).toHaveBeenCalledWith(expect.stringContaining(baseGlyphs));
    expect(warnSpy).toHaveBeenCalledWith(expect.stringContaining(overlayGlyphs));
  });

  it("should resolve relative glyph URLs against the overlay style URL", async () => {
    // arrange
    const baseGlyphs = "https://example.com/fonts/{fontstack}/{range}.pbf";
    const map = createMockMap(baseGlyphs);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        url: "https://example.com/styles/overlay.json",
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [],
          glyphs: "../fonts/{fontstack}/{range}.pbf",
        }),
      }),
    );

    // act
    const result = await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: "https://example.com/styles/overlay.json", referrerPolicy: null }],
      null,
    );

    // assert — ../fonts/ relative to /styles/overlay.json resolves to /fonts/
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should resolve relative glyph URLs against the final redirect URL, not the original request URL", async () => {
    // arrange
    const baseGlyphs = "https://cdn.example.com/v2/fonts/{fontstack}/{range}.pbf";
    const map = createMockMap(baseGlyphs);
    const originalUrl = "https://example.com/style.json";
    const redirectedUrl = "https://cdn.example.com/v2/style.json";
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        url: redirectedUrl,
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [],
          glyphs: "./fonts/{fontstack}/{range}.pbf",
        }),
      }),
    );

    // act
    const result = await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: originalUrl, referrerPolicy: null }],
      null,
    );

    // assert — ./fonts/ relative to redirectedUrl resolves to https://cdn.example.com/v2/fonts/...
    // if resolved against originalUrl it would be https://example.com/fonts/... which differs from baseGlyphs
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should proceed when overlay fetch fails", async () => {
    // arrange
    const baseGlyphs = "https://fonts.example.com/{fontstack}/{range}.pbf";
    const map = createMockMap(baseGlyphs);
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: false, status: 404 }));

    // act
    const result = await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: "https://example.com/overlay.json", referrerPolicy: null }],
      null,
    );

    // assert — only base glyph URL in set (1 unique), so proceed
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should proceed when overlay fetch throws", async () => {
    // arrange
    const baseGlyphs = "https://fonts.example.com/{fontstack}/{range}.pbf";
    const map = createMockMap(baseGlyphs);
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new Error("network failure")));

    // act
    const result = await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: "https://example.com/overlay.json", referrerPolicy: null }],
      null,
    );

    // assert — only base glyph URL in set (1 unique), so proceed
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should proceed when no styles define glyphs", async () => {
    // arrange
    const map = createMockMap(undefined);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        url: "https://example.com/overlay.json",
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [],
        }),
      }),
    );

    // act
    const result = await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: "https://example.com/overlay.json", referrerPolicy: null }],
      null,
    );

    // assert — no glyph URLs at all, so proceed
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should detect conflict when only overlay defines glyphs and base does not", async () => {
    // arrange
    const map = createMockMap(undefined);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockImplementation((url: string) => {
        if (url === "https://example.com/overlay-a.json") {
          return Promise.resolve({
            ok: true,
            url: "https://example.com/overlay-a.json",
            json: () => Promise.resolve({ glyphs: "https://fonts-a.example.com/glyphs" }),
          });
        }
        if (url === "https://example.com/overlay-b.json") {
          return Promise.resolve({
            ok: true,
            url: "https://example.com/overlay-b.json",
            json: () => Promise.resolve({ glyphs: "https://fonts-b.example.com/glyphs" }),
          });
        }
        return Promise.resolve({ ok: false });
      }),
    );
    const warnSpy = vi.spyOn(console, "warn").mockImplementation(() => {});

    // act
    const result = await validateComposedGlyphs(
      map,
      [
        { styleId: "overlay-a", url: "https://example.com/overlay-a.json", referrerPolicy: null },
        { styleId: "overlay-b", url: "https://example.com/overlay-b.json", referrerPolicy: null },
      ],
      null,
    );

    // assert — two different overlay glyph URLs, no base
    expect(result).toEqual({ proceed: false });
    expect(warnSpy).toHaveBeenCalled();
  });

  it("should pass referrerPolicy to overlay style fetches when configured", async () => {
    // arrange
    const map = createMockMap("https://fonts.example.com/{fontstack}/{range}.pbf");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      url: "https://example.com/overlay.json",
      json: vi.fn().mockResolvedValue({
        version: 8,
        sources: {},
        layers: [],
        glyphs: "https://fonts.example.com/{fontstack}/{range}.pbf",
      }),
    });
    vi.stubGlobal("fetch", fetchMock);

    // act
    await validateComposedGlyphs(
      map,
      [{ styleId: "overlay", url: "https://example.com/overlay.json", referrerPolicy: "origin" }],
      null,
    );

    // assert
    expect(fetchMock).toHaveBeenCalledWith("https://example.com/overlay.json", { referrerPolicy: "origin" });
  });

  it("should preserve per-style referrer policies across multiple overlay fetches", async () => {
    // arrange
    const map = createMockMap("https://fonts.example.com/{fontstack}/{range}.pbf");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      url: "https://example.com/a.json",
      json: vi.fn().mockResolvedValue({
        version: 8,
        sources: {},
        layers: [],
        glyphs: "https://fonts.example.com/{fontstack}/{range}.pbf",
      }),
    });
    vi.stubGlobal("fetch", fetchMock);

    // act
    await validateComposedGlyphs(
      map,
      [
        { styleId: "overlay-a", url: "https://example.com/a.json", referrerPolicy: "origin" },
        { styleId: "overlay-b", url: "https://example.com/b.json", referrerPolicy: "no-referrer" },
      ],
      null,
    );

    // assert
    expect(fetchMock).toHaveBeenNthCalledWith(1, "https://example.com/a.json", { referrerPolicy: "origin" });
    expect(fetchMock).toHaveBeenNthCalledWith(2, "https://example.com/b.json", { referrerPolicy: "no-referrer" });
  });
});

describe("applyComposedStyles", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    window.Spillgebees = {
      Map: {
        composedStyleLayerIds: new Map(),
      },
    } as never;
  });

  it("should resolve relative source URLs against the final redirect URL, not the original request URL", async () => {
    // arrange
    const originalUrl = "https://example.com/style.json";
    const redirectedUrl = "https://cdn.example.com/v2/style.json";
    const map = {
      getSource: vi.fn().mockReturnValue(undefined),
      addSource: vi.fn(),
      hasImage: vi.fn().mockReturnValue(true),
      getLayer: vi.fn().mockReturnValue(undefined),
      addLayer: vi.fn(),
    } as unknown as Parameters<typeof applyComposedStyles>[0];
    window.Spillgebees.Map.composedStyleLayerIds.set(map, new Map());
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        url: redirectedUrl,
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {
            "my-source": {
              type: "vector",
              url: "./tiles.json",
            },
          },
          layers: [],
        }),
      }),
    );

    // act
    await applyComposedStyles(map, [{ styleId: "overlay", url: originalUrl, referrerPolicy: null }]);

    // assert — ./tiles.json relative to redirectedUrl should resolve to https://cdn.example.com/v2/tiles.json
    // if resolved against originalUrl, it would be https://example.com/tiles.json (wrong)
    expect(map.addSource).toHaveBeenCalledWith(
      "sgb-overlay-style-overlay-my-source",
      expect.objectContaining({
        url: "https://cdn.example.com/v2/tiles.json",
      }),
    );
  });

  it("should fetch each overlay style using its own referrer policy", async () => {
    // arrange
    const map = {
      getSource: vi.fn().mockReturnValue(undefined),
      addSource: vi.fn(),
      hasImage: vi.fn().mockReturnValue(true),
      getLayer: vi.fn().mockReturnValue(undefined),
      addLayer: vi.fn(),
    } as unknown as Parameters<typeof applyComposedStyles>[0];
    window.Spillgebees.Map.composedStyleLayerIds.set(map, new Map());
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        url: "https://example.com/a.json",
        json: vi.fn().mockResolvedValue({ version: 8, sources: {}, layers: [] }),
      })
      .mockResolvedValueOnce({
        ok: true,
        url: "https://example.com/b.json",
        json: vi.fn().mockResolvedValue({ version: 8, sources: {}, layers: [] }),
      });
    vi.stubGlobal("fetch", fetchMock);

    // act
    await applyComposedStyles(map, [
      { styleId: "a", url: "https://example.com/a.json", referrerPolicy: "origin" },
      { styleId: "b", url: "https://example.com/b.json", referrerPolicy: "no-referrer" },
    ]);

    // assert
    expect(fetchMock).toHaveBeenNthCalledWith(1, "https://example.com/a.json", { referrerPolicy: "origin" });
    expect(fetchMock).toHaveBeenNthCalledWith(2, "https://example.com/b.json", { referrerPolicy: "no-referrer" });
  });

  it("should register composed layer baseline filters without cloning", async () => {
    // arrange
    const map = {
      getSource: vi.fn().mockReturnValue(undefined),
      addSource: vi.fn(),
      hasImage: vi.fn().mockReturnValue(true),
      getLayer: vi.fn().mockReturnValue(undefined),
      addLayer: vi.fn(),
    } as unknown as Parameters<typeof applyComposedStyles>[0];
    window.Spillgebees.Map.composedStyleLayerIds.set(map, new Map());
    const filter = ["==", ["get", "railway"], "proposed"];
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        url: "https://example.com/railway.json",
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [{ id: "railway-lifecycle-proposed", type: "line", filter }],
        }),
      }),
    );

    // act
    await applyComposedStyles(map, [
      { styleId: "railway", url: "https://example.com/railway.json", referrerPolicy: null },
    ]);

    // assert
    expect(
      window.Spillgebees.Map.composedStyleLayerIds.get(map)?.get("railway\u0000railway-lifecycle-proposed")
        ?.originalFilter,
    ).toBe(filter);
  });

  it("should register original visibility and tags from the style JSON", async () => {
    // arrange
    const map = {
      getSource: vi.fn().mockReturnValue(undefined),
      addSource: vi.fn(),
      hasImage: vi.fn().mockReturnValue(true),
      getLayer: vi.fn().mockReturnValue(undefined),
      addLayer: vi.fn(),
    } as unknown as Parameters<typeof applyComposedStyles>[0];
    window.Spillgebees.Map.composedStyleLayerIds.set(map, new Map());
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        url: "https://example.com/railway.json",
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [
            {
              id: "tram-line-fill",
              type: "line",
              layout: { visibility: "none" },
              metadata: { "sgb:tags": ["tram", "active"] },
            },
          ],
        }),
      }),
    );

    // act
    await applyComposedStyles(map, [
      { styleId: "railway", url: "https://example.com/railway.json", referrerPolicy: null },
    ]);

    // assert
    const registration = window.Spillgebees.Map.composedStyleLayerIds.get(map)?.get("railway\u0000tram-line-fill");
    expect(registration?.originalVisible).toBe(false);
    expect(registration?.tags).toEqual(["tram", "active"]);
  });

  it("should reapply an existing style when forced so changed slot options can move layers", async () => {
    // arrange
    const layers = new Set(["sgb-slot:sgb:composed-ground", "sgb-slot:sgb:composed-labels"]);
    const map = {
      getSource: vi.fn().mockReturnValue(undefined),
      addSource: vi.fn(),
      removeSource: vi.fn(),
      hasImage: vi.fn().mockReturnValue(true),
      removeImage: vi.fn(),
      getLayer: vi.fn((id: string) => (layers.has(id) ? { id } : undefined)),
      addLayer: vi.fn((layer: { id: string }) => {
        layers.add(layer.id);
      }),
      removeLayer: vi.fn((id: string) => {
        layers.delete(id);
      }),
    } as unknown as Parameters<typeof applyComposedStyles>[0];
    window.Spillgebees.Map.composedStyleLayerIds.set(map, new Map());
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      url: "https://example.com/rail.json",
      json: vi.fn().mockResolvedValue({
        version: 8,
        sources: {},
        layers: [{ id: "station-dots", type: "circle" }],
      }),
    });
    vi.stubGlobal("fetch", fetchMock);

    // act
    await applyComposedStyles(map, [
      { styleId: "rail", url: "https://example.com/rail.json", referrerPolicy: null, slot: "below-labels" },
    ]);
    await applyComposedStyles(
      map,
      [{ styleId: "rail", url: "https://example.com/rail.json", referrerPolicy: null, slot: "above-labels" }],
      { forceReapply: true },
    );

    // assert: the second apply reuses the cached style JSON instead of refetching
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(map.removeLayer).toHaveBeenCalledWith("sgb-overlay-style-rail-station-dots");
    expect(map.addLayer).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({ id: "sgb-overlay-style-rail-station-dots" }),
      "sgb-slot:sgb:composed-ground",
    );
    expect(map.addLayer).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({ id: "sgb-overlay-style-rail-station-dots" }),
      "sgb-slot:sgb:composed-labels",
    );
  });
});

describe("resolveLayerSlot", () => {
  it("puts symbol layers above labels and everything else below by default", () => {
    expect(resolveLayerSlot({ id: "labels", type: "symbol" }, {})).toBe("above-labels");
    expect(resolveLayerSlot({ id: "tracks", type: "line" }, {})).toBe("below-labels");
    expect(resolveLayerSlot({ id: "platforms", type: "fill" }, {})).toBe("below-labels");
    expect(resolveLayerSlot({ id: "stations", type: "circle" }, {})).toBe("below-labels");
  });

  it("applies the style's slot to every layer", () => {
    expect(resolveLayerSlot({ id: "tracks", type: "line" }, { slot: "above-labels" })).toBe("above-labels");
    expect(resolveLayerSlot({ id: "labels", type: "symbol" }, { slot: "below-labels" })).toBe("below-labels");
  });

  it("lets sgb:slot metadata beat the style's slot", () => {
    const layer = { id: "stations", type: "circle", metadata: { "sgb:slot": "above-labels" } };
    expect(resolveLayerSlot(layer, {})).toBe("above-labels");
    expect(resolveLayerSlot(layer, { slot: "below-labels" })).toBe("above-labels");
  });

  it("lets per-layer slots beat sgb:slot metadata", () => {
    const layer = { id: "stations", type: "circle", metadata: { "sgb:slot": "above-labels" } };
    expect(resolveLayerSlot(layer, { layerSlots: { stations: "below-labels" } })).toBe("below-labels");
  });

  it("ignores unknown slot values", () => {
    const layer = { id: "tracks", type: "line", metadata: { "sgb:slot": "everywhere" } };
    expect(resolveLayerSlot(layer, { layerSlots: { tracks: "nope" as never } })).toBe("below-labels");
  });
});

describe("composed style layer slots", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    window.Spillgebees = {
      Map: {
        composedStyleLayerIds: new Map(),
      },
    } as never;
  });

  function createSlottedMap(anchorIds: string[]) {
    return {
      getSource: vi.fn().mockReturnValue(undefined),
      addSource: vi.fn(),
      hasImage: vi.fn().mockReturnValue(true),
      getLayer: vi.fn((id: string) => (anchorIds.includes(id) ? { id } : undefined)),
      addLayer: vi.fn(),
    } as unknown as Parameters<typeof applyComposedStyles>[0];
  }

  function stubOverlayFetch(layers: Record<string, unknown>[]): void {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        url: "https://example.com/style.json",
        json: vi.fn().mockResolvedValue({ version: 8, sources: {}, layers }),
      }),
    );
  }

  it("inserts below-labels layers before the composed ground anchor and above-labels layers before the composed labels anchor", async () => {
    // arrange
    const map = createSlottedMap(["sgb-slot:sgb:composed-ground", "sgb-slot:sgb:composed-labels"]);
    window.Spillgebees.Map.composedStyleLayerIds.set(map, new Map());
    stubOverlayFetch([
      { id: "tracks", type: "line" },
      { id: "platforms", type: "fill" },
      { id: "station-labels", type: "symbol" },
    ]);

    // act
    await applyComposedStyles(map, [{ styleId: "rail", url: "https://example.com/style.json", referrerPolicy: null }]);

    // assert: each slot keeps the style's own order because every layer
    // targets the same fixed anchor
    expect(map.addLayer).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({ id: "sgb-overlay-style-rail-tracks" }),
      "sgb-slot:sgb:composed-ground",
    );
    expect(map.addLayer).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({ id: "sgb-overlay-style-rail-platforms" }),
      "sgb-slot:sgb:composed-ground",
    );
    expect(map.addLayer).toHaveBeenNthCalledWith(
      3,
      expect.objectContaining({ id: "sgb-overlay-style-rail-station-labels" }),
      "sgb-slot:sgb:composed-labels",
    );
  });

  it("honours per-layer slots from the request", async () => {
    // arrange
    const map = createSlottedMap(["sgb-slot:sgb:composed-ground", "sgb-slot:sgb:composed-labels"]);
    window.Spillgebees.Map.composedStyleLayerIds.set(map, new Map());
    stubOverlayFetch([{ id: "station-dots", type: "circle" }]);

    // act
    await applyComposedStyles(map, [
      {
        styleId: "rail",
        url: "https://example.com/style.json",
        referrerPolicy: null,
        layerSlots: { "station-dots": "above-labels" },
      },
    ]);

    // assert
    expect(map.addLayer).toHaveBeenCalledWith(
      expect.objectContaining({ id: "sgb-overlay-style-rail-station-dots" }),
      "sgb-slot:sgb:composed-labels",
    );
  });

  it("appends layers when the slot anchors are missing", async () => {
    // arrange
    const map = createSlottedMap([]);
    window.Spillgebees.Map.composedStyleLayerIds.set(map, new Map());
    stubOverlayFetch([
      { id: "tracks", type: "line" },
      { id: "station-labels", type: "symbol" },
    ]);

    // act
    await applyComposedStyles(map, [{ styleId: "rail", url: "https://example.com/style.json", referrerPolicy: null }]);

    // assert
    expect(map.addLayer).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({ id: "sgb-overlay-style-rail-tracks" }),
      undefined,
    );
    expect(map.addLayer).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({ id: "sgb-overlay-style-rail-station-labels" }),
      undefined,
    );
  });
});
