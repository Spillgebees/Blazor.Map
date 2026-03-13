import { Marker } from "leaflet";
import { beforeEach, describe, expect, it, vi } from "vitest";

import "./riseTooltip";

describe("riseTooltip", () => {
  let marker: Marker;
  let iconElement: HTMLElement;
  let tooltipContainer: HTMLElement;

  beforeEach(() => {
    iconElement = document.createElement("div");
    tooltipContainer = document.createElement("div");

    marker = Object.create(Marker.prototype) as Marker;
    marker._icon = iconElement;
    marker._zIndex = 100;
    marker._bringToFront = vi.fn();
    marker._resetZIndex = vi.fn();
    marker.on = vi.fn().mockReturnThis();
    marker.off = vi.fn().mockReturnThis();

    // Mocks required for Leaflet's original _initIcon to succeed.
    // createIcon returns the same iconElement to avoid the "addIcon" path
    // which would require getPane().appendChild().
    const mockIcon = {
      createIcon: vi.fn(() => iconElement),
      createShadow: vi.fn(() => null),
      options: {},
    };
    marker.options = {
      riseOnHover: true,
      riseOffset: 250,
      icon: mockIcon,
    } as unknown as Marker["options"];
    (marker as unknown as Record<string, unknown>)._zoomAnimated = false;
    (marker as unknown as Record<string, unknown>)._removeShadow = vi.fn();
    (marker as unknown as Record<string, unknown>)._initInteraction = vi.fn();

    const mockTooltip = { _container: tooltipContainer };
    marker.getTooltip = vi.fn(() => mockTooltip as unknown as ReturnType<Marker["getTooltip"]>);
  });

  it("should raise tooltip z-index on marker mouseover", () => {
    // arrange
    marker._initIcon();

    // act
    iconElement.dispatchEvent(new MouseEvent("mouseover", { bubbles: true }));

    // assert
    expect(tooltipContainer.style.zIndex).toBe("10000");
  });

  it("should reset tooltip z-index on marker mouseout when not moving to tooltip", () => {
    // arrange
    marker._initIcon();
    iconElement.dispatchEvent(new MouseEvent("mouseover", { bubbles: true }));
    const unrelatedElement = document.createElement("div");

    // act
    iconElement.dispatchEvent(new MouseEvent("mouseout", { bubbles: true, relatedTarget: unrelatedElement }));

    // assert
    expect(tooltipContainer.style.zIndex).toBe("");
  });

  it("should not reset tooltip z-index on marker mouseout when moving to tooltip", () => {
    // arrange
    marker._initIcon();
    iconElement.dispatchEvent(new MouseEvent("mouseover", { bubbles: true }));

    // act
    iconElement.dispatchEvent(new MouseEvent("mouseout", { bubbles: true, relatedTarget: tooltipContainer }));

    // assert
    expect(tooltipContainer.style.zIndex).toBe("10000");
  });

  it("should bring marker to front on tooltip mouseenter", () => {
    // arrange
    marker._initIcon();

    // act
    tooltipContainer.dispatchEvent(new MouseEvent("mouseenter", { bubbles: false }));

    // assert
    expect(marker._bringToFront).toHaveBeenCalled();
    expect(tooltipContainer.style.zIndex).toBe("10000");
  });

  it("should reset marker and tooltip on tooltip mouseleave when not moving to marker", () => {
    // arrange
    marker._initIcon();
    tooltipContainer.dispatchEvent(new MouseEvent("mouseenter", { bubbles: false }));
    const unrelatedElement = document.createElement("div");

    // act
    tooltipContainer.dispatchEvent(new MouseEvent("mouseleave", { bubbles: false, relatedTarget: unrelatedElement }));

    // assert
    expect(marker._resetZIndex).toHaveBeenCalled();
    expect(tooltipContainer.style.zIndex).toBe("");
  });

  it("should not reset on tooltip mouseleave when moving to marker icon", () => {
    // arrange
    marker._initIcon();
    tooltipContainer.dispatchEvent(new MouseEvent("mouseenter", { bubbles: false }));

    // act
    tooltipContainer.dispatchEvent(new MouseEvent("mouseleave", { bubbles: false, relatedTarget: iconElement }));

    // assert
    expect(marker._resetZIndex).not.toHaveBeenCalled();
  });

  it("should not add listeners when riseOnHover is false", () => {
    // arrange
    marker.options.riseOnHover = false;
    marker._initIcon();

    // act
    iconElement.dispatchEvent(new MouseEvent("mouseover", { bubbles: true }));

    // assert
    expect(tooltipContainer.style.zIndex).toBe("");
  });

  it("should clean up listeners on subsequent _initIcon calls", () => {
    // arrange
    marker._initIcon();
    const removeEventSpy = vi.spyOn(iconElement, "removeEventListener");

    // act — re-initialize
    marker._initIcon();

    // assert
    expect(removeEventSpy).toHaveBeenCalledWith("mouseover", expect.any(Function));
    expect(removeEventSpy).toHaveBeenCalledWith("mouseout", expect.any(Function));
  });

  it("should listen for tooltipopen to attach tooltip listeners", () => {
    // arrange & act
    marker._initIcon();

    // assert
    expect(marker.on).toHaveBeenCalledWith("tooltipopen", expect.any(Function));
  });

  it("should handle tooltip container not yet available", () => {
    // arrange
    marker.getTooltip = vi.fn(() => undefined as unknown as ReturnType<Marker["getTooltip"]>);
    marker._initIcon();

    // act — should not throw
    iconElement.dispatchEvent(new MouseEvent("mouseover", { bubbles: true }));

    // assert — no error thrown, tooltip z-index stays default
    expect(tooltipContainer.style.zIndex).toBe("");
  });
});
