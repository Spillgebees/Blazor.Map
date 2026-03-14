import type { DotNet } from "@microsoft/dotnet-js-interop";
import type { IControl, StyleSpecification } from "maplibre-gl";
import {
  FullscreenControl,
  GeolocateControl,
  Map as MapLibreMap,
  NavigationControl,
  ScaleControl,
  TerrainControl,
} from "maplibre-gl";
import { CenterControl } from "./controls/centerControl";
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
import type { IMapControlOptions } from "./interfaces/controls";
import type { ICircle, IMarker, IPolyline } from "./interfaces/features";
import type { ICoordinate, IFitBoundsOptions, IMapOptions, IMapStyle, ITileOverlay } from "./interfaces/map";
import type { FeatureStorage } from "./types/feature-storage";

export const PROTOCOL_VERSION = 1;

const DEFAULT_STYLE_URL = "https://tiles.openfreemap.org/styles/liberty";

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
      setMapOptions,
      setTheme,
      fitBounds,
      flyTo,
      resize,
      disposeMap,
    },
    maps: new Map<HTMLElement, MapLibreMap>(),
    features: new Map<MapLibreMap, FeatureStorage>(),
    overlays: new Map<MapLibreMap, Map<string, unknown>>(),
    controls: new Map<MapLibreMap, Set<IControl>>(),
    styles: new Map<MapLibreMap, string | StyleSpecification>(),
    dotNetHelpers: new Map<MapLibreMap, DotNet.DotNetObject>(),
  };
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
    const wmsUrl = [
      `${baseUrl}?SERVICE=WMS`,
      `&VERSION=${version}`,
      // biome-ignore lint/security/noSecrets: WMS query parameter, not a secret
      "&REQUEST=GetMap",
      `&LAYERS=${layers}`,
      `&FORMAT=${format}`,
      `&TRANSPARENT=${String(transparent)}`,
      "&SRS=EPSG:3857",
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
  controlOptions: IMapControlOptions,
  theme: string,
  markers: IMarker[],
  circles: ICircle[],
  polylines: IPolyline[],
  overlays: ITileOverlay[],
): void {
  const style = buildStyleFromOptions(mapOptions.style);

  const map = new MapLibreMap({
    container: mapElement,
    style,
    center: [mapOptions.center.longitude, mapOptions.center.latitude],
    zoom: mapOptions.zoom,
    pitch: mapOptions.pitch,
    bearing: mapOptions.bearing,
    minZoom: mapOptions.minZoom ?? undefined,
    maxZoom: mapOptions.maxZoom ?? undefined,
    interactive: mapOptions.interactive,
    cooperativeGestures: mapOptions.cooperativeGestures,
    attributionControl: true,
  });

  // Store the map instance
  window.Spillgebees.Map.maps.set(mapElement, map);

  // Store the dotNetHelper for event callbacks
  window.Spillgebees.Map.dotNetHelpers.set(map, dotNetHelper);

  // Initialize empty feature storage
  const featureStorage: FeatureStorage = {
    markers: new Map(),
    circleData: new Map(),
    polylineData: new Map(),
  };
  window.Spillgebees.Map.features.set(map, featureStorage);

  // Initialize overlay storage
  window.Spillgebees.Map.overlays.set(map, new Map());

  // Initialize control storage
  window.Spillgebees.Map.controls.set(map, new Set());

  // Track the current style for diffing in setMapOptions
  window.Spillgebees.Map.styles.set(map, style);

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
    setControls(mapElement, controlOptions);

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

    // Handle fitBoundsOptions
    if (mapOptions.fitBoundsOptions) {
      fitBounds(mapElement, mapOptions.fitBoundsOptions);
    }

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

    // Notify C# that the map is ready
    dotNetHelper.invokeMethodAsync(callbackName);
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

  // Clean up style storage
  window.Spillgebees.Map.styles.delete(map);

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

  // Update center and zoom
  map.jumpTo({
    center: [mapOptions.center.longitude, mapOptions.center.latitude],
    zoom: mapOptions.zoom,
  });

  // Update pitch and bearing
  map.setPitch(mapOptions.pitch);
  map.setBearing(mapOptions.bearing);

  // Update style only if it actually changed (setStyle triggers a full tile reload)
  const newStyle = buildStyleFromOptions(mapOptions.style);
  const currentStyle = window.Spillgebees.Map.styles.get(map);
  const newStyleKey = typeof newStyle === "string" ? newStyle : JSON.stringify(newStyle);
  const currentStyleKey = typeof currentStyle === "string" ? currentStyle : JSON.stringify(currentStyle);

  if (newStyleKey !== currentStyleKey) {
    map.setStyle(newStyle);
    window.Spillgebees.Map.styles.set(map, newStyle);
  }

  // Handle projection
  const projection = mapOptions.projection === "globe" ? "globe" : "mercator";
  map.setProjection(projection);
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
    if (existingOverlays.has(overlay.id)) continue; // already exists

    const sourceId = `sgb-overlay-${overlay.id}`;
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

export function setControls(mapElement: HTMLElement, controlOptions: IMapControlOptions): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  // Remove all existing controls
  const existingControls = window.Spillgebees.Map.controls.get(map);
  if (existingControls) {
    for (const control of existingControls) {
      map.removeControl(control);
    }
    existingControls.clear();
  }

  const controls = window.Spillgebees.Map.controls.get(map) ?? new Set<IControl>();

  // Navigation control (zoom + compass)
  if (controlOptions.navigation?.enable) {
    const nav = new NavigationControl({
      showCompass: controlOptions.navigation.showCompass,
      showZoom: controlOptions.navigation.showZoom,
    });
    map.addControl(nav, controlOptions.navigation.position);
    controls.add(nav);
  }

  // Scale control
  if (controlOptions.scale?.enable) {
    const scale = new ScaleControl({
      unit: controlOptions.scale.unit,
    });
    map.addControl(scale, controlOptions.scale.position);
    controls.add(scale);
  }

  // Fullscreen control
  if (controlOptions.fullscreen?.enable) {
    const fs = new FullscreenControl();
    map.addControl(fs, controlOptions.fullscreen.position);
    controls.add(fs);
  }

  // Geolocate control
  if (controlOptions.geolocate?.enable) {
    const geo = new GeolocateControl({
      trackUserLocation: controlOptions.geolocate.trackUser,
    });
    map.addControl(geo, controlOptions.geolocate.position);
    controls.add(geo);
  }

  // Terrain control
  if (controlOptions.terrain?.enable) {
    const terrain = new TerrainControl();
    map.addControl(terrain, controlOptions.terrain.position);
    controls.add(terrain);
  }

  // Custom center control
  if (controlOptions.center?.enable) {
    const center = new CenterControl(controlOptions.center);
    map.addControl(center, controlOptions.center.position);
    controls.add(center);
  }

  window.Spillgebees.Map.controls.set(map, controls);
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
