export interface IMapControlOptions {
  navigation: INavigationControlOptions | null;
  scale: IScaleControlOptions | null;
  fullscreen: IFullscreenControlOptions | null;
  geolocate: IGeolocateControlOptions | null;
  terrain: ITerrainControlOptions | null;
  center: ICenterControlOptions | null;
}

export interface INavigationControlOptions {
  enable: boolean;
  position: ControlPosition;
  showCompass: boolean;
  showZoom: boolean;
  order?: number;
}

export interface IScaleControlOptions {
  enable: boolean;
  position: ControlPosition;
  unit: "metric" | "imperial" | "nautical";
  order?: number;
}

export interface IFullscreenControlOptions {
  enable: boolean;
  position: ControlPosition;
  order?: number;
}

export interface IGeolocateControlOptions {
  enable: boolean;
  position: ControlPosition;
  trackUser: boolean;
  order?: number;
}

export interface ITerrainControlOptions {
  enable: boolean;
  position: ControlPosition;
  order?: number;
}

export interface ICenterControlOptions {
  enable: boolean;
  position: ControlPosition;
  order?: number;
}

export interface ILegendControlOptions {
  enable: boolean;
  position: ControlPosition;
  order?: number;
  title: string | null;
  collapsible: boolean;
  initiallyOpen: boolean;
  className: string | null;
}

export type ControlPosition = "top-left" | "top-right" | "bottom-left" | "bottom-right";
