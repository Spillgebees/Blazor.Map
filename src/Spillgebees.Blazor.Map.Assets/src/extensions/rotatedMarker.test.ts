import { Marker, Point } from "leaflet";
import { beforeEach, describe, expect, it, vi } from "vitest";

import "./rotatedMarker";

// biome-ignore lint/security/noSecrets: CSS transform values, not secrets
const INITIAL_TRANSFORM = "translate3d(100px, 200px, 0)";

describe("rotatedMarker", () => {
  let marker: Marker;
  let iconElement: HTMLElement;
  let shadowElement: HTMLElement;

  beforeEach(() => {
    iconElement = document.createElement("div");
    shadowElement = document.createElement("div");

    marker = Object.create(Marker.prototype) as Marker;
    marker.options = {} as Marker["options"];
    marker._icon = iconElement;
    (marker as unknown as Record<string, unknown>)._shadow = shadowElement;
  });

  it("should add rotateZ transform when rotationAngle is set", () => {
    // arrange
    marker.options.rotationAngle = 45;
    iconElement.style.transform = INITIAL_TRANSFORM;
    shadowElement.style.transform = INITIAL_TRANSFORM;

    // act
    marker._setPos(new Point(100, 200));

    // assert
    // biome-ignore lint/security/noSecrets: false positive
    expect(iconElement.style.transform).toContain("rotateZ(45deg)");
  });

  it("should not add rotation when rotationAngle is not set", () => {
    // arrange
    iconElement.style.transform = INITIAL_TRANSFORM;

    // act
    marker._setPos(new Point(100, 200));

    // assert
    expect(iconElement.style.transform).not.toContain("rotateZ");
  });

  it("should not add rotation when rotationAngle is 0", () => {
    // arrange
    marker.options.rotationAngle = 0;
    iconElement.style.transform = INITIAL_TRANSFORM;

    // act
    marker._setPos(new Point(100, 200));

    // assert
    expect(iconElement.style.transform).not.toContain("rotateZ");
  });

  it("should update rotation via setRotationAngle()", () => {
    // arrange
    marker.options.rotationAngle = 45;
    marker.update = vi.fn().mockReturnThis();

    // act
    marker.setRotationAngle(90);

    // assert
    expect(marker.options.rotationAngle).toBe(90);
    expect(marker.update).toHaveBeenCalledOnce();
  });

  it("should set transform-origin from rotationOrigin option", () => {
    // arrange
    marker.options.rotationAngle = 45;
    marker.options.rotationOrigin = "top left";
    iconElement.style.transform = INITIAL_TRANSFORM;

    // act
    marker._setPos(new Point(100, 200));

    // assert
    expect(iconElement.style.transformOrigin).toBe("top left");
  });

  it("should use 'center center' as default rotationOrigin", () => {
    // arrange
    marker.options.rotationAngle = 45;
    iconElement.style.transform = INITIAL_TRANSFORM;

    // act
    marker._setPos(new Point(100, 200));

    // assert
    expect(iconElement.style.transformOrigin).toBe("center center");
  });

  it("should not rotate the shadow element", () => {
    // arrange
    marker.options.rotationAngle = 45;
    iconElement.style.transform = INITIAL_TRANSFORM;
    shadowElement.style.transform = INITIAL_TRANSFORM;

    // act
    marker._setPos(new Point(100, 200));

    // assert
    expect(shadowElement.style.transform).not.toContain("rotateZ");
  });

  it("should update rotationOrigin via setRotationOrigin()", () => {
    // arrange
    marker.update = vi.fn().mockReturnThis();

    // act
    marker.setRotationOrigin("bottom right");

    // assert
    expect(marker.options.rotationOrigin).toBe("bottom right");
    expect(marker.update).toHaveBeenCalledOnce();
  });
});
