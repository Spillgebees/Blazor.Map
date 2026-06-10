export interface ICoordinate {
  latitude: number;
  longitude: number;
}

export interface IPixelPoint {
  x: number;
  y: number;
}

export interface IMapStyle {
  id: string | null;
  url: string | null;
  referrerPolicy?: ReferrerPolicy | null;
  rasterSource: IRasterTileSource | null;
  wmsSource: IWmsTileSource | null;
}

export type ReferrerPolicy =
  | "no-referrer"
  | "no-referrer-when-downgrade"
  | "origin"
  | "origin-when-cross-origin"
  | "same-origin"
  | "strict-origin"
  | "strict-origin-when-cross-origin"
  | "unsafe-url";

export interface IRasterTileSource {
  urlTemplate: string;
  attribution: string;
  tileSize: number;
  referrerPolicy?: ReferrerPolicy | null;
}

export interface IWmsTileSource {
  baseUrl: string;
  layers: string;
  attribution: string;
  format: string;
  transparent: boolean;
  version: string;
  tileSize: number;
  referrerPolicy?: ReferrerPolicy | null;
}
