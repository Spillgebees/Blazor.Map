import { DotNet } from "@microsoft/dotnet-js-interop";
import DotNetObject = DotNet.DotNetObject;
import {
    ISpillgebeesMarker,
    ISpillgebeesCircleMarker,
    ISpillgebeesPolyline,
    ISpillgebeesTileLayer,
    ISpillgebeesMapOptions,
    ISpillgebeesMapControlOptions
} from "./interfaces/map";
import {
    Map as LeafletMap,
    MapOptions,
    LatLng,
    Marker as LeafletMarker,
    CircleMarker as LeafletCircleMarker,
    Polyline as LeafletPolyline,
    Layer as LeafletLayer,
    TileLayer,
    TileLayerOptions as LeafletTileLayerOptions,
    Control
} from "leaflet";
import {CenterControl} from "./controls";

export function bootstrap() {
    window.Spillgebees = window.Spillgebees || {};
    window.Spillgebees.Map = window.Spillgebees.Map || {};
    window.Spillgebees.Map.mapFunctions = window.Spillgebees.Map.mapFunctions || {
        createMap: createMap,
        setLayers: setLayers,
        setTileLayers: setTileLayers,
        invalidateSize: invalidateSize,
        disposeMap: disposeMap
    };
    window.Spillgebees.Map.maps = window.Spillgebees.Map.maps || new Map<HTMLElement, LeafletMap>();
    window.Spillgebees.Map.layers = window.Spillgebees.Map.layers || new Map<LeafletMap, Set<LeafletLayer>>();
    window.Spillgebees.Map.tileLayers = window.Spillgebees.Map.tileLayers || new Map<LeafletMap, Set<TileLayer>>();
}

const createMap = async (
    dotNetHelper: DotNetObject,
    invokableDotNetMethodName: string,
    mapContainer: HTMLElement,
    mapOptions: ISpillgebeesMapOptions,
    mapControlOptions: ISpillgebeesMapControlOptions,
    tileLayers: ISpillgebeesTileLayer[]): Promise<void> => {

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
    window.Spillgebees.Map.maps.set(mapContainer, map);

    const tileLayerSet = new Set<TileLayer>();
    leafletTileLayers.forEach(tileLayer => tileLayerSet.add(tileLayer));
    window.Spillgebees.Map.tileLayers.set(map, tileLayerSet);

    if (mapControlOptions) {
        addMapControls(map, mapControlOptions);
    }

    await dotNetHelper.invokeMethodAsync(invokableDotNetMethodName);
};

const setLayers = (
    mapContainer: HTMLElement,
    markers: Set<ISpillgebeesMarker>,
    circleMarkers: Set<ISpillgebeesCircleMarker>,
    polylines: Set<ISpillgebeesPolyline>): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (map === undefined) {
        return;
    }

    let layers = window.Spillgebees.Map.layers.get(map);
    if (layers === undefined) {
        layers = new Set<LeafletLayer>;
    }
    else
    {
        layers.forEach(layer => map.removeLayer(layer))
        layers.clear()
    }

    polylines.forEach(polyline => {
        const layer = new LeafletPolyline(
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
            })
        map.addLayer(layer)
        layers?.add(layer)
    });
    markers.forEach(marker => {
        const leafletMarker = new LeafletMarker(
            new LatLng(
                marker.coordinate.latitude,
                marker.coordinate.longitude),
            {
                title: marker.title
            })
        map.addLayer(leafletMarker)
        layers?.add(leafletMarker)
    });
    circleMarkers.forEach(circleMarker => {
        const layer = new LeafletCircleMarker(
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
            })
        map.addLayer(layer)
        layers?.add(layer)
    });
    window.Spillgebees.Map.layers.set(map, layers);
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

const addMapControls = (map: LeafletMap, controlOptions: ISpillgebeesMapControlOptions): void => {
    console.warn(controlOptions);

    if (controlOptions.zoomControlOptions.enable) {
        const zoomControl = new Control.Zoom({
            position: controlOptions.zoomControlOptions.position
        });
        map.addControl(zoomControl);
    }

    if (controlOptions.scaleControlOptions.enable) {
        const scaleControl = new Control.Scale({
            position: controlOptions.scaleControlOptions.position,
            metric: controlOptions.scaleControlOptions.showMetric ?? true,
            imperial: controlOptions.scaleControlOptions.showImperial ?? false
        });
        map.addControl(scaleControl);
    }

    if (controlOptions.centerControlOptions.enable) {
        const centerControl = new CenterControl(
            map,
            controlOptions.centerControlOptions);
        map.addControl(centerControl);
    }
}

const invalidateSize = (mapContainer: HTMLElement): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (map === undefined) {
        return;
    }
    map.invalidateSize();
}

const disposeMap = (mapContainer: HTMLElement): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (map === undefined) {
        return;
    }

    const layers = window.Spillgebees.Map.layers.get(map);
    if (layers) {
        layers.forEach(layer => map.removeLayer(layer));
        window.Spillgebees.Map.layers.delete(map);
    }

    const tileLayers = window.Spillgebees.Map.tileLayers.get(map);
    if (tileLayers) {
        tileLayers.forEach(tileLayer => map.removeLayer(tileLayer));
        window.Spillgebees.Map.tileLayers.delete(map);
    }

    window.Spillgebees.Map.maps.delete(mapContainer);
}
