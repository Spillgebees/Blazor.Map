import type { DotNet } from "@microsoft/dotnet-js-interop";
import type { Control, Map as LeafletMap, TileLayer } from "leaflet";
import type { LayerStorage } from "../types/layers";
import type {
  ISpillgebeesCircleMarker,
  ISpillgebeesFitBoundsOptions,
  ISpillgebeesMapControlOptions,
  ISpillgebeesMapOptions,
  ISpillgebeesMarker,
  ISpillgebeesPolyline,
  ISpillgebeesTileLayer,
} from "./map";

interface Spillgebees {
  Map: SpillgebeesMap;
}

interface SpillgebeesMap {
  mapFunctions: MapFunctions;
  maps: Map<HTMLElement, LeafletMap>;
  layers: Map<LeafletMap, LayerStorage>;
  tileLayers: Map<LeafletMap, Set<TileLayer>>;
  controls: Map<LeafletMap, Set<Control>>;
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
    polylines: ISpillgebeesPolyline[],
  ) => Promise<void>;
  setLayers: (
    mapContainer: HTMLElement,
    markers: ISpillgebeesMarker[],
    circleMarkers: ISpillgebeesCircleMarker[],
    polylines: ISpillgebeesPolyline[],
  ) => void;
  setTileLayers: (mapContainer: HTMLElement, tileLayers: ISpillgebeesTileLayer[]) => void;
  setMapControls: (mapContainer: HTMLElement, mapControlOptions: ISpillgebeesMapControlOptions) => void;
  setMapOptions: (mapContainer: HTMLElement, mapOptions: ISpillgebeesMapOptions) => void;
  invalidateSize: (mapContainer: HTMLElement) => void;
  fitBounds: (mapContainer: HTMLElement, fitBoundsOptions: ISpillgebeesFitBoundsOptions) => void;
  disposeMap: (mapContainer: HTMLElement) => void;
}

export type { Spillgebees, SpillgebeesMap, MapFunctions };
