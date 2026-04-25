import type { DotNet } from "@microsoft/dotnet-js-interop";
import type { IControl, RequestParameters, StyleSpecification } from "maplibre-gl";
import {
  FullscreenControl,
  GeolocateControl,
  Map as MapLibreMap,
  NavigationControl,
  ScaleControl,
  TerrainControl,
} from "maplibre-gl";
import { CenterControl } from "./controls/centerControl";
import { LegendControl } from "./controls/legendControl";
import { addMarkers, removeMarkers, updateMarkers } from "./features/markers";
import {
  addCircles,
  addPolylines,
  ensureShapeLayers,
  removeCircles,
  removePolylines,
  setupShapePopupHandlers,
  updateCircles,
  updatePolylines,
} from "./features/shapes";
import type { ControlPosition, ILegendMapControl, IMapControl } from "./interfaces/controls";
import type { ICircle, IMarker, IPolyline } from "./interfaces/features";
import type {
  ICoordinate,
  IFitBoundsOptions,
  IMapImageDefinition,
  IMapOptions,
  IMapStyle,
  ITileOverlay,
} from "./interfaces/map";
import type {
  ComposedStyleLayerRegistration,
  CustomControlRegistration,
  LayerEventSubscription,
  OverlayStyleRequestOptions,
  RegisteredMapImage,
  RegisteredMapLayer,
  RegisteredMapSource,
  VisibilityGroupRegistration,
} from "./interfaces/spillgebees";
import { applySceneMutations, replayStyleReloadState } from "./runtime/sceneMutations";
import {
  addImage,
  addMapLayer,
  addMapSource,
  closePopup,
  getClusterExpansionZoom,
  moveMapLayer,
  removeMapLayer,
  removeMapSource,
  setFeatureState,
  setFilter,
  setLayerVisibility,
  setLayerZoomRange,
  setLayoutProperty,
  setPaintProperty,
  setSourceData,
  setSourceDataAnimated,
  showPopup,
  unregisterLayerEvents,
  wireLayerEvents,
} from "./sources/geojson";
import { applyOverlayStyles, validateComposedGlyphs } from "./styles/composition";
import type { FeatureStorage } from "./types/feature-storage";

export const PROTOCOL_VERSION = 13;

const LEGEND_CONTROL_KIND = "legend";

interface OrderedControlRegistration {
  controlId: string;
  control: IControl;
  position: ControlPosition;
  order: number;
  declarationOrder: number;
}

function createDefaultControls(): IMapControl[] {
  return [];
}

const DEFAULT_STYLE_URL = "https://tiles.openfreemap.org/styles/liberty";

function toRequestReferrerPolicy(value: unknown): RequestParameters["referrerPolicy"] | undefined {
  return typeof value === "string" ? (value as RequestParameters["referrerPolicy"]) : undefined;
}

function resolveStyleTileReferrerPolicy(style: IMapStyle | null): RequestParameters["referrerPolicy"] | undefined {
  if (!style) {
    return undefined;
  }

  if (style.rasterSource) {
    return toRequestReferrerPolicy(style.rasterSource.referrerPolicy ?? style.referrerPolicy);
  }

  if (style.wmsSource) {
    return toRequestReferrerPolicy(style.wmsSource.referrerPolicy ?? style.referrerPolicy);
  }

  return toRequestReferrerPolicy(style.referrerPolicy);
}

function resolveStyleReferrerPolicy(style: IMapStyle | null): RequestParameters["referrerPolicy"] | undefined {
  if (!style) {
    return undefined;
  }

  return toRequestReferrerPolicy(style.referrerPolicy);
}

function buildOverlayOriginMap(overlays: ITileOverlay[]): Map<string, RequestParameters["referrerPolicy"]> {
  const originMap = new Map<string, RequestParameters["referrerPolicy"]>();

  for (const overlay of overlays) {
    const policy = toRequestReferrerPolicy(overlay.referrerPolicy);
    if (policy) {
      try {
        const origin = new URL(overlay.urlTemplate).origin;
        originMap.set(origin, policy);
      } catch {
        // invalid URL template, skip
      }
    }
  }

  return originMap;
}

function createTransformRequest(
  mapOptions: IMapOptions,
  overlays: ITileOverlay[],
): (url: string, resourceType?: string) => RequestParameters | undefined {
  const overlayOrigins = buildOverlayOriginMap(overlays);

  return (url: string, resourceType?: string) => {
    const stylesList = mapOptions.styles ?? (mapOptions.style ? [mapOptions.style] : [null]);

    if (resourceType === "Style") {
      for (const style of stylesList) {
        const referrerPolicy = resolveStyleReferrerPolicy(style);
        if (referrerPolicy) {
          return { url, referrerPolicy };
        }
      }
      return undefined;
    }

    if (resourceType === "Tile" || resourceType === "Source") {
      // check style-level tile referrer policies first
      for (const style of stylesList) {
        const referrerPolicy = resolveStyleTileReferrerPolicy(style);
        if (referrerPolicy) {
          return { url, referrerPolicy };
        }
      }

      // fall back to overlay origin matching
      try {
        const requestOrigin = new URL(url).origin;
        const overlayPolicy = overlayOrigins.get(requestOrigin);
        if (overlayPolicy) {
          return { url, referrerPolicy: overlayPolicy };
        }
      } catch {
        // invalid URL, skip
      }

      return undefined;
    }

    return undefined;
  };
}

function createMapTransformRequest(
  getMap: () => MapLibreMap | null,
): (url: string, resourceType?: string) => RequestParameters | undefined {
  return (url: string, resourceType?: string) => {
    const map = getMap();
    if (!map) {
      return undefined;
    }

    const requestContext = window.Spillgebees.Map.requestContexts.get(map);
    if (!requestContext) {
      return undefined;
    }

    return createTransformRequest(requestContext.mapOptions, requestContext.overlays)(url, resourceType);
  };
}

