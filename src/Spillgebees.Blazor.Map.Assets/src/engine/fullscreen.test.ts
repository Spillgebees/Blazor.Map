import { afterEach, describe, expect, it, vi } from "vitest";
import { createFullscreenController } from "./fullscreen";

// jsdom ships no Fullscreen API, so we fake the bits the controller depends on and drive
// the `fullscreenchange` event by hand. Two worlds are exercised: a browser that supports
// the native API, and one that does not (pseudo-fullscreen fallback).

function setFullscreenElement(element: Element | null): void {
  Object.defineProperty(document, "fullscreenElement", {
    configurable: true,
    get: () => element,
  });
}

function fireFullscreenChange(): void {
  document.dispatchEvent(new Event("fullscreenchange"));
}

function withNativeApi(target: HTMLElement): {
  requestFullscreen: ReturnType<typeof vi.fn>;
  exitFullscreen: ReturnType<typeof vi.fn>;
} {
  const requestFullscreen = vi.fn().mockImplementation(() => {
    setFullscreenElement(target);
    fireFullscreenChange();
    return Promise.resolve();
  });
  const exitFullscreen = vi.fn().mockImplementation(() => {
    setFullscreenElement(null);
    fireFullscreenChange();
    return Promise.resolve();
  });
  target.requestFullscreen = requestFullscreen as unknown as HTMLElement["requestFullscreen"];
  Object.defineProperty(document, "exitFullscreen", { configurable: true, value: exitFullscreen });
  Object.defineProperty(document, "fullscreenEnabled", { configurable: true, value: true });
  setFullscreenElement(null);
  return { requestFullscreen, exitFullscreen };
}

function withoutNativeApi(target: HTMLElement): void {
  // a target with no requestFullscreen forces the pseudo-fullscreen path
  (target as { requestFullscreen?: unknown }).requestFullscreen = undefined;
  Object.defineProperty(document, "fullscreenEnabled", { configurable: true, value: false });
}

afterEach(() => {
  setFullscreenElement(null);
});

describe("createFullscreenController", () => {
  describe("native Fullscreen API", () => {
    it("enters fullscreen on the target and reflects state", async () => {
      const target = document.createElement("div");
      const api = withNativeApi(target);
      const controller = createFullscreenController(target);

      expect(controller.isFullscreen()).toBe(false);
      await controller.enter();

      expect(api.requestFullscreen).toHaveBeenCalledTimes(1);
      expect(controller.isFullscreen()).toBe(true);
    });

    it("exits fullscreen", async () => {
      const target = document.createElement("div");
      const api = withNativeApi(target);
      const controller = createFullscreenController(target);
      await controller.enter();

      await controller.exit();

      expect(api.exitFullscreen).toHaveBeenCalledTimes(1);
      expect(controller.isFullscreen()).toBe(false);
    });

    it("toggle flips the current state", async () => {
      const target = document.createElement("div");
      withNativeApi(target);
      const controller = createFullscreenController(target);

      await controller.toggle();
      expect(controller.isFullscreen()).toBe(true);
      await controller.toggle();
      expect(controller.isFullscreen()).toBe(false);
    });

    it("notifies subscribers on state change and stops after unsubscribe", async () => {
      const target = document.createElement("div");
      withNativeApi(target);
      const controller = createFullscreenController(target);
      const observed: boolean[] = [];
      const unsubscribe = controller.onChange((value) => observed.push(value));

      await controller.enter();
      await controller.exit();
      unsubscribe();
      await controller.enter();

      expect(observed).toEqual([true, false]);
    });

    it("dispose detaches listeners", async () => {
      const target = document.createElement("div");
      withNativeApi(target);
      const controller = createFullscreenController(target);
      const observed: boolean[] = [];
      controller.onChange((value) => observed.push(value));

      controller.dispose();
      // a stray native change after dispose must not reach subscribers
      setFullscreenElement(target);
      fireFullscreenChange();

      expect(observed).toEqual([]);
    });
  });

  describe("pseudo-fullscreen fallback", () => {
    it("toggles a fallback class and reports state when the API is unavailable", async () => {
      const target = document.createElement("div");
      withoutNativeApi(target);
      const controller = createFullscreenController(target);
      const observed: boolean[] = [];
      controller.onChange((value) => observed.push(value));

      await controller.enter();
      expect(controller.isFullscreen()).toBe(true);
      expect(target.classList.contains("sgb-map-pseudo-fullscreen")).toBe(true);

      await controller.exit();
      expect(controller.isFullscreen()).toBe(false);
      expect(target.classList.contains("sgb-map-pseudo-fullscreen")).toBe(false);
      expect(observed).toEqual([true, false]);
    });
  });
});
