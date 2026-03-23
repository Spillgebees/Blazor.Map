// noinspection JSUnusedGlobalSymbols

import { bootstrap } from "./map";
import "./styles.scss";

export function beforeWebStart(options: unknown) {
  if (window.hasBeforeStartBeenCalledForSpillgebeesMap) {
    return;
  }

  beforeStart(options);
}

export function afterWebStarted(options: unknown) {
  if (window.hasAfterStartedBeenCalledForSpillgebeesMap) {
    return;
  }

  afterStarted(options);
}

export function beforeWebAssemblyStart(options: unknown) {
  if (window.hasBeforeStartBeenCalledForSpillgebeesMap) {
    return;
  }

  beforeStart(options);
}

export function afterWebAssemblyStarted(options: unknown) {
  if (window.hasAfterStartedBeenCalledForSpillgebeesMap) {
    return;
  }

  afterStarted(options);
}

export function beforeServerStart(options: unknown) {
  if (window.hasBeforeStartBeenCalledForSpillgebeesMap) {
    return;
  }

  beforeStart(options);
}

export function afterServerStarted(options: unknown) {
  if (window.hasAfterStartedBeenCalledForSpillgebeesMap) {
    return;
  }

  afterStarted(options);
}

export function beforeStart(_: unknown) {
  window.hasBeforeStartBeenCalledForSpillgebeesMap = true;
  bootstrap();
}

export function afterStarted(_: unknown) {
  window.hasAfterStartedBeenCalledForSpillgebeesMap = true;
}