function updateMapRequestContext(map: MapLibreMap, mapOptions: IMapOptions, overlays: ITileOverlay[]): void {
  window.Spillgebees.Map.requestContexts.set(map, {
    mapOptions: structuredClone(mapOptions),
    overlays: structuredClone(overlays),
  });
}

function isNamespaceCompatible(): boolean {
  try {
    return window.Spillgebees?.Map?.getProtocolVersion?.() === PROTOCOL_VERSION;
  } catch {
    return false;
  }
}

function initializeNamespace(): void {
  window.Spillgebees.Map = {
    getProtocolVersion: () => PROTOCOL_VERSION,
    mapFunctions: {
      createMap,
      syncFeatures,
      setOverlays,
      setControls,
      setControlContent,
      removeControlContent,
      setMapOptions,
      setTheme,
      fitBounds,
      flyTo,
      resize,
      setImages,
      disposeMap,
      applySceneMutations,
      addMapSource,
      removeMapSource,
      setSourceData,
      setSourceDataAnimated,
      addMapLayer,
      moveMapLayer,
      removeMapLayer,
      addImage,
      showPopup,
      closePopup,
      setFeatureState,
      setTrackedEntityFeatureState,
      getClusterExpansionZoom,
      getCenter,
      hasLayer,
      hasStyleLayer,
      getZoom,
      getBounds,
      queryRenderedFeatures,
      setFilter,
      setLayerVisibility,
      setStyleLayerVisibility,
      setLayerZoomRange,
      setLayoutProperty,
      setPaintProperty,
      wireLayerEvents,
      unregisterLayerEvents,
    },
    maps: new Map<HTMLElement, MapLibreMap>(),
    features: new Map<MapLibreMap, FeatureStorage>(),
    overlays: new Map<MapLibreMap, Map<string, unknown>>(),
    controls: new Map<MapLibreMap, Set<IControl>>(),
    customControlRegistrations: new Map<MapLibreMap, Map<string, CustomControlRegistration>>(),
    styles: new Map<MapLibreMap, string | StyleSpecification>(),
    mapOptions: new Map<MapLibreMap, IMapOptions>(),
    dotNetHelpers: new Map<MapLibreMap, DotNet.DotNetObject>(),
    controlsPayload: new Map<MapLibreMap, IMapControl[]>(),
    sourceSpecs: new Map<MapLibreMap, Map<string, RegisteredMapSource>>(),
    layerSpecs: new Map<MapLibreMap, Map<string, RegisteredMapLayer>>(),
    imageRegistrations: new Map<MapLibreMap, Map<string, RegisteredMapImage>>(),
    layerEventSubscriptions: new Map<MapLibreMap, Map<string, LayerEventSubscription>>(),
    visibilityGroups: new Map<MapLibreMap, Map<string, VisibilityGroupRegistration>>(),
    overlayStyleUrls: new Map<MapLibreMap, string[]>(),
    overlayStyleRequests: new Map<MapLibreMap, OverlayStyleRequestOptions[]>(),
    composedStyleLayerIds: new Map<MapLibreMap, Map<string, ComposedStyleLayerRegistration>>(),
    pendingStyleReloads: new WeakSet<MapLibreMap>(),
    requestContexts: new Map<MapLibreMap, { mapOptions: IMapOptions; overlays: ITileOverlay[] }>(),
    imageSyncVersion: new Map<MapLibreMap, number>(),
  };
}

function _getRegisteredSourceStore(map: MapLibreMap): Map<string, RegisteredMapSource> {
  const existing = window.Spillgebees.Map.sourceSpecs.get(map);
  if (existing) {
    return existing;
  }

  const created = new Map<string, RegisteredMapSource>();
  window.Spillgebees.Map.sourceSpecs.set(map, created);
  return created;
}

function _getRegisteredLayerStore(map: MapLibreMap): Map<string, RegisteredMapLayer> {
  const existing = window.Spillgebees.Map.layerSpecs.get(map);
  if (existing) {
    return existing;
  }

  const created = new Map<string, RegisteredMapLayer>();
  window.Spillgebees.Map.layerSpecs.set(map, created);
  return created;
}

function getRegisteredImageStore(map: MapLibreMap): Map<string, RegisteredMapImage> {
  const existing = window.Spillgebees.Map.imageRegistrations.get(map);
  if (existing) {
    return existing;
  }

  const created = new Map<string, RegisteredMapImage>();
  window.Spillgebees.Map.imageRegistrations.set(map, created);
  return created;
}

function getComposedStyleLayerStore(map: MapLibreMap): Map<string, ComposedStyleLayerRegistration> {
  const existing = window.Spillgebees.Map.composedStyleLayerIds.get(map);
  if (existing) {
    return existing;
  }

  const created = new Map<string, ComposedStyleLayerRegistration>();
  window.Spillgebees.Map.composedStyleLayerIds.set(map, created);
  return created;
}

function getComposedStyleLayerKey(styleId: string, layerId: string): string {
  return `${styleId}\u0000${layerId}`;
}

function getResolvedStyleLayerId(map: MapLibreMap, styleId: string, layerId: string): string | null {
  const registration = getComposedStyleLayerStore(map).get(getComposedStyleLayerKey(styleId, layerId));
  return registration?.runtimeLayerId ?? null;
}

function getComposedOverlayStyles(mapOptions: IMapOptions): OverlayStyleRequestOptions[] {
  const stylesList = mapOptions.styles ?? (mapOptions.style ? [mapOptions.style] : [null]);

  return stylesList
    .slice(1)
    .filter((style): style is IMapStyle & { id: string; url: string } => style?.id != null && style.url != null)
    .map((style) => ({ styleId: style.id, url: style.url, referrerPolicy: style.referrerPolicy ?? null }));
}

function getBaseStyleId(mapOptions: IMapOptions): string | null {
  const stylesList = mapOptions.styles ?? (mapOptions.style ? [mapOptions.style] : [null]);
  return stylesList[0]?.id ?? null;
}

