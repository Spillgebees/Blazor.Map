import { type GeoJSONSource, type LngLat, type Map as MapLibreMap, Popup } from "maplibre-gl";
import type { ICircle, IPolyline, IPopupOptions } from "../interfaces/features";
import type { FeatureStorage } from "../types/feature-storage";

const CIRCLES_SOURCE_ID = "sgb-circles-source";
const CIRCLES_LAYER_ID = "sgb-circles-layer";
const POLYLINES_SOURCE_ID = "sgb-polylines-source";
const POLYLINES_LAYER_ID = "sgb-polylines-layer";

// --- Circle layer ---

function ensureCircleLayer(map: MapLibreMap): void {
  if (!map.getSource(CIRCLES_SOURCE_ID)) {
    map.addSource(CIRCLES_SOURCE_ID, {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    });

    map.addLayer({
      id: CIRCLES_LAYER_ID,
      type: "circle",
      source: CIRCLES_SOURCE_ID,
      paint: {
        // Data-driven styling — read from feature properties with sensible defaults
        "circle-radius": ["coalesce", ["get", "radius"], 8],
        "circle-color": ["coalesce", ["get", "color"], "#3388ff"],
        "circle-opacity": ["coalesce", ["get", "opacity"], 1],
        "circle-stroke-color": ["coalesce", ["get", "strokeColor"], "transparent"],
        "circle-stroke-width": ["coalesce", ["get", "strokeWidth"], 0],
        "circle-stroke-opacity": ["coalesce", ["get", "strokeOpacity"], 1],
      },
    });
  }
}

function syncCircleSource(map: MapLibreMap, storage: FeatureStorage): void {
  const source = map.getSource<GeoJSONSource>(CIRCLES_SOURCE_ID);
  if (!source) {
    return;
  }

  const features = Array.from(storage.circleData.values());
  source.setData({ type: "FeatureCollection", features });
}

function circleToFeature(circle: ICircle): GeoJSON.Feature<GeoJSON.Point> {
  return {
    type: "Feature",
    id: circle.id,
    geometry: {
      type: "Point",
      coordinates: [circle.position.longitude, circle.position.latitude],
    },
    properties: {
      id: circle.id,
      radius: circle.radius,
      color: circle.color,
      opacity: circle.opacity,
      strokeColor: circle.strokeColor,
      strokeWidth: circle.strokeWidth,
      strokeOpacity: circle.strokeOpacity,
      popup: circle.popup ? JSON.stringify(circle.popup) : null,
    },
  };
}

export function addCircles(map: MapLibreMap, circles: ICircle[], storage: FeatureStorage): void {
  ensureCircleLayer(map);
  for (const circle of circles) {
    storage.circleData.set(circle.id, circleToFeature(circle));
  }
  syncCircleSource(map, storage);
}

export function updateCircles(map: MapLibreMap, circles: ICircle[], storage: FeatureStorage): void {
  for (const circle of circles) {
    storage.circleData.set(circle.id, circleToFeature(circle));
  }
  syncCircleSource(map, storage);
}

export function removeCircles(map: MapLibreMap, circleIds: string[], storage: FeatureStorage): void {
  for (const id of circleIds) {
    storage.circleData.delete(id);
  }
  syncCircleSource(map, storage);
}

// --- Polyline layer ---

function ensurePolylineLayer(map: MapLibreMap): void {
  if (!map.getSource(POLYLINES_SOURCE_ID)) {
    map.addSource(POLYLINES_SOURCE_ID, {
      type: "geojson",
      data: { type: "FeatureCollection", features: [] },
    });

    map.addLayer({
      id: POLYLINES_LAYER_ID,
      type: "line",
      source: POLYLINES_SOURCE_ID,
      layout: {
        "line-join": "round",
        "line-cap": "round",
      },
      paint: {
        "line-color": ["coalesce", ["get", "color"], "#3388ff"],
        "line-width": ["coalesce", ["get", "width"], 3],
        "line-opacity": ["coalesce", ["get", "opacity"], 1],
      },
    });
  }
}

