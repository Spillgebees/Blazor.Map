import type { IControl, Map as MapLibreMap } from "maplibre-gl";
import type { FeatureStorage } from "./types/feature-storage";

export const PROTOCOL_VERSION = 1;

function isNamespaceCompatible(): boolean {
  try {
    return window.Spillgebees?.Map?.getProtocolVersion?.() === PROTOCOL_VERSION;
  } catch {
    return false;
  }
}

function initializeNamespace(): void {
  window.Spillgebees.Map = {
    getProtocolVersion: () => PROTOCOL_VERSION,
    mapFunctions: {
      createMap,
      syncFeatures,
      setOverlays,
      setControls,
      setMapOptions,
      setTheme,
      fitBounds,
      flyTo,
      resize,
      disposeMap,
    },
    maps: new Map<HTMLElement, MapLibreMap>(),
    features: new Map<MapLibreMap, FeatureStorage>(),
    overlays: new Map<MapLibreMap, Map<string, unknown>>(),
    controls: new Map<MapLibreMap, Set<IControl>>(),
  };
}

export function bootstrap(): void {
  window.Spillgebees = window.Spillgebees || ({} as typeof window.Spillgebees);

  if (isNamespaceCompatible()) {
    // Already initialized with the correct protocol version — nothing to do.
    return;
  }

  // Either no namespace exists, or an outdated/incompatible one is present.
  // Force-clear and reinitialize with the current version.
  initializeNamespace();
}

// Stub functions — will be implemented in later phases

export const createMap = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 2
};

export const syncFeatures = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 4
};

export const setOverlays = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 5
};

export const setControls = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 3
};

export const setMapOptions = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 6
};

export const setTheme = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 6
};

export const fitBounds = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 7
};

export const flyTo = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 7
};

export const resize = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 6
};

export const disposeMap = (..._args: unknown[]): void => {
  // Not yet implemented — Phase 8
};