function replayRegisteredImages(mapElement: HTMLElement): void | Promise<void> {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  const images = Array.from(getRegisteredImageStore(map).values());
  if (images.length === 0) {
    return;
  }

  return setImages(mapElement, images);
}

function getCustomControlStore(map: MapLibreMap): Map<string, CustomControlRegistration> {
  const existing = window.Spillgebees.Map.customControlRegistrations.get(map);
  if (existing) {
    return existing;
  }

  const created = new Map<string, CustomControlRegistration>();
  window.Spillgebees.Map.customControlRegistrations.set(map, created);
  return created;
}

function getOrderedRegistrations(map: MapLibreMap, controlsPayload: IMapControl[]): OrderedControlRegistration[] {
  const registrations: OrderedControlRegistration[] = [];
  for (const [declarationOrder, controlDefinition] of controlsPayload.entries()) {
    if (!controlDefinition.enabled) {
      continue;
    }

    const customRegistration = getCustomControlStore(map).get(controlDefinition.controlId);
    let control = customRegistration?.control;
    if (!control) {
      control = createControlFromDefinition(controlDefinition);
    }

    if (!control) {
      continue;
    }

    registrations.push({
      controlId: controlDefinition.controlId,
      control,
      position: controlDefinition.position,
      order: controlDefinition.order,
      declarationOrder,
    });
  }

  registrations.sort((left, right) => {
    if (left.position !== right.position) {
      // cross-position ordering is irrelevant; stable sort preserves declaration order across buckets
      return 0;
    }

    if (left.order !== right.order) {
      return left.order - right.order;
    }

    if (left.declarationOrder !== right.declarationOrder) {
      return left.declarationOrder - right.declarationOrder;
    }

    return left.controlId.localeCompare(right.controlId);
  });

  return registrations;
}

function createControlFromDefinition(control: IMapControl): IControl | null {
  switch (control.kind) {
    case "navigation":
      return new NavigationControl({
        showCompass: control.showCompass,
        showZoom: control.showZoom,
      });
    case "scale":
      return new ScaleControl({
        unit: control.unit,
      });
    case "fullscreen":
      return new FullscreenControl();
    case "geolocate":
      return new GeolocateControl({
        trackUserLocation: control.trackUser,
      });
    case "terrain":
      return new TerrainControl();
    case "center":
      return new CenterControl();
    case "legend":
    case "content":
      return null;
    default:
      return null;
  }
}

function ensureUniqueControlIds(controlsPayload: IMapControl[]): void {
  const seen = new Set<string>();
  for (const control of controlsPayload) {
    if (!control.controlId || control.controlId.trim().length === 0) {
      throw new Error("Control IDs must be non-empty.");
    }

    if (seen.has(control.controlId)) {
      throw new Error(`Control IDs must be unique. Duplicate ID: '${control.controlId}'.`);
    }

    seen.add(control.controlId);
  }
}

function recomposeControls(mapElement: HTMLElement, controlsPayload: IMapControl[]): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  ensureUniqueControlIds(controlsPayload);
  window.Spillgebees.Map.controlsPayload.set(map, structuredClone(controlsPayload));

  const existingControls = window.Spillgebees.Map.controls.get(map);
  if (existingControls) {
    for (const control of existingControls) {
      map.removeControl(control);
    }
    existingControls.clear();
  }

  const controls = window.Spillgebees.Map.controls.get(map) ?? new Set<IControl>();
  for (const registration of getOrderedRegistrations(map, controlsPayload)) {
    map.addControl(registration.control, registration.position);
    controls.add(registration.control);
  }

  window.Spillgebees.Map.controls.set(map, controls);
}

function hasImageChanged(previous: RegisteredMapImage | undefined, next: IMapImageDefinition): boolean {
  if (!previous) {
    return true;
  }

  return (
    previous.url !== next.url ||
    previous.width !== next.width ||
    previous.height !== next.height ||
    previous.pixelRatio !== next.pixelRatio ||
    previous.sdf !== next.sdf
  );
}

async function addImageRegistration(
  mapElement: HTMLElement,
  definition: IMapImageDefinition,
  shouldAbort: () => boolean,
): Promise<void> {
  await addImage(
    mapElement,
    definition.name,
    definition.url,
    definition.width,
    definition.height,
    definition.pixelRatio,
    definition.sdf,
    shouldAbort,
  );
}

export async function setImages(mapElement: HTMLElement, images: IMapImageDefinition[]): Promise<void> {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  const currentVersion = (window.Spillgebees.Map.imageSyncVersion.get(map) ?? 0) + 1;
  window.Spillgebees.Map.imageSyncVersion.set(map, currentVersion);

  const imageStore = getRegisteredImageStore(map);
  const nextByName = new Map<string, IMapImageDefinition>();
  for (const image of images) {
    nextByName.set(image.name, image);
  }

  for (const existingName of Array.from(imageStore.keys())) {
    if (!nextByName.has(existingName)) {
      if (map.hasImage(existingName)) {
        map.removeImage(existingName);
      }

      imageStore.delete(existingName);
    }
  }

  for (const image of images) {
    const previous = imageStore.get(image.name);
    const changed = hasImageChanged(previous, image);
    const missingAtRuntime = !map.hasImage(image.name);

    if (changed && !missingAtRuntime) {
      map.removeImage(image.name);
    }

    if (changed || missingAtRuntime) {
      await addImageRegistration(mapElement, image, () => {
        const latestVersion = window.Spillgebees.Map.imageSyncVersion.get(map);
        return latestVersion !== currentVersion;
      });
    }

    const latestVersion = window.Spillgebees.Map.imageSyncVersion.get(map);
    if (latestVersion !== currentVersion) {
      imageStore.delete(image.name);
      return;
    }

    if (changed || missingAtRuntime) {
      imageStore.set(image.name, {
        name: image.name,
        url: image.url,
        width: image.width,
        height: image.height,
        pixelRatio: image.pixelRatio,
        sdf: image.sdf,
      });
    }
  }
}

