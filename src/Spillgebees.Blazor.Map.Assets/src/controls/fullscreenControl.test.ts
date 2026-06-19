import type { Map as MapLibreMap } from "maplibre-gl";
import { describe, expect, it, vi } from "vitest";
import type { FullscreenController } from "../engine/fullscreen";
import { FullscreenControl } from "./fullscreenControl";

function createMapStub(): MapLibreMap {
  return { getContainer: vi.fn() } as unknown as MapLibreMap;
}

interface ControllerStub extends FullscreenController {
  fire(isFullscreen: boolean): void;
}

function createControllerStub(): ControllerStub {
  let state = false;
  const subscribers: ((value: boolean) => void)[] = [];
  return {
    isFullscreen: () => state,
    enter: vi.fn(async () => {}),
    exit: vi.fn(async () => {}),
    toggle: vi.fn(async () => {}),
    onChange(cb) {
      subscribers.push(cb);
      return () => {
        const index = subscribers.indexOf(cb);
        if (index >= 0) {
          subscribers.splice(index, 1);
        }
      };
    },
    dispose: vi.fn(),
    fire(isFullscreen: boolean) {
      state = isFullscreen;
      for (const cb of [...subscribers]) {
        cb(isFullscreen);
      }
    },
  };
}

describe("FullscreenControl", () => {
  describe("onAdd", () => {
    it("renders a button with the default enter glyph and accessible name", () => {
      const control = new FullscreenControl(createControllerStub());
      const container = control.onAdd(createMapStub());

      expect(container.className).toContain("sgb-map-fullscreen-control");
      const button = container.querySelector<HTMLButtonElement>("button");
      expect(button).not.toBeNull();
      expect(button?.querySelector("svg")).not.toBeNull();
      expect(button?.getAttribute("aria-label")).toBe("Enter fullscreen");
    });

    it("honours custom enter and exit icons", () => {
      const control = new FullscreenControl(createControllerStub(), {
        enterIcon: '<svg data-testid="custom-enter"></svg>',
        exitIcon: '<svg data-testid="custom-exit"></svg>',
        enterTitle: "Go big",
        exitTitle: "Go small",
      });
      const container = control.onAdd(createMapStub());

      const button = container.querySelector<HTMLButtonElement>("button");
      expect(button?.querySelector("[data-testid=custom-enter]")).not.toBeNull();
      expect(button?.getAttribute("aria-label")).toBe("Go big");
      expect(button?.getAttribute("title")).toBe("Go big");
    });
  });

  describe("click", () => {
    it("toggles fullscreen through the controller", () => {
      const controller = createControllerStub();
      const control = new FullscreenControl(controller);
      const container = control.onAdd(createMapStub());

      container.querySelector("button")?.click();
      expect(controller.toggle).toHaveBeenCalledTimes(1);
    });
  });

  describe("state changes", () => {
    it("swaps to the exit glyph and label when fullscreen is entered", () => {
      const controller = createControllerStub();
      const control = new FullscreenControl(controller, {
        enterIcon: '<svg data-testid="custom-enter"></svg>',
        exitIcon: '<svg data-testid="custom-exit"></svg>',
      });
      const container = control.onAdd(createMapStub());
      const button = container.querySelector<HTMLButtonElement>("button");

      controller.fire(true);

      expect(button?.querySelector("[data-testid=custom-exit]")).not.toBeNull();
      expect(button?.querySelector("[data-testid=custom-enter]")).toBeNull();
      expect(button?.getAttribute("aria-label")).toBe("Exit fullscreen");
      expect(button?.getAttribute("title")).toBe("Exit fullscreen");
    });
  });

  describe("onRemove", () => {
    it("unsubscribes from the controller and removes the DOM", () => {
      const controller = createControllerStub();
      const control = new FullscreenControl(controller);
      const container = control.onAdd(createMapStub());
      document.body.appendChild(container);

      control.onRemove();

      expect(document.body.contains(container)).toBe(false);
      // a state change after removal must not touch detached DOM
      expect(() => controller.fire(true)).not.toThrow();
    });

    it("does not throw when called without prior onAdd", () => {
      expect(() => new FullscreenControl(createControllerStub()).onRemove()).not.toThrow();
    });
  });
});
