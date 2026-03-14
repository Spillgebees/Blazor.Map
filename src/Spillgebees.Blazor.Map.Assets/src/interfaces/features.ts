import type { ICoordinate, IPoint } from "./map";

export interface IMarker {
  id: string;
  position: ICoordinate;
  title: string | null;
  popup: IPopupOptions | null;
  icon: IMarkerIcon | null;
  color: string | null;
  scale: number | null;
  rotation: number | null;
  draggable: boolean;
  opacity: number | null;
  className: string | null;
}

export interface IMarkerIcon {
  url: string;
  size: IPoint | null;
  anchor: IPoint | null;
}

export interface ICircle {
  id: string;
  position: ICoordinate;
  radius: number;
  color: string | null;
  opacity: number | null;
  strokeColor: string | null;
  strokeWidth: number | null;
  strokeOpacity: number | null;
  popup: IPopupOptions | null;
}

export interface IPolyline {
  id: string;
  coordinates: ICoordinate[];
  color: string | null;
  width: number | null;
  opacity: number | null;
  popup: IPopupOptions | null;
}

export interface IPopupOptions {
  content: string;
  trigger: "click" | "hover" | "permanent";
  anchor: "auto" | "top" | "bottom" | "left" | "right";
  offset: IPoint | null;
  closeButton: boolean;
  maxWidth: string | null;
  className: string | null;
}
