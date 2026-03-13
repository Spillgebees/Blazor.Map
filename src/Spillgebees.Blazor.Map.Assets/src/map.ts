import type { DotNet } from "@microsoft/dotnet-js-interop";

type DotNetObject = DotNet.DotNetObject;

import {
  Control,
  LatLng,
  CircleMarker as LeafletCircleMarker,
  Icon as LeafletIcon,
  type Layer as LeafletLayer,
  Map as LeafletMap,
  Marker as LeafletMarker,
  Polyline as LeafletPolyline,
  type TileLayerOptions as LeafletTileLayerOptions,
  type MapOptions,
  TileLayer,
} from "leaflet";
import { CenterControl } from "./controls";
import {
  type ISpillgebeesCircleMarker,
  type ISpillgebeesMapControlOptions,
  type ISpillgebeesMapOptions,
  type ISpillgebeesMarker,
  type ISpillgebeesMarkerIcon,
  type ISpillgebeesPolyline,
  type ISpillgebeesTileLayer,
  MapTheme,
} from "./interfaces/map";
import type { LayerStorage, LayerTuple } from "./types/layers";
import { fitBounds, fitBoundsForMap } from "./utils/fitBoundsForMap";
import { convertToLeafletTooltip } from "./utils/tooltip";

const createLeafletIcon = (icon: ISpillgebeesMarkerIcon): LeafletIcon => {
  return new LeafletIcon({
    iconUrl: icon.iconUrl,
    ...(icon.iconSize != null && { iconSize: icon.iconSize }),
    ...(icon.iconAnchor != null && { iconAnchor: icon.iconAnchor }),
    ...(icon.popupAnchor != null && { popupAnchor: icon.popupAnchor }),
    ...(icon.tooltipAnchor != null && { tooltipAnchor: icon.tooltipAnchor }),
    ...(icon.shadowUrl != null && { shadowUrl: icon.shadowUrl }),
    ...(icon.shadowSize != null && { shadowSize: icon.shadowSize }),
    ...(icon.shadowAnchor != null && { shadowAnchor: icon.shadowAnchor }),
    ...(icon.className != null && { className: icon.className }),
  });
};

export function bootstrap() {
  window.Spillgebees = window.Spillgebees || {};
  window.Spillgebees.Map = window.Spillgebees.Map || {};
  window.Spillgebees.Map.mapFunctions = window.Spillgebees.Map.mapFunctions || {
    createMap: createMap,
    addLayers: addLayers,
    updateLayers: updateLayers,
    removeLayers: removeLayers,
    setTileLayers: setTileLayers,
    setMapControls: setMapControls,
    setMapOptions: setMapOptions,
    invalidateSize: invalidateSize,
    fitBounds: fitBounds,
    disposeMap: disposeMap,
  };
  window.Spillgebees.Map.maps = window.Spillgebees.Map.maps || new Map<HTMLElement, LeafletMap>();
  window.Spillgebees.Map.layers = window.Spillgebees.Map.layers || new Map<LeafletMap, LayerStorage>();
  window.Spillgebees.Map.tileLayers = window.Spillgebees.Map.tileLayers || new Map<LeafletMap, Set<TileLayer>>();
  window.Spillgebees.Map.controls = window.Spillgebees.Map.controls || new Map<LeafletMap, Set<Control>>();
}

