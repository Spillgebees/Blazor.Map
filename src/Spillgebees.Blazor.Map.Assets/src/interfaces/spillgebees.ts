import { DotNet } from "@microsoft/dotnet-js-interop";
import { Map as LeafletMap, Layer as LeafletLayer, TileLayer } from "leaflet";
import {
    ISpillgebeesCircleMarker, ISpillgebeesMapControlOptions,
    ISpillgebeesMapOptions, ISpillgebeesMarker, ISpillgebeesPolyline, ISpillgebeesTileLayer
} from "./map";

interface Spillgebees {
    Map: SpillgebeesMap;
}

interface SpillgebeesMap {
    mapFunctions: MapFunctions;
    maps: Map<HTMLElement, LeafletMap>;
    layers: Map<LeafletMap, Set<LeafletLayer>>;
    tileLayers: Map<LeafletMap, Set<TileLayer>>;
}

interface MapFunctions {
    createMap: (
        dotNetHelper: DotNet.DotNetObject,
        invokableDotNetMethodName: string,
        mapContainer: HTMLElement,
        mapOptions: ISpillgebeesMapOptions,
        mapControlOptions: ISpillgebeesMapControlOptions,
        tileLayers: ISpillgebeesTileLayer[]) => Promise<void>;
    setLayers: (
        mapContainer: HTMLElement,
        markers: ISpillgebeesMarker[],
        circleMarkers: ISpillgebeesCircleMarker[],
        polylines: ISpillgebeesPolyline[]) => void;
    setTileLayers: (
        mapContainer: HTMLElement,
        tileLayers: ISpillgebeesTileLayer[]) => void;
    disposeMap: (mapContainer: HTMLElement) => void;
}

export { Spillgebees, SpillgebeesMap, MapFunctions };
