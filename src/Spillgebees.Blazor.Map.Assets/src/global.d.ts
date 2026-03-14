import type { SpillgebeesMapNamespace } from "./interfaces/spillgebees";

declare global {
  // noinspection JSUnusedGlobalSymbols
  interface Window {
    Spillgebees: {
      Map: SpillgebeesMapNamespace;
    };
    hasBeforeStartBeenCalledForSpillgebeesMap: boolean;
    hasAfterStartedBeenCalledForSpillgebeesMap: boolean;
  }
}
