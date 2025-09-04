interface SpillgebeesCoordinate {
    latitude: number;
    longitude: number;
}

interface SpillgebeesMarker {
    coordinate: SpillgebeesCoordinate;
    title: string;
    description: string;
    icon: string;
}

export { SpillgebeesCoordinate, SpillgebeesMarker };
