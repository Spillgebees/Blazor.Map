import type { IControl, Map as MapLibreMap } from "maplibre-gl";
import type { FeatureStorage } from "../types/feature-storage";

export interface SpillgebeesMapNamespace {
  getProtocolVersion: () => number;
  mapFunctions: Record<string, (...args: unknown[]) => unknown>;
  maps: Map<HTMLElement, MapLibreMap>;
  features: Map<MapLibreMap, FeatureStorage>;
  overlays: Map<MapLibreMap, Map<string, unknown>>;
  controls: Map<MapLibreMap, Set<IControl>>;
}
