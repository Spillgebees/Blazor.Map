import type { DotNet } from "@microsoft/dotnet-js-interop";
import type { IControl, Map as MapLibreMap, StyleSpecification } from "maplibre-gl";
import type { FeatureStorage } from "../types/feature-storage";
import type { ILegendControlOptions, IMapControlOptions } from "./controls";
import type { IMapOptions } from "./map";

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

export interface SpillgebeesMapNamespace {
  getProtocolVersion: () => number;
  mapFunctions: Record<string, (...args: unknown[]) => unknown>;
  maps: Map<HTMLElement, MapLibreMap>;
  features: Map<MapLibreMap, FeatureStorage>;
  overlays: Map<MapLibreMap, Map<string, unknown>>;
  controls: Map<MapLibreMap, Set<IControl>>;
  legendControls: Map<MapLibreMap, IControl>;
  legendControlOptions: Map<MapLibreMap, ILegendControlOptions | null>;
  styles: Map<MapLibreMap, string | StyleSpecification>;
  mapOptions: Map<MapLibreMap, IMapOptions>;
  dotNetHelpers: Map<MapLibreMap, DotNet.DotNetObject>;
  controlOptions: Map<MapLibreMap, IMapControlOptions>;
  sourceSpecs: Map<MapLibreMap, Map<string, RegisteredMapSource>>;
  layerSpecs: Map<MapLibreMap, Map<string, RegisteredMapLayer>>;
  imageRegistrations: Map<MapLibreMap, Map<string, RegisteredMapImage>>;
  layerEventSubscriptions: Map<MapLibreMap, Map<string, LayerEventSubscription>>;
  visibilityGroups: Map<MapLibreMap, Map<string, VisibilityGroupRegistration>>;
  overlayStyleUrls: Map<MapLibreMap, string[]>;
  composedStyleLayerIds: Map<MapLibreMap, Map<string, ComposedStyleLayerRegistration>>;
  pendingStyleReloads: WeakSet<MapLibreMap>;
}