function registerStyleReloadHandlers(mapElement: HTMLElement, map: MapLibreMap): void {
  map.on("styledata", async () => {
    if (!window.Spillgebees.Map.pendingStyleReloads.has(map)) {
      return;
    }

    window.Spillgebees.Map.pendingStyleReloads.delete(map);
    ensureShapeLayers(map);
    setupShapePopupHandlers(map);

    const featureStorage = window.Spillgebees.Map.features.get(map);
    if (featureStorage) {
      const markers = Array.from(featureStorage.markers.values()).map((entry) => entry.data);
      const markerIds = markers.map((marker) => marker.id);

      if (markerIds.length > 0) {
        removeMarkers(markerIds, featureStorage);
        addMarkers(map, markers, featureStorage);
      }

      const circles = Array.from(featureStorage.circles.values());
      if (circles.length > 0) {
        updateCircles(map, circles, featureStorage);
      }

      const polylines = Array.from(featureStorage.polylines.values());
      if (polylines.length > 0) {
        updatePolylines(map, polylines, featureStorage);
      }
    }

    const controlsPayload = window.Spillgebees.Map.controlsPayload.get(map);
    if (controlsPayload) {
      setControls(mapElement, controlsPayload);
    }

    const overlays = Array.from((window.Spillgebees.Map.overlays.get(map) ?? new Map()).values()) as ITileOverlay[];
    setOverlays(mapElement, overlays);

    await replayStyleReloadState(mapElement, {
      replayImages: () => replayRegisteredImages(mapElement),
      replayComposedOverlays: async () => {
        const mapOptions = window.Spillgebees.Map.mapOptions.get(map);
        const overlayStyles = window.Spillgebees.Map.overlayStyleRequests.get(map) ?? [];
        if (overlayStyles.length === 0) {
          return;
        }

        const composedGlyphsUrl = mapOptions?.composedGlyphsUrl ?? null;
        if (composedGlyphsUrl) {
          const currentGlyphs = map.getStyle()?.glyphs;
          if (currentGlyphs !== composedGlyphsUrl) {
            const style = map.getStyle();
            style.glyphs = composedGlyphsUrl;
            map.setStyle(style, { diff: true });
          }
        }

        await applyOverlayStyles(map, overlayStyles, {
          forceReapply: true,
        });
      },
      onAfterReplay: async () => {
        const dotNetHelper = window.Spillgebees.Map.dotNetHelpers.get(map);
        if (!dotNetHelper) {
          return;
        }

        // biome-ignore lint/security/noSecrets: C# callback method name, not a secret
        await dotNetHelper.invokeMethodAsync("OnMapStyleReloadedAsync");
      },
    });
  });
}

export function bootstrap(): void {
  window.Spillgebees = window.Spillgebees || ({} as typeof window.Spillgebees);

  if (isNamespaceCompatible()) {
    // Already initialized with the correct protocol version — nothing to do.
    return;
  }

  // Either no namespace exists, or an outdated/incompatible one is present.
  // Force-clear and reinitialize with the current version.
  initializeNamespace();
}

// --- Helper: build MapLibre style from IMapStyle ---

/**
 * Converts an `IMapStyle` (from C# interop) to a MapLibre-compatible style.
 * Returns either a string URL or a `StyleSpecification` object.
 */
export function buildStyleFromOptions(style: IMapStyle | null): string | StyleSpecification {
  if (style === null) {
    return DEFAULT_STYLE_URL;
  }

  // Prefer URL over raster/WMS sources
  if (style.url) {
    return style.url;
  }

  if (style.rasterSource) {
    return {
      version: 8,
      sources: {
        "raster-tiles": {
          type: "raster",
          tiles: [style.rasterSource.urlTemplate],
          tileSize: style.rasterSource.tileSize,
          attribution: style.rasterSource.attribution,
        },
      },
      layers: [{ id: "raster-layer", type: "raster", source: "raster-tiles" }],
    };
  }

  if (style.wmsSource) {
    const { baseUrl, layers, format, transparent, version, tileSize } = style.wmsSource;
    // WMS 1.3.0 introduced CRS; all earlier versions (1.0.0, 1.1.0, 1.1.1) use SRS
    const crsParam = version === "1.3.0" ? "CRS" : "SRS";
    const wmsUrl = [
      `${baseUrl}?SERVICE=WMS`,
      `&VERSION=${version}`,
      // biome-ignore lint/security/noSecrets: WMS query parameter, not a secret
      "&REQUEST=GetMap",
      `&LAYERS=${layers}`,
      `&FORMAT=${format}`,
      `&TRANSPARENT=${String(transparent)}`,
      `&${crsParam}=EPSG:3857`,
      "&STYLES=",
      `&WIDTH=${String(tileSize)}`,
      `&HEIGHT=${String(tileSize)}`,
      "&BBOX={bbox-epsg-3857}",
    ].join("");

    return {
      version: 8,
      sources: {
        "raster-tiles": {
          type: "raster",
          tiles: [wmsUrl],
          tileSize: style.wmsSource.tileSize,
          attribution: style.wmsSource.attribution,
        },
      },
      layers: [{ id: "raster-layer", type: "raster", source: "raster-tiles" }],
    };
  }

  // Fallback to default if no style configuration is recognized
  return DEFAULT_STYLE_URL;
}

// --- Core map lifecycle functions ---

