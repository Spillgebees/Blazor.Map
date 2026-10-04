import type { Map as MapLibreMap, StyleSpecification } from "maplibre-gl";
import {
  COMPOSED_GROUND_SLOT,
  COMPOSED_LABELS_SLOT,
  COMPOSED_STYLE_PREFIX,
  type LayerSlot,
  slotAnchorLayerId,
} from "../engine/slots";
import { layerTags } from "../engine/visibility";
import type { ReferrerPolicy } from "../interfaces/map";
import type { ComposedStyleLayerRegistration, ComposedStyleRequest } from "../interfaces/spillgebees";

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
 * Tracks which composed styles are currently applied to a map.
 */
export interface ComposedStyleState {
  sourceIds: string[];
  layerIds: string[];
  imageIds: string[];
  composedLayerIds: ComposedStyleLayerRegistration[];
}

export interface ApplyComposedStylesOptions {
  forceReapply?: boolean;
}

function createFetchOptions(referrerPolicy: ReferrerPolicy | null | undefined): RequestInit | undefined {
  return referrerPolicy ? { referrerPolicy } : undefined;
}

export function fetchStyleJson(url: string, referrerPolicy: ReferrerPolicy | null): Promise<Response> {
  return fetch(url, createFetchOptions(referrerPolicy));
}

interface CachedStyleJson {
  json: unknown;
  /** Final URL after redirects, used to resolve relative sprite, tile, and glyph URLs. */
  resolvedUrl: string;
}

// per-map cache of fetched style JSON, so recomposing (slot changes, base style
// switches, glyph validation) doesn't refetch. it lives as long as the map, so a
// style updated on the server only shows up in a new map instance
const styleJsonCache = new WeakMap<MapLibreMap, Map<string, CachedStyleJson>>();

/**
 * Fetches (or returns the cached) composed style JSON for a URL.
 * Returns null after logging when the response is not OK; fetch/parse errors propagate.
 */
async function fetchStyleJsonCached(
  map: MapLibreMap,
  url: string,
  referrerPolicy: ReferrerPolicy | null,
): Promise<CachedStyleJson | null> {
  let cache = styleJsonCache.get(map);
  if (!cache) {
    cache = new Map();
    styleJsonCache.set(map, cache);
  }

  const cached = cache.get(url);
  if (cached) {
    return cached;
  }

  const response = await fetchStyleJson(url, referrerPolicy);
  if (!response.ok) {
    // biome-ignore lint/suspicious/noConsole: library warning for developers
    console.warn(`[Spillgebees.Map] Failed to fetch composed style: ${url} (${String(response.status)})`);
    return null;
  }

  const entry: CachedStyleJson = { json: await response.json(), resolvedUrl: response.url };
  cache.set(url, entry);
  return entry;
}

// WeakMap so entries are GC'd when the map instance is collected
const appliedStyles = new WeakMap<MapLibreMap, Map<string, ComposedStyleState>>();

function getAppliedStyles(map: MapLibreMap): Map<string, ComposedStyleState> {
  let applied = appliedStyles.get(map);
  if (!applied) {
    applied = new Map();
    appliedStyles.set(map, applied);
  }
  return applied;
}

function layerFilter(layer: StyleSpecification["layers"][number]): unknown {
  return "filter" in layer ? layer.filter : null;
}

/**
 * Composes styles on top of the base style.
 * Fetches each style's JSON, loads its sprites, then merges sources and layers.
 */
