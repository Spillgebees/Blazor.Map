import { vi } from "vitest";

export interface MockMapInstance {
  on: ReturnType<typeof vi.fn>;
  remove: ReturnType<typeof vi.fn>;
  resize: ReturnType<typeof vi.fn>;
  setPitch: ReturnType<typeof vi.fn>;
  setBearing: ReturnType<typeof vi.fn>;
  setStyle: ReturnType<typeof vi.fn>;
  jumpTo: ReturnType<typeof vi.fn>;
  setProjection: ReturnType<typeof vi.fn>;
  addControl: ReturnType<typeof vi.fn>;
  removeControl: ReturnType<typeof vi.fn>;
  getContainer: ReturnType<typeof vi.fn>;
  fitBounds: ReturnType<typeof vi.fn>;
  flyTo: ReturnType<typeof vi.fn>;
}

export interface MockMarkerInstance {
  setLngLat: ReturnType<typeof vi.fn>;
  addTo: ReturnType<typeof vi.fn>;
  remove: ReturnType<typeof vi.fn>;
  setPopup: ReturnType<typeof vi.fn>;
  getElement: ReturnType<typeof vi.fn>;
  setRotation: ReturnType<typeof vi.fn>;
  setOffset: ReturnType<typeof vi.fn>;
  setDraggable: ReturnType<typeof vi.fn>;
  on: ReturnType<typeof vi.fn>;
}

export interface MockPopupInstance {
  setLngLat: ReturnType<typeof vi.fn>;
  setHTML: ReturnType<typeof vi.fn>;
  setText: ReturnType<typeof vi.fn>;
  addTo: ReturnType<typeof vi.fn>;
  remove: ReturnType<typeof vi.fn>;
  setDOMContent: ReturnType<typeof vi.fn>;
  getElement: ReturnType<typeof vi.fn>;
  isOpen: ReturnType<typeof vi.fn>;
  on: ReturnType<typeof vi.fn>;
}

let latestMockMapInstance: MockMapInstance | null = null;
const loadCallbacks: Array<() => void> = [];

export function getLatestMockMapInstance(): MockMapInstance | null {
  return latestMockMapInstance;
}

export function resetMockMapState(): void {
  latestMockMapInstance = null;
  loadCallbacks.length = 0;
  MockMarker.mockClear();
  MockPopup.mockClear();
}

/**
 * Triggers all registered "load" event callbacks.
 * Call this in tests after createMap to simulate MapLibre's load event.
 */
export function fireLoadEvent(): void {
  for (const cb of loadCallbacks) {
    cb();
  }
  loadCallbacks.length = 0;
}

// Must use function declaration (not arrow) so it can be called with `new`
const MockMapConstructor = vi.fn().mockImplementation(function (this: MockMapInstance) {
  this.on = vi.fn().mockImplementation((event: string, callback: () => void) => {
    if (event === "load") {
      loadCallbacks.push(callback);
    }
    return this;
  });
  this.remove = vi.fn();
  this.resize = vi.fn();
  this.setPitch = vi.fn();
  this.setBearing = vi.fn();
  this.setStyle = vi.fn();
  this.jumpTo = vi.fn();
  this.setProjection = vi.fn();
  this.addControl = vi.fn();
  this.removeControl = vi.fn();
  this.getContainer = vi.fn().mockReturnValue(document.createElement("div"));
  this.fitBounds = vi.fn();
  this.flyTo = vi.fn();
  latestMockMapInstance = this;
});

/**
 * The mock Map constructor spy. Use this to assert on constructor call arguments
 * (e.g., `expect(getMockMapConstructor()).toHaveBeenCalledWith(...)`).
 */
export function getMockMapConstructor() {
  return MockMapConstructor;
}

const MockMarker = vi.fn().mockImplementation(function (this: MockMarkerInstance) {
  this.setLngLat = vi.fn().mockReturnThis();
  this.addTo = vi.fn().mockReturnThis();
  this.remove = vi.fn();
  this.setPopup = vi.fn().mockReturnThis();
  this.getElement = vi.fn().mockReturnValue(document.createElement("div"));
  this.setRotation = vi.fn().mockReturnThis();
  this.setOffset = vi.fn().mockReturnThis();
  this.setDraggable = vi.fn().mockReturnThis();
  this.on = vi.fn().mockReturnThis();
});

/**
 * The mock Marker constructor spy. Use this to assert on constructor call arguments.
 */
export function getMockMarkerConstructor() {
  return MockMarker;
}

const MockPopup = vi.fn().mockImplementation(function (this: MockPopupInstance) {
  this.setLngLat = vi.fn().mockReturnThis();
  this.setHTML = vi.fn().mockReturnThis();
  this.setText = vi.fn().mockReturnThis();
  this.addTo = vi.fn().mockReturnThis();
  this.remove = vi.fn();
  this.setDOMContent = vi.fn().mockReturnThis();
  this.getElement = vi.fn().mockReturnValue(document.createElement("div"));
  this.isOpen = vi.fn().mockReturnValue(false);
  this.on = vi.fn().mockReturnThis();
});

/**
 * The mock Popup constructor spy. Use this to assert on constructor call arguments.
 */
export function getMockPopupConstructor() {
  return MockPopup;
}

// Mock the maplibre-gl module
vi.mock("maplibre-gl", () => {
  return {
    Map: MockMapConstructor,
    Marker: MockMarker,
    Popup: MockPopup,
    NavigationControl: vi.fn(),
    ScaleControl: vi.fn(),
    FullscreenControl: vi.fn(),
    GeolocateControl: vi.fn(),
    TerrainControl: vi.fn(),
  };
});
