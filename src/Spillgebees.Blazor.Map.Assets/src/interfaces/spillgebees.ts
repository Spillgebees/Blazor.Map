import { DotNet } from "@microsoft/dotnet-js-interop";
import { Map as LeafletMap, Layer as LeafletLayer } from "leaflet";
import {ISpillgebeesCircleMarker, ISpillgebeesCoordinate, ISpillgebeesMarker, ISpillgebeesPolyline} from "./map";

interface Spillgebees {
    Map: SpillgebeesMap;
}

interface SpillgebeesMap {
    mapFunctions: MapFunctions;
    maps: Map<HTMLElement, LeafletMap>;
    layers: Map<LeafletMap, Set<LeafletLayer>>
}

interface MapFunctions {
    createMap: (
        dotNetHelper: DotNet.DotNetObject,
        invokableDotNetMethodName: string,
        mapContainer: HTMLElement,
        center: ISpillgebeesCoordinate,
        zoom: number) => Promise<void>;
    setLayers: (
        mapContainer: HTMLElement,
        markers: ISpillgebeesMarker[],
        circleMarkers: ISpillgebeesCircleMarker[],
        polylines: ISpillgebeesPolyline[]) => void;
    disposeMap: (mapContainer: HTMLElement) => void;
}

export { Spillgebees, SpillgebeesMap, MapFunctions };
