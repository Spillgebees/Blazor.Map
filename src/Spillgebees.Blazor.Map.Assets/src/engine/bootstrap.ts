// .NET-facing glue for the map engine: creates MapLibre maps, adapts them to the
// EngineMap surface, and exposes the interop entry points under
// window.Spillgebees.Engine.

import { Map as MapLibreMap, type Popup as MapLibrePopup, type RequestParameters } from "maplibre-gl";
import type { IMapStyle } from "../interfaces/map";
import type { OverlayStyleRequestOptions } from "../interfaces/spillgebees";
import { buildStyleFromOptions } from "../styles/base-style";
import { applyOverlayStyles, validateComposedGlyphs } from "../styles/composition";
import { type ControlsController, createControlsController } from "./controls";
import { createFullscreenController, type FullscreenController } from "./fullscreen";
import { createMarkerController, createMarkerPopup, type MarkerController } from "./markers";
import type { MapConfigData, Op } from "./ops";
import { createPopupController, type PopupController } from "./popups";
import { createEngine, type Engine, type EngineMap } from "./runtime";
import { wireShapePopups } from "./shape-popups";

interface DotNetObjectReference {
  invokeMethodAsync<T>(methodName: string, ...args: unknown[]): Promise<T>;
}

interface EngineStyleOptions {
  /** Raw style: a URL string or a style JSON object. Used when `styles` is absent. */
  style?: string | object | null;
  /** Typed styles: index 0 is the base map, the rest compose as overlay styles. */
  styles?: IMapStyle[] | null;
  composedGlyphsUrl?: string | null;
}

interface EngineCreateOptions extends EngineStyleOptions {
  center: [number, number];
  zoom?: number;
  theme?: string | null;
  /** Constructor-only behavior switches. */
  interactive?: boolean | null;
  cooperativeGestures?: boolean | null;
  /** CSS font shorthands preloaded via document.fonts (fire-and-forget). */
  webFonts?: string[] | null;
  /** Reapplied whenever the map's parameters change (map.configure op). */
  config?: MapConfigData | null;
}

interface EngineInstance {
  map: MapLibreMap;
  engine: Engine;
  markers: MarkerController;
  controls: ControlsController;
  fullscreen: FullscreenController;
  popups: PopupController;
  router: DotNetObjectReference;
  baseStyleKey: string;
  overlayStyles: OverlayStyleRequestOptions[];
  composedGlyphsUrl: string | null;
  pendingStyleReload: boolean;
  isLoaded: boolean;
  /** Typed styles for referrer-policy resolution (empty for raw styles). */
  policyStyles: IMapStyle[];
  /** Per-origin referrer policies (tile overlays), fed by map.requestPolicy ops. */
  originPolicies: Map<string, string>;
  /** Transient text/html popup (popup.show / popup.close). */
  activePopup: MapLibrePopup | null;
  /** Last explicitly applied pixel ratio override, if any. */
  pixelRatioOverride: number | undefined;
}

export interface EngineNamespace {
  createMap(container: HTMLElement, optionsJson: string, router: DotNetObjectReference): void;
  applyOps(container: HTMLElement, opsJson: string): void;
  pushMotion(container: HTMLElement, layerId: string, frame: Uint8Array): void;
  setSourceData(
    container: HTMLElement,
    sourceId: string,
    dataJson: string,
    animateMs: number | null,
    animateEasing?: string | null,
  ): void;
  setStyles(container: HTMLElement, stylesJson: string): void;
  setTheme(container: HTMLElement, theme: string): void;
  dispose(container: HTMLElement): void;
  // read side — queries return values, so they cannot ride the one-way ops channel
  getView(container: HTMLElement): { lng: number; lat: number; zoom: number; bearing: number; pitch: number } | null;
  getBounds(
    container: HTMLElement,
  ): { southwest: { latitude: number; longitude: number }; northeast: { latitude: number; longitude: number } } | null;
  hasLayer(container: HTMLElement, layerId: string): boolean;
  hasStyleLayer(container: HTMLElement, styleId: string, layerId: string): boolean;
  queryRenderedFeatures(
    container: HTMLElement,
    point: { x: number; y: number },
    layerIds: string[] | null,
  ): Array<{ id: unknown; layerId: string | null; geometry: unknown; properties: unknown }>;
}

