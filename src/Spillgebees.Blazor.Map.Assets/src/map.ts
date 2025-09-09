import { DotNet } from "@microsoft/dotnet-js-interop";
import DotNetObject = DotNet.DotNetObject;
import {
    ISpillgebeesMarker,
    ISpillgebeesCircleMarker,
    ISpillgebeesPolyline,
    ISpillgebeesTileLayer,
    ISpillgebeesMapOptions,
    ISpillgebeesMapControlOptions,
    MapTheme
} from "./interfaces/map";
import {
    Map as LeafletMap,
    MapOptions,
    LatLng,
    Marker as LeafletMarker,
    CircleMarker as LeafletCircleMarker,
    Polyline as LeafletPolyline,
    TileLayer,
    TileLayerOptions as LeafletTileLayerOptions,
    Control
} from "leaflet";
import {CenterControl} from "./controls";
import { LayerTuple, LayerStorage } from "./types/layers";
import {fitToLayers, fitToLayersById} from "./utils/fitToLayers";
import { convertToLeafletTooltip } from "./utils/tooltip";

export function bootstrap() {
    window.Spillgebees = window.Spillgebees || {};
    window.Spillgebees.Map = window.Spillgebees.Map || {};
    window.Spillgebees.Map.mapFunctions = window.Spillgebees.Map.mapFunctions || {
        createMap: createMap,
        setLayers: setLayers,
        setTileLayers: setTileLayers,
        setMapControls: setMapControls,
        setMapOptions: setMapOptions,
        invalidateSize: invalidateSize,
        fitToLayers: fitToLayersById,
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
    polylines: ISpillgebeesPolyline[]): Promise<void> => {

    const leafletTileLayers = tileLayers.map(tileLayer => {
        const options: LeafletTileLayerOptions = {
            attribution: tileLayer.attribution,
            detectRetina: tileLayer.detectRetina,
        };
        return new TileLayer(tileLayer.urlTemplate, options);
    });

    const leafletMapOptions: MapOptions = {
        center: new LatLng(mapOptions.center.latitude, mapOptions.center.longitude),
        zoom: mapOptions.zoom,
        layers: leafletTileLayers,
        zoomControl: false
    };

    const map = new LeafletMap(mapContainer, leafletMapOptions);
    if (!mapOptions.showLeafletPrefix) {
        map.attributionControl.setPrefix(false);
    }

    if (mapOptions.theme === MapTheme.Dark) {
        mapContainer.classList.add('sgb-map-dark');
    }

    window.Spillgebees.Map.maps.set(mapContainer, map);

    const tileLayerSet = new Set<TileLayer>();
    leafletTileLayers.forEach(tileLayer => tileLayerSet.add(tileLayer));
    window.Spillgebees.Map.tileLayers.set(map, tileLayerSet);

    if (mapControlOptions) {
        setMapControls(mapContainer, mapControlOptions);
    }

    setLayers(mapContainer, markers, circleMarkers, polylines);

    if (mapOptions.fitToLayerIds) {
        const layerStorage = window.Spillgebees.Map.layers.get(map);
        if (layerStorage) {
            fitToLayers(map, layerStorage, mapOptions.fitToLayerIds);
        }
    }

    await dotNetHelper.invokeMethodAsync(invokableDotNetMethodName);
};

const setLayers = (
    mapContainer: HTMLElement,
    markers: ISpillgebeesMarker[],
    circleMarkers: ISpillgebeesCircleMarker[],
    polylines: ISpillgebeesPolyline[]): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (!map) {
        return;
    }

    let layerStorage = window.Spillgebees.Map.layers.get(map);
    if (!layerStorage) {
        layerStorage = { byId: new Map(), byLeaflet: new Map() };
        window.Spillgebees.Map.layers.set(map, layerStorage);
    } else {
        layerStorage.byLeaflet.forEach((_, leafletLayer) => map.removeLayer(leafletLayer));
        layerStorage.byId.clear();
        layerStorage.byLeaflet.clear();
    }

    polylines.forEach(polyline => {
        const leafletLayer = new LeafletPolyline(
            polyline.coordinates.map(coordinate => new LatLng(coordinate.latitude, coordinate.longitude)),
            {
                smoothFactor: polyline.smoothFactor,
                noClip: polyline.noClip,
                stroke: polyline.stroke,
                color: polyline.strokeColor,
                weight: polyline.strokeWeight,
                opacity: polyline.strokeOpacity,
                fill: polyline.fill,
                fillColor: polyline.fillColor,
                fillOpacity: polyline.fillOpacity
            }
        );

        const layerTuple: LayerTuple = { model: polyline, leaflet: leafletLayer };

        layerStorage.byId.set(polyline.id, layerTuple);
        layerStorage.byLeaflet.set(leafletLayer, layerTuple);

        if (polyline.tooltip) {
            const tooltip = convertToLeafletTooltip(polyline.tooltip);
            leafletLayer.bindTooltip(tooltip);
        }

        map.addLayer(leafletLayer);
    });
    markers.forEach(marker => {
        const leafletLayer = new LeafletMarker(
            new LatLng(
                marker.coordinate.latitude,
                marker.coordinate.longitude),
            {
                title: marker.title
            }
        );

        const layerTuple: LayerTuple = { model: marker, leaflet: leafletLayer };

        layerStorage.byId.set(marker.id, layerTuple);
        layerStorage.byLeaflet.set(leafletLayer, layerTuple);

        if (marker.tooltip) {
            const tooltip = convertToLeafletTooltip(marker.tooltip);
            leafletLayer.bindTooltip(tooltip);
        }

        map.addLayer(leafletLayer);
    });
    circleMarkers.forEach(circleMarker => {
        const leafletLayer = new LeafletCircleMarker(
            new LatLng(
                circleMarker.coordinate.latitude,
                circleMarker.coordinate.longitude),
            {
                radius: circleMarker.radius,
                stroke: circleMarker.stroke,
                color: circleMarker.strokeColor,
                weight: circleMarker.strokeWeight,
                opacity: circleMarker.strokeOpacity,
                fill: circleMarker.fill,
                fillColor: circleMarker.fillColor,
                fillOpacity: circleMarker.fillOpacity
            }
        );

        const layerTuple: LayerTuple = { model: circleMarker, leaflet: leafletLayer };

        layerStorage.byId.set(circleMarker.id, layerTuple);
        layerStorage.byLeaflet.set(leafletLayer, layerTuple);

        if (circleMarker.tooltip) {
            const tooltip = convertToLeafletTooltip(circleMarker.tooltip);
            leafletLayer.bindTooltip(tooltip);
        }

        map.addLayer(leafletLayer);
    });

    map.invalidateSize()
}

