import { beforeEach, describe, expect, it, vi } from "vitest";
import { validateComposedGlyphs } from "./composition";

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
    const result = await validateComposedGlyphs(map, ["https://example.com/overlay.json"], composedGlyphsUrl);

    // assert
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: composedGlyphsUrl });
  });

  it("should return null effective glyph URL when composedGlyphsUrl matches base style glyphs", async () => {
    // arrange
    const sharedGlyphs = "https://fonts.example.com/{fontstack}/{range}.pbf";
    const map = createMockMap(sharedGlyphs);

    // act
    const result = await validateComposedGlyphs(map, ["https://example.com/overlay.json"], sharedGlyphs);

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
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [],
          glyphs: sharedGlyphs,
        }),
      }),
    );

    // act
    const result = await validateComposedGlyphs(map, ["https://example.com/overlay.json"], null);

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
    const result = await validateComposedGlyphs(map, ["https://example.com/overlay.json"], null);

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
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [],
          glyphs: "../fonts/{fontstack}/{range}.pbf",
        }),
      }),
    );

    // act
    const result = await validateComposedGlyphs(map, ["https://example.com/styles/overlay.json"], null);

    // assert — ../fonts/ relative to /styles/overlay.json resolves to /fonts/
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should proceed when overlay fetch fails", async () => {
    // arrange
    const baseGlyphs = "https://fonts.example.com/{fontstack}/{range}.pbf";
    const map = createMockMap(baseGlyphs);
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: false, status: 404 }));

    // act
    const result = await validateComposedGlyphs(map, ["https://example.com/overlay.json"], null);

    // assert — only base glyph URL in set (1 unique), so proceed
    expect(result).toEqual({ proceed: true, effectiveGlyphsUrl: null });
  });

  it("should proceed when overlay fetch throws", async () => {
    // arrange
    const baseGlyphs = "https://fonts.example.com/{fontstack}/{range}.pbf";
    const map = createMockMap(baseGlyphs);
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new Error("network failure")));

    // act
    const result = await validateComposedGlyphs(map, ["https://example.com/overlay.json"], null);

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
        json: vi.fn().mockResolvedValue({
          version: 8,
          sources: {},
          layers: [],
        }),
      }),
    );

    // act
    const result = await validateComposedGlyphs(map, ["https://example.com/overlay.json"], null);

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
            json: () => Promise.resolve({ glyphs: "https://fonts-a.example.com/glyphs" }),
          });
        }
        if (url === "https://example.com/overlay-b.json") {
          return Promise.resolve({
            ok: true,
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
      ["https://example.com/overlay-a.json", "https://example.com/overlay-b.json"],
      null,
    );

    // assert — two different overlay glyph URLs, no base
    expect(result).toEqual({ proceed: false });
    expect(warnSpy).toHaveBeenCalled();
  });
});