const instances = new Map<HTMLElement, EngineInstance>();

export function bootstrapEngine(): void {
  window.Spillgebees = window.Spillgebees || ({} as typeof window.Spillgebees);
  if (!window.Spillgebees.Map?.maps) {
    // the shared registries style composition and diagnostics read
    window.Spillgebees.Map = {
      maps: new Map(),
      composedStyleLayerIds: new Map(),
    } as typeof window.Spillgebees.Map;
  }

  window.Spillgebees.Engine = {
    createMap,
    applyOps,
    pushMotion,
    setSourceData,
    setStyles,
    setTheme,
    dispose,
    getView,
    getBounds,
    hasLayer,
    hasStyleLayer,
    queryRenderedFeatures,
  };
}

function resolveBaseStyle(options: EngineStyleOptions): string | object {
  if (options.styles && options.styles.length > 0) {
    return buildStyleFromOptions(options.styles[0]);
  }

  // no style configured: buildStyleFromOptions(null) yields the documented default
  // (OpenFreeMap Liberty, no API key required)
  return options.style ?? buildStyleFromOptions(null);
}

// The MapLibre interaction handlers behind each follow gesture group.
// - zoom covers the desktop zoom handlers
// - orientation covers drag-to-rotate (which tilts and turns together) plus touch pitch
// - pinch zoom/rotate is left alone since one touch gesture drives both and cannot be split per group
interface InteractionHandler {
  enable(): void;
  disable(): void;
  isEnabled(): boolean;
}

function interactionHandlers(map: MapLibreMap, group: "zoom" | "orientation"): InteractionHandler[] {
  const handler = map as unknown as Record<string, InteractionHandler | undefined>;
  const names = group === "zoom" ? ["scrollZoom", "doubleClickZoom", "boxZoom"] : ["dragRotate", "touchPitch"];
  return names.map((name) => handler[name]).filter((value): value is InteractionHandler => value != null);
}

function styleKey(style: string | object): string {
  return typeof style === "string" ? style : JSON.stringify(style);
}

function toOverlayRequests(
  styles: IMapStyle[] | null | undefined,
  onError: (error: unknown) => void,
): OverlayStyleRequestOptions[] {
  const requests: OverlayStyleRequestOptions[] = [];
  for (const style of styles?.slice(1) ?? []) {
    if (!style.url) {
      onError(
        new Error(
          `Overlay style '${style.id ?? "?"}' must be a URL style; raster/WMS overlays compose as tile overlays instead.`,
        ),
      );
      continue;
    }

    requests.push({
      styleId: style.id ?? style.url,
      url: style.url,
      referrerPolicy: style.referrerPolicy ?? null,
    });
  }

  return requests;
}

async function composeOverlays(instance: EngineInstance, onError: (error: unknown) => void): Promise<void> {
  if (instance.overlayStyles.length === 0 || !instance.isLoaded) {
    // pre-load composition is deferred to the load handler
    return;
  }

  try {
    const glyphResult = await validateComposedGlyphs(instance.map, instance.overlayStyles, instance.composedGlyphsUrl);
    if (!glyphResult.proceed) {
      return;
    }

    if (glyphResult.effectiveGlyphsUrl && instance.map.getStyle()?.glyphs !== glyphResult.effectiveGlyphsUrl) {
      const style = instance.map.getStyle();
      style.glyphs = glyphResult.effectiveGlyphsUrl;
      instance.map.setStyle(style, { diff: true });
    }

    await applyOverlayStyles(instance.map, instance.overlayStyles, { forceReapply: true });
  } catch (error) {
    onError(error);
  }
}

