// Op vocabulary for the map engine, mirrored by EngineOps.cs on the .NET side.
// Ops are the only way scene structure crosses the .NET → JS boundary; they apply in
// array order and double as the replay log after a style change.

export type AnimationEasing = "linear" | "easeInOut";

export interface AnimationConfig {
  durationMs: number;
  easing?: AnimationEasing;
}

/** How a camera gesture behaves while following: leave it free, hold a target (with or without letting
 * the user nudge it back), or clear the follow when the user uses it. */
export type FollowGestureMode = "free" | "anchored" | "locked" | "clear";

export interface FollowCameraConfig {
  zoomMode: FollowGestureMode;
  zoom?: number | null;
  // Pitch and bearing share one MapLibre gesture, so they share one mode.
  orientationMode: FollowGestureMode;
  pitch?: number | null;
  bearingSource: "keepcurrent" | "fixed" | "matchheading";
  bearing?: number | null;
  offset?: { x: number; y: number } | null;
}

export interface FollowInteractionConfig {
  clearOnUserPan: boolean;
  clearWhenFeatureMissing: boolean;
}

export interface EntityLayerConfig {
  /** MapLibre GeoJSON source cluster options, passed through (cluster, clusterRadius, …). */
  cluster?: Record<string, unknown> | null;
  /** Position interpolation applied to motion updates. */
  animation?: AnimationConfig | null;
  /** Layer ids whose hover should set entity feature-state locally (no interop). */
  hoverLayerIds?: string[] | null;
  /** Layer ids where clicking a cluster zooms to its expansion zoom, handled locally. */
  clusterZoomLayerIds?: string[] | null;
  /** Creates the sibling decoration source (`{id}-decorations`). */
  decorations?: boolean | null;
  /**
   * Cluster options for the decoration source. Decorations cluster in lockstep with
   * primaries (with their own minPoints) so they hide when their entities cluster,
   * without distorting the primary source's point_count.
   */
  decorationCluster?: Record<string, unknown> | null;
}

export interface EntityDecorationUpsert {
  /** Decoration slot, 0–62; the decoration feature id is index * 64 + 1 + slot. */
  slot: number;
  id: string;
  text?: string | null;
  icon?: string | null;
  anchor?: string | null;
  offset?: [number, number] | null;
  displayMode?: string | null;
  color?: string | null;
  textSize?: number | null;
  iconSize?: number | null;
  rot?: number | null;
  sortKey?: number | null;
  haloColor?: string | null;
  haloWidth?: number | null;
  iconColor?: string | null;
}

export interface EntityUpsert {
  /** Entity index assigned by the .NET side; doubles as the primary feature id (× 64). */
  idx: number;
  /** Stable string id, stored in feature properties for events/debugging. */
  id: string;
  lng: number;
  lat: number;
  icon?: string | null;
  size?: number | null;
  rot?: number | null;
  anchor?: string | null;
  offset?: [number, number] | null;
  color?: string | null;
  sortKey?: number | null;
  hover?: { scale?: number | null; raise?: boolean | null } | null;
  props?: Record<string, unknown> | null;
  decorations?: EntityDecorationUpsert[] | null;
}

/**
 * Visibility targeting vocabulary (mirrors the public MapDisplayTarget model):
 * runtime layers by id, style layers (composed or base; empty layerIds = whole style),
 * feature subsets via filter composition, and metadata-tagged style layers.
 */
export type VisibilityTarget =
  | { kind: "runtimeLayer"; layerIds: string[] }
  | { kind: "styleLayer"; styleId: string; layerIds: string[] }
  | { kind: "styleLayerFeatures"; styleId: string; layerIds: string[]; filter: unknown }
  | { kind: "styleLayerTag"; styleId: string; tags: string[] };

export interface OverlayPartConfig {
  id: string;
  visible: boolean;
  targets: VisibilityTarget[];
}

export interface EventHandlers {
  click?: number;
  enter?: number;
  leave?: number;
}

export interface MarkerPopupData {
  content: string;
  contentMode: "text" | "rawHtml";
  trigger: "click" | "hover" | "permanent";
  anchor: string;
  offset?: { x: number; y: number } | null;
  closeButton: boolean;
  maxWidth?: string | null;
  className?: string | null;
}

export interface MarkerIconData {
  url: string;
  size?: { x: number; y: number } | null;
  anchor?: { x: number; y: number } | null;
}

export interface LatLng {
  latitude: number;
  longitude: number;
}

/**
 * Map-level configuration applied as a whole on load and whenever the map's
 * parameters change (the whole config reapplies; individual setters are idempotent).
 */
export interface MapConfigData {
  pitch: number;
  bearing: number;
  projection: "mercator" | "globe";
  minZoom?: number | null;
  maxZoom?: number | null;
  maxBounds?: { southwest: LatLng; northeast: LatLng } | null;
  /** "browserDefault" | "roundedUpDevicePixelRatio"; explicit pixelRatio wins. */
  pixelRatioMode?: string | null;
  pixelRatio?: number | null;
}

