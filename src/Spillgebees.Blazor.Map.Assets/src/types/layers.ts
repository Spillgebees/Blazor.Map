import type {
  CircleMarker as LeafletCircleMarker,
  Layer as LeafletLayer,
  Marker as LeafletMarker,
  Polyline as LeafletPolyline,
} from "leaflet";
import type { ISpillgebeesCircleMarker, ISpillgebeesMarker, ISpillgebeesPolyline } from "../interfaces/map";

export type LayerTuple =
  | { model: ISpillgebeesMarker; leaflet: LeafletMarker }
  | { model: ISpillgebeesCircleMarker; leaflet: LeafletCircleMarker }
  | { model: ISpillgebeesPolyline; leaflet: LeafletPolyline };

export interface LayerStorage {
  byId: Map<string, LayerTuple>;
  byLeaflet: Map<LeafletLayer, LayerTuple>;
}