export function createMap(
  dotNetHelper: DotNet.DotNetObject,
  callbackName: string,
  mapElement: HTMLElement,
  mapOptions: IMapOptions,
  controlsPayload: IMapControl[],
  theme: string,
  markers: IMarker[],
  circles: ICircle[],
  polylines: IPolyline[],
  overlays: ITileOverlay[],
): void {
  // preload web fonts (fire-and-forget — fonts load in parallel with map init)
  if (mapOptions.webFonts) {
    for (const font of mapOptions.webFonts) {
      document.fonts.load(font);
    }
  }

  // Resolve styles: Styles list takes precedence over single Style
  const stylesList = mapOptions.styles ?? (mapOptions.style ? [mapOptions.style] : [null]);
  const baseStyle = buildStyleFromOptions(stylesList[0] ?? null);
  const overlayStyles = getComposedOverlayStyles(mapOptions);
  const overlayStyleUrls = overlayStyles.map((style) => style.url);

  let map: MapLibreMap | null = null;
  const transformRequest = createMapTransformRequest(() => map);
  map = new MapLibreMap({
    container: mapElement,
    style: baseStyle,
    center: [mapOptions.center.longitude, mapOptions.center.latitude],
    zoom: mapOptions.zoom,
    pitch: mapOptions.pitch,
    bearing: mapOptions.bearing,
    minZoom: mapOptions.minZoom ?? undefined,
    maxZoom: mapOptions.maxZoom ?? undefined,
    maxBounds: mapOptions.maxBounds
      ? [
          [mapOptions.maxBounds.southwest.longitude, mapOptions.maxBounds.southwest.latitude],
          [mapOptions.maxBounds.northeast.longitude, mapOptions.maxBounds.northeast.latitude],
        ]
      : undefined,
    interactive: mapOptions.interactive,
    cooperativeGestures: mapOptions.cooperativeGestures,
    attributionControl: true,
    transformRequest,
  });

  // Store the map instance
  window.Spillgebees.Map.maps.set(mapElement, map);

  // Store the dotNetHelper for event callbacks
  window.Spillgebees.Map.dotNetHelpers.set(map, dotNetHelper);

  // Initialize empty feature storage
  const featureStorage: FeatureStorage = {
    markers: new Map(),
    circles: new Map(),
    polylines: new Map(),
    circleData: new Map(),
    polylineData: new Map(),
  };
  window.Spillgebees.Map.features.set(map, featureStorage);

  // Initialize overlay storage
  window.Spillgebees.Map.overlays.set(map, new Map());

  // Initialize control storage
  window.Spillgebees.Map.controls.set(map, new Set());
  window.Spillgebees.Map.controlsPayload.set(map, structuredClone(controlsPayload));
  window.Spillgebees.Map.customControlRegistrations.set(map, new Map());
  window.Spillgebees.Map.mapOptions.set(map, mapOptions);
  window.Spillgebees.Map.requestContexts.set(map, {
    mapOptions: structuredClone(mapOptions),
    overlays: structuredClone(overlays),
  });

  // Initialize custom style state stores
  window.Spillgebees.Map.sourceSpecs.set(map, new Map());
  window.Spillgebees.Map.layerSpecs.set(map, new Map());
  window.Spillgebees.Map.imageRegistrations.set(map, new Map());
  window.Spillgebees.Map.layerEventSubscriptions.set(map, new Map());
  window.Spillgebees.Map.visibilityGroups.set(map, new Map());
  window.Spillgebees.Map.overlayStyleUrls.set(map, [...overlayStyleUrls]);
  window.Spillgebees.Map.overlayStyleRequests.set(map, structuredClone(overlayStyles));
  window.Spillgebees.Map.composedStyleLayerIds.set(map, new Map());
  window.Spillgebees.Map.imageSyncVersion.set(map, 0);

  // Track the current style for diffing in setMapOptions
  window.Spillgebees.Map.styles.set(map, baseStyle);

  registerStyleReloadHandlers(mapElement, map);

  // Apply theme
  if (theme === "dark") {
    mapElement.classList.add("sgb-map-dark");
  }

  // Handle map load event
  map.on("load", () => {
    // Handle projection
    if (mapOptions.projection === "globe") {
      map.setProjection("globe");
    }

    // Apply controls
    setControls(mapElement, controlsPayload);

    // Set up shape layers (circles + polylines) — needed for popup event handlers
    ensureShapeLayers(map);
    setupShapePopupHandlers(map);

    // Apply features
    syncFeatures(mapElement, {
      markers: { added: markers, updated: [], removedIds: [] },
      circles: { added: circles, updated: [], removedIds: [] },
      polylines: { added: polylines, updated: [], removedIds: [] },
    });

    // Apply overlays — Phase 6
    setOverlays(mapElement, overlays);

    // fitBoundsOptions is applied from C# after resize (OnMapInitializedAsync)
    // to ensure the container has its final dimensions

    // Wire map events for C# callbacks
    map.on("click", (e: { lngLat: { lat: number; lng: number } }) => {
      // biome-ignore lint/security/noSecrets: C# callback method name, not a secret
      dotNetHelper.invokeMethodAsync("OnMapClickCallbackAsync", {
        position: { latitude: e.lngLat.lat, longitude: e.lngLat.lng },
      });
    });

    map.on("moveend", () => {
      const center = map.getCenter();
      // biome-ignore lint/security/noSecrets: C# callback method name, not a secret
      dotNetHelper.invokeMethodAsync("OnMoveEndCallbackAsync", {
        center: { latitude: center.lat, longitude: center.lng },
        zoom: map.getZoom(),
        bearing: map.getBearing(),
        pitch: map.getPitch(),
      });
    });

    map.on("zoomend", () => {
      const center = map.getCenter();
      // biome-ignore lint/security/noSecrets: C# callback method name, not a secret
      dotNetHelper.invokeMethodAsync("OnZoomEndCallbackAsync", {
        center: { latitude: center.lat, longitude: center.lng },
        zoom: map.getZoom(),
        bearing: map.getBearing(),
        pitch: map.getPitch(),
      });
    });

    // Apply overlay styles (async — fetches style JSONs and merges sources/layers)
    if (overlayStyles.length > 0) {
      (async () => {
        const glyphResult = await validateComposedGlyphs(map, overlayStyles, mapOptions.composedGlyphsUrl);

        if (glyphResult.proceed) {
          if (glyphResult.effectiveGlyphsUrl) {
            const style = map.getStyle();
            style.glyphs = glyphResult.effectiveGlyphsUrl;
            map.setStyle(style, { diff: true });
          }

          await applyOverlayStyles(map, overlayStyles);
        }

        dotNetHelper.invokeMethodAsync(callbackName);
      })();
    } else {
      dotNetHelper.invokeMethodAsync(callbackName);
    }
  });
}

