import type { ControlPosition, Point } from "leaflet";

export interface ISpillgebeesCoordinate {
  latitude: number;
  longitude: number;
}

export interface ISpillgebeesTooltipOffset {
  x: number;
  y: number;
}

export interface ISpillgebeesTooltip {
  content: string;
  offset: ISpillgebeesTooltipOffset | null;
  direction: "top" | "bottom" | "left" | "right" | "center" | "auto" | null;
  permanent: boolean;
  sticky: boolean;
  interactive: boolean;
  opacity: number | null;
  className: string | null;
}

export interface ISpillgebeesPath {
  id: string;
  stroke: boolean;
  strokeColor: string | null;
  strokeWeight: number | null;
  strokeOpacity: number | null;
  fill: boolean;
  fillColor: string | null;
  fillOpacity: number | null;
  tooltip: ISpillgebeesTooltip | null;
}

export interface ISpillgebeesMarkerIcon {
  iconUrl: string;
  iconSize: [number, number] | null;
  iconAnchor: [number, number] | null;
  popupAnchor: [number, number] | null;
  tooltipAnchor: [number, number] | null;
  shadowUrl: string | null;
  shadowSize: [number, number] | null;
  shadowAnchor: [number, number] | null;
  className: string | null;
}

export interface ISpillgebeesMarker extends ISpillgebeesPath {
  coordinate: ISpillgebeesCoordinate;
  title: string | null;
  icon: ISpillgebeesMarkerIcon | null;
  rotationAngle: number | null;
  rotationOrigin: string | null;
  zIndexOffset: number | null;
  riseOnHover: boolean | null;
  riseOffset: number | null;
}

export interface ISpillgebeesCircleMarker extends ISpillgebeesPath {
  coordinate: ISpillgebeesCoordinate;
  radius: number;
}

export interface ISpillgebeesPolyline extends ISpillgebeesPath {
  coordinates: ISpillgebeesCoordinate[];
  smoothFactor: number | null;
  noClip: boolean;
}

export interface ISpillgebeesTileLayerOptions {
  detectRetina: boolean | null;
  tileSize: number | null;
}

export interface ISpillgebeesWmsLayerOptions {
  layers: string;
  format: string | null;
  transparent: boolean | null;
  version: string | null;
  styles: string | null;
}

export interface ISpillgebeesTileLayer {
  urlTemplate: string;
  attribution: string;
  tile: ISpillgebeesTileLayerOptions | null;
  wms: ISpillgebeesWmsLayerOptions | null;
}

export interface ISpillgebeesMapOptions {
  center: ISpillgebeesCoordinate;
  zoom: number;
  showLeafletPrefix: boolean;
  fitBoundsOptions: ISpillgebeesFitBoundsOptions | null;
  theme: number;
}

export interface ISpillgebeesZoomControlOptions {
  enable: boolean;
  position: ControlPosition;
  showZoomInButton: boolean;
  showZoomOutButton: boolean;
}

export interface ISpillgebeesScaleControlOptions {
  enable: boolean;
  position: ControlPosition;
  showMetric: boolean | null;
  showImperial: boolean | null;
}

export interface ISpillgebeesCenterControlOptions {
  enable: boolean;
  position: ControlPosition;
  center: ISpillgebeesCoordinate;
  zoom: number;
  fitBoundsOptions: ISpillgebeesFitBoundsOptions | null;
}

export interface ISpillgebeesMapControlOptions {
  zoomControlOptions: ISpillgebeesZoomControlOptions;
  scaleControlOptions: ISpillgebeesScaleControlOptions;
  centerControlOptions: ISpillgebeesCenterControlOptions;
}

export interface ISpillgebeesFitBoundsOptions {
  layerIds: string[];
  topLeftPadding: Point | null;
  bottomRightPadding: Point | null;
  padding: Point | null;
}

export enum MapTheme {
  Default,
  Dark,
}
