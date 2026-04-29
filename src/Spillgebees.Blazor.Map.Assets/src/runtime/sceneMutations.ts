import {
  addMapLayer,
  addMapSource,
  applyVisibilityGroup,
  moveMapLayer,
  rebindLayerEvents,
  reconcileLayerOrdering,
  removeMapLayer,
  removeMapSource,
  removeVisibilityGroup,
  setFilter,
  setLayerZoomRange,
  setLayoutProperty,
  setPaintProperty,
  setSourceData,
  setSourceDataAnimated,
  setVisibilityGroup,
  unregisterLayerEvents,
  wireLayerEvents,
} from "../sources/geojson";
import { getLayerEventStore, getSceneLayerStore, getSceneSourceStore, getVisibilityGroupStore } from "./registry";
import type { SceneMutationBatch } from "./types";

interface SceneReplayOptions {
  includeVisibilityGroups?: boolean;
}

interface StyleReloadReplayOptions {
  replayImages?: () => void | Promise<void>;
  replayComposedOverlays?: () => Promise<void>;
  onAfterReplay?: () => Promise<void>;
}

function isPromiseLike(value: unknown): value is PromiseLike<void> {
  return (
    (typeof value === "object" || typeof value === "function") &&
    value !== null &&
    "then" in value &&
    typeof value.then === "function"
  );
}

export function applySceneMutations(mapElement: HTMLElement, batch: SceneMutationBatch): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  for (const mutation of batch.mutations) {
    switch (mutation.kind) {
      case "addSource":
        addMapSource(mapElement, mutation.sourceId, mutation.sourceSpec);
        break;
      case "removeSource":
        removeMapSource(mapElement, mutation.sourceId);
        break;
      case "setSourceData":
        setSourceData(mapElement, mutation.sourceId, mutation.data);
        break;
      case "setSourceDataAnimated":
        setSourceDataAnimated(
          mapElement,
          mutation.sourceId,
          mutation.data,
          mutation.animationDuration,
          mutation.animationEasing,
        );
        break;
      case "addLayer":
        addMapLayer(mapElement, mutation.layerSpec, mutation.beforeLayerId, mutation.ordering);
        break;
      case "removeLayer":
        removeMapLayer(mapElement, mutation.layerId);
        break;
      case "moveLayer":
        moveMapLayer(mapElement, mutation.layerId, mutation.beforeLayerId);
        break;
      case "setPaintProperty":
        setPaintProperty(mapElement, mutation.layerId, mutation.propertyName, mutation.propertyValue);
        break;
      case "setLayoutProperty":
        setLayoutProperty(mapElement, mutation.layerId, mutation.propertyName, mutation.propertyValue);
        break;
      case "setFilter":
        setFilter(mapElement, mutation.layerId, mutation.filter);
        break;
      case "setLayerZoomRange":
        setLayerZoomRange(mapElement, mutation.layerId, mutation.minZoom, mutation.maxZoom);
        break;
      case "wireLayerEvents":
        wireLayerEvents(
          mapElement,
          mutation.layerId,
          mutation.dotNetRef,
          mutation.onClick,
          mutation.onMouseEnter,
          mutation.onMouseLeave,
        );
        break;
      case "unregisterLayerEvents":
        unregisterLayerEvents(mapElement, mutation.layerId);
        break;
      case "setVisibilityGroup":
        setVisibilityGroup(mapElement, mutation.groupId, mutation.visible, mutation.targets);
        break;
      case "removeVisibilityGroup":
        removeVisibilityGroup(mapElement, mutation.groupId);
        break;
      case "reconcileOrdering":
        reconcileLayerOrdering(map);
        break;
    }
  }
}

export function replaySceneRegistrations(mapElement: HTMLElement, options?: SceneReplayOptions): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  for (const { sourceId, sourceSpec } of getSceneSourceStore(map).values()) {
    addMapSource(mapElement, sourceId, sourceSpec);
  }

  for (const { layerSpec, beforeLayerId, ordering } of getSceneLayerStore(map).values()) {
    addMapLayer(mapElement, layerSpec, beforeLayerId, ordering);
  }

  reconcileLayerOrdering(map);

  for (const [layerId] of getLayerEventStore(map).entries()) {
    rebindLayerEvents(mapElement, layerId);
  }

  if (options?.includeVisibilityGroups === false) {
    return;
  }

  replayVisibilityGroups(mapElement);
}

export function replayVisibilityGroups(mapElement: HTMLElement): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  for (const group of getVisibilityGroupStore(map).values()) {
    applyVisibilityGroup(mapElement, group);
  }
}

export async function replayStyleReloadState(
  mapElement: HTMLElement,
  options?: StyleReloadReplayOptions,
): Promise<void> {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  const replayImagesResult = options?.replayImages?.();
  if (isPromiseLike(replayImagesResult)) {
    await replayImagesResult;
  }
  replaySceneRegistrations(mapElement, { includeVisibilityGroups: false });
  await options?.replayComposedOverlays?.();
  replayVisibilityGroups(mapElement);
  await options?.onAfterReplay?.();
}
