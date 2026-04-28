import { beforeEach, describe, expect, it } from "vitest";
import { createMockDotNetHelper } from "../../test/dotNetHelperMock";
import {
  getLatestMockMapInstance,
  getMockMarkerConstructor,
  getMockPopupConstructor,
  resetMockMapState,
} from "../../test/maplibreMock";
import { resetWindowGlobals } from "../../test/windowSetup";
import type { IMapControl } from "../interfaces/controls";
import type { IMarker, IPopupOptions } from "../interfaces/features";
import type { IMapOptions } from "../interfaces/map";
import { bootstrap, createMap } from "../map";
import type { FeatureStorage } from "../types/feature-storage";
import { addMarkers, removeMarkers, updateMarkers } from "./markers";

function createDefaultMapOptions(overrides?: Partial<IMapOptions>): IMapOptions {
  return {
    center: { latitude: 51.505, longitude: -0.09 },
    zoom: 13,
    style: null,
    composedGlyphsUrl: null,
    pitch: 0,
    bearing: 0,
    projection: "mercator",
    terrain: false,
    terrainExaggeration: 1.0,
    fitBoundsOptions: null,
    minZoom: null,
    maxZoom: null,
    interactive: true,
    cooperativeGestures: false,
    ...overrides,
  };
}

function createDefaultControls(): IMapControl[] {
  return [];
}

function createDefaultMarker(overrides?: Partial<IMarker>): IMarker {
  return {
    id: "marker-1",
    position: { latitude: 51.505, longitude: -0.09 },
    title: null,
    popup: null,
    icon: null,
    color: null,
    scale: null,
    rotation: null,
    rotationAlignment: null,
    pitchAlignment: null,
    draggable: false,
    opacity: null,
    className: null,
    ...overrides,
  };
}

function createEmptyFeatureStorage(): FeatureStorage {
  return {
    markers: new Map(),
    circleData: new Map(),
    polylineData: new Map(),
  };
}

function setupMapAndGetInstance(): { map: MockMapInstance; mapElement: HTMLElement } {
  const mapElement = document.createElement("div");
  const dotNetHelper = createMockDotNetHelper();
  createMap(
    dotNetHelper,
    "OnMapInitialized",
    mapElement,
    createDefaultMapOptions(),
    createDefaultControls(),
    "light",
    [],
    [],
    [],
    [],
  );
  const map = getLatestMockMapInstance()!;
  return { map, mapElement };
}

