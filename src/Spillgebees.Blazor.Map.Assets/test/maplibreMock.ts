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
  getSource: ReturnType<typeof vi.fn>;
  addSource: ReturnType<typeof vi.fn>;
  addLayer: ReturnType<typeof vi.fn>;
  getLayer: ReturnType<typeof vi.fn>;
  getCanvas: ReturnType<typeof vi.fn>;
  removeLayer: ReturnType<typeof vi.fn>;
  removeSource: ReturnType<typeof vi.fn>;
  getCenter: ReturnType<typeof vi.fn>;
  getZoom: ReturnType<typeof vi.fn>;
  getBearing: ReturnType<typeof vi.fn>;
  getPitch: ReturnType<typeof vi.fn>;
}

export interface MockMarkerInstance {
  setLngLat: ReturnType<typeof vi.fn>;
  addTo: ReturnType<typeof vi.fn>;
  remove: ReturnType<typeof vi.fn>;
  setPopup: ReturnType<typeof vi.fn>;
  togglePopup: ReturnType<typeof vi.fn>;
  getElement: ReturnType<typeof vi.fn>;
  setRotation: ReturnType<typeof vi.fn>;
  setOffset: ReturnType<typeof vi.fn>;
  setDraggable: ReturnType<typeof vi.fn>;
  on: ReturnType<typeof vi.fn>;
  getLngLat: ReturnType<typeof vi.fn>;
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
const eventCallbacks = new Map<string, Array<(e?: unknown) => void>>();
const mockSources = new Map<string, { setData: ReturnType<typeof vi.fn>; [key: string]: unknown }>();

export function getLatestMockMapInstance(): MockMapInstance | null {
  return latestMockMapInstance;
}

/**
 * Returns the mock sources map, allowing tests to inspect sources
 * added via `map.addSource()`.
 */
export function getMockMapSources() {
  return mockSources;
}

export function resetMockMapState(): void {
  latestMockMapInstance = null;
  loadCallbacks.length = 0;
  eventCallbacks.clear();
  mockSources.clear();
  MockMarker.mockClear();
  MockPopup.mockClear();
}

/**
 * Fires a registered map event callback (e.g., "click", "moveend", "zoomend").
 * Call this in tests after createMap + fireLoadEvent to simulate MapLibre events.
 */
export function fireMapEvent(event: string, eventData?: unknown): void {
  const callbacks = eventCallbacks.get(event);
  if (callbacks) {
    for (const cb of callbacks) {
      cb(eventData);
    }
  }
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
  this.on = vi.fn().mockImplementation((...args: unknown[]) => {
    const event = args[0] as string;
    // MapLibre supports both map.on(event, callback) and map.on(event, layerId, callback)
    // Only store callbacks (functions), not layer-scoped events (where args[1] is a string)
    const callback = typeof args[1] === "function" ? (args[1] as (e?: unknown) => void) : undefined;
    if (event === "load" && callback) {
      loadCallbacks.push(callback);
    }
    if (callback) {
      if (!eventCallbacks.has(event)) {
        eventCallbacks.set(event, []);
      }
      eventCallbacks.get(event)!.push(callback);
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
  this.getSource = vi.fn().mockImplementation((id: string) => mockSources.get(id));
  this.addSource = vi.fn().mockImplementation((id: string, source: Record<string, unknown>) => {
    mockSources.set(id, { setData: vi.fn(), ...source });
  });
  this.addLayer = vi.fn();
  this.getLayer = vi.fn();
  this.getCanvas = vi.fn().mockReturnValue({ style: {} });
  this.removeLayer = vi.fn();
  this.removeSource = vi.fn();
  this.getCenter = vi.fn().mockReturnValue({ lng: 0, lat: 0 });
  this.getZoom = vi.fn().mockReturnValue(0);
  this.getBearing = vi.fn().mockReturnValue(0);
  this.getPitch = vi.fn().mockReturnValue(0);
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
  this.togglePopup = vi.fn().mockReturnThis();
  this.getElement = vi.fn().mockReturnValue(document.createElement("div"));
  this.setRotation = vi.fn().mockReturnThis();
  this.setOffset = vi.fn().mockReturnThis();
  this.setDraggable = vi.fn().mockReturnThis();
  this.on = vi.fn().mockReturnThis();
  this.getLngLat = vi.fn().mockReturnValue({ lng: 0, lat: 0 });
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
