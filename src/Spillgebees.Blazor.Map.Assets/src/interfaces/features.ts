import type { ICoordinate, IPixelPoint } from "./map";

export interface IMarker {
  id: string;
  position: ICoordinate;
  title: string | null;
  popup: IPopupOptions | null;
  icon: IMarkerIcon | null;
  color: string | null;
  scale: number | null;
  rotation: number | null;
  rotationAlignment: string | null;
  pitchAlignment: string | null;
  draggable: boolean;
  opacity: number | null;
  className: string | null;
}

export interface IMarkerIcon {
  url: string;
  size: IPixelPoint | null;
  anchor: IPixelPoint | null;
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
  contentMode: "text" | "rawHtml";
  trigger: "click" | "hover" | "permanent";
  anchor: "auto" | "top" | "bottom" | "left" | "right";
  offset: IPixelPoint | null;
  closeButton: boolean;
  maxWidth: string | null;
  className: string | null;
}
