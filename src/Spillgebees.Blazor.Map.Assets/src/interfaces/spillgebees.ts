import { DotNet } from "@microsoft/dotnet-js-interop";
import { Map as LeafletMap } from "leaflet";
import { SpillgebeesCoordinate, SpillgebeesMarker } from "./map";

interface Spillgebees {
    Map: SpillgebeesMap;
}

interface SpillgebeesMap {
    mapFunctions: MapFunctions;
    maps: Map<HTMLElement, LeafletMap>;
}

interface MapFunctions {
    createMap: (
        dotNetHelper: DotNet.DotNetObject,
        invokableDotNetMethodName: string,
        mapContainer: HTMLElement,
        center: SpillgebeesCoordinate,
        zoom: number) => Promise<void>;
    addMarkers: (mapContainer: HTMLElement, markers: SpillgebeesMarker[]) => void;
    disposeMap: (mapContainer: HTMLElement) => void;
}

export { Spillgebees, SpillgebeesMap, MapFunctions };
