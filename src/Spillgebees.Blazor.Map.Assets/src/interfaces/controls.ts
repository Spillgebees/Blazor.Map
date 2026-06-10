export interface IMapControlBase {
  kind: string;
  controlId: string;
  visible: boolean;
  position: "top-left" | "top-right" | "bottom-left" | "bottom-right";
  order: number;
}

export interface ILegendMapControl extends IMapControlBase {
  kind: "legend";
  title: string | null;
  collapsible: boolean;
  initiallyOpen: boolean;
  className: string | null;
}

export interface IPanelMapControl extends IMapControlBase {
  kind: "panel";
  label: string;
  title: string | null;
  initiallyOpen: boolean;
  isOpen: boolean | null;
  maxWidth: string | null;
  className: string | null;
}

export interface IContentMapControl extends IMapControlBase {
  kind: "content";
  className: string | null;
}