function createMap(container: HTMLElement, optionsJson: string, router: DotNetObjectReference): void {
  dispose(container);

  const options = JSON.parse(optionsJson) as EngineCreateOptions;
  const reportError = (error: unknown) =>
    void router.invokeMethodAsync("OnMapEvent", "error", { message: String(error) });
  const baseStyle = resolveBaseStyle(options);

  // preload web fonts (fire-and-forget — fonts load in parallel with map init)
  for (const font of options.webFonts ?? []) {
    document.fonts.load(font);
  }

  const config = options.config;
  let mapForTransform: MapLibreMap | null = null;
  const map = new MapLibreMap({
    container,
    // biome-ignore lint/suspicious/noExplicitAny: raw style passes through to MapLibre
    style: baseStyle as any,
    center: options.center,
    zoom: options.zoom ?? 0,
    pitch: config?.pitch ?? 0,
    bearing: config?.bearing ?? 0,
    minZoom: config?.minZoom ?? undefined,
    maxZoom: config?.maxZoom ?? undefined,
    maxBounds: config?.maxBounds ? toMaxBounds(config.maxBounds) : undefined,
    interactive: options.interactive ?? true,
    cooperativeGestures: options.cooperativeGestures ?? false,
    pixelRatio: config ? resolvePixelRatio(config) : undefined,
    transformRequest: (url, resourceType) => transformRequest(mapForTransform, url, resourceType),
  });
  mapForTransform = map;

  // marker click/dragend interactions land on the router's dedicated JSInvokables
  const markers = createMarkerController(map, (kind, markerId, lng, lat) => {
    // biome-ignore lint/security/noSecrets: C# callback method names, not secrets
    const method = kind === "click" ? "OnMarkerClickCallbackAsync" : "OnMarkerDragEndCallbackAsync";
    void router.invokeMethodAsync(method, { markerId, position: { latitude: lat, longitude: lng } });
  });
  // control/popup interactions (panel state, popup close) ride the generic handler-id channel
  const emit = (handlerId: number, payload: unknown) => void router.invokeMethodAsync("OnEvent", handlerId, payload);
  // Blazor renders component content (control placeholders, popup content) as a
  // sibling of the map container — resolve DOM conventions from the component root.
  const contentRoot = container.parentElement ?? container;
  // one fullscreen primitive per map, shared by the built-in control and the imperative API;
  // state changes (control, API, or the user pressing Esc) surface to .NET as a map event
  const fullscreen = createFullscreenController(container);
  fullscreen.onChange(
    (isFullscreen) => void router.invokeMethodAsync("OnMapEvent", "fullscreenchanged", { isFullscreen }),
  );
  const controls = createControlsController(map, contentRoot, emit, fullscreen);
  const popups = createPopupController(map, contentRoot, emit);
  const engine = createEngine(
    toEngineMap(() => instance, map, markers, controls, fullscreen, popups),
    {
      onEvent: emit,
      onError: reportError,
      onFollowCleared: (reason) => void router.invokeMethodAsync("OnMapEvent", "followcleared", { reason }),
    },
  );

  const instance: EngineInstance = {
    map,
    engine,
    markers,
    controls,
    fullscreen,
    popups,
    router,
    baseStyleKey: styleKey(baseStyle),
    overlayStyles: toOverlayRequests(options.styles, reportError),
    composedGlyphsUrl: options.composedGlyphsUrl ?? null,
    pendingStyleReload: false,
    isLoaded: false,
    policyStyles: options.styles ?? [],
    originPolicies: new Map(),
    activePopup: null,
    pixelRatioOverride: config ? resolvePixelRatio(config) : undefined,
  };
  instances.set(container, instance);
  // register in the shared map registry so diagnostics/tooling can find engine maps
  window.Spillgebees.Map?.maps?.set(container, map);
  applyThemeClass(container, options.theme ?? "light");

  map.on("load", async () => {
    instance.isLoaded = true;
    if (config) {
      applyConfig(instance, config);
    }
    wireShapePopups(map);
    await composeOverlays(instance, reportError);
    void router.invokeMethodAsync("OnMapEvent", "load", {});
  });
  // after a base style change, re-apply the engine scene and overlay styles
  map.on("styledata", () => {
    if (!instance.pendingStyleReload) {
      return;
    }

    instance.pendingStyleReload = false;
    void (async () => {
      instance.engine.replay();
      wireShapePopups(map);
      await composeOverlays(instance, reportError);
      void router.invokeMethodAsync("OnMapEvent", "stylereloaded", {});
    })();
  });
  map.on("moveend", () => void router.invokeMethodAsync("OnMapEvent", "moveend", viewPayload(map)));
  map.on("zoomend", () => void router.invokeMethodAsync("OnMapEvent", "zoomend", viewPayload(map)));
  map.on(
    "click",
    (event) => void router.invokeMethodAsync("OnMapEvent", "click", { lng: event.lngLat.lng, lat: event.lngLat.lat }),
  );
}

