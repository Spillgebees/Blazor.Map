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
}

export interface IScaleControlOptions {
  enable: boolean;
  position: ControlPosition;
  unit: "metric" | "imperial" | "nautical";
}

export interface IFullscreenControlOptions {
  enable: boolean;
  position: ControlPosition;
}

export interface IGeolocateControlOptions {
  enable: boolean;
  position: ControlPosition;
  trackUser: boolean;
}

export interface ITerrainControlOptions {
  enable: boolean;
  position: ControlPosition;
}

export interface ICenterControlOptions {
  enable: boolean;
  position: ControlPosition;
}

export interface ILegendControlOptions {
  enable: boolean;
  position: ControlPosition;
  title: string | null;
  collapsible: boolean;
  initiallyOpen: boolean;
  className: string | null;
}

export type ControlPosition = "top-left" | "top-right" | "bottom-left" | "bottom-right";
