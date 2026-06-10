// Popup interactions for the convenience shape layers (Circles/Polylines parameters and
// the MapCircle/MapPolyline components). The shapes themselves are ordinary engine
// sources/layers; this wires the click/hover popup behavior over them. Popup options
// travel as a JSON string feature property (MapLibre flattens nested properties to
// strings at render time anyway).

import type { LngLatLike, Map as MapLibreMap, Popup as MapLibrePopup } from "maplibre-gl";
import { Popup } from "maplibre-gl";
import type { MarkerPopupData } from "./ops";

export const CIRCLES_LAYER_ID = "sgb-circles-layer";
export const POLYLINES_LAYER_ID = "sgb-polylines-layer";

// WeakMaps so entries are GC'd when the MapLibreMap instance is collected
const activeHoverPopups = new WeakMap<MapLibreMap, MapLibrePopup>();
const handlerSubscriptions = new WeakMap<
  MapLibreMap,
  Map<string, { click: (e: ShapeMouseEvent) => void; mouseEnter: (e: ShapeMouseEvent) => void; mouseLeave: () => void }>
>();

interface ShapeMouseEvent {
  lngLat: LngLatLike;
  features?: Array<{ properties?: Record<string, unknown> }>;
}

function showShapePopup(map: MapLibreMap, lngLat: LngLatLike, options: MarkerPopupData): void {
  removeActiveHoverPopup(map);

  // Hover popups should not show close buttons
  const showCloseButton = options.trigger === "click" && options.closeButton;

  const popup = new Popup({
    closeButton: showCloseButton,
    closeOnClick: options.trigger === "click",
    maxWidth: options.maxWidth ?? "240px",
    className: options.className ?? undefined,
    anchor: options.anchor !== "auto" ? (options.anchor as never) : "bottom",
    offset: options.offset ? [options.offset.x, options.offset.y] : undefined,
  }).setLngLat(lngLat);

  if (options.contentMode === "rawHtml") {
    popup.setHTML(options.content);
  } else {
    popup.setText(options.content);
  }

  popup.addTo(map);

  if (options.trigger === "hover") {
    activeHoverPopups.set(map, popup);
  }
}

function removeActiveHoverPopup(map: MapLibreMap): void {
  const popup = activeHoverPopups.get(map);
  if (popup) {
    popup.remove();
    activeHoverPopups.delete(map);
  }
}

/**
 * Extracts the popup position from a feature. For Point geometries (circles), uses the
 * feature's center; for other geometries (polylines), falls back to the event's lngLat
 * (the mouse/click position on the shape).
 */
function getPopupLngLat(feature: GeoJSON.Feature, fallback: LngLatLike): LngLatLike {
  if (feature.geometry.type === "Point") {
    const [lng, lat] = (feature.geometry as GeoJSON.Point).coordinates;
    return { lng, lat };
  }
  return fallback;
}

function parsePopup(e: ShapeMouseEvent): { feature: GeoJSON.Feature; popup: MarkerPopupData } | null {
  const feature = e.features?.[0];
  const raw = feature?.properties?.popup;
  if (!raw) {
    return null;
  }

  // MapLibre serializes feature properties to flat strings — parse back to object
  return { feature: feature as GeoJSON.Feature, popup: JSON.parse(raw as string) as MarkerPopupData };
}

function attachLayerPopupHandlers(map: MapLibreMap, layerId: string): void {
  let store = handlerSubscriptions.get(map);
  if (!store) {
    store = new Map();
    handlerSubscriptions.set(map, store);
  }

  const existing = store.get(layerId);
  if (existing) {
    map.off("click", layerId, existing.click as never);
    map.off("mouseenter", layerId, existing.mouseEnter as never);
    map.off("mouseleave", layerId, existing.mouseLeave);
  }

  const click = (e: ShapeMouseEvent) => {
    const parsed = parsePopup(e);
    if (parsed?.popup.trigger !== "click") {
      return;
    }

    showShapePopup(map, getPopupLngLat(parsed.feature, e.lngLat), parsed.popup);
  };

  const mouseEnter = (e: ShapeMouseEvent) => {
    const parsed = parsePopup(e);
    if (parsed?.popup.trigger !== "hover") {
      return;
    }

    map.getCanvas().style.cursor = "pointer";
    showShapePopup(map, getPopupLngLat(parsed.feature, e.lngLat), parsed.popup);
  };

  const mouseLeave = () => {
    map.getCanvas().style.cursor = "";
    removeActiveHoverPopup(map);
  };

  map.on("click", layerId, click as never);
  map.on("mouseenter", layerId, mouseEnter as never);
  map.on("mouseleave", layerId, mouseLeave);

  store.set(layerId, { click, mouseEnter, mouseLeave });
}

/** Idempotent — called on load and again after each style replay. */
export function wireShapePopups(map: MapLibreMap): void {
  attachLayerPopupHandlers(map, CIRCLES_LAYER_ID);
  attachLayerPopupHandlers(map, POLYLINES_LAYER_ID);
}