function setStyles(container: HTMLElement, stylesJson: string): void {
  const instance = instances.get(container);
  if (!instance) {
    return;
  }

  const reportError = (error: unknown) =>
    void instance.router.invokeMethodAsync("OnMapEvent", "error", { message: String(error) });
  const options = JSON.parse(stylesJson) as EngineStyleOptions;
  const baseStyle = resolveBaseStyle(options);
  const newKey = styleKey(baseStyle);
  instance.overlayStyles = toOverlayRequests(options.styles, reportError);
  instance.composedGlyphsUrl = options.composedGlyphsUrl ?? null;
  instance.policyStyles = options.styles ?? [];

  if (newKey !== instance.baseStyleKey) {
    instance.baseStyleKey = newKey;
    instance.pendingStyleReload = true;
    // biome-ignore lint/suspicious/noExplicitAny: raw style passes through to MapLibre
    instance.map.setStyle(baseStyle as any);
    return;
  }

  // base unchanged: only the overlay set / glyph endpoint changed
  void composeOverlays(instance, reportError);
}

function setTheme(container: HTMLElement, theme: string): void {
  applyThemeClass(container, theme);
}

function applyThemeClass(container: HTMLElement, theme: string): void {
  container.classList.toggle("sgb-map-dark", theme === "dark");
}

function applyOps(container: HTMLElement, opsJson: string): void {
  instances.get(container)?.engine.applyOps(JSON.parse(opsJson) as Op[]);
}

function pushMotion(container: HTMLElement, layerId: string, frame: Uint8Array): void {
  instances.get(container)?.engine.pushMotion(layerId, frame);
}

/**
 * Raw-text data lane: .NET sends GeoJSON text untouched (no C#-side parse, no
 * ops-channel re-serialization); it is parsed exactly once here and then rides the
 * normal source.setData op for store bookkeeping and rAF scheduling.
 */
function setSourceData(
  container: HTMLElement,
  sourceId: string,
  dataJson: string,
  animateMs: number | null,
  animateEasing?: string | null,
): void {
  const data = dataJson.trimStart().startsWith("{") ? (JSON.parse(dataJson) as object) : dataJson;
  const animate = animateMs
    ? { durationMs: animateMs, easing: (animateEasing ?? undefined) as "linear" | "easeInOut" | undefined }
    : null;
  instances.get(container)?.engine.applyOps([{ op: "source.setData", id: sourceId, data, animate }]);
}

function dispose(container: HTMLElement): void {
  const instance = instances.get(container);
  if (!instance) {
    return;
  }

  instances.delete(container);
  window.Spillgebees.Map?.maps?.delete(container);
  instance.activePopup?.remove();
  instance.popups.dispose();
  instance.controls.dispose();
  instance.fullscreen.dispose();
  instance.markers.dispose();
  instance.engine.dispose();
  instance.map.remove();
}

// --- read side: queries return values, so they cannot ride the one-way ops channel ---

