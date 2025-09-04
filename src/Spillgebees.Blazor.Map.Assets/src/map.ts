import { DotNet } from "@microsoft/dotnet-js-interop";
import { SpillgebeesCoordinate } from "./interfaces/map";
import DotNetObject = DotNet.DotNetObject;


import { Map as LeafletMap, MapOptions, LatLng, Marker, TileLayer } from "leaflet";

export function bootstrap() {
    window.Spillgebees = window.Spillgebees || {};
    window.Spillgebees.Map = window.Spillgebees.Map || {};
    window.Spillgebees.Map.mapFunctions = window.Spillgebees.Map.mapFunctions || {
        createMap: createMap,
        addMarkers: addMarkers,
        invalidateSize: invalidateSize,
        disposeMap: disposeMap
    };
    window.Spillgebees.Map.maps = window.Spillgebees.Map.maps || new Map<HTMLElement, LeafletMap>();
}

const createMap = async (
    dotNetHelper: DotNetObject,
    invokableDotNetMethodName: string,
    mapContainer: HTMLElement,
    center: SpillgebeesCoordinate,
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
    window.Spillgebees.Map.maps.set(mapContainer, map);

    await dotNetHelper.invokeMethodAsync(invokableDotNetMethodName);
};

const addMarkers = (mapContainer: HTMLElement, markers: Marker[]): void => {
    const map = window.Spillgebees.Map.maps.get(mapContainer);
    if (map === undefined) {
        return;
    }
    markers.forEach(marker => map.addLayer(marker));
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
