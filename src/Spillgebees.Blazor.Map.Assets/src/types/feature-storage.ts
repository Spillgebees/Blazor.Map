import type { Marker as MapLibreMarker, Popup as MapLibrePopup } from "maplibre-gl";
import type { ICircle, IMarker, IPolyline } from "../interfaces/features";

export interface MarkerEntry {
  data: IMarker;
  marker: MapLibreMarker;
  popup: MapLibrePopup | null;
  hoverPopup: MapLibrePopup | null;
}

export interface FeatureStorage {
  markers: Map<string, MarkerEntry>;
  circles: Map<string, ICircle>;
  polylines: Map<string, IPolyline>;
  // Circles and polylines are managed via GeoJSON sources, not individual objects
  circleData: Map<string, GeoJSON.Feature>;
  polylineData: Map<string, GeoJSON.Feature>;
}
