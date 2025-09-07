import { Map as LeafletMap, LatLngBounds, Marker as LeafletMarker, CircleMarker as LeafletCircleMarker, Polyline as LeafletPolyline } from "leaflet";
import { LayerStorage } from "../types/layers";

export const fitToLayer = (map: LeafletMap, layerStorage: LayerStorage, layerId: string): void => {
    const layerTuple = layerStorage.byId.get(layerId);
    if (!layerTuple) {
        return;
    }

    const { leaflet } = layerTuple;

    if (leaflet instanceof LeafletMarker || leaflet instanceof LeafletCircleMarker) {
        map.fitBounds(new LatLngBounds([leaflet.getLatLng()]));
    } else {
        map.fitBounds(leaflet.getBounds());
    }
};

export const fitToLayerById = (mapContainer: HTMLElement, layerId: string): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (!map) {
        return;
    }

    const layerStorage = window.Spillgebees.Map.layers.get(map);
    if (!layerStorage) {
        return;
    }

    fitToLayer(map, layerStorage, layerId);
};
