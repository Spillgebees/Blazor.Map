// The single source of truth for fullscreen behaviour, shared by the built-in control
// (controls/fullscreenControl.ts) and the imperative map API (SgbMap.ToggleFullscreenAsync).
// Mirrors MapLibre's own FullscreenControl mechanics: the native Fullscreen API when present,
// a pseudo-fullscreen CSS fallback otherwise. State changes are event-driven only — there is
// no polling and nothing runs on the render loop.

const PSEUDO_FULLSCREEN_CLASS = "sgb-map-pseudo-fullscreen";

export interface FullscreenController {
  isFullscreen(): boolean;
  enter(): Promise<void>;
  exit(): Promise<void>;
  toggle(): Promise<void>;
  /** Subscribe to state changes; returns an unsubscribe function. */
  onChange(callback: (isFullscreen: boolean) => void): () => void;
  dispose(): void;
}

// Older Safari / iPadOS expose the Fullscreen API under webkit prefixes; mirror MapLibre's own
// control by resolving whichever variant the browser ships. iPhone Safari ships neither, so we
// fall back to pseudo-fullscreen (a CSS class that fills the viewport).
interface WebkitFullscreenTarget extends HTMLElement {
  webkitRequestFullscreen?: () => Promise<void> | void;
}
interface WebkitFullscreenDocument extends Document {
  webkitFullscreenElement?: Element | null;
  webkitExitFullscreen?: () => Promise<void> | void;
}

export function createFullscreenController(target: HTMLElement): FullscreenController {
  const subscribers = new Set<(isFullscreen: boolean) => void>();

  const webkitTarget = target as WebkitFullscreenTarget;
  const webkitDocument = document as WebkitFullscreenDocument;
  const useStandard = typeof target.requestFullscreen === "function";
  const useWebkit = !useStandard && typeof webkitTarget.webkitRequestFullscreen === "function";
  const apiSupported = useStandard || useWebkit;
  const changeEvent = useWebkit ? "webkitfullscreenchange" : "fullscreenchange";
  const fullscreenElement = (): Element | null =>
    useWebkit ? (webkitDocument.webkitFullscreenElement ?? null) : (document.fullscreenElement ?? null);
  const requestFullscreen = (): Promise<void> | void =>
    useWebkit ? webkitTarget.webkitRequestFullscreen?.() : target.requestFullscreen();
  const exitFullscreen = (): Promise<void> | void =>
    useWebkit ? webkitDocument.webkitExitFullscreen?.() : document.exitFullscreen();

  let pseudoActive = false;
  let lastState = false;

  const nativeActive = (): boolean => fullscreenElement() === target;
  const current = (): boolean => (apiSupported ? nativeActive() : pseudoActive);

  const notify = (): void => {
    const state = current();
    if (state === lastState) {
      return;
    }

    lastState = state;
    for (const callback of [...subscribers]) {
      callback(state);
    }
  };

  const onNativeChange = (): void => notify();
  if (apiSupported) {
    document.addEventListener(changeEvent, onNativeChange);
  }

  const enter = async (): Promise<void> => {
    if (current()) {
      return;
    }

    if (apiSupported) {
      // a denied request (no user gesture, permissions policy) rejects; that is an expected,
      // benign outcome — state stays false (derived from the change event), so just swallow it
      try {
        await requestFullscreen();
      } catch {}
    } else {
      pseudoActive = true;
      target.classList.add(PSEUDO_FULLSCREEN_CLASS);
      notify();
    }
  };

  const exit = async (): Promise<void> => {
    if (!current()) {
      return;
    }

    if (apiSupported) {
      try {
        await exitFullscreen();
      } catch {}
    } else {
      pseudoActive = false;
      target.classList.remove(PSEUDO_FULLSCREEN_CLASS);
      notify();
    }
  };

  return {
    isFullscreen: current,
    enter,
    exit,
    toggle: () => (current() ? exit() : enter()),
    onChange(callback) {
      subscribers.add(callback);
      return () => {
        subscribers.delete(callback);
      };
    },
    dispose() {
      if (apiSupported) {
        document.removeEventListener(changeEvent, onNativeChange);
      }
      // a pseudo-fullscreen fallback would otherwise strand the container fixed/full-viewport
      pseudoActive = false;
      target.classList.remove(PSEUDO_FULLSCREEN_CLASS);
      subscribers.clear();
    },
  };
}