export function disposeMap(mapElement: HTMLElement): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);

  if (!map) {
    return;
  }

  // Clean up feature storage
  window.Spillgebees.Map.features.delete(map);

  // Clean up overlay storage
  window.Spillgebees.Map.overlays.delete(map);

  // Clean up control storage
  window.Spillgebees.Map.controls.delete(map);
  window.Spillgebees.Map.customControlRegistrations.delete(map);
  window.Spillgebees.Map.controlsPayload.delete(map);

  // Clean up style storage
  window.Spillgebees.Map.styles.delete(map);
  window.Spillgebees.Map.mapOptions.delete(map);
  window.Spillgebees.Map.requestContexts.delete(map);

  // Clean up custom style state stores
  window.Spillgebees.Map.sourceSpecs.delete(map);
  window.Spillgebees.Map.layerSpecs.delete(map);
  window.Spillgebees.Map.imageRegistrations.delete(map);
  window.Spillgebees.Map.layerEventSubscriptions.delete(map);
  window.Spillgebees.Map.visibilityGroups.delete(map);
  window.Spillgebees.Map.overlayStyleUrls.delete(map);
  window.Spillgebees.Map.overlayStyleRequests.delete(map);
  window.Spillgebees.Map.composedStyleLayerIds.delete(map);
  window.Spillgebees.Map.imageSyncVersion.delete(map);

  // Clean up dotNetHelper storage
  window.Spillgebees.Map.dotNetHelpers.delete(map);

  // Destroy the MapLibre map and release WebGL resources
  map.remove();

  // Remove from the map store
  window.Spillgebees.Map.maps.delete(mapElement);
}

export function setMapOptions(mapElement: HTMLElement, mapOptions: IMapOptions): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);

  if (!map) {
    return;
  }

  window.Spillgebees.Map.mapOptions.set(map, mapOptions);
  const existingOverlays = Array.from(
    (window.Spillgebees.Map.overlays.get(map) ?? new Map()).values(),
  ) as ITileOverlay[];
  updateMapRequestContext(map, mapOptions, existingOverlays);

  // Update pitch and bearing
  map.setPitch(mapOptions.pitch);
  map.setBearing(mapOptions.bearing);

  // Update maxBounds
  if (mapOptions.maxBounds) {
    map.setMaxBounds([
      [mapOptions.maxBounds.southwest.longitude, mapOptions.maxBounds.southwest.latitude],
      [mapOptions.maxBounds.northeast.longitude, mapOptions.maxBounds.northeast.latitude],
    ]);
  } else {
    map.setMaxBounds(undefined as unknown as Parameters<typeof map.setMaxBounds>[0]);
  }

  // Update minZoom and maxZoom
  map.setMinZoom(mapOptions.minZoom ?? undefined);
  map.setMaxZoom(mapOptions.maxZoom ?? undefined);

  // Resolve styles list
  const stylesList = mapOptions.styles ?? (mapOptions.style ? [mapOptions.style] : [null]);
  const newBaseStyle = buildStyleFromOptions(stylesList[0] ?? null);
  const overlayStyles = getComposedOverlayStyles(mapOptions);
  const overlayStyleUrls = overlayStyles.map((style) => style.url);
  window.Spillgebees.Map.overlayStyleUrls.set(map, [...overlayStyleUrls]);
  window.Spillgebees.Map.overlayStyleRequests.set(map, structuredClone(overlayStyles));

  // Update base style only if it actually changed (setStyle triggers a full tile reload)
  const currentStyle = window.Spillgebees.Map.styles.get(map);
  const newStyleKey = typeof newBaseStyle === "string" ? newBaseStyle : JSON.stringify(newBaseStyle);
  const currentStyleKey = typeof currentStyle === "string" ? currentStyle : JSON.stringify(currentStyle);

  getComposedStyleLayerStore(map).clear();

  if (newStyleKey !== currentStyleKey) {
    window.Spillgebees.Map.pendingStyleReloads.add(map);
    map.setStyle(newBaseStyle);
    window.Spillgebees.Map.styles.set(map, newBaseStyle);
  } else {
    // Base style unchanged — validate glyphs and update overlays
    void (async () => {
      const glyphResult = await validateComposedGlyphs(map, overlayStyles, mapOptions.composedGlyphsUrl);

      if (!glyphResult.proceed) {
        return;
      }

      if (glyphResult.effectiveGlyphsUrl) {
        const style = map.getStyle();
        style.glyphs = glyphResult.effectiveGlyphsUrl;
        map.setStyle(style, { diff: true });
      }

      await applyOverlayStyles(map, overlayStyles);
    })();
  }

  // Handle projection (only when changed)
  // getProjection() returns undefined when the style has no explicit projection field
  const newProjection = mapOptions.projection === "globe" ? "globe" : "mercator";
  const currentProjection = map.getProjection()?.type ?? "mercator";
  if (currentProjection !== newProjection) {
    map.setProjection(newProjection);
  }

  // Update viewport last so nothing can override it
  if (mapOptions.fitBoundsOptions) {
    fitBounds(mapElement, mapOptions.fitBoundsOptions);
  } else {
    map.jumpTo({
      center: [mapOptions.center.longitude, mapOptions.center.latitude],
      zoom: mapOptions.zoom,
    });
  }
}

export function setTheme(mapElement: HTMLElement, theme: string): void {
  if (theme === "dark") {
    mapElement.classList.add("sgb-map-dark");
  } else {
    mapElement.classList.remove("sgb-map-dark");
  }
}

export function resize(mapElement: HTMLElement): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);

  if (!map) {
    return;
  }

  map.resize();
}

// --- Stub functions — will be implemented in later phases ---