export async function applyComposedStyles(
  map: MapLibreMap,
  styles: ComposedStyleRequest[],
  options?: ApplyComposedStylesOptions,
): Promise<void> {
  const applied = getAppliedStyles(map);
  const currentStyleIds = new Set(styles.map((style) => style.styleId));
  const composedStyleLayerIds = window.Spillgebees.Map.composedStyleLayerIds.get(map) ?? new Map();

  composedStyleLayerIds.clear();

  if (options?.forceReapply) {
    for (const [styleId, state] of applied) {
      if (currentStyleIds.has(styleId)) {
        removeComposedStyle(map, state);
        applied.delete(styleId);
      }
    }
  }

  // Remove styles that are no longer in the list
  for (const [styleId, state] of applied) {
    if (!currentStyleIds.has(styleId)) {
      removeComposedStyle(map, state);
      applied.delete(styleId);
    }
  }

  // Apply new styles in order
  for (const request of styles) {
    const { styleId, url } = request;
    if (applied.has(styleId)) {
      const existingState = applied.get(styleId);
      if (existingState) {
        registerComposedLayerIds(composedStyleLayerIds, existingState);
      }
      continue;
    }

    try {
      const cached = await fetchStyleJsonCached(map, url, request.referrerPolicy);
      if (!cached) {
        continue;
      }

      const state = await mergeStyleIntoMap(
        map,
        cached.json as StyleSpecification,
        `${COMPOSED_STYLE_PREFIX}-${styleId}`,
        styleId,
        cached.resolvedUrl,
        request,
      );
      applied.set(styleId, state);
      registerComposedLayerIds(composedStyleLayerIds, state);
    } catch (error) {
      // biome-ignore lint/suspicious/noConsole: library warning for developers
      console.warn(`[Spillgebees.Map] Error applying composed style ${url}:`, error);
    }
  }

  window.Spillgebees.Map.composedStyleLayerIds.set(map, composedStyleLayerIds);
}

function registerComposedLayerIds(store: Map<string, ComposedStyleLayerRegistration>, state: ComposedStyleState): void {
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
        // Individual image extraction failure, skip silently
      }
    }

    imageBitmap.close();
  } catch {
    // Sprite loading failure, layers will render without icons
  }

  return imageIds;
}

function isLayerSlot(value: unknown): value is LayerSlot {
  return value === "above-labels" || value === "below-labels";
}

/**
 * Picks the slot a composed style layer paints in. The consumer's per-layer slot
 * wins, then `sgb:slot` metadata from the style author, then the style's slot. With
 * none of those, symbol layers go above labels and everything else below.
 */
export function resolveLayerSlot(
  layer: { id: string; type?: string; metadata?: unknown },
  request: Pick<ComposedStyleRequest, "slot" | "layerSlots">,
): LayerSlot {
  const layerSlot = request.layerSlots?.[layer.id];
  if (isLayerSlot(layerSlot)) {
    return layerSlot;
  }

  const metadataSlot = (layer.metadata as Record<string, unknown> | null | undefined)?.["sgb:slot"];
  if (isLayerSlot(metadataSlot)) {
    return metadataSlot;
  }

  if (isLayerSlot(request.slot)) {
    return request.slot;
  }

  return layer.type === "symbol" ? "above-labels" : "below-labels";
}

/**
 * Merges a composed style's sources, layers, and sprite images into the map.
 * Sprite images are loaded BEFORE layers to ensure icon-image references resolve.
 */
