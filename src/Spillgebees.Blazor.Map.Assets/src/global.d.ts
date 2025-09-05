import { Spillgebees } from "./interfaces/spillgebees";

declare global {
    // noinspection JSUnusedGlobalSymbols
    interface Window {
        Spillgebees: Spillgebees;
        hasBeforeStartBeenCalledForSpillgebeesMap: boolean;
        hasAfterStartedBeenCalledForSpillgebeesMap: boolean;
    }
}