export interface IFeatureSyncPayload {
  markers: { added: IMarker[]; updated: IMarker[]; removedIds: string[] };
  circles: { added: ICircle[]; updated: ICircle[]; removedIds: string[] };
  polylines: { added: IPolyline[]; updated: IPolyline[]; removedIds: string[] };
}

export function syncFeatures(mapElement: HTMLElement, payload: IFeatureSyncPayload): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  const storage = window.Spillgebees.Map.features.get(map);
  if (!storage) {
    return;
  }

  // Handle markers
  if (payload.markers.removedIds.length > 0) {
    removeMarkers(payload.markers.removedIds, storage);
  }
  if (payload.markers.added.length > 0) {
    addMarkers(map, payload.markers.added, storage);
  }
  if (payload.markers.updated.length > 0) {
    updateMarkers(map, payload.markers.updated, storage);
  }

  // Handle circles
  if (payload.circles.removedIds.length > 0) {
    removeCircles(map, payload.circles.removedIds, storage);
  }
  if (payload.circles.added.length > 0) {
    addCircles(map, payload.circles.added, storage);
  }
  if (payload.circles.updated.length > 0) {
    updateCircles(map, payload.circles.updated, storage);
  }

  // Handle polylines
  if (payload.polylines.removedIds.length > 0) {
    removePolylines(map, payload.polylines.removedIds, storage);
  }
  if (payload.polylines.added.length > 0) {
    addPolylines(map, payload.polylines.added, storage);
  }
  if (payload.polylines.updated.length > 0) {
    updatePolylines(map, payload.polylines.updated, storage);
  }
}

export function setOverlays(mapElement: HTMLElement, overlays: ITileOverlay[]): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const existingOverlays = window.Spillgebees.Map.overlays.get(map) ?? new Map<string, unknown>();
  const currentMapOptions = window.Spillgebees.Map.mapOptions.get(map);
  if (currentMapOptions) {
    updateMapRequestContext(map, currentMapOptions, overlays);
  }
  const newOverlayIds = new Set(overlays.map((o) => o.id));

  // Remove overlays that are no longer in the list
  for (const [id] of existingOverlays) {
    if (!newOverlayIds.has(id)) {
      if (map.getLayer(`sgb-overlay-${id}`)) {
        map.removeLayer(`sgb-overlay-${id}`);
      }
      if (map.getSource(`sgb-overlay-${id}`)) {
        map.removeSource(`sgb-overlay-${id}`);
      }
      existingOverlays.delete(id);
    }
  }

  // Add new overlays
  for (const overlay of overlays) {
    const sourceId = `sgb-overlay-${overlay.id}`;

    if (existingOverlays.has(overlay.id) && map.getSource(sourceId) && map.getLayer(sourceId)) {
      continue;
    }

    map.addSource(sourceId, {
      type: "raster",
      tiles: [overlay.urlTemplate],
      tileSize: overlay.tileSize,
      attribution: overlay.attribution,
    });

    map.addLayer({
      id: sourceId, // use same ID for source and layer for simplicity
      type: "raster",
      source: sourceId,
      paint: {
        "raster-opacity": overlay.opacity,
      },
    });

    existingOverlays.set(overlay.id, overlay);
  }

  window.Spillgebees.Map.overlays.set(map, existingOverlays);
}

export function setControls(mapElement: HTMLElement, controlsPayload: IMapControl[]): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  recomposeControls(mapElement, controlsPayload);
}

function validateControlForContent(mapElement: HTMLElement, controlId: string, kind: string): IMapControl | null {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return null;
  }

  const controlsPayload = window.Spillgebees.Map.controlsPayload.get(map) ?? [];
  const control = controlsPayload.find((entry) => entry.controlId === controlId) ?? null;
  if (!control) {
    // biome-ignore lint/suspicious/noConsole: explicit control diagnostics for interop mismatches
    console.warn(
      `[Spillgebees.Map] setControlContent ignored: controlId='${controlId}' was not found for expected kind='${kind}'.`,
    );
    return null;
  }

  if (control.kind !== kind) {
    // biome-ignore lint/suspicious/noConsole: explicit control diagnostics for interop mismatches
    console.warn(
      `[Spillgebees.Map] setControlContent ignored: controlId='${controlId}' expected kind='${kind}' but actual kind='${control.kind}'.`,
    );
    return null;
  }

  return control;
}

export function setControlContent(
  mapElement: HTMLElement,
  controlId: string,
  kind: string,
  placeholderHost?: HTMLElement,
  contentRoot?: HTMLElement,
): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  if (kind !== LEGEND_CONTROL_KIND) {
    throw new Error(`Unsupported control content kind '${kind}' for control '${controlId}'.`);
  }

  const customControlStore = getCustomControlStore(map);
  const existingRegistration = customControlStore.get(controlId);
  const controlDefinition = validateControlForContent(mapElement, controlId, kind);
  if (!controlDefinition) {
    return;
  }

  if (
    existingRegistration &&
    existingRegistration.kind === kind &&
    existingRegistration.control instanceof LegendControl &&
    kind === LEGEND_CONTROL_KIND
  ) {
    existingRegistration.control.update(controlDefinition as ILegendMapControl);
    customControlStore.set(controlId, existingRegistration);
    return;
  }

  let control: IControl;
  if (kind === LEGEND_CONTROL_KIND) {
    if (!(placeholderHost instanceof HTMLElement) || !(contentRoot instanceof HTMLElement)) {
      return;
    }

    control = new LegendControl(controlDefinition as ILegendMapControl, placeholderHost, contentRoot);
  } else {
    throw new Error(`Unsupported control content kind '${kind}' for control '${controlId}'.`);
  }

  if (existingRegistration) {
    const controls = window.Spillgebees.Map.controls.get(map);
    if (controls?.has(existingRegistration.control)) {
      map.removeControl(existingRegistration.control);
      controls.delete(existingRegistration.control);
    }
  }

  customControlStore.set(controlId, {
    controlId,
    kind: kind as "legend" | "content",
    control,
  });

  recomposeControls(mapElement, window.Spillgebees.Map.controlsPayload.get(map) ?? createDefaultControls());
}

