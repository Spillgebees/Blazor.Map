import { DotNet } from "@microsoft/dotnet-js-interop";
import DotNetObject = DotNet.DotNetObject;
import { ISpillgebeesCoordinate, ISpillgebeesMarker, ISpillgebeesCircleMarker, ISpillgebeesPolyline } from "./interfaces/map";
import {
    Map as LeafletMap,
    MapOptions,
    LatLng,
    Marker as LeafletMarker,
    CircleMarker as LeafletCircleMarker,
    Polyline as LeafletPolyline,
    Layer as LeafletLayer,
    TileLayer
} from "leaflet";

export function bootstrap() {
    window.Spillgebees = window.Spillgebees || {};
    window.Spillgebees.Map = window.Spillgebees.Map || {};
    window.Spillgebees.Map.mapFunctions = window.Spillgebees.Map.mapFunctions || {
        createMap: createMap,
        setLayers: setLayers,
        invalidateSize: invalidateSize,
        disposeMap: disposeMap
    };
    window.Spillgebees.Map.maps = window.Spillgebees.Map.maps || new Map<HTMLElement, LeafletMap>();
    window.Spillgebees.Map.layers = window.Spillgebees.Map.layers || new Map<LeafletMap, Set<LeafletLayer>>();
}

const createMap = async (
    dotNetHelper: DotNetObject,
    invokableDotNetMethodName: string,
    mapContainer: HTMLElement,
    center: ISpillgebeesCoordinate,
    zoom: number): Promise<void> => {

    let mapOptions: MapOptions = {
        center: new LatLng(center.latitude, center.longitude),
        zoom: zoom,
        layers: [
            new TileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; OpenStreetMap'
            })
        ]
    };

    const map = new LeafletMap(mapContainer, mapOptions);
    // hide leaflet prefix
    map.attributionControl.setPrefix(false);
    window.Spillgebees.Map.maps.set(mapContainer, map);

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

const disposeMap = (mapContainer: HTMLElement): void => {
    if (window.Spillgebees.Map.maps.get(mapContainer) === undefined
        || !window.Spillgebees.Map.maps.has(mapContainer)) {
        return;
    }

    window.Spillgebees.Map.maps.delete(mapContainer);
}

const invalidateSize = (mapContainer: HTMLElement): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (map === undefined) {
        return;
    }
    map.invalidateSize();
}
