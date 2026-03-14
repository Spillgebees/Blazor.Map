import type { Marker as MapLibreMarker, Popup as MapLibrePopup } from "maplibre-gl";

export interface MarkerEntry {
  marker: MapLibreMarker;
  popup: MapLibrePopup | null;
  hoverPopup: MapLibrePopup | null;
}

export interface FeatureStorage {
  markers: Map<string, MarkerEntry>;
  // Circles and polylines are managed via GeoJSON sources, not individual objects
  circleData: Map<string, GeoJSON.Feature>;
  polylineData: Map<string, GeoJSON.Feature>;
}