describe("addMarkers", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should create a MapLibre marker with correct lng/lat (coordinate swap)", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({
      position: { latitude: 48.8566, longitude: 2.3522 },
    });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert — MapLibre expects [lng, lat], not [lat, lng]
    const markerInstance = getMockMarkerConstructor().mock.results[0]?.value;
    expect(markerInstance.setLngLat).toHaveBeenCalledWith([2.3522, 48.8566]);
    expect(markerInstance.addTo).toHaveBeenCalledWith(map);
  });

  it("should create marker with custom icon element", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({
      icon: {
        url: "https://example.com/icon.png",
        size: { x: 32, y: 32 },
        anchor: null,
      },
    });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert — should pass an element option with the icon
    const constructorArgs = getMockMarkerConstructor().mock.calls[0]?.[0];
    expect(constructorArgs.element).toBeDefined();
    expect(constructorArgs.element).toBeInstanceOf(HTMLElement);
    const img = constructorArgs.element.querySelector("img");
    expect(img).not.toBeNull();
    expect(img!.src).toBe("https://example.com/icon.png");
    expect(img!.width).toBe(32);
    expect(img!.height).toBe(32);
  });

  it("should create marker with color and scale (default marker)", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({
      color: "#ff0000",
      scale: 1.5,
    });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert
    const constructorArgs = getMockMarkerConstructor().mock.calls[0]?.[0];
    expect(constructorArgs.color).toBe("#ff0000");
    expect(constructorArgs.scale).toBe(1.5);
  });

  it("should create marker with rotation", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({
      rotation: 45,
    });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert
    const constructorArgs = getMockMarkerConstructor().mock.calls[0]?.[0];
    expect(constructorArgs.rotation).toBe(45);
  });

  it("should create marker with click popup (setPopup)", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Hello</p>",
      contentMode: "rawHtml",
      trigger: "click",
      anchor: "auto",
      offset: null,
      closeButton: true,
      maxWidth: "300px",
      className: null,
    };
    const marker = createDefaultMarker({ popup });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert — click popup should be attached via setPopup
    const markerInstance = getMockMarkerConstructor().mock.results[0]?.value;
    expect(markerInstance.setPopup).toHaveBeenCalled();
    const popupConstructorArgs = getMockPopupConstructor().mock.calls[0]?.[0];
    expect(popupConstructorArgs.closeButton).toBe(true);
    expect(popupConstructorArgs.closeOnClick).toBe(true);
    expect(popupConstructorArgs.maxWidth).toBe("300px");
  });

  it("should use setText for text popup content", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Hello</p>",
      contentMode: "text",
      trigger: "click",
      anchor: "auto",
      offset: null,
      closeButton: true,
      maxWidth: null,
      className: null,
    };
    const marker = createDefaultMarker({ popup });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert
    const popupInstance = getMockPopupConstructor().mock.results[0]?.value;
    expect(popupInstance.setText).toHaveBeenCalledWith("<p>Hello</p>");
    expect(popupInstance.setHTML).not.toHaveBeenCalled();
  });

  it("should create marker with hover popup (mouseenter/mouseleave listeners)", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Hover info</p>",
      contentMode: "rawHtml",
      trigger: "hover",
      anchor: "auto",
      offset: null,
      closeButton: false,
      maxWidth: null,
      className: null,
    };
    const marker = createDefaultMarker({ popup });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert — hover popup is attached to marker via setPopup, toggled on mouseenter/mouseleave
    const markerInstance = getMockMarkerConstructor().mock.results[0]?.value;
    expect(markerInstance.setPopup).toHaveBeenCalled();
    // togglePopup is NOT called initially (only on mouseenter)
    expect(markerInstance.togglePopup).not.toHaveBeenCalled();
    const markerElement = markerInstance.getElement();
    // Verify addEventListener was called for mouseenter and mouseleave
    expect(markerElement.addEventListener).toBeDefined();
    // Hover popups should not have close button
    const popupConstructorArgs = getMockPopupConstructor().mock.calls[0]?.[0];
    expect(popupConstructorArgs.closeButton).toBe(false);
  });

  it("should create marker with permanent popup (always visible, no close button)", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Label</p>",
      contentMode: "rawHtml",
      trigger: "permanent",
      anchor: "top",
      offset: null,
      closeButton: false,
      maxWidth: "200px",
      className: "label-popup",
    };
    const marker = createDefaultMarker({
      position: { latitude: 51.505, longitude: -0.09 },
      popup,
    });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert — permanent popup is attached to marker so it follows z-index on hover
    const markerInstance = getMockMarkerConstructor().mock.results[0]?.value;
    expect(markerInstance.setPopup).toHaveBeenCalled();
    expect(markerInstance.togglePopup).toHaveBeenCalled();
    // Permanent popups should have no close button
    const popupConstructorArgs = getMockPopupConstructor().mock.calls[0]?.[0];
    expect(popupConstructorArgs.closeButton).toBe(false);
    expect(popupConstructorArgs.closeOnClick).toBe(false);
  });

  it("should store entries in FeatureStorage", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const markers = [createDefaultMarker({ id: "m1" }), createDefaultMarker({ id: "m2" })];

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], markers, storage);

    // assert
    expect(storage.markers.size).toBe(2);
    expect(storage.markers.has("m1")).toBe(true);
    expect(storage.markers.has("m2")).toBe(true);
    const entry = storage.markers.get("m1")!;
    expect(entry.marker).toBeDefined();
  });

  it("should handle marker with custom icon anchor offset", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({
      icon: {
        url: "https://example.com/pin.png",
        size: { x: 22, y: 40 },
        anchor: { x: 11, y: 40 },
      },
    });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert — should set anchor to "top-left" and offset to negate the anchor point
    const constructorArgs = getMockMarkerConstructor().mock.calls[0]?.[0];
    expect(constructorArgs.anchor).toBe("top-left");
    expect(constructorArgs.offset).toEqual([-11, -40]);
  });

  it("should handle marker with draggable and opacity", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({
      draggable: true,
      opacity: 0.5,
    });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert
    const constructorArgs = getMockMarkerConstructor().mock.calls[0]?.[0];
    expect(constructorArgs.draggable).toBe(true);
    expect(constructorArgs.opacity).toBe(0.5);
  });

  it("should preserve marker opacity as a numeric runtime value", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({
      opacity: 0.25,
    });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert
    const constructorArgs = getMockMarkerConstructor().mock.calls[0]?.[0];
    expect(typeof constructorArgs.opacity).toBe("number");
    expect(constructorArgs.opacity).toBe(0.25);
  });

  it("should handle marker with className", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({
      className: "my-marker",
    });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert
    const constructorArgs = getMockMarkerConstructor().mock.calls[0]?.[0];
    expect(constructorArgs.className).toBe("my-marker");
  });

  it("should create popup with custom anchor when not auto", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Anchored</p>",
      contentMode: "rawHtml",
      trigger: "click",
      anchor: "bottom",
      offset: null,
      closeButton: true,
      maxWidth: null,
      className: null,
    };
    const marker = createDefaultMarker({ popup });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert
    const popupConstructorArgs = getMockPopupConstructor().mock.calls[0]?.[0];
    expect(popupConstructorArgs.anchor).toBe("bottom");
  });

  it("should create popup with offset when provided", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Offset</p>",
      contentMode: "rawHtml",
      trigger: "click",
      anchor: "auto",
      offset: { x: 10, y: 20 },
      closeButton: true,
      maxWidth: null,
      className: null,
    };
    const marker = createDefaultMarker({ popup });

    // act
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    // assert
    const popupConstructorArgs = getMockPopupConstructor().mock.calls[0]?.[0];
    expect(popupConstructorArgs.offset).toEqual([10, 20]);
  });
});

