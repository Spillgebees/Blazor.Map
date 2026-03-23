import type { Map as MapLibreMap } from "maplibre-gl";
import { describe, expect, it, vi } from "vitest";
import "../../test/maplibreMock";
import { resetWindowGlobals } from "../../test/windowSetup";
import type { ICenterControlOptions } from "../interfaces/controls";
import { bootstrap } from "../map";
import { CenterControl } from "./centerControl";

function createMockMap(overrides?: Partial<Record<string, unknown>>): MapLibreMap {
  return {
    getContainer: vi.fn().mockReturnValue(document.createElement("div")),
    flyTo: vi.fn(),
    ...overrides,
  } as unknown as MapLibreMap;
}

function createDefaultCenterOptions(overrides?: Partial<ICenterControlOptions>): ICenterControlOptions {
  return {
    enable: true,
    position: "top-right",
    center: { latitude: 51.505, longitude: -0.09 },
    zoom: 13,
    fitBoundsOptions: null,
    ...overrides,
  };
}

describe("CenterControl", () => {
  describe("onAdd", () => {
    it("should create the expected DOM structure", () => {
      // arrange
      const options = createDefaultCenterOptions();
      const control = new CenterControl(options);
      const mockMap = createMockMap();

      // act
      const container = control.onAdd(mockMap);

      // assert
      expect(container).toBeInstanceOf(HTMLDivElement);
      expect(container.children).toHaveLength(1);

      const button = container.querySelector("button");
      expect(button).not.toBeNull();
      expect(button!.type).toBe("button");
      expect(button!.title).toBe("Re-center map");
      expect(button!.getAttribute("aria-label")).toBe("Re-center map");

      const svg = button!.querySelector("svg");
      expect(svg).not.toBeNull();
    });

    it("should set correct CSS classes on the container", () => {
      // arrange
      const options = createDefaultCenterOptions();
      const control = new CenterControl(options);
      const mockMap = createMockMap();

      // act
      const container = control.onAdd(mockMap);

      // assert
      expect(container.classList.contains("maplibregl-ctrl")).toBe(true);
      expect(container.classList.contains("sgb-map-ctrl-group")).toBe(true);
      expect(container.classList.contains("sgb-map-center-control")).toBe(true);
    });

    it("should set correct CSS class on the button", () => {
      // arrange
      const options = createDefaultCenterOptions();
      const control = new CenterControl(options);
      const mockMap = createMockMap();

      // act
      const container = control.onAdd(mockMap);

      // assert
      const button = container.querySelector("button")!;
      expect(button.classList.contains("sgb-map-center-control-button")).toBe(true);
    });
  });

  describe("onRemove", () => {
    it("should clean up the container and map references", () => {
      // arrange
      const options = createDefaultCenterOptions();
      const control = new CenterControl(options);
      const mockMap = createMockMap();
      const container = control.onAdd(mockMap);
      const parentElement = document.createElement("div");
      parentElement.appendChild(container);

      // act
      control.onRemove();

      // assert
      expect(parentElement.children).toHaveLength(0);
    });

    it("should not throw when called without prior onAdd", () => {
      // arrange
      const options = createDefaultCenterOptions();
      const control = new CenterControl(options);

      // act & assert
      expect(() => control.onRemove()).not.toThrow();
    });
  });

  describe("click handler", () => {
    it("should call map.flyTo with center and zoom when center is set", () => {
      // arrange
      const options = createDefaultCenterOptions({
        center: { latitude: 48.8566, longitude: 2.3522 },
        zoom: 15,
      });
      const control = new CenterControl(options);
      const mockMap = createMockMap();
      const container = control.onAdd(mockMap);
      const button = container.querySelector("button")!;

      // act
      button.click();

      // assert
      expect(mockMap.flyTo).toHaveBeenCalledWith({
        center: [2.3522, 48.8566],
        zoom: 15,
      });
    });

    it("should pass undefined zoom when zoom is null", () => {
      // arrange
      const options = createDefaultCenterOptions({
        center: { latitude: 48.8566, longitude: 2.3522 },
        zoom: null,
      });
      const control = new CenterControl(options);
      const mockMap = createMockMap();
      const container = control.onAdd(mockMap);
      const button = container.querySelector("button")!;

      // act
      button.click();

      // assert
      expect(mockMap.flyTo).toHaveBeenCalledWith({
        center: [2.3522, 48.8566],
        zoom: undefined,
      });
    });

    it("should call the global fitBounds function when fitBoundsOptions is set", () => {
      // arrange
      resetWindowGlobals();
      bootstrap();

      const fitBoundsMock = vi.fn();
      window.Spillgebees.Map.mapFunctions.fitBounds = fitBoundsMock;

      const mapElement = document.createElement("div");
      const fitBoundsOptions = {
        featureIds: ["feature-1", "feature-2"],
        padding: null,
        topLeftPadding: null,
        bottomRightPadding: null,
      };
      const options = createDefaultCenterOptions({
        fitBoundsOptions,
        center: { latitude: 48.8566, longitude: 2.3522 },
        zoom: 15,
      });
      const control = new CenterControl(options);
      const mockMap = createMockMap({
        getContainer: vi.fn().mockReturnValue(mapElement),
      });
      const container = control.onAdd(mockMap);
      const button = container.querySelector("button")!;

      // act
      button.click();

      // assert — fitBounds should be called instead of flyTo
      expect(fitBoundsMock).toHaveBeenCalledWith(mapElement, fitBoundsOptions);
      expect(mockMap.flyTo).not.toHaveBeenCalled();
    });

    it("should not throw when map reference is null", () => {
      // arrange
      const options = createDefaultCenterOptions();
      const control = new CenterControl(options);
      const mockMap = createMockMap();
      const container = control.onAdd(mockMap);
      const button = container.querySelector("button")!;

      // Remove map reference
      control.onRemove();

      // act & assert
      expect(() => button.click()).not.toThrow();
      expect(mockMap.flyTo).not.toHaveBeenCalled();
    });

    it("should not call flyTo when center is null and no fitBoundsOptions", () => {
      // arrange
      const options = createDefaultCenterOptions({
        center: null,
        zoom: 13,
        fitBoundsOptions: null,
      });
      const control = new CenterControl(options);
      const mockMap = createMockMap();
      const container = control.onAdd(mockMap);
      const button = container.querySelector("button")!;

      // act
      button.click();

      // assert
      expect(mockMap.flyTo).not.toHaveBeenCalled();
    });
  });
});