function getView(container: HTMLElement) {
  const map = instances.get(container)?.map;
  if (!map) {
    return null;
  }

  const center = map.getCenter();
  return { lng: center.lng, lat: center.lat, zoom: map.getZoom(), bearing: map.getBearing(), pitch: map.getPitch() };
}

function getBounds(container: HTMLElement) {
  const map = instances.get(container)?.map;
  if (!map) {
    return null;
  }

  const bounds = map.getBounds();
  return {
    southwest: { latitude: bounds.getSouthWest().lat, longitude: bounds.getSouthWest().lng },
    northeast: { latitude: bounds.getNorthEast().lat, longitude: bounds.getNorthEast().lng },
  };
}

function hasLayer(container: HTMLElement, layerId: string): boolean {
  return Boolean(instances.get(container)?.map.getLayer(layerId));
}

function hasStyleLayer(container: HTMLElement, styleId: string, layerId: string): boolean {
  const instance = instances.get(container);
  if (!instance) {
    return false;
  }

  // composed overlay-style layers register under prefixed runtime ids
  const composed = window.Spillgebees.Map?.composedStyleLayerIds?.get(instance.map)?.get(`${styleId}\u0000${layerId}`);
  if (composed) {
    return Boolean(instance.map.getLayer(composed.runtimeLayerId));
  }

  return Boolean(instance.map.getLayer(layerId));
}

function queryRenderedFeatures(container: HTMLElement, point: { x: number; y: number }, layerIds: string[] | null) {
  const map = instances.get(container)?.map;
  if (!map) {
    return [];
  }

  const features = map.queryRenderedFeatures([point.x, point.y], layerIds?.length ? { layers: layerIds } : undefined);
  return features.map((feature) => ({
    id: feature.id,
    layerId: feature.layer?.id ?? null,
    geometry: feature.geometry,
    properties: feature.properties ?? null,
  }));
}

function toMaxBounds(bounds: {
  southwest: { latitude: number; longitude: number };
  northeast: { latitude: number; longitude: number };
}): [[number, number], [number, number]] {
  return [
    [bounds.southwest.longitude, bounds.southwest.latitude],
    [bounds.northeast.longitude, bounds.northeast.latitude],
  ];
}

function resolvePixelRatio(config: MapConfigData): number | undefined {
  if (config.pixelRatio != null) {
    return config.pixelRatio;
  }

  // biome-ignore lint/security/noSecrets: enum member name, not a secret
  if (config.pixelRatioMode === "roundedUpDevicePixelRatio") {
    return Math.max(1, Math.ceil(window.devicePixelRatio || 1));
  }

  return undefined;
}

function applyConfig(instance: EngineInstance, config: MapConfigData): void {
  const map = instance.map;
  map.setPitch(config.pitch);
  map.setBearing(config.bearing);
  map.setMinZoom(config.minZoom ?? null);
  map.setMaxZoom(config.maxZoom ?? null);
  map.setMaxBounds(config.maxBounds ? toMaxBounds(config.maxBounds) : (undefined as never));

  // getProjection() returns undefined when the style has no explicit projection field
  const nextProjection = config.projection === "globe" ? "globe" : "mercator";
  const currentProjection = map.getProjection()?.type ?? "mercator";
  if (currentProjection !== nextProjection) {
    map.setProjection({ type: nextProjection });
  }

  const nextPixelRatio = resolvePixelRatio(config);
  if (nextPixelRatio === undefined) {
    if (instance.pixelRatioOverride !== undefined) {
      const browserPixelRatio = window.devicePixelRatio || 1;
      if (instance.pixelRatioOverride !== browserPixelRatio) {
        map.setPixelRatio(browserPixelRatio);
      }

      instance.pixelRatioOverride = browserPixelRatio;
    }
  } else if (instance.pixelRatioOverride !== nextPixelRatio) {
    map.setPixelRatio(nextPixelRatio);
    instance.pixelRatioOverride = nextPixelRatio;
  }
}

