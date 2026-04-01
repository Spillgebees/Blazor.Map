import type { Map as MapLibreMap, StyleSpecification } from "maplibre-gl";
import type { ReferrerPolicy } from "../interfaces/map";
import type { OverlayStyleRequestOptions } from "../interfaces/spillgebees";

/**
 * Prefix used for all overlay-managed sources and layers to avoid ID collisions
 * with the base style and with user-added sources/layers.
 */
const OVERLAY_PREFIX = "sgb-overlay-style";

/**
 * Resolves a potentially relative URL against a base URL.
 * Handles ArcGIS-style relative paths (e.g., "../", "./sprites/sprite").
 */
function resolveUrl(url: string, baseUrl: string): string {
  try {
    return new URL(url, baseUrl).href;
  } catch {
    return url;
  }
}

/**
 * Resolves a template URL (containing `{...}` placeholders) against a base URL.
 * Preserves template placeholders by temporarily replacing them before URL resolution,
 * then restoring them afterward to avoid percent-encoding.
 */
function resolveTemplateUrl(url: string, baseUrl: string): string {
  // if the URL is already absolute, return as-is to avoid encoding template placeholders
  if (/^https?:\/\//i.test(url)) {
    return url;
  }

  // temporarily replace template placeholders to avoid percent-encoding
  const placeholders: string[] = [];
  const escaped = url.replace(/\{[^}]+}/g, (match) => {
    placeholders.push(match);
    return `__TEMPLATE_${String(placeholders.length - 1)}__`;
  });

  const resolved = resolveUrl(escaped, baseUrl);

  // restore original placeholders
  return resolved.replace(/__TEMPLATE_(\d+)__/g, (_, index) => placeholders[Number(index)]);
}

/**
 * Tracks which overlay styles are currently applied to a map.
 */
export interface OverlayStyleState {
  sourceIds: string[];
  layerIds: string[];
  imageIds: string[];
  composedLayerIds: Array<{
    styleId: string;
    originalLayerId: string;
    runtimeLayerId: string;
  }>;
}

export interface ApplyOverlayStyleOptions {
  forceReapply?: boolean;
}

function createFetchOptions(referrerPolicy: ReferrerPolicy | null | undefined): RequestInit | undefined {
  return referrerPolicy ? { referrerPolicy } : undefined;
}

export function fetchStyleJson(url: string, referrerPolicy: ReferrerPolicy | null): Promise<Response> {
  return fetch(url, createFetchOptions(referrerPolicy));
}

// WeakMap so entries are GC'd when the map instance is collected
const appliedOverlays = new WeakMap<MapLibreMap, Map<string, OverlayStyleState>>();

function getOverlayMap(map: MapLibreMap): Map<string, OverlayStyleState> {
  let overlays = appliedOverlays.get(map);
  if (!overlays) {
    overlays = new Map();
    appliedOverlays.set(map, overlays);
  }
  return overlays;
}

/**
 * Applies overlay styles on top of the base style.
 * Fetches each overlay style JSON, loads its sprites, then merges sources and layers.
 */
export async function applyOverlayStyles(
  map: MapLibreMap,
  overlayStyles: OverlayStyleRequestOptions[],
  options?: ApplyOverlayStyleOptions,
): Promise<void> {
  const overlays = getOverlayMap(map);
  const currentStyleIds = new Set(overlayStyles.map((overlayStyle) => overlayStyle.styleId));
  const composedStyleLayerIds = window.Spillgebees.Map.composedStyleLayerIds.get(map) ?? new Map();

  composedStyleLayerIds.clear();

  if (options?.forceReapply) {
    for (const [styleId, state] of overlays) {
      if (currentStyleIds.has(styleId)) {
        removeOverlayStyle(map, state);
        overlays.delete(styleId);
      }
    }
  }

  // Remove overlays that are no longer in the list
  for (const [styleId, state] of overlays) {
    if (!currentStyleIds.has(styleId)) {
      removeOverlayStyle(map, state);
      overlays.delete(styleId);
    }
  }

  // Apply new overlays in order
  for (let i = 0; i < overlayStyles.length; i++) {
    const { styleId, url } = overlayStyles[i];
    if (overlays.has(styleId)) {
      const existingState = overlays.get(styleId);
      if (existingState) {
        registerComposedLayerIds(composedStyleLayerIds, existingState);
      }
      continue;
    }

    try {
      const response = await fetchStyleJson(url, overlayStyles[i].referrerPolicy);
      if (!response.ok) {
        // biome-ignore lint/suspicious/noConsole: library warning for developers
        console.warn(`[Spillgebees.Map] Failed to fetch overlay style: ${url} (${String(response.status)})`);
        continue;
      }

      const styleJson = (await response.json()) as StyleSpecification;
      const state = await mergeStyleIntoMap(
        map,
        styleJson,
        `${OVERLAY_PREFIX}-${styleId}`,
        styleId,
        response.url,
        overlayStyles[i].referrerPolicy,
      );
      overlays.set(styleId, state);
      registerComposedLayerIds(composedStyleLayerIds, state);
    } catch (error) {
      // biome-ignore lint/suspicious/noConsole: library warning for developers
      console.warn(`[Spillgebees.Map] Error applying overlay style ${url}:`, error);
    }
  }

  window.Spillgebees.Map.composedStyleLayerIds.set(map, composedStyleLayerIds);
}

