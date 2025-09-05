import { Marker as LeafletMarker, CircleMarker as LeafletCircleMarker, Polyline as LeafletPolyline, Layer as LeafletLayer } from "leaflet";
import { ISpillgebeesMarker, ISpillgebeesCircleMarker, ISpillgebeesPolyline } from "../interfaces/map";

export type LayerTuple = 
    | { model: ISpillgebeesMarker; leaflet: LeafletMarker }
    | { model: ISpillgebeesCircleMarker; leaflet: LeafletCircleMarker }
    | { model: ISpillgebeesPolyline; leaflet: LeafletPolyline };

export interface LayerStorage {
    byId: Map<string, LayerTuple>;
    byLeaflet: Map<LeafletLayer, LayerTuple>;
}