function syncPolylineSource(map: MapLibreMap, storage: FeatureStorage): void {
  const source = map.getSource<GeoJSONSource>(POLYLINES_SOURCE_ID);
  if (!source) {
    return;
  }

  const features = Array.from(storage.polylineData.values());
  source.setData({ type: "FeatureCollection", features });
}

function polylineToFeature(polyline: IPolyline): GeoJSON.Feature<GeoJSON.LineString> {
  return {
    type: "Feature",
    id: polyline.id,
    geometry: {
      type: "LineString",
      coordinates: polyline.coordinates.map((c) => [c.longitude, c.latitude]),
    },
    properties: {
      id: polyline.id,
      color: polyline.color,
      width: polyline.width,
      opacity: polyline.opacity,
      popup: polyline.popup ? JSON.stringify(polyline.popup) : null,
    },
  };
}

export function addPolylines(map: MapLibreMap, polylines: IPolyline[], storage: FeatureStorage): void {
  ensurePolylineLayer(map);
  for (const polyline of polylines) {
    storage.polylineData.set(polyline.id, polylineToFeature(polyline));
  }
  syncPolylineSource(map, storage);
}

export function updatePolylines(map: MapLibreMap, polylines: IPolyline[], storage: FeatureStorage): void {
  for (const polyline of polylines) {
    storage.polylineData.set(polyline.id, polylineToFeature(polyline));
  }
  syncPolylineSource(map, storage);
}

export function removePolylines(map: MapLibreMap, polylineIds: string[], storage: FeatureStorage): void {
  for (const id of polylineIds) {
    storage.polylineData.delete(id);
  }
  syncPolylineSource(map, storage);
}

// --- Shape popup handlers ---

// WeakMap so entries are GC'd when the MapLibreMap instance is collected (e.g., after disposeMap)
const activeHoverPopups = new WeakMap<MapLibreMap, Popup>();

function showShapePopup(map: MapLibreMap, lngLat: LngLat, options: IPopupOptions): void {
  removeActiveHoverPopup(map);

  const popup = new Popup({
    closeButton: options.closeButton,
    closeOnClick: options.trigger !== "permanent",
    maxWidth: options.maxWidth ?? "240px",
    className: options.className ?? undefined,
    anchor: options.anchor !== "auto" ? options.anchor : undefined,
    offset: options.offset ? [options.offset.x, options.offset.y] : undefined,
  })
    .setLngLat(lngLat)
    .setHTML(options.content)
    .addTo(map);

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

function attachLayerPopupHandlers(map: MapLibreMap, layerId: string): void {
  map.on("click", layerId, (e) => {
    if (!e.features?.length) {
      return;
    }
    const props = e.features[0].properties;
    if (!props?.popup) {
      return;
    }

    // MapLibre serializes feature properties to flat strings — parse back to object
    const popupData: IPopupOptions = JSON.parse(props.popup as string);
    if (popupData.trigger !== "click") {
      return;
    }

    showShapePopup(map, e.lngLat, popupData);
  });

  map.on("mouseenter", layerId, (e) => {
    if (!e.features?.length) {
      return;
    }
    const props = e.features[0].properties;
    if (!props?.popup) {
      return;
    }

    const popupData: IPopupOptions = JSON.parse(props.popup as string);
    if (popupData.trigger !== "hover") {
      return;
    }

    map.getCanvas().style.cursor = "pointer";
    showShapePopup(map, e.lngLat, popupData);
  });

  map.on("mouseleave", layerId, () => {
    map.getCanvas().style.cursor = "";
    removeActiveHoverPopup(map);
  });
}

export function setupShapePopupHandlers(map: MapLibreMap): void {
  attachLayerPopupHandlers(map, CIRCLES_LAYER_ID);
  attachLayerPopupHandlers(map, POLYLINES_LAYER_ID);
}

export function ensureShapeLayers(map: MapLibreMap): void {
  ensureCircleLayer(map);
  ensurePolylineLayer(map);
}
