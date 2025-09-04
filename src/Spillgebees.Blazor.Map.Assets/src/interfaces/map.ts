interface ISpillgebeesCoordinate {
    latitude: number;
    longitude: number;
}

interface ISpillgebeesPath {
    stroke: boolean | undefined;
    strokeColor: string | undefined;
    strokeWeight: number | undefined;
    strokeOpacity: number | undefined;
    fill: boolean | undefined;
    fillColor: string | undefined;
    fillOpacity: number | undefined;
}

interface ISpillgebeesMarker extends ISpillgebeesPath {
    coordinate: ISpillgebeesCoordinate;
    title: string | undefined;
    icon: string | undefined;
}

interface ISpillgebeesCircleMarker extends ISpillgebeesPath {
    coordinate: ISpillgebeesCoordinate;
    radius: number | 10;
}

interface ISpillgebeesPolyline extends ISpillgebeesPath {
    coordinates: Array<ISpillgebeesCoordinate>;
    smoothFactor: number | 1.0;
    noClip: boolean | false;
}

export { ISpillgebeesCoordinate, ISpillgebeesMarker, ISpillgebeesCircleMarker, ISpillgebeesPolyline };