function registerComposedLayerIds(
  store: Map<string, { runtimeLayerId: string; styleId: string; originalLayerId: string }>,
  state: OverlayStyleState,
): void {
  for (const layer of state.composedLayerIds) {
    store.set(`${layer.styleId}\u0000${layer.originalLayerId}`, layer);
  }
}

/**
 * Loads sprite images from a sprite URL and adds them individually via map.addImage().
 * This avoids the namespacing and async issues of map.addSprite().
 *
 * Sprite format: {spriteUrl}.json contains image metadata, {spriteUrl}.png is the spritesheet.
 * For retina: {spriteUrl}@2x.json and {spriteUrl}@2x.png.
 */
async function loadSpriteImages(
  map: MapLibreMap,
  spriteUrl: string,
  referrerPolicy: ReferrerPolicy | null,
): Promise<string[]> {
  const imageIds: string[] = [];
  const pixelRatio = window.devicePixelRatio >= 2 ? 2 : 1;
  const suffix = pixelRatio === 2 ? "@2x" : "";

  try {
    // Fetch sprite metadata
    const metaResponse = await fetch(`${spriteUrl}${suffix}.json`, createFetchOptions(referrerPolicy));
    if (!metaResponse.ok) {
      return imageIds;
    }
    const metadata = (await metaResponse.json()) as Record<
      string,
      { x: number; y: number; width: number; height: number; pixelRatio: number }
    >;

    // Fetch spritesheet image
    const imageResponse = await fetch(`${spriteUrl}${suffix}.png`, createFetchOptions(referrerPolicy));
    if (!imageResponse.ok) {
      return imageIds;
    }
    const imageBlob = await imageResponse.blob();
    const imageBitmap = await createImageBitmap(imageBlob);

    // Extract each sprite image and add to map
    for (const [name, meta] of Object.entries(metadata)) {
      if (map.hasImage(name)) {
        continue; // don't overwrite base style images
      }

      try {
        // Extract the sub-image from the spritesheet
        const canvas = new OffscreenCanvas(meta.width, meta.height);
        const ctx = canvas.getContext("2d");
        if (!ctx) {
          continue;
        }
        ctx.drawImage(imageBitmap, meta.x, meta.y, meta.width, meta.height, 0, 0, meta.width, meta.height);
        const imageData = ctx.getImageData(0, 0, meta.width, meta.height);

        map.addImage(name, imageData, {
          pixelRatio: meta.pixelRatio ?? pixelRatio,
        });
        imageIds.push(name);
      } catch {
        // Individual image extraction failure — skip silently
      }
    }

    imageBitmap.close();
  } catch {
    // Sprite loading failure — layers will render without icons
  }

  return imageIds;
}

/**
 * Merges an overlay style's sources, layers, and sprite images into the map.
 * Sprite images are loaded BEFORE layers to ensure icon-image references resolve.
 */
