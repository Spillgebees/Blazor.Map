import type { Map as MapLibreMap } from "maplibre-gl";
import { describe, expect, it, vi } from "vitest";
import { CenterControl } from "./centerControl";

function createMapStub(): MapLibreMap {
  return { getContainer: vi.fn() } as unknown as MapLibreMap;
}

describe("CenterControl", () => {
  describe("onAdd", () => {
    it("should create the expected DOM structure", () => {
      const control = new CenterControl();
      const container = control.onAdd(createMapStub());

      expect(container.className).toContain("sgb-map-center-control");
      const button = container.querySelector("button.sgb-map-center-control-button");
      expect(button).not.toBeNull();
      expect(button?.getAttribute("aria-label")).toBe("Re-center map");
      expect(button?.querySelector("svg")).not.toBeNull();
    });
  });

  describe("onRemove", () => {
    it("should clean up the container", () => {
      const control = new CenterControl();
      const container = control.onAdd(createMapStub());
      document.body.appendChild(container);

      control.onRemove();
      expect(document.body.contains(container)).toBe(false);
    });

    it("should not throw when called without prior onAdd", () => {
      expect(() => new CenterControl().onRemove()).not.toThrow();
    });
  });

  describe("click", () => {
    it("should surface the re-center intent to the host", () => {
      const onClick = vi.fn();
      const control = new CenterControl(onClick);
      const container = control.onAdd(createMapStub());

      container.querySelector("button")?.click();
      expect(onClick).toHaveBeenCalledTimes(1);
    });

    it("should not throw without a callback", () => {
      const control = new CenterControl();
      const container = control.onAdd(createMapStub());

      expect(() => container.querySelector("button")?.click()).not.toThrow();
    });
  });
});
