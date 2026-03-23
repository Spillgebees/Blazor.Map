export interface ICoordinate {
  latitude: number;
  longitude: number;
}

export interface IPoint {
  x: number;
  y: number;
}

export interface IMapOptions {
  center: ICoordinate;
  zoom: number;
  style: IMapStyle | null;
  styles: IMapStyle[] | null;
  composedGlyphsUrl: string | null;
  pitch: number;
  bearing: number;
  projection: "mercator" | "globe";
  terrain: boolean;
  terrainExaggeration: number;
  fitBoundsOptions: IFitBoundsOptions | null;
  minZoom: number | null;
  maxZoom: number | null;
  maxBounds: IMapBounds | null;
  interactive: boolean;
  cooperativeGestures: boolean;
  webFonts: string[] | null;
}

export interface IMapBounds {
  southwest: ICoordinate;
  northeast: ICoordinate;
}

export interface IMapStyle {
  id: string | null;
  url: string | null;
  rasterSource: IRasterTileSource | null;
  wmsSource: IWmsTileSource | null;
}

export interface IRasterTileSource {
  urlTemplate: string;
  attribution: string;
  tileSize: number;
}

export interface IWmsTileSource {
  baseUrl: string;
  layers: string;
  attribution: string;
  format: string;
  transparent: boolean;
  version: string;
  tileSize: number;
}

export interface ITileOverlay {
  id: string;
  urlTemplate: string;
  attribution: string;
  tileSize: number;
  opacity: number;
}

export interface IFitBoundsOptions {
  featureIds: string[];
  padding: IPoint | null;
  topLeftPadding: IPoint | null;
  bottomRightPadding: IPoint | null;
}
