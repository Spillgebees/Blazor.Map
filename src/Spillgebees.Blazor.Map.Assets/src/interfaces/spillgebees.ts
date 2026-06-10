import type { Map as MapLibreMap } from "maplibre-gl";
import type { ReferrerPolicy } from "./map";

/** A composed overlay-style layer registered under a prefixed runtime layer id. */
export interface ComposedStyleLayerRegistration {
  runtimeLayerId: string;
  styleId: string;
  originalLayerId: string;
  originalVisible?: boolean;
}

export interface OverlayStyleRequestOptions {
  styleId: string;
  url: string;
  referrerPolicy: ReferrerPolicy | null;
}

/**
 * The shared per-map registries: the map registry (diagnostics/tests) and the
 * composed-style layer index (style composition + engine visibility resolution).
 */
export interface SpillgebeesMapNamespace {
  maps: Map<HTMLElement, MapLibreMap>;
  composedStyleLayerIds: Map<MapLibreMap, Map<string, ComposedStyleLayerRegistration>>;
}