async function mergeStyleIntoMap(
  map: MapLibreMap,
  style: StyleSpecification,
  prefix: string,
  styleId: string,
  styleUrl: string,
  referrerPolicy: ReferrerPolicy | null,
): Promise<OverlayStyleState> {
  const sourceIds: string[] = [];
  const layerIds: string[] = [];
  let imageIds: string[] = [];
  const composedLayerIds: Array<{ styleId: string; originalLayerId: string; runtimeLayerId: string }> = [];

  // Build a mapping from original source IDs to prefixed IDs
  const sourceIdMap = new Map<string, string>();

  // 1. Add sources — resolve relative URLs against the style's base URL
  if (style.sources) {
    for (const [originalId, sourceSpec] of Object.entries(style.sources)) {
      const prefixedId = `${prefix}-${originalId}`;
      sourceIdMap.set(originalId, prefixedId);

      if (!map.getSource(prefixedId)) {
        // Deep clone to avoid mutating the original style object
        const resolvedSpec = { ...sourceSpec } as Record<string, unknown>;

        // Resolve relative source URLs (TileJSON, tile templates, etc.)
        if (typeof resolvedSpec.url === "string") {
          resolvedSpec.url = resolveUrl(resolvedSpec.url, styleUrl);
        }
        if (Array.isArray(resolvedSpec.tiles)) {
          resolvedSpec.tiles = (resolvedSpec.tiles as string[]).map((t) => resolveUrl(t, styleUrl));
        }

        map.addSource(prefixedId, resolvedSpec as Parameters<MapLibreMap["addSource"]>[1]);
        sourceIds.push(prefixedId);
      }
    }
  }

  // 2. Load sprite images BEFORE adding layers — resolve relative sprite URL
  if (style.sprite) {
    const rawSpriteUrl = typeof style.sprite === "string" ? style.sprite : undefined;
    if (rawSpriteUrl) {
      const resolvedSpriteUrl = resolveUrl(rawSpriteUrl, styleUrl);
      imageIds = await loadSpriteImages(map, resolvedSpriteUrl, referrerPolicy);
    }
  }

  // 3. Add layers (after sprites are loaded)
  if (style.layers) {
    for (const layer of style.layers) {
      const prefixedLayerId = `${prefix}-${layer.id}`;

      if (map.getLayer(prefixedLayerId)) {
        composedLayerIds.push({ styleId, originalLayerId: layer.id, runtimeLayerId: prefixedLayerId });
        continue;
      }

      // Clone the layer and remap IDs
      const remappedLayer = { ...layer, id: prefixedLayerId } as Record<string, unknown>;

      // Remap source reference
      if ("source" in layer && typeof layer.source === "string") {
        const prefixedSource = sourceIdMap.get(layer.source);
        if (prefixedSource) {
          remappedLayer.source = prefixedSource;
        }
      }

      // Skip background layers — they'd cover the base map
      if (layer.type === "background") {
        continue;
      }

      try {
        map.addLayer(remappedLayer as Parameters<MapLibreMap["addLayer"]>[0]);
        layerIds.push(prefixedLayerId);
        composedLayerIds.push({ styleId, originalLayerId: layer.id, runtimeLayerId: prefixedLayerId });
      } catch (error) {
        // biome-ignore lint/suspicious/noConsole: library warning for developers
        console.warn(`[Spillgebees.Map] Failed to add overlay layer ${prefixedLayerId}:`, error);
      }
    }
  }

  return { sourceIds, layerIds, imageIds, composedLayerIds };
}

/**
 * Validates glyph compatibility across base and overlay styles, returning
 * the effective glyph URL that should be applied to the map (or null if
 * no rewrite is needed).
 *
 * When `composedGlyphsUrl` is provided, it acts as an explicit override.
 * When absent, the function fetches each overlay style to compare glyph
 * endpoints and rejects composition if they conflict.
 */
export async function validateComposedGlyphs(
  map: MapLibreMap,
  overlayStyles: OverlayStyleRequestOptions[],
  composedGlyphsUrl: string | null,
): Promise<{ proceed: true; effectiveGlyphsUrl: string | null } | { proceed: false }> {
  if (overlayStyles.length === 0) {
    return { proceed: true, effectiveGlyphsUrl: null };
  }

  const baseGlyphs = map.getStyle()?.glyphs ?? null;

  if (composedGlyphsUrl) {
    if (baseGlyphs !== composedGlyphsUrl) {
      return { proceed: true, effectiveGlyphsUrl: composedGlyphsUrl };
    }
    return { proceed: true, effectiveGlyphsUrl: null };
  }

  // no explicit override — check compatibility by fetching overlay styles
  const glyphUrls = new Set<string>();
  if (baseGlyphs) {
    glyphUrls.add(baseGlyphs);
  }

  for (const overlayStyle of overlayStyles) {
    try {
      const response = await fetchStyleJson(overlayStyle.url, overlayStyle.referrerPolicy);
      if (!response.ok) {
        continue;
      }

      const styleJson = (await response.json()) as { glyphs?: string };
      if (styleJson.glyphs) {
        const resolved = resolveTemplateUrl(styleJson.glyphs, response.url);
        glyphUrls.add(resolved);
      }
    } catch {
      // fetch failure — skip this overlay for glyph validation
    }
  }

  if (glyphUrls.size <= 1) {
    return { proceed: true, effectiveGlyphsUrl: null };
  }

  const urlList = Array.from(glyphUrls).join(", ");
  // biome-ignore lint/suspicious/noConsole: library warning for developers
  console.warn(
    `[Spillgebees.Map] Composed map styles require a single shared glyph endpoint. The supplied base and overlay styles resolve to different glyphs URLs, and overlay glyph URLs are ignored during composition. Set ComposedGlyphsUrl to a shared font service or use styles that already share the same glyph endpoint. Resolved glyph URLs: ${urlList}`,
  );

  return { proceed: false };
}

/**
 * Removes all sources, layers, and images added by an overlay style.
 */
function removeOverlayStyle(map: MapLibreMap, state: OverlayStyleState): void {
  // Remove layers first (they reference sources)
  for (const layerId of state.layerIds) {
    if (map.getLayer(layerId)) {
      map.removeLayer(layerId);
    }
  }

  // Remove sources
  for (const sourceId of state.sourceIds) {
    if (map.getSource(sourceId)) {
      map.removeSource(sourceId);
    }
  }

  // Remove images
  for (const imageId of state.imageIds) {
    if (map.hasImage(imageId)) {
      map.removeImage(imageId);
    }
  }
}
