import { DotNet } from "@microsoft/dotnet-js-interop";
import { Map as LeafletMap, TileLayer } from "leaflet";
import {
    ISpillgebeesCircleMarker, ISpillgebeesMapControlOptions,
    ISpillgebeesMapOptions, ISpillgebeesMarker, ISpillgebeesPolyline, ISpillgebeesTileLayer
} from "./map";
import { LayerStorage } from "../types/layers";

interface Spillgebees {
    Map: SpillgebeesMap;
}

interface SpillgebeesMap {
    mapFunctions: MapFunctions;
    maps: Map<HTMLElement, LeafletMap>;
    layers: Map<LeafletMap, LayerStorage>;
    tileLayers: Map<LeafletMap, Set<TileLayer>>;
}

interface MapFunctions {
    createMap: (
        dotNetHelper: DotNet.DotNetObject,
        invokableDotNetMethodName: string,
        mapContainer: HTMLElement,
        mapOptions: ISpillgebeesMapOptions,
        mapControlOptions: ISpillgebeesMapControlOptions,
        tileLayers: ISpillgebeesTileLayer[],
        markers: ISpillgebeesMarker[],
        circleMarkers: ISpillgebeesCircleMarker[],
        polylines: ISpillgebeesPolyline[]) => Promise<void>;
    setLayers: (
        mapContainer: HTMLElement,
        markers: ISpillgebeesMarker[],
        circleMarkers: ISpillgebeesCircleMarker[],
        polylines: ISpillgebeesPolyline[]) => void;
    setTileLayers: (
        mapContainer: HTMLElement,
        tileLayers: ISpillgebeesTileLayer[]) => void;
    fitToLayer: (mapContainer: HTMLElement, layerId: string) => void;
    disposeMap: (mapContainer: HTMLElement) => void;
}

export { Spillgebees, SpillgebeesMap, MapFunctions };
