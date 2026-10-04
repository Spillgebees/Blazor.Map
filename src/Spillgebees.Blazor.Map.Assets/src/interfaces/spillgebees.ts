import type { Map as MapLibreMap } from "maplibre-gl";
import type { LayerSlot } from "../engine/slots";
import type { ReferrerPolicy } from "./map";

/** A composed style layer registered under a prefixed runtime layer id. */
export interface ComposedStyleLayerRegistration {
  runtimeLayerId: string;
  styleId: string;
  originalLayerId: string;
  originalVisible: boolean;
  originalFilter: unknown | undefined;
  /** Metadata tags (`sgb:tags`, or `tags`) from the composed style JSON. */
  tags: string[];
}

export interface ComposedStyleRequest {
  styleId: string;
  url: string;
  referrerPolicy: ReferrerPolicy | null;
  slot?: LayerSlot | null;
  layerSlots?: Record<string, LayerSlot> | null;
}

/**
 * The shared per-map registries: the map registry (diagnostics/tests) and the
 * composed-style layer index (style composition + engine visibility resolution).
 */
export interface SpillgebeesMapNamespace {
  maps: Map<HTMLElement, MapLibreMap>;
  composedStyleLayerIds: Map<MapLibreMap, Map<string, ComposedStyleLayerRegistration>>;
}
