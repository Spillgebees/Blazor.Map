import { Map as LeafletMap, LatLngBounds, Marker as LeafletMarker, CircleMarker as LeafletCircleMarker } from "leaflet";
import { LayerStorage } from "../types/layers";
import {ISpillgebeesFitBoundsOptions} from "../interfaces/map";

export const fitBoundsForMap = (map: LeafletMap, layerStorage: LayerStorage, fitBoundsOptions: ISpillgebeesFitBoundsOptions): void => {
    let mergedLayerBounds: LatLngBounds | undefined;
    fitBoundsOptions.layerIds
        .map(layerId => {
            const layerTuple = layerStorage.byId.get(layerId);
            if (!layerTuple) {
                return;
            }

            const { leaflet } = layerTuple;
            return leaflet;
        })
        .filter(layer => layer !== undefined)
        .map(layer => {
            if (layer instanceof LeafletMarker || layer instanceof LeafletCircleMarker) {
                return new LatLngBounds([layer.getLatLng()]);
            } else {
                return layer.getBounds();
            }
        })
        .forEach((bounds, index, array) => {
            if (!mergedLayerBounds)
            {
                mergedLayerBounds = bounds;
            }
            else if (index > 0 && index < array.length && mergedLayerBounds) {
                mergedLayerBounds.extend(bounds);
            }
        });

    if (mergedLayerBounds)
    {
        map.fitBounds(mergedLayerBounds, {
            paddingTopLeft: fitBoundsOptions.topLeftPadding,
            paddingBottomRight: fitBoundsOptions.bottomRightPadding,
            padding: fitBoundsOptions.padding
        });
    }
};

export const fitBounds = (mapContainer: HTMLElement, fitBoundsOptions: ISpillgebeesFitBoundsOptions): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (!map) {
        return;
    }

    const layerStorage = window.Spillgebees.Map.layers.get(map);
    if (!layerStorage) {
        return;
    }

    fitBoundsForMap(map, layerStorage, fitBoundsOptions);
};
