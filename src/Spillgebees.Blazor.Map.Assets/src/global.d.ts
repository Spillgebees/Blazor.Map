import type { EngineNamespace } from "./engine/bootstrap";
import type { SpillgebeesMapNamespace } from "./interfaces/spillgebees";

declare global {
  // noinspection JSUnusedGlobalSymbols
  interface Window {
    Spillgebees: {
      Map: SpillgebeesMapNamespace;
      Engine?: EngineNamespace;
    };
    hasBeforeStartBeenCalledForSpillgebeesMap: boolean;
    hasAfterStartedBeenCalledForSpillgebeesMap: boolean;
  }
}