const createMap = async (
  dotNetHelper: DotNetObject,
  invokableDotNetMethodName: string,
  mapContainer: HTMLElement,
  mapOptions: ISpillgebeesMapOptions,
  mapControlOptions: ISpillgebeesMapControlOptions,
  tileLayers: ISpillgebeesTileLayer[],
  markers: ISpillgebeesMarker[],
  circleMarkers: ISpillgebeesCircleMarker[],
  polylines: ISpillgebeesPolyline[],
): Promise<void> => {
  const leafletTileLayers = tileLayers.map((tileLayer) => {
    const options: LeafletTileLayerOptions = {
      attribution: tileLayer.attribution,
      ...(tileLayer.detectRetina != null && { detectRetina: tileLayer.detectRetina }),
      ...(tileLayer.tileSize != null && { tileSize: tileLayer.tileSize }),
    };
    return new TileLayer(tileLayer.urlTemplate, options);
  });

  const leafletMapOptions: MapOptions = {
    center: new LatLng(mapOptions.center.latitude, mapOptions.center.longitude),
    zoom: mapOptions.zoom,
    layers: leafletTileLayers,
    zoomControl: false,
  };

  const map = new LeafletMap(mapContainer, leafletMapOptions);
  if (!mapOptions.showLeafletPrefix) {
    map.attributionControl.setPrefix(false);
  }

  if (mapOptions.theme === MapTheme.Dark) {
    mapContainer.classList.add("sgb-map-dark");
  }

  window.Spillgebees.Map.maps.set(mapContainer, map);

  const tileLayerSet = new Set<TileLayer>();
  for (const tileLayer of leafletTileLayers) {
    tileLayerSet.add(tileLayer);
  }
  window.Spillgebees.Map.tileLayers.set(map, tileLayerSet);

  if (mapControlOptions) {
    setMapControls(mapContainer, mapControlOptions);
  }

  addLayers(mapContainer, markers, circleMarkers, polylines);

  if (mapOptions.fitBoundsOptions) {
    const layerStorage = window.Spillgebees.Map.layers.get(map);
    if (layerStorage) {
      fitBoundsForMap(map, layerStorage, mapOptions.fitBoundsOptions);
    }
  }

  await dotNetHelper.invokeMethodAsync(invokableDotNetMethodName);
};

/**
 * Adds new layers to the map without affecting existing layers.
 * Initializes layer storage if it doesn't exist yet.
 */
const addLayers = (
  mapContainer: HTMLElement,
  markers: ISpillgebeesMarker[],
  circleMarkers: ISpillgebeesCircleMarker[],
  polylines: ISpillgebeesPolyline[],
): void => {
  const map = window.Spillgebees.Map.maps.get(mapContainer);
  if (!map) {
    return;
  }

  let layerStorage = window.Spillgebees.Map.layers.get(map);
  if (!layerStorage) {
    layerStorage = { byId: new Map(), byLeaflet: new Map() };
    window.Spillgebees.Map.layers.set(map, layerStorage);
  }

  for (const polyline of polylines) {
    const leafletLayer = new LeafletPolyline(
      polyline.coordinates.map((coordinate) => new LatLng(coordinate.latitude, coordinate.longitude)),
      {
        ...(polyline.smoothFactor != null && { smoothFactor: polyline.smoothFactor }),
        noClip: polyline.noClip,
        stroke: polyline.stroke,
        ...(polyline.strokeColor != null && { color: polyline.strokeColor }),
        ...(polyline.strokeWeight != null && { weight: polyline.strokeWeight }),
        ...(polyline.strokeOpacity != null && { opacity: polyline.strokeOpacity }),
        fill: polyline.fill,
        ...(polyline.fillColor != null && { fillColor: polyline.fillColor }),
        ...(polyline.fillOpacity != null && { fillOpacity: polyline.fillOpacity }),
      },
    );

    const layerTuple: LayerTuple = { model: polyline, leaflet: leafletLayer };

    layerStorage.byId.set(polyline.id, layerTuple);
    layerStorage.byLeaflet.set(leafletLayer, layerTuple);

    if (polyline.tooltip) {
      const tooltip = convertToLeafletTooltip(polyline.tooltip);
      leafletLayer.bindTooltip(tooltip);
    }

    map.addLayer(leafletLayer);
  }

  for (const marker of markers) {
    const leafletLayer = new LeafletMarker(new LatLng(marker.coordinate.latitude, marker.coordinate.longitude), {
      ...(marker.title != null && { title: marker.title }),
      ...(marker.icon != null && { icon: createLeafletIcon(marker.icon) }),
      ...(marker.rotationAngle != null && { rotationAngle: marker.rotationAngle }),
      ...(marker.rotationOrigin != null && { rotationOrigin: marker.rotationOrigin }),
      ...(marker.zIndexOffset != null && { zIndexOffset: marker.zIndexOffset }),
      ...(marker.riseOnHover != null && { riseOnHover: marker.riseOnHover }),
      ...(marker.riseOffset != null && { riseOffset: marker.riseOffset }),
    });

    const layerTuple: LayerTuple = { model: marker, leaflet: leafletLayer };

    layerStorage.byId.set(marker.id, layerTuple);
    layerStorage.byLeaflet.set(leafletLayer, layerTuple);

    if (marker.tooltip) {
      const tooltip = convertToLeafletTooltip(marker.tooltip);
      leafletLayer.bindTooltip(tooltip);
    }

    map.addLayer(leafletLayer);
  }

  for (const circleMarker of circleMarkers) {
    const leafletLayer = new LeafletCircleMarker(
      new LatLng(circleMarker.coordinate.latitude, circleMarker.coordinate.longitude),
      {
        radius: circleMarker.radius,
        stroke: circleMarker.stroke,
        ...(circleMarker.strokeColor != null && { color: circleMarker.strokeColor }),
        ...(circleMarker.strokeWeight != null && { weight: circleMarker.strokeWeight }),
        ...(circleMarker.strokeOpacity != null && { opacity: circleMarker.strokeOpacity }),
        fill: circleMarker.fill,
        ...(circleMarker.fillColor != null && { fillColor: circleMarker.fillColor }),
        ...(circleMarker.fillOpacity != null && { fillOpacity: circleMarker.fillOpacity }),
      },
    );

    const layerTuple: LayerTuple = { model: circleMarker, leaflet: leafletLayer };

    layerStorage.byId.set(circleMarker.id, layerTuple);
    layerStorage.byLeaflet.set(leafletLayer, layerTuple);

    if (circleMarker.tooltip) {
      const tooltip = convertToLeafletTooltip(circleMarker.tooltip);
      leafletLayer.bindTooltip(tooltip);
    }

    map.addLayer(leafletLayer);
  }
};

