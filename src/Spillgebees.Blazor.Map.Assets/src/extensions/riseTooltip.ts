import type { Tooltip } from "leaflet";
import { Marker } from "leaflet";

/**
 * Extension to add "rise on hover" behavior for tooltips on markers.
 * When `riseOnHover` option is enabled on a marker, its tooltip (if any) will be visually "raised"
 * above other map elements when either the marker or its tooltip is hovered.
 *
 * This is achieved by temporarily setting a high z-index on the tooltip container while hovered.
 * The implementation ensures that the tooltip remains above the marker and other elements during interactions,
 * and properly resets when no longer hovered.
 */

declare module "leaflet" {
  interface Marker {
    _icon?: HTMLElement;
    _initIcon(): void;
    _zIndex: number;
    _bringToFront(): void;
    _resetZIndex(): void;
    _riseTooltipCleanup?: () => void;
  }

  interface Tooltip {
    _container?: HTMLElement;
  }
}

const RISEN_TOOLTIP_Z_INDEX = "10000";

const originalInitIcon = Marker.prototype._initIcon;
Marker.prototype._initIcon = function (this: Marker): void {
  // Clean up previous listeners if icon is being re-initialized
  this._riseTooltipCleanup?.();
  this._riseTooltipCleanup = undefined;

  originalInitIcon.call(this);

  if (!this.options.riseOnHover || !this._icon) {
    return;
  }

  const markerIcon = this._icon;

  const getTooltipContainer = (): HTMLElement | undefined => {
    const tooltip = this.getTooltip() as Tooltip | undefined;
    return tooltip?._container ?? undefined;
  };

  const raiseTooltip = (): void => {
    const container = getTooltipContainer();
    if (container) {
      container.style.zIndex = RISEN_TOOLTIP_Z_INDEX;
    }
  };

  const resetTooltip = (): void => {
    const container = getTooltipContainer();
    if (container) {
      container.style.zIndex = "";
    }
  };

  const onMarkerMouseEnter = (): void => {
    raiseTooltip();
  };

  const onMarkerMouseLeave = (e: MouseEvent): void => {
    const related = e.relatedTarget as Node | null;
    const tooltipContainer = getTooltipContainer();

    // If pointer moved to the tooltip, don't reset, tooltip handlers will take over
    if (tooltipContainer?.contains(related)) {
      return;
    }

    resetTooltip();
  };

  const onTooltipMouseEnter = (): void => {
    this._bringToFront();
    raiseTooltip();
  };

  const onTooltipMouseLeave = (e: MouseEvent): void => {
    const related = e.relatedTarget as Node | null;

    // If pointer moved back to the marker icon, don't reset, marker handlers will take over
    if (markerIcon.contains(related)) {
      return;
    }

    this._resetZIndex();
    resetTooltip();
  };

  markerIcon.addEventListener("mouseenter", onMarkerMouseEnter);
  markerIcon.addEventListener("mouseleave", onMarkerMouseLeave);

  // Tooltip may not be bound yet at _initIcon time, so we also listen for
  // tooltipopen to attach tooltip-side listeners dynamically.
  let currentTooltipContainer: HTMLElement | null = null;

  const attachTooltipListeners = (): void => {
    const container = getTooltipContainer();
    if (container && container !== currentTooltipContainer) {
      // Clean up old tooltip listeners if tooltip was re-bound
      if (currentTooltipContainer) {
        currentTooltipContainer.removeEventListener("mouseenter", onTooltipMouseEnter);
        currentTooltipContainer.removeEventListener("mouseleave", onTooltipMouseLeave);
      }
      container.addEventListener("mouseenter", onTooltipMouseEnter);
      container.addEventListener("mouseleave", onTooltipMouseLeave);
      currentTooltipContainer = container;
    }
  };

  // Try to attach immediately (for permanent tooltips already rendered)
  attachTooltipListeners();

  // Also attach on tooltipopen for non-permanent tooltips
  this.on("tooltipopen", attachTooltipListeners);

  this._riseTooltipCleanup = (): void => {
    markerIcon.removeEventListener("mouseenter", onMarkerMouseEnter);
    markerIcon.removeEventListener("mouseleave", onMarkerMouseLeave);

    if (currentTooltipContainer) {
      currentTooltipContainer.removeEventListener("mouseenter", onTooltipMouseEnter);
      currentTooltipContainer.removeEventListener("mouseleave", onTooltipMouseLeave);
    }

    this.off("tooltipopen", attachTooltipListeners);
  };
};
