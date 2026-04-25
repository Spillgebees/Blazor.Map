import type { DotNet } from "@microsoft/dotnet-js-interop";
import type { IControl, Map as MapLibreMap, StyleSpecification } from "maplibre-gl";
import type { FeatureStorage } from "../types/feature-storage";
import type { IMapControl } from "./controls";
import type { IMapImageDefinition, IMapOptions, ITileOverlay, ReferrerPolicy } from "./map";

export interface RegisteredMapSource {
  sourceId: string;
  sourceSpec: Record<string, unknown>;
}

export interface RegisteredMapLayer {
  layerId: string;
  layerSpec: Record<string, unknown>;
  beforeId: string | null;
  imperativeBeforeId?: string | null;
  ordering: {
    declarationOrder: number;
    stack: string | null;
    beforeStack: string | null;
    afterStack: string | null;
  };
}

export interface RegisteredMapImage {
  name: string;
  url: string;
  width: number;
  height: number;
  pixelRatio: number;
  sdf: boolean;
}

export interface VisibilityGroupTargetRegistration {
  styleId: string;
  layerIds: string[];
}

export interface VisibilityGroupRegistration {
  groupId: string;
  visible: boolean;
  targets: VisibilityGroupTargetRegistration[];
}

export interface LayerEventSubscription {
  dotNetRef?: DotNet.DotNetObject;
  click?: (event: { lngLat: { lat: number; lng: number }; features?: Array<{ properties?: unknown }> }) => void;
  mouseEnter?: (event: { lngLat: { lat: number; lng: number }; features?: Array<{ properties?: unknown }> }) => void;
  mouseLeave?: () => void;
  onStyleData?: () => void;
}

export interface ComposedStyleLayerRegistration {
  runtimeLayerId: string;
  styleId: string;
  originalLayerId: string;
}

export interface OverlayStyleRequestOptions {
  styleId: string;
  url: string;
  referrerPolicy: ReferrerPolicy | null;
}

export interface CustomControlRegistration {
  controlId: string;
  kind: "legend" | "content";
  control: IControl;
}

export interface SpillgebeesMapNamespace {
  mapFunctions: Record<string, (...args: unknown[]) => unknown>;
  maps: Map<HTMLElement, MapLibreMap>;
  features: Map<MapLibreMap, FeatureStorage>;
  overlays: Map<MapLibreMap, Map<string, ITileOverlay>>;
  controls: Map<MapLibreMap, Set<IControl>>;
  customControlRegistrations: Map<MapLibreMap, Map<string, CustomControlRegistration>>;
  styles: Map<MapLibreMap, string | StyleSpecification>;
  mapOptions: Map<MapLibreMap, IMapOptions>;
  dotNetHelpers: Map<MapLibreMap, DotNet.DotNetObject>;
  controlsPayload: Map<MapLibreMap, IMapControl[]>;
  sourceSpecs: Map<MapLibreMap, Map<string, RegisteredMapSource>>;
  layerSpecs: Map<MapLibreMap, Map<string, RegisteredMapLayer>>;
  imageRegistrations: Map<MapLibreMap, Map<string, RegisteredMapImage>>;
  layerEventSubscriptions: Map<MapLibreMap, Map<string, LayerEventSubscription>>;
  visibilityGroups: Map<MapLibreMap, Map<string, VisibilityGroupRegistration>>;
  overlayStyleUrls: Map<MapLibreMap, string[]>;
  overlayStyleRequests: Map<MapLibreMap, OverlayStyleRequestOptions[]>;
  composedStyleLayerIds: Map<MapLibreMap, Map<string, ComposedStyleLayerRegistration>>;
  pendingStyleReloads: WeakSet<MapLibreMap>;
  requestContexts: Map<MapLibreMap, { mapOptions: IMapOptions; overlays: ITileOverlay[] }>;
  imageSyncVersion: Map<MapLibreMap, number>;
}

export interface SpillgebeesMapFunctions {
  setImages: (mapElement: HTMLElement, images: IMapImageDefinition[]) => Promise<void>;
}