describe("updateMarkers", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should remove old marker and create new one", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({ id: "m1" });
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);

    const oldEntry = storage.markers.get("m1")!;
    const updatedMarker = createDefaultMarker({
      id: "m1",
      position: { latitude: 52.0, longitude: 0.0 },
    });

    // act
    updateMarkers(map as unknown as Parameters<typeof updateMarkers>[0], [updatedMarker], storage);

    // assert — old marker was removed
    expect(oldEntry.marker.remove).toHaveBeenCalled();
    // New marker was created and stored
    const newEntry = storage.markers.get("m1")!;
    expect(newEntry).toBeDefined();
    expect(newEntry.marker).not.toBe(oldEntry.marker);
  });

  it("should skip markers not in storage", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const unknownMarker = createDefaultMarker({ id: "nonexistent" });

    // act & assert — should not throw
    expect(() =>
      updateMarkers(map as unknown as Parameters<typeof updateMarkers>[0], [unknownMarker], storage),
    ).not.toThrow();
    expect(storage.markers.size).toBe(0);
  });
});

describe("removeMarkers", () => {
  beforeEach(() => {
    resetWindowGlobals();
    resetMockMapState();
    bootstrap();
  });

  it("should remove marker and popup from map", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Hello</p>",
      contentMode: "rawHtml",
      trigger: "click",
      anchor: "auto",
      offset: null,
      closeButton: true,
      maxWidth: null,
      className: null,
    };
    const marker = createDefaultMarker({ id: "m1", popup });
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);
    const entry = storage.markers.get("m1")!;

    // act
    removeMarkers(["m1"], storage);

    // assert
    expect(entry.marker.remove).toHaveBeenCalled();
    expect(entry.popup?.remove).toHaveBeenCalled();
  });

  it("should delete from storage", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const marker = createDefaultMarker({ id: "m1" });
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);
    expect(storage.markers.size).toBe(1);

    // act
    removeMarkers(["m1"], storage);

    // assert
    expect(storage.markers.size).toBe(0);
    expect(storage.markers.has("m1")).toBe(false);
  });

  it("should skip unknown marker ids without throwing", () => {
    // arrange
    const storage = createEmptyFeatureStorage();

    // act & assert
    expect(() => removeMarkers(["nonexistent"], storage)).not.toThrow();
  });

  it("should remove hover popup from map", () => {
    // arrange
    const { map } = setupMapAndGetInstance();
    const storage = createEmptyFeatureStorage();
    const popup: IPopupOptions = {
      content: "<p>Hover</p>",
      contentMode: "rawHtml",
      trigger: "hover",
      anchor: "auto",
      offset: null,
      closeButton: false,
      maxWidth: null,
      className: null,
    };
    const marker = createDefaultMarker({ id: "m1", popup });
    addMarkers(map as unknown as Parameters<typeof addMarkers>[0], [marker], storage);
    const entry = storage.markers.get("m1")!;

    // act
    removeMarkers(["m1"], storage);

    // assert
    expect(entry.hoverPopup?.remove).toHaveBeenCalled();
    expect(entry.marker.remove).toHaveBeenCalled();
  });
});