/**
 * Incrementally updates existing layers by ID without recreating them.
 * Layers not found by ID are silently skipped. Use `addLayers` to add new layers.
 *
 * For markers: updates position, rotation, icon (when non-null), and tooltip.
 * When `marker.icon` is null, the icon is not changed (the icon set during `addLayers` is preserved).
 *
 * For circleMarkers: updates position, radius, path styles, and tooltip.
 *
 * For polylines: updates coordinates, path styles, and tooltip.
 */
const updateLayers = (
  mapContainer: HTMLElement,
  markers: ISpillgebeesMarker[],
  circleMarkers: ISpillgebeesCircleMarker[],
  polylines: ISpillgebeesPolyline[],
): void => {
  const map = window.Spillgebees.Map.maps.get(mapContainer);
  if (!map) {
    return;
  }

  const layerStorage = window.Spillgebees.Map.layers.get(map);
  if (!layerStorage) {
    return;
  }

  for (const marker of markers) {
    const existing = layerStorage.byId.get(marker.id);
    if (!existing) {
      continue;
    }

    const leafletMarker = existing.leaflet as LeafletMarker;

    leafletMarker.setLatLng(new LatLng(marker.coordinate.latitude, marker.coordinate.longitude));

    if (marker.rotationAngle != null) {
      leafletMarker.setRotationAngle(marker.rotationAngle);
    } else {
      leafletMarker.setRotationAngle(0);
    }

    if (marker.icon != null) {
      leafletMarker.setIcon(createLeafletIcon(marker.icon));
    }

    if (marker.zIndexOffset != null) {
      leafletMarker.setZIndexOffset(marker.zIndexOffset);
    } else {
      leafletMarker.setZIndexOffset(0);
    }

    // Only rebind tooltip if it actually changed which avoids DOM recreation jitter and
    // tooltip disappearing during map interactions (mousedown/drag).
    // Note: JSON.stringify comparison relies on deterministic property ordering from
    // System.Text.Json serialization on the C# side.
    if (JSON.stringify(marker.tooltip) !== JSON.stringify(existing.model.tooltip)) {
      leafletMarker.unbindTooltip();
      if (marker.tooltip) {
        leafletMarker.bindTooltip(convertToLeafletTooltip(marker.tooltip));
      }
    }

    // Update the stored model
    const updatedTuple: LayerTuple = { model: marker, leaflet: leafletMarker };
    layerStorage.byId.set(marker.id, updatedTuple);
    layerStorage.byLeaflet.set(leafletMarker as unknown as LeafletLayer, updatedTuple);
  }

  for (const circleMarker of circleMarkers) {
    const existing = layerStorage.byId.get(circleMarker.id);
    if (!existing) {
      continue;
    }

    const leafletCircleMarker = existing.leaflet as LeafletCircleMarker;

    leafletCircleMarker.setLatLng(new LatLng(circleMarker.coordinate.latitude, circleMarker.coordinate.longitude));
    leafletCircleMarker.setRadius(circleMarker.radius);
    leafletCircleMarker.setStyle({
      stroke: circleMarker.stroke,
      ...(circleMarker.strokeColor != null && { color: circleMarker.strokeColor }),
      ...(circleMarker.strokeWeight != null && { weight: circleMarker.strokeWeight }),
      ...(circleMarker.strokeOpacity != null && { opacity: circleMarker.strokeOpacity }),
      fill: circleMarker.fill,
      ...(circleMarker.fillColor != null && { fillColor: circleMarker.fillColor }),
      ...(circleMarker.fillOpacity != null && { fillOpacity: circleMarker.fillOpacity }),
    });

    // See marker updateLayers for JSON.stringify ordering note
    if (JSON.stringify(circleMarker.tooltip) !== JSON.stringify(existing.model.tooltip)) {
      leafletCircleMarker.unbindTooltip();
      if (circleMarker.tooltip) {
        leafletCircleMarker.bindTooltip(convertToLeafletTooltip(circleMarker.tooltip));
      }
    }

    // Update the stored model
    const updatedTuple: LayerTuple = { model: circleMarker, leaflet: leafletCircleMarker };
    layerStorage.byId.set(circleMarker.id, updatedTuple);
    layerStorage.byLeaflet.set(leafletCircleMarker as unknown as LeafletLayer, updatedTuple);
  }

  for (const polyline of polylines) {
    const existing = layerStorage.byId.get(polyline.id);
    if (!existing) {
      continue;
    }

    const leafletPolyline = existing.leaflet as LeafletPolyline;

    leafletPolyline.setLatLngs(
      polyline.coordinates.map((coordinate) => new LatLng(coordinate.latitude, coordinate.longitude)),
    );
    leafletPolyline.setStyle({
      stroke: polyline.stroke,
      ...(polyline.strokeColor != null && { color: polyline.strokeColor }),
      ...(polyline.strokeWeight != null && { weight: polyline.strokeWeight }),
      ...(polyline.strokeOpacity != null && { opacity: polyline.strokeOpacity }),
      fill: polyline.fill,
      ...(polyline.fillColor != null && { fillColor: polyline.fillColor }),
      ...(polyline.fillOpacity != null && { fillOpacity: polyline.fillOpacity }),
    });

    // See marker updateLayers for JSON.stringify ordering note
    if (JSON.stringify(polyline.tooltip) !== JSON.stringify(existing.model.tooltip)) {
      leafletPolyline.unbindTooltip();
      if (polyline.tooltip) {
        leafletPolyline.bindTooltip(convertToLeafletTooltip(polyline.tooltip));
      }
    }

    // Update the stored model
    const updatedTuple: LayerTuple = { model: polyline, leaflet: leafletPolyline };
    layerStorage.byId.set(polyline.id, updatedTuple);
    layerStorage.byLeaflet.set(leafletPolyline as unknown as LeafletLayer, updatedTuple);
  }
};

