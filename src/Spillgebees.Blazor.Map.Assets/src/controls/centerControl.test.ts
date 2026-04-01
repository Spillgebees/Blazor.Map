import type { Map as MapLibreMap } from "maplibre-gl";
import { describe, expect, it, vi } from "vitest";
import "../../test/maplibreMock";
import { resetWindowGlobals } from "../../test/windowSetup";
import type { IMapOptions } from "../interfaces/map";
import { bootstrap } from "../map";
import { CenterControl } from "./centerControl";

function createMockMap(overrides?: Partial<Record<string, unknown>>): MapLibreMap {
  return {
    getContainer: vi.fn().mockReturnValue(document.createElement("div")),
    flyTo: vi.fn(),
    ...overrides,
  } as unknown as MapLibreMap;
}

function createDefaultMapOptions(overrides?: Partial<IMapOptions>): IMapOptions {
  return {
    center: { latitude: 51.505, longitude: -0.09 },
    zoom: 13,
    style: null,
    styles: null,
    composedGlyphsUrl: null,
    pitch: 0,
    bearing: 0,
    projection: "mercator",
    terrain: false,
    terrainExaggeration: 1.0,
    fitBoundsOptions: null,
    minZoom: null,
    maxZoom: null,
    maxBounds: null,
    interactive: true,
    cooperativeGestures: false,
    webFonts: null,
    ...overrides,
  };
}

describe("CenterControl", () => {
  describe("onAdd", () => {
    it("should create the expected DOM structure", () => {
      // arrange
      const control = new CenterControl();
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
      const control = new CenterControl();
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
      const control = new CenterControl();
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
      const control = new CenterControl();
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
      const control = new CenterControl();

      // act & assert
      expect(() => control.onRemove()).not.toThrow();
    });
  });

  describe("click handler", () => {
    it("should call map.flyTo with center and zoom from stored mapOptions", () => {
      // arrange
      resetWindowGlobals();
      bootstrap();

      const mockMap = createMockMap();
      const mapOptions = createDefaultMapOptions({
        center: { latitude: 48.8566, longitude: 2.3522 },
        zoom: 15,
      });
      window.Spillgebees.Map.mapOptions.set(mockMap, mapOptions);

      const control = new CenterControl();
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

    it("should call the global fitBounds function when mapOptions has fitBoundsOptions", () => {
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
      const mockMap = createMockMap({
        getContainer: vi.fn().mockReturnValue(mapElement),
      });
      const mapOptions = createDefaultMapOptions({
        center: { latitude: 48.8566, longitude: 2.3522 },
        zoom: 15,
        fitBoundsOptions,
      });
      window.Spillgebees.Map.mapOptions.set(mockMap, mapOptions);

      const control = new CenterControl();
      const container = control.onAdd(mockMap);
      const button = container.querySelector("button")!;

      // act
      button.click();

      // assert
      expect(fitBoundsMock).toHaveBeenCalledWith(mapElement, fitBoundsOptions);
      expect(mockMap.flyTo).not.toHaveBeenCalled();
    });

    it("should not throw when map reference is null", () => {
      // arrange
      const control = new CenterControl();
      const mockMap = createMockMap();
      const container = control.onAdd(mockMap);
      const button = container.querySelector("button")!;

      // remove map reference
      control.onRemove();

      // act & assert
      expect(() => button.click()).not.toThrow();
      expect(mockMap.flyTo).not.toHaveBeenCalled();
    });

    it("should not throw when mapOptions is not available for the map", () => {
      // arrange
      resetWindowGlobals();
      bootstrap();

      const mockMap = createMockMap();
      // deliberately not setting mapOptions for this map

      const control = new CenterControl();
      const container = control.onAdd(mockMap);
      const button = container.querySelector("button")!;

      // act & assert
      expect(() => button.click()).not.toThrow();
      expect(mockMap.flyTo).not.toHaveBeenCalled();
    });
  });
});
