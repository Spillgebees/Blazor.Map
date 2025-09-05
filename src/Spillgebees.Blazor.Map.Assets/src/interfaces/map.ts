import { ControlPosition } from "leaflet";

export interface ISpillgebeesCoordinate {
    latitude: number;
    longitude: number;
}

export interface ISpillgebeesPath {
    stroke: boolean | undefined;
    strokeColor: string | undefined;
    strokeWeight: number | undefined;
    strokeOpacity: number | undefined;
    fill: boolean | undefined;
    fillColor: string | undefined;
    fillOpacity: number | undefined;
}

export interface ISpillgebeesMarker extends ISpillgebeesPath {
    coordinate: ISpillgebeesCoordinate;
    title: string | undefined;
    icon: string | undefined;
}

export interface ISpillgebeesCircleMarker extends ISpillgebeesPath {
    coordinate: ISpillgebeesCoordinate;
    radius: number | 10;
}

export interface ISpillgebeesPolyline extends ISpillgebeesPath {
    coordinates: ISpillgebeesCoordinate[];
    smoothFactor: number | 1.0;
    noClip: boolean | false;
}

export interface ISpillgebeesTileLayer {
    urlTemplate: string;
    attribution: string;
    detectRetina?: boolean | undefined;
    tileSize: number | undefined;
}

export interface ISpillgebeesMapOptions {
    center: ISpillgebeesCoordinate;
    zoom: number;
    showLeafletPrefix: boolean;
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
    showMetric?: boolean | undefined;
    showImperial?: boolean | undefined;
}

export interface ISpillgebeesCenterControlOptions {
    enable: boolean;
    position: ControlPosition;
    center: ISpillgebeesCoordinate;
    zoom: number;
}

export interface ISpillgebeesMapControlOptions {
    zoomControlOptions: ISpillgebeesZoomControlOptions;
    scaleControlOptions: ISpillgebeesScaleControlOptions;
    centerControlOptions: ISpillgebeesCenterControlOptions;
}
