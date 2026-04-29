export interface IMapControlBase {
  kind: string;
  controlId: string;
  enabled: boolean;
  position: ControlPosition;
  order: number;
}

export interface INavigationMapControl extends IMapControlBase {
  kind: "navigation";
  showCompass: boolean;
  showZoom: boolean;
}

export interface IScaleMapControl extends IMapControlBase {
  kind: "scale";
  unit: "metric" | "imperial" | "nautical";
}

export interface IFullscreenMapControl extends IMapControlBase {
  kind: "fullscreen";
}

export interface IGeolocateMapControl extends IMapControlBase {
  kind: "geolocate";
  trackUser: boolean;
}

export interface ITerrainMapControl extends IMapControlBase {
  kind: "terrain";
  sourceId: string;
}

export interface ICenterMapControl extends IMapControlBase {
  kind: "center";
}

export interface ILegendMapControl extends IMapControlBase {
  kind: "legend";
  title: string | null;
  collapsible: boolean;
  initiallyOpen: boolean;
  className: string | null;
}

export interface IContentMapControl extends IMapControlBase {
  kind: "content";
  className: string | null;
}

export type IMapControl =
  | INavigationMapControl
  | IScaleMapControl
  | IFullscreenMapControl
  | IGeolocateMapControl
  | ITerrainMapControl
  | ICenterMapControl
  | ILegendMapControl
  | IContentMapControl;

export type ControlPosition = "top-left" | "top-right" | "bottom-left" | "bottom-right";
