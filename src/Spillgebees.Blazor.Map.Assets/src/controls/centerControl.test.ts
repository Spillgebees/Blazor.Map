import type { Map as LeafletMap } from "leaflet";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MockDomEvent, MockMap } from "../../test/leafletMock";

vi.mock("leaflet", async () => {
  const { createLeafletMock } = await import("../../test/leafletMock");
  return createLeafletMock();
});

import type { CenterControlOptions } from "./centerControl";
import { CenterControl } from "./centerControl";

describe("CenterControl", () => {
  let mockMap: MockMap;
  let options: CenterControlOptions;

  beforeEach(() => {
    vi.clearAllMocks();
    mockMap = new MockMap();
    options = {
      center: { latitude: 49.6, longitude: 6.1 },
      zoom: 13,
      position: "topleft",
      fitBoundsOptions: null,
    };
  });

  describe("onAdd", () => {
    it("should create a container with expected classes and attributes", () => {
      // arrange
      const control = new CenterControl(mockMap as unknown as LeafletMap, options);

      // act
      const container = control.onAdd(mockMap as unknown as LeafletMap);

      // assert
      expect(container.tagName.toLowerCase()).toBe("div");
      expect(container.className).toContain("leaflet-bar");
      expect(container.className).toContain("leaflet-control");
      expect(container.className).toContain("sgb-map-center-control");

      const button = container.querySelector("a");
      expect(button).not.toBeNull();
      expect(button!.title).toBe("Center map");
      expect(button!.href).toContain("#");
      expect(button!.getAttribute("role")).toBe("button");
      expect(button!.getAttribute("aria-label")).toBe("Center map");
      expect(button!.className).toContain("leaflet-control-button");
      expect(button!.className).toContain("sgb-map-center-control-button");
    });

    it("should bind click handlers via DomEvent", () => {
      // arrange
      const control = new CenterControl(mockMap as unknown as LeafletMap, options);

      // act
      control.onAdd(mockMap as unknown as LeafletMap);

      // assert
      expect(MockDomEvent.on).toHaveBeenCalledTimes(2);
      // First call: DomEvent.on(button, 'click', DomEvent.stop)
      expect(MockDomEvent.on).toHaveBeenCalledWith(expect.any(HTMLElement), "click", MockDomEvent.stop);
      // Second call: DomEvent.on(button, 'click', this.centerView, this)
      expect(MockDomEvent.on).toHaveBeenCalledWith(
        expect.any(HTMLElement),
        "click",
        expect.any(Function),
        expect.any(CenterControl),
      );
    });
  });

  describe("onRemove", () => {
    it("should unbind click handler via DomEvent.off", () => {
      // arrange
      const control = new CenterControl(mockMap as unknown as LeafletMap, options);
      control.onAdd(mockMap as unknown as LeafletMap);
      vi.clearAllMocks();

      // act
      control.onRemove(mockMap as unknown as LeafletMap);

      // assert
      expect(MockDomEvent.off).toHaveBeenCalledWith(
        expect.any(HTMLElement),
        "click",
        expect.any(Function),
        expect.any(CenterControl),
      );
    });
  });
});
