import type { Point } from "leaflet";
import { Marker } from "leaflet";

/**
 * Extension to add rotation capabilities to Leaflet markers.
 * When `rotationAngle` option is set on a marker, its icon (and shadow, if present) will be rotated by the specified angle in degrees.
 * The rotation is applied via CSS transforms and can be updated dynamically using the provided methods.
 */

declare module "leaflet" {
  interface MarkerOptions {
    rotationAngle?: number;
    rotationOrigin?: string;
  }

  interface Marker {
    _icon?: HTMLElement;
    _setPos(pos: Point): void;
    update(): this;
    setRotationAngle(angle: number): this;
    setRotationOrigin(origin: string): this;
  }
}

const originalSetPos = Marker.prototype._setPos;

Marker.prototype._setPos = function (this: Marker, pos: Point): void {
  originalSetPos.call(this, pos);

  const angle = this.options.rotationAngle;
  if (angle != null && angle !== 0 && this._icon) {
    this._icon.style.transform += ` rotateZ(${angle}deg)`;
    this._icon.style.transformOrigin = this.options.rotationOrigin ?? "center center";
  }
};

Marker.prototype.setRotationAngle = function (this: Marker, angle: number): Marker {
  this.options.rotationAngle = angle;
  this.update();
  return this;
};

Marker.prototype.setRotationOrigin = function (this: Marker, origin: string): Marker {
  this.options.rotationOrigin = origin;
  this.update();
  return this;
};

const markerPrototype = Marker.prototype as unknown as Record<string, unknown>;
const originalOnDrag = markerPrototype._onDrag as ((e: unknown) => void) | undefined;

if (typeof originalOnDrag === "function") {
  markerPrototype._onDrag = function (this: Marker, e: unknown): void {
    originalOnDrag.call(this, e);

    const angle = this.options.rotationAngle;
    if (angle != null && angle !== 0 && this._icon) {
      this._icon.style.transform += ` rotateZ(${angle}deg)`;
      this._icon.style.transformOrigin = this.options.rotationOrigin ?? "center center";
    }
  };
}