function toRequestPolicy(value: unknown): RequestParameters["referrerPolicy"] | undefined {
  return typeof value === "string" ? (value as RequestParameters["referrerPolicy"]) : undefined;
}

/**
 * Applies referrer policies from typed styles (style + tile requests) and tile
 * overlays (per-origin, registered via map.requestPolicy ops).
 */
function transformRequest(map: MapLibreMap | null, url: string, resourceType?: string): RequestParameters | undefined {
  const instance = map ? [...instances.values()].find((entry) => entry.map === map) : undefined;
  if (!instance) {
    return undefined;
  }

  if (resourceType === "Style") {
    for (const style of instance.policyStyles) {
      const policy = toRequestPolicy(style.referrerPolicy);
      if (policy) {
        return { url, referrerPolicy: policy };
      }
    }

    return undefined;
  }

  if (resourceType === "Tile" || resourceType === "Source") {
    for (const style of instance.policyStyles) {
      const policy = toRequestPolicy(
        style.rasterSource?.referrerPolicy ?? style.wmsSource?.referrerPolicy ?? style.referrerPolicy,
      );
      if (policy) {
        return { url, referrerPolicy: policy };
      }
    }

    try {
      const origin = new URL(url).origin;
      const originPolicy = toRequestPolicy(instance.originPolicies.get(origin));
      if (originPolicy) {
        return { url, referrerPolicy: originPolicy };
      }
    } catch {
      // invalid URL, skip
    }
  }

  return undefined;
}

function viewPayload(map: MapLibreMap): Record<string, unknown> {
  const center = map.getCenter();
  return { lng: center.lng, lat: center.lat, zoom: map.getZoom(), bearing: map.getBearing(), pitch: map.getPitch() };
}