async function mergeStyleIntoMap(
  map: MapLibreMap,
  style: StyleSpecification,
  prefix: string,
  styleId: string,
  styleUrl: string,
  request: ComposedStyleRequest,
): Promise<ComposedStyleState> {
  const referrerPolicy = request.referrerPolicy;
  const sourceIds: string[] = [];
  const layerIds: string[] = [];
  let imageIds: string[] = [];
  const composedLayerIds: ComposedStyleState["composedLayerIds"] = [];

  // Build a mapping from original source IDs to prefixed IDs
  const sourceIdMap = new Map<string, string>();

  // 1. Add sources, resolving relative URLs against the style's URL
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

  // 2. Load sprite images BEFORE adding layers, resolving a relative sprite URL
  if (style.sprite) {
    const rawSpriteUrl = typeof style.sprite === "string" ? style.sprite : undefined;
    if (rawSpriteUrl) {
      const resolvedSpriteUrl = resolveUrl(rawSpriteUrl, styleUrl);
      imageIds = await loadSpriteImages(map, resolvedSpriteUrl, referrerPolicy);
    }
  }

  // 3. Add layers (after sprites are loaded). Below-labels layers go in before the
  // composed ground anchor, above-labels layers before the composed labels anchor.
  // Inserting every layer before the same anchor keeps the style's own order. Without
  // anchors (engine not bootstrapped), layers are appended instead.
  const slotAnchors: Record<LayerSlot, string | undefined> = {
    "below-labels": map.getLayer(slotAnchorLayerId(COMPOSED_GROUND_SLOT))
      ? slotAnchorLayerId(COMPOSED_GROUND_SLOT)
      : undefined,
    "above-labels": map.getLayer(slotAnchorLayerId(COMPOSED_LABELS_SLOT))
      ? slotAnchorLayerId(COMPOSED_LABELS_SLOT)
      : undefined,
  };

  if (style.layers) {
    for (const layer of style.layers) {
      const prefixedLayerId = `${prefix}-${layer.id}`;

      if (map.getLayer(prefixedLayerId)) {
        composedLayerIds.push({
          styleId,
          originalLayerId: layer.id,
          runtimeLayerId: prefixedLayerId,
          originalVisible: layer.layout?.visibility !== "none",
          originalFilter: layerFilter(layer),
          tags: layerTags(layer.metadata),
        });
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

      // Skip background layers, they'd cover the base map
      if (layer.type === "background") {
        continue;
      }

      try {
        map.addLayer(
          remappedLayer as Parameters<MapLibreMap["addLayer"]>[0],
          slotAnchors[resolveLayerSlot(layer, request)],
        );
        layerIds.push(prefixedLayerId);
        composedLayerIds.push({
          styleId,
          originalLayerId: layer.id,
          runtimeLayerId: prefixedLayerId,
          originalVisible: layer.layout?.visibility !== "none",
          originalFilter: layerFilter(layer),
          tags: layerTags(layer.metadata),
        });
      } catch (error) {
        // biome-ignore lint/suspicious/noConsole: library warning for developers
        console.warn(`[Spillgebees.Map] Failed to add composed style layer ${prefixedLayerId}:`, error);
      }
    }
  }

  return { sourceIds, layerIds, imageIds, composedLayerIds };
}

/**
 * Validates glyph compatibility across the base and composed styles, returning
 * the effective glyph URL that should be applied to the map (or null if
 * no rewrite is needed).
 *
 * When `composedGlyphsUrl` is provided, it acts as an explicit override.
 * When absent, the function fetches each composed style to compare glyph
 * endpoints and rejects composition if they conflict.
 */
export async function validateComposedGlyphs(
  map: MapLibreMap,
  styles: ComposedStyleRequest[],
  composedGlyphsUrl: string | null,
): Promise<{ proceed: true; effectiveGlyphsUrl: string | null } | { proceed: false }> {
  if (styles.length === 0) {
    return { proceed: true, effectiveGlyphsUrl: null };
  }

  const baseGlyphs = map.getStyle()?.glyphs ?? null;

  if (composedGlyphsUrl) {
    if (baseGlyphs !== composedGlyphsUrl) {
      return { proceed: true, effectiveGlyphsUrl: composedGlyphsUrl };
    }
    return { proceed: true, effectiveGlyphsUrl: null };
  }

  // no explicit override, check compatibility by fetching the composed styles
  const glyphUrls = new Set<string>();
  if (baseGlyphs) {
    glyphUrls.add(baseGlyphs);
  }

  for (const style of styles) {
    try {
      const cached = await fetchStyleJsonCached(map, style.url, style.referrerPolicy);
      if (!cached) {
        continue;
      }

      const styleJson = cached.json as { glyphs?: string };
      if (styleJson.glyphs) {
        const resolved = resolveTemplateUrl(styleJson.glyphs, cached.resolvedUrl);
        glyphUrls.add(resolved);
      }
    } catch {
      // fetch failure, skip this style for glyph validation
    }
  }

  if (glyphUrls.size <= 1) {
    return { proceed: true, effectiveGlyphsUrl: null };
  }

  const urlList = Array.from(glyphUrls).join(", ");
  // biome-ignore lint/suspicious/noConsole: library warning for developers
  console.warn(
    `[Spillgebees.Map] Composed map styles require a single shared glyph endpoint. The supplied base and composed styles resolve to different glyphs URLs, and composed styles' glyph URLs are ignored during composition. Set ComposedGlyphsUrl to a shared font service or use styles that already share the same glyph endpoint. Resolved glyph URLs: ${urlList}`,
  );

  return { proceed: false };
}

/**
 * Removes all sources, layers, and images added by a composed style.
 */
function removeComposedStyle(map: MapLibreMap, state: ComposedStyleState): void {
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