/**
 * Removes specific layers by their IDs.
 * Unknown IDs are silently skipped.
 */
const removeLayers = (
  mapContainer: HTMLElement,
  markerIds: string[],
  circleMarkerIds: string[],
  polylineIds: string[],
): void => {
  const map = window.Spillgebees.Map.maps.get(mapContainer);
  if (!map) {
    return;
  }

  const layerStorage = window.Spillgebees.Map.layers.get(map);
  if (!layerStorage) {
    return;
  }

  const allIds = [...markerIds, ...circleMarkerIds, ...polylineIds];

  for (const id of allIds) {
    const existing = layerStorage.byId.get(id);
    if (!existing) {
      continue;
    }

    map.removeLayer(existing.leaflet);
    layerStorage.byId.delete(id);
    layerStorage.byLeaflet.delete(existing.leaflet as unknown as LeafletLayer);
  }
};

const setTileLayers = (mapContainer: HTMLElement, tileLayers: ISpillgebeesTileLayer[]): void => {
  const map = window.Spillgebees.Map.maps.get(mapContainer);
  if (map === undefined) {
    return;
  }

  let existingTileLayers = window.Spillgebees.Map.tileLayers.get(map);
  if (existingTileLayers === undefined) {
    existingTileLayers = new Set<TileLayer>();
  } else {
    for (const tileLayer of existingTileLayers) {
      map.removeLayer(tileLayer);
    }
    existingTileLayers.clear();
  }
  tileLayers.forEach((tileLayer) => {
    const options: LeafletTileLayerOptions = {
      attribution: tileLayer.attribution,
      ...(tileLayer.detectRetina != null && { detectRetina: tileLayer.detectRetina }),
      ...(tileLayer.tileSize != null && { tileSize: tileLayer.tileSize }),
    };

    const leafletTileLayer = new TileLayer(tileLayer.urlTemplate, options);
    map.addLayer(leafletTileLayer);
    existingTileLayers.add(leafletTileLayer);
  });

  window.Spillgebees.Map.tileLayers.set(map, existingTileLayers);
};