function toEngineMap(
  getInstance: () => EngineInstance,
  map: MapLibreMap,
  markers: MarkerController,
  controls: ControlsController,
  fullscreen: FullscreenController,
  popups: PopupController,
): EngineMap {
  return {
    setMarker: (marker) => markers.set(marker),
    removeMarker: (id) => markers.remove(id),
    markerPosition: (id) => markers.position(id),
    showPopup(position, options) {
      const instance = getInstance();
      instance.activePopup?.remove();
      instance.activePopup = createMarkerPopup(options).setLngLat([position.longitude, position.latitude]).addTo(map);
    },
    closeActivePopup() {
      const instance = getInstance();
      instance.activePopup?.remove();
      instance.activePopup = null;
    },
    configure: (config) => applyConfig(getInstance(), config),
    resize: () => void map.resize(),
    setFullscreen(state) {
      if (state == null) {
        void fullscreen.toggle();
      } else if (state) {
        void fullscreen.enter();
      } else {
        void fullscreen.exit();
      }
    },
    setRequestPolicy(origin, policy) {
      const policies = getInstance().originPolicies;
      if (policy) {
        policies.set(origin, policy);
      } else {
        policies.delete(origin);
      }
    },
    flyTo: (options) => void map.flyTo(options as never),
    fitBounds: (bounds, options) => void map.fitBounds(bounds, options as never),
    setControl: (control) => controls.set(control),
    removeControl: (id) => controls.remove(id),
    setControlContent: (id, events) => controls.setContent(id, events),
    removeControlContent: (id) => controls.removeContent(id),
    setPopup: (popup) => popups.set(popup),
    removePopup: (id) => popups.remove(id),
    addSource: (id, spec) => void map.addSource(id, spec as never),
    removeSource: (id) => void map.removeSource(id),
    getSource: (id) => map.getSource(id),
    addLayer: (spec, beforeId) => void map.addLayer(spec as never, beforeId),
    removeLayer: (id) => void map.removeLayer(id),
    getLayer: (id) => map.getLayer(id),
    moveLayer: (id, beforeId) => void map.moveLayer(id, beforeId),
    setPaintProperty: (id, name, value) => void map.setPaintProperty(id, name, value),
    setLayoutProperty: (id, name, value) => void map.setLayoutProperty(id, name, value),
    setFilter: (id, filter) => void map.setFilter(id, filter as never),
    setLayerZoomRange: (id, min, max) => void map.setLayerZoomRange(id, min, max),
    setFeatureState: (target, state) => void map.setFeatureState(target as never, state),
    removeFeatureState: (target) => void map.removeFeatureState(target as never),
    easeTo: (options) => void map.easeTo(options as never),
    getZoom: () => map.getZoom(),
    getBearing: () => map.getBearing(),
    getPitch: () => map.getPitch(),
    // Assumes one active lock per group (the follow controller holds at most one and restores before
    // re-locking); it captures absolute enabled state rather than reference counting, so overlapping
    // unreleased locks on the same group would not compose.
    lockInteraction: (group) => {
      const handlers = interactionHandlers(map, group);
      const wasEnabled = handlers.map((handler) => handler.isEnabled());
      for (const handler of handlers) {
        handler.disable();
      }
      return () => {
        handlers.forEach((handler, index) => {
          if (wasEnabled[index]) {
            handler.enable();
          }
        });
      };
    },
    addImage: (id, image, options) => void map.addImage(id, image as never, options as never),
    removeImage: (id) => void map.removeImage(id),
    hasImage: (id) => map.hasImage(id),
    loadImageData: renderImageData,
    // Forwards both the layer-scoped (event, layerId, handler) and map-level (event, handler) forms.
    on: (...args: unknown[]) => void (map.on as (...a: unknown[]) => unknown)(...args),
    off: (...args: unknown[]) => void (map.off as (...a: unknown[]) => unknown)(...args),
    listStyleLayers: () => (map.getStyle()?.layers ?? []) as never,
    resolveComposedLayer: (styleId, layerId) => {
      const registration = window.Spillgebees.Map?.composedStyleLayerIds?.get(map)?.get(`${styleId}\u0000${layerId}`);
      return registration
        ? { layerId: registration.runtimeLayerId, visible: registration.originalVisible ?? true }
        : null;
    },
    listComposedLayers: (styleId) => {
      const registrations = window.Spillgebees.Map?.composedStyleLayerIds?.get(map);
      if (!registrations) {
        return [];
      }

      return [...registrations.values()]
        .filter((registration) => registration.styleId === styleId)
        .map((registration) => ({
          layerId: registration.runtimeLayerId,
          visible: registration.originalVisible ?? true,
        }));
    },
  };
}

/**
 * Renders an image URL — including SVG data URIs, which map.loadImage() rejects — to
 * ImageData at device resolution, so SVG icons stay crisp on high-DPI displays.
 */
async function renderImageData(
  url: string,
  options: Record<string, unknown> | null,
): Promise<{ data: unknown; options?: Record<string, unknown> }> {
  const image = new Image();
  image.crossOrigin = "anonymous";
  await new Promise<void>((resolve, reject) => {
    image.onload = () => resolve();
    image.onerror = () => reject(new Error(`Failed to load image: ${url}`));
    image.src = url;
  });

  const width = typeof options?.width === "number" ? options.width : image.naturalWidth;
  const height = typeof options?.height === "number" ? options.height : image.naturalHeight;
  const pixelRatio = typeof options?.pixelRatio === "number" ? options.pixelRatio : 1;
  const sdf = options?.sdf === true;

  const dpr = window.devicePixelRatio || 1;
  const renderWidth = Math.max(1, Math.round(width * dpr));
  const renderHeight = Math.max(1, Math.round(height * dpr));

  const canvas = new OffscreenCanvas(renderWidth, renderHeight);
  const context = canvas.getContext("2d");
  if (!context) {
    throw new Error("Could not acquire a 2d canvas context for image rendering");
  }

  context.drawImage(image, 0, 0, renderWidth, renderHeight);
  return {
    data: context.getImageData(0, 0, renderWidth, renderHeight),
    options: { pixelRatio: pixelRatio * dpr, sdf },
  };
}
