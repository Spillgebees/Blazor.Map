import type { Map as MapLibreMap } from "maplibre-gl";
import type {
  LayerEventSubscription,
  RegisteredMapLayer,
  RegisteredMapSource,
  VisibilityGroupRegistration,
} from "../interfaces/spillgebees";

function getOrCreateStore<T>(root: Map<MapLibreMap, Map<string, T>>, map: MapLibreMap): Map<string, T> {
  const existing = root.get(map);
  if (existing) {
    return existing;
  }

  const created = new Map<string, T>();
  root.set(map, created);
  return created;
}

export function getSceneSourceStore(map: MapLibreMap): Map<string, RegisteredMapSource> {
  return getOrCreateStore(window.Spillgebees.Map.sourceSpecs, map);
}

export function getSceneLayerStore(map: MapLibreMap): Map<string, RegisteredMapLayer> {
  return getOrCreateStore(window.Spillgebees.Map.layerSpecs, map);
}

export function getLayerEventStore(map: MapLibreMap): Map<string, LayerEventSubscription> {
  return getOrCreateStore(window.Spillgebees.Map.layerEventSubscriptions, map);
}

export function getVisibilityGroupStore(map: MapLibreMap): Map<string, VisibilityGroupRegistration> {
  return getOrCreateStore(window.Spillgebees.Map.visibilityGroups, map);
}