const setMapControls = (mapContainer: HTMLElement, controlOptions: ISpillgebeesMapControlOptions): void => {
  const map = window.Spillgebees.Map.maps.get(mapContainer);
  if (map === undefined) {
    return;
  }

  const existingControls = window.Spillgebees.Map.controls.get(map);
  if (existingControls) {
    for (const control of existingControls) {
      map.removeControl(control);
    }
  }

  const newControls = new Set<Control>();
  if (controlOptions.zoomControlOptions.enable) {
    const zoomControl = new Control.Zoom({
      position: controlOptions.zoomControlOptions.position,
    });
    map.addControl(zoomControl);
    newControls.add(zoomControl);
  }

  if (controlOptions.scaleControlOptions.enable) {
    const scaleControl = new Control.Scale({
      position: controlOptions.scaleControlOptions.position,
      metric: controlOptions.scaleControlOptions.showMetric ?? true,
      imperial: controlOptions.scaleControlOptions.showImperial ?? false,
    });
    map.addControl(scaleControl);
    newControls.add(scaleControl);
  }

  if (controlOptions.centerControlOptions.enable) {
    const centerControl = new CenterControl(map, controlOptions.centerControlOptions);
    map.addControl(centerControl);
    newControls.add(centerControl);
  }

  window.Spillgebees.Map.controls.set(map, newControls);
};

const invalidateSize = (mapContainer: HTMLElement): void => {
  const map = window.Spillgebees.Map.maps.get(mapContainer);
  if (map === undefined) {
    return;
  }
  map.invalidateSize();
};

const setMapOptions = (mapContainer: HTMLElement, mapOptions: ISpillgebeesMapOptions): void => {
  if (mapOptions.theme === MapTheme.Dark) {
    mapContainer.classList.add("sgb-map-dark");
  } else {
    mapContainer.classList.remove("sgb-map-dark");
  }
};

const disposeMap = (mapContainer: HTMLElement): void => {
  const map = window.Spillgebees.Map.maps.get(mapContainer);
  if (map === undefined) {
    return;
  }

  const layerStorage = window.Spillgebees.Map.layers.get(map);
  if (layerStorage) {
    for (const leafletLayer of layerStorage.byLeaflet.keys()) {
      map.removeLayer(leafletLayer);
    }
    window.Spillgebees.Map.layers.delete(map);
  }

  const tileLayers = window.Spillgebees.Map.tileLayers.get(map);
  if (tileLayers) {
    for (const tileLayer of tileLayers) {
      map.removeLayer(tileLayer);
    }
    window.Spillgebees.Map.tileLayers.delete(map);
  }

  const controls = window.Spillgebees.Map.controls.get(map);
  if (controls) {
    for (const control of controls) {
      map.removeControl(control);
    }
    window.Spillgebees.Map.controls.delete(map);
  }

  map.remove();
  window.Spillgebees.Map.maps.delete(mapContainer);
};
