// noinspection JSUnusedGlobalSymbols

import { bootstrap } from "./map";
import './styles.scss';

export function beforeWebStart(options: any) {
    if (window.hasBeforeStartBeenCalledForSpillgebeesMap) {
        return;
    }

    beforeStart(options);
}

export function afterWebStarted(options: any) {
    if (window.hasAfterStartedBeenCalledForSpillgebeesMap) {
        return;
    }

    afterStarted(options);
}

export function beforeWebAssemblyStart(options: any) {
    if (window.hasBeforeStartBeenCalledForSpillgebeesMap) {
        return;
    }

    beforeStart(options);
}

export function afterWebAssemblyStarted(options: any) {
    if (window.hasAfterStartedBeenCalledForSpillgebeesMap) {
        return
    }

    afterStarted(options);
}

export function beforeServerStart(options: any) {
    if (window.hasBeforeStartBeenCalledForSpillgebeesMap) {
        return;
    }

    beforeStart(options);
}

export function afterServerStarted(options: any) {
    if (window.hasAfterStartedBeenCalledForSpillgebeesMap) {
        return;
    }

    afterStarted(options);
}

export function beforeStart(_: any) {
    window.hasBeforeStartBeenCalledForSpillgebeesMap = true;
    bootstrap();
}

export function afterStarted(_: any) {
    window.hasAfterStartedBeenCalledForSpillgebeesMap = true;
}