export function removeControlContent(mapElement: HTMLElement, controlId: string): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  const customControlStore = getCustomControlStore(map);
  const existingRegistration = customControlStore.get(controlId);
  if (!existingRegistration) {
    return;
  }

  customControlStore.delete(controlId);
  recomposeControls(mapElement, window.Spillgebees.Map.controlsPayload.get(map) ?? createDefaultControls());
}

export function fitBounds(mapElement: HTMLElement, options: IFitBoundsOptions): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const storage = window.Spillgebees.Map.features.get(map);
  if (!storage) return;

  // Collect all coordinates for the requested feature IDs
  const coordinates: [number, number][] = [];

  for (const id of options.featureIds) {
    // Check markers
    const markerEntry = storage.markers.get(id);
    if (markerEntry) {
      const lngLat = markerEntry.marker.getLngLat();
      coordinates.push([lngLat.lng, lngLat.lat]);
      continue;
    }

    // Check circles
    const circleFeature = storage.circleData.get(id);
    if (circleFeature && circleFeature.geometry.type === "Point") {
      const coords = (circleFeature.geometry as GeoJSON.Point).coordinates;
      coordinates.push([coords[0], coords[1]]);
      continue;
    }

    // Check polylines
    const polylineFeature = storage.polylineData.get(id);
    if (polylineFeature && polylineFeature.geometry.type === "LineString") {
      const lineCoords = (polylineFeature.geometry as GeoJSON.LineString).coordinates;
      for (const coord of lineCoords) {
        coordinates.push([coord[0], coord[1]]);
      }
    }
  }

  if (coordinates.length === 0) return;

  // Calculate bounds
  let minLng = coordinates[0][0];
  let maxLng = coordinates[0][0];
  let minLat = coordinates[0][1];
  let maxLat = coordinates[0][1];

  for (const [lng, lat] of coordinates) {
    if (lng < minLng) minLng = lng;
    if (lng > maxLng) maxLng = lng;
    if (lat < minLat) minLat = lat;
    if (lat > maxLat) maxLat = lat;
  }

  // Build padding options
  const fitOptions: Record<string, unknown> = {};

  if (options.padding) {
    fitOptions.padding = {
      top: options.padding.y,
      bottom: options.padding.y,
      left: options.padding.x,
      right: options.padding.x,
    };
  } else if (options.topLeftPadding || options.bottomRightPadding) {
    fitOptions.padding = {
      top: options.topLeftPadding?.y ?? 0,
      left: options.topLeftPadding?.x ?? 0,
      bottom: options.bottomRightPadding?.y ?? 0,
      right: options.bottomRightPadding?.x ?? 0,
    };
  }

  map.fitBounds(
    [
      [minLng, minLat],
      [maxLng, maxLat],
    ],
    fitOptions,
  );
}

export function flyTo(
  mapElement: HTMLElement,
  center: ICoordinate,
  zoom: number | null,
  bearing: number | null,
  pitch: number | null,
): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const options: Record<string, unknown> = {
    center: [center.longitude, center.latitude],
  };

  if (zoom !== null) options.zoom = zoom;
  if (bearing !== null) options.bearing = bearing;
  if (pitch !== null) options.pitch = pitch;

  map.flyTo(options);
}

export function getCenter(mapElement: HTMLElement): ICoordinate | null {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return null;
  }

  const center = map.getCenter();
  return { latitude: center.lat, longitude: center.lng };
}

export function getZoom(mapElement: HTMLElement): number | null {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return null;
  }

  return map.getZoom();
}

export function hasLayer(mapElement: HTMLElement, layerId: string): boolean {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return false;
  }

  return map.getLayer(layerId) !== undefined;
}

export function hasStyleLayer(mapElement: HTMLElement, styleId: string, layerId: string): boolean {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return false;
  }

  const mapOptions = window.Spillgebees.Map.mapOptions.get(map);
  if (mapOptions && getBaseStyleId(mapOptions) === styleId) {
    return map.getLayer(layerId) !== undefined;
  }

  const runtimeLayerId = getResolvedStyleLayerId(map, styleId, layerId);
  if (!runtimeLayerId) {
    return false;
  }

  return map.getLayer(runtimeLayerId) !== undefined;
}

export function getBounds(mapElement: HTMLElement): { southwest: ICoordinate; northeast: ICoordinate } | null {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return null;
  }

  const bounds = map.getBounds();
  const southwest = bounds.getSouthWest();
  const northeast = bounds.getNorthEast();

  return {
    southwest: { latitude: southwest.lat, longitude: southwest.lng },
    northeast: { latitude: northeast.lat, longitude: northeast.lng },
  };
}

export function queryRenderedFeatures(
  mapElement: HTMLElement,
  point: { x: number; y: number },
  layerIds: string[] | null,
): Array<{ id: unknown; layerId: string | null; geometry: unknown; properties: unknown }> {
  const map = window.Spillgebees.Map.maps.get(mapElement);
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

export function setStyleLayerVisibility(
  mapElement: HTMLElement,
  styleId: string,
  layerId: string,
  visible: boolean,
): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  const mapOptions = window.Spillgebees.Map.mapOptions.get(map);
  if (mapOptions && getBaseStyleId(mapOptions) === styleId) {
    setLayerVisibility(mapElement, layerId, visible);
    return;
  }

  const runtimeLayerId = getResolvedStyleLayerId(map, styleId, layerId);
  if (!runtimeLayerId) {
    return;
  }

  setLayerVisibility(mapElement, runtimeLayerId, visible);
}

export function setTrackedEntityFeatureState(
  mapElement: HTMLElement,
  primarySourceId: string,
  decorationSourceId: string | null,
  entityId: string,
  state: Record<string, unknown>,
): void {
  setFeatureState(mapElement, primarySourceId, entityId, state);

  if (decorationSourceId) {
    setFeatureState(mapElement, decorationSourceId, entityId, state);
  }
}
