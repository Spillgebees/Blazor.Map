import { beforeEach, describe, expect, it, vi } from "vitest";
import { applyOverlayStyles, validateComposedGlyphs } from "./composition";

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

describe("applyOverlayStyles", () => {
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
    } as unknown as Parameters<typeof applyOverlayStyles>[0];
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
    await applyOverlayStyles(map, [{ styleId: "overlay", url: originalUrl, referrerPolicy: null }]);

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
    } as unknown as Parameters<typeof applyOverlayStyles>[0];
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
    await applyOverlayStyles(map, [
      { styleId: "a", url: "https://example.com/a.json", referrerPolicy: "origin" },
      { styleId: "b", url: "https://example.com/b.json", referrerPolicy: "no-referrer" },
    ]);

    // assert
    expect(fetchMock).toHaveBeenNthCalledWith(1, "https://example.com/a.json", { referrerPolicy: "origin" });
    expect(fetchMock).toHaveBeenNthCalledWith(2, "https://example.com/b.json", { referrerPolicy: "no-referrer" });
  });
});
