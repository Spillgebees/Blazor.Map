import { vi } from "vitest";

export class MockLatLng {
  lat: number;
  lng: number;
  constructor(lat: number, lng: number) {
    this.lat = lat;
    this.lng = lng;
  }
}

export class MockLatLngBounds {
  _southWest: MockLatLng;
  _northEast: MockLatLng;

  constructor(latlngs?: MockLatLng[] | [MockLatLng, MockLatLng]) {
    if (latlngs && latlngs.length > 0) {
      this._southWest = latlngs[0];
      this._northEast = latlngs[latlngs.length - 1];
    } else {
      this._southWest = new MockLatLng(0, 0);
      this._northEast = new MockLatLng(0, 0);
    }
  }

  extend = vi.fn().mockReturnThis();
  getSouthWest = vi.fn(() => this._southWest);
  getNorthEast = vi.fn(() => this._northEast);
}

export class MockTooltip {
  _options: Record<string, unknown>;
  constructor(options?: Record<string, unknown>) {
    this._options = options ?? {};
  }
}

export class MockTileLayer {
  _url: string;
  _options: Record<string, unknown>;
  constructor(url: string, options?: Record<string, unknown>) {
    this._url = url;
    this._options = options ?? {};
  }
  addTo = vi.fn().mockReturnThis();
  remove = vi.fn().mockReturnThis();
}

export class MockMarker {
  _latlng: MockLatLng;
  _options: Record<string, unknown>;
  constructor(latlng: MockLatLng, options?: Record<string, unknown>) {
    this._latlng = latlng;
    this._options = options ?? {};
  }
  getLatLng = vi.fn(() => this._latlng);
  getBounds = vi.fn(() => new MockLatLngBounds([this._latlng]));
  bindTooltip = vi.fn().mockReturnThis();
  unbindTooltip = vi.fn().mockReturnThis();
  addTo = vi.fn().mockReturnThis();
  remove = vi.fn().mockReturnThis();
}

export class MockCircleMarker {
  _latlng: MockLatLng;
  _options: Record<string, unknown>;
  constructor(latlng: MockLatLng, options?: Record<string, unknown>) {
    this._latlng = latlng;
    this._options = options ?? {};
  }
  getLatLng = vi.fn(() => this._latlng);
  getBounds = vi.fn(() => new MockLatLngBounds([this._latlng]));
  bindTooltip = vi.fn().mockReturnThis();
  unbindTooltip = vi.fn().mockReturnThis();
  addTo = vi.fn().mockReturnThis();
  remove = vi.fn().mockReturnThis();
}

export class MockPolyline {
  _latlngs: MockLatLng[];
  _options: Record<string, unknown>;
  constructor(latlngs: MockLatLng[], options?: Record<string, unknown>) {
    this._latlngs = latlngs;
    this._options = options ?? {};
  }
  getLatLng = vi.fn();
  getBounds = vi.fn(() => new MockLatLngBounds(this._latlngs));
  bindTooltip = vi.fn().mockReturnThis();
  unbindTooltip = vi.fn().mockReturnThis();
  addTo = vi.fn().mockReturnThis();
  remove = vi.fn().mockReturnThis();
}

export class MockControl {
  _options: Record<string, unknown>;
  constructor(options?: Record<string, unknown>) {
    this._options = options ?? {};
  }
  addTo = vi.fn().mockReturnThis();
  remove = vi.fn().mockReturnThis();
}

export class MockZoomControl extends MockControl {
  constructor(options?: Record<string, unknown>) {
    super(options);
  }
}

export class MockScaleControl extends MockControl {
  constructor(options?: Record<string, unknown>) {
    super(options);
  }
}

export class MockAttributionControl {
  setPrefix = vi.fn();
}

export class MockMap {
  _container: HTMLElement | undefined;
  _options: Record<string, unknown>;
  attributionControl = new MockAttributionControl();

  constructor(
    container?: HTMLElement | string,
    options?: Record<string, unknown>,
  ) {
    if (container instanceof HTMLElement) {
      this._container = container;
    }
    this._options = options ?? {};
  }
  addLayer = vi.fn().mockReturnThis();
  removeLayer = vi.fn().mockReturnThis();
  addControl = vi.fn().mockReturnThis();
  removeControl = vi.fn().mockReturnThis();
  fitBounds = vi.fn().mockReturnThis();
  setView = vi.fn().mockReturnThis();
  invalidateSize = vi.fn().mockReturnThis();
  remove = vi.fn().mockReturnThis();
}

export class MockDomUtil {
  static create = vi.fn(
    (tagName: string, className?: string, container?: HTMLElement) => {
      const el = document.createElement(tagName);
      if (className) {
        el.className = className;
      }
      if (container) {
        container.appendChild(el);
      }
      return el;
    },
  );
}

export class MockDomEvent {
  static on = vi.fn(
    (_el: HTMLElement, _type: string, _fn: unknown, _context?: unknown) =>
      MockDomEvent,
  );
  static off = vi.fn(
    (_el: HTMLElement, _type: string, _fn: unknown, _context?: unknown) =>
      MockDomEvent,
  );
  static stop = vi.fn();
}

export function createLeafletMock() {
  return {
    Map: MockMap,
    LatLng: MockLatLng,
    LatLngBounds: MockLatLngBounds,
    Marker: MockMarker,
    CircleMarker: MockCircleMarker,
    Polyline: MockPolyline,
    TileLayer: MockTileLayer,
    Tooltip: MockTooltip,
    Control: Object.assign(MockControl, {
      Zoom: MockZoomControl,
      Scale: MockScaleControl,
    }),
    DomUtil: MockDomUtil,
    DomEvent: MockDomEvent,
  };
}
