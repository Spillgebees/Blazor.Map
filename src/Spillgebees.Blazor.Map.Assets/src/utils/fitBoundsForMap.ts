import {
  LatLngBounds,
  CircleMarker as LeafletCircleMarker,
  type Map as LeafletMap,
  Marker as LeafletMarker,
} from "leaflet";
import type { ISpillgebeesFitBoundsOptions } from "../interfaces/map";
import type { LayerStorage } from "../types/layers";

export const fitBoundsForMap = (
  map: LeafletMap,
  layerStorage: LayerStorage,
  fitBoundsOptions: ISpillgebeesFitBoundsOptions,
): void => {
  let mergedLayerBounds: LatLngBounds | undefined;
  fitBoundsOptions.layerIds
    .map((layerId) => layerStorage.byId.get(layerId))
    .filter((layerTuple) => layerTuple !== undefined)
    .map((layerTuple) => layerTuple.leaflet)
    .map((layer) => {
      if (layer instanceof LeafletMarker || layer instanceof LeafletCircleMarker) {
        return new LatLngBounds([layer.getLatLng()]);
      }
      return layer.getBounds();
    })
    .forEach((bounds) => {
      if (!mergedLayerBounds) {
        mergedLayerBounds = bounds;
      } else {
        mergedLayerBounds.extend(bounds);
      }
    });

  if (mergedLayerBounds) {
    map.fitBounds(mergedLayerBounds, {
      ...(fitBoundsOptions.topLeftPadding != null && { paddingTopLeft: fitBoundsOptions.topLeftPadding }),
      ...(fitBoundsOptions.bottomRightPadding != null && { paddingBottomRight: fitBoundsOptions.bottomRightPadding }),
      ...(fitBoundsOptions.padding != null && { padding: fitBoundsOptions.padding }),
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