/**
 * Flat control wire shape: one interface for every control kind, with the
 * kind-specific fields simply optional.
 */
export interface ControlData {
  kind: string;
  controlId: string;
  visible: boolean;
  position: string;
  order: number;
  showCompass?: boolean | null;
  showZoom?: boolean | null;
  unit?: string | null;
  trackUser?: boolean | null;
  sourceId?: string | null;
  title?: string | null;
  collapsible?: boolean | null;
  initiallyOpen?: boolean | null;
  className?: string | null;
  label?: string | null;
  isOpen?: boolean | null;
  maxWidth?: string | null;
  /** Engine event handler ids (e.g. center-control click). */
  events?: { click?: number | null } | null;
}

export interface ControlContentEvents {
  /** Engine event handler id receiving `{ open: boolean }` panel state changes. */
  openChanged?: number | null;
}

export interface PopupData {
  id: string;
  position: { latitude: number; longitude: number };
  options: MarkerPopupData;
  events?: {
    /** Engine event handler id invoked when the user closes the popup. */
    closed?: number | null;
  } | null;
}

/** Wire shape of the public C# `Marker` record (camelCase, nulls omitted). */
export interface MarkerData {
  id: string;
  position: { latitude: number; longitude: number };
  title?: string | null;
  popup?: MarkerPopupData | null;
  icon?: MarkerIconData | null;
  color?: string | null;
  scale?: number | null;
  rotation?: number | null;
  rotationAlignment?: string | null;
  pitchAlignment?: string | null;
  draggable?: boolean;
  opacity?: number | null;
  className?: string | null;
}

export type Op =
  | { op: "source.add"; id: string; spec: Record<string, unknown> }
  | { op: "source.remove"; id: string }
  | { op: "source.setData"; id: string; data: unknown; animate?: AnimationConfig | null }
  | { op: "source.clusterZoom"; id: string; layerIds: string[] }
  | { op: "layer.add"; id: string; spec: Record<string, unknown>; slot?: string | null; before?: string | null }
  | { op: "layer.remove"; id: string }
  | { op: "layer.setPaint"; id: string; name: string; value: unknown }
  | { op: "layer.setLayout"; id: string; name: string; value: unknown }
  | { op: "layer.setFilter"; id: string; filter: unknown }
  | { op: "layer.setZoom"; id: string; min: number; max: number }
  | { op: "layer.move"; id: string; slot?: string | null; before?: string | null }
  | { op: "slot.define"; id: string; before?: string | null }
  | { op: "entities.create"; id: string; config: EntityLayerConfig }
  | { op: "entities.configure"; id: string; config: Partial<EntityLayerConfig> }
  | { op: "entities.remove"; id: string }
  | { op: "entities.upsert"; id: string; epoch: number; upserts: EntityUpsert[]; removes: number[] }
  | { op: "entities.select"; id: string; selected: number[] }
  | { op: "visibility.set"; id: string; visible: boolean; targets: VisibilityTarget[] }
  | { op: "visibility.remove"; id: string }
  | { op: "overlay.set"; id: string; visible: boolean; targets: VisibilityTarget[]; parts: OverlayPartConfig[] }
  | { op: "overlay.remove"; id: string }
  | { op: "image.add"; id: string; url: string; options?: Record<string, unknown> | null }
  | { op: "image.remove"; id: string }
  | { op: "marker.set"; marker: MarkerData }
  | { op: "marker.remove"; id: string }
  | { op: "control.set"; control: ControlData }
  | { op: "control.remove"; id: string }
  | { op: "control.content"; id: string; events?: ControlContentEvents | null }
  | { op: "control.removeContent"; id: string }
  | { op: "popup.set"; popup: PopupData }
  | { op: "popup.remove"; id: string }
  | { op: "popup.show"; position: LatLng; options: MarkerPopupData }
  | { op: "popup.close" }
  | { op: "map.configure"; config: MapConfigData }
  | { op: "map.resize" }
  | { op: "map.requestPolicy"; origin: string; policy?: string | null }
  | { op: "camera.flyTo"; center?: LatLng | null; zoom?: number | null; bearing?: number | null; pitch?: number | null }
  | {
      op: "camera.fitFeatures";
      featureIds: string[];
      padding?: { x: number; y: number } | null;
      topLeftPadding?: { x: number; y: number } | null;
      bottomRightPadding?: { x: number; y: number } | null;
    }
  | {
      op: "camera.follow";
      layerId: string;
      entityId: string;
      camera?: FollowCameraConfig | null;
      animation?: AnimationConfig | null;
      interaction?: FollowInteractionConfig | null;
    }
  | { op: "camera.clearFollow" }
  | {
      op: "source.featureState";
      id: string;
      featureId: string | number;
      state: Record<string, unknown>;
      sourceLayer?: string | null;
    }
  | { op: "events.set"; layerId: string; handlers: EventHandlers }
  | { op: "events.clear"; layerId: string };

export type OpKind = Op["op"];