const setTileLayers = (
    mapContainer: HTMLElement,
    tileLayers: ISpillgebeesTileLayer[]): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (map === undefined) {
        return;
    }

    let existingTileLayers = window.Spillgebees.Map.tileLayers.get(map);
    if (existingTileLayers === undefined) {
        existingTileLayers = new Set<TileLayer>();
    }
    else {
        existingTileLayers.forEach(tileLayer => map.removeLayer(tileLayer));
        existingTileLayers.clear();
    }
    tileLayers.forEach(tileLayer => {
        const options: LeafletTileLayerOptions = {
            attribution: tileLayer.attribution,
            detectRetina: tileLayer.detectRetina,
            tileSize: tileLayer.tileSize,
        };

        const leafletTileLayer = new TileLayer(tileLayer.urlTemplate, options);
        map.addLayer(leafletTileLayer);
        existingTileLayers.add(leafletTileLayer);
    });

    window.Spillgebees.Map.tileLayers.set(map, existingTileLayers);
}

const setMapControls = (mapContainer: HTMLElement, controlOptions: ISpillgebeesMapControlOptions): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (map === undefined) {
        return;
    }

    const existingControls = window.Spillgebees.Map.controls.get(map);
    if (existingControls)
    {
        existingControls.forEach(control => map.removeControl(control));
    }

    const newControls = new Set<Control>();
    if (controlOptions.zoomControlOptions.enable) {
        const zoomControl = new Control.Zoom({
            position: controlOptions.zoomControlOptions.position
        });
        map.addControl(zoomControl);
        newControls.add(zoomControl);
    }

    if (controlOptions.scaleControlOptions.enable) {
        const scaleControl = new Control.Scale({
            position: controlOptions.scaleControlOptions.position,
            metric: controlOptions.scaleControlOptions.showMetric ?? true,
            imperial: controlOptions.scaleControlOptions.showImperial ?? false
        });
        map.addControl(scaleControl);
        newControls.add(scaleControl);
    }

    if (controlOptions.centerControlOptions.enable) {
        const centerControl = new CenterControl(
            map,
            controlOptions.centerControlOptions);
        map.addControl(centerControl);
        newControls.add(centerControl);
    }

    window.Spillgebees.Map.controls.set(map, newControls);
}

const invalidateSize = (mapContainer: HTMLElement): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (map === undefined) {
        return;
    }
    map.invalidateSize();
}

const setMapOptions = (mapContainer: HTMLElement, mapOptions: ISpillgebeesMapOptions): void => {
    if (mapOptions.theme === MapTheme.Dark) {
        mapContainer.classList.add('sgb-map-dark');
    } else {
        mapContainer.classList.remove('sgb-map-dark');
    }
};

const disposeMap = (mapContainer: HTMLElement): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (map === undefined) {
        return;
    }

    const layerStorage = window.Spillgebees.Map.layers.get(map);
    if (layerStorage) {
        layerStorage.byLeaflet.forEach((_, leafletLayer) => map.removeLayer(leafletLayer));
        window.Spillgebees.Map.layers.delete(map);
    }

    const tileLayers = window.Spillgebees.Map.tileLayers.get(map);
    if (tileLayers) {
        tileLayers.forEach(tileLayer => map.removeLayer(tileLayer));
        window.Spillgebees.Map.tileLayers.delete(map);
    }

    window.Spillgebees.Map.maps.delete(mapContainer);
}
