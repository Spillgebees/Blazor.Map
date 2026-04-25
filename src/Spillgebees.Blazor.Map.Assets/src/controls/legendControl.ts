import type { IControl, Map as MapLibreMap } from "maplibre-gl";
import type { ILegendMapControl } from "../interfaces/controls";

const LEGEND_ICON_SVG = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none">
  <rect x="3.5" y="5.5" width="3" height="3" rx="0.5" fill="currentColor"/>
  <circle cx="5" cy="12" r="1.5" fill="currentColor"/>
  <polygon points="5,15.2 6.7,18.3 3.3,18.3" fill="currentColor"/>
  <line x1="9" y1="7" x2="19" y2="7" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
  <line x1="9" y1="12" x2="16" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
  <line x1="9" y1="17" x2="20" y2="17" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
</svg>`;

const CLOSE_ICON_SVG = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 14 14" width="14" height="14" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
  <line x1="3" y1="3" x2="11" y2="11"/>
  <line x1="11" y1="3" x2="3" y2="11"/>
</svg>`;

export class LegendControl implements IControl {
  private static readonly EDGE_MARGIN = 10;
  private static readonly NEIGHBOR_CLEARANCE = 8;
  private static readonly MIN_HEIGHT = 160;
  private static readonly PANEL_GAP = 4;

  private _options: ILegendMapControl;
  private readonly _placeholderHost: HTMLElement;
  private readonly _contentRoot: HTMLElement;
  private _container: HTMLDivElement | null = null;
  private _body: HTMLDivElement | null = null;
  private _panel: HTMLDivElement | null = null;
  private _toggleButton: HTMLButtonElement | null = null;
  private _header: HTMLDivElement | null = null;
  private _title: HTMLDivElement | null = null;
  private _isOpen: boolean;
  private _mapContainer: HTMLElement | null = null;
  private _resizeObserver: ResizeObserver | null = null;
  private _mutationObserver: MutationObserver | null = null;

  constructor(options: ILegendMapControl, placeholderHost: HTMLElement, contentRoot: HTMLElement) {
    this._options = options;
    this._placeholderHost = placeholderHost;
    this._contentRoot = contentRoot;
    this._isOpen = options.collapsible ? options.initiallyOpen : true;
  }

  onAdd(map: MapLibreMap): HTMLElement {
    this._container = document.createElement("div");
    this._mapContainer = typeof map.getContainer === "function" ? map.getContainer() : null;
    this._rebuildShell();
    this._syncMaxHeight();
    this._observeLayout();
    queueMicrotask(() => this._syncMaxHeight());

    return this._container;
  }

  onRemove(): void {
    this._resizeObserver?.disconnect();
    this._resizeObserver = null;
    this._mutationObserver?.disconnect();
    this._mutationObserver = null;
    this._mapContainer = null;
    this._placeholderHost.appendChild(this._contentRoot);
    this._contentRoot.hidden = true;
    this._container?.remove();
    this._container = null;
    this._header = null;
    this._title = null;
    this._body = null;
    this._panel = null;
    this._toggleButton = null;
  }

  update(options: ILegendMapControl): void {
    const positionChanged = this._options.position !== options.position;
    const hadStructuralShell = Boolean(this._options.title) || this._options.collapsible;
    const hasStructuralShell = Boolean(options.title) || options.collapsible;
    const structureChanged =
      hadStructuralShell !== hasStructuralShell ||
      this._options.collapsible !== options.collapsible ||
      Boolean(this._options.title) !== Boolean(options.title);
    this._options = options;

    if (structureChanged) {
      this._isOpen = options.collapsible ? options.initiallyOpen : true;
    }

    if (!this._container) {
      return;
    }

    if (positionChanged) {
      this.onRemove();
      this.onAdd({} as MapLibreMap);
      return;
    }

    if (structureChanged) {
      this._rebuildShell();
    }

    this._container.className = this._buildContainerClassName();
    if (this._title) {
      this._title.textContent = this._options.title ?? "";
    }

    this._observeLayout();
    this._syncMaxHeight();
    this._setOpen(this._isOpen);
  }

  private _rebuildShell(): void {
    if (!this._container) {
      return;
    }

    this._container.replaceChildren();
    this._container.className = this._buildContainerClassName();
    this._header = null;
    this._title = null;
    this._toggleButton = null;
    this._panel = null;

    if (this._options.collapsible) {
      this._buildCollapsibleShell();
    } else {
      this._buildStaticShell();
    }

    this._contentRoot.hidden = false;
    this._setOpen(this._isOpen);
  }

  private _buildCollapsibleShell(): void {
    if (!this._container) {
      return;
    }

    // toggle wrapper — a standard ctrl-group with a single button inside,
    // identical structure to the center control so overflow: hidden clips corners
    const toggleWrapper = document.createElement("div");
    toggleWrapper.className = "sgb-map-ctrl-group";

    this._toggleButton = document.createElement("button");
    this._toggleButton.type = "button";
    this._toggleButton.className = "sgb-map-legend-toggle";
    this._toggleButton.innerHTML = LEGEND_ICON_SVG;
    this._toggleButton.addEventListener("click", () => this._setOpen(!this._isOpen));
    toggleWrapper.appendChild(this._toggleButton);
    this._container.appendChild(toggleWrapper);

    // panel (second child, hidden when closed)
    this._panel = document.createElement("div");
    this._panel.className = "sgb-map-legend-panel";

    // panel header (only if title is set)
    if (this._options.title) {
      this._header = document.createElement("div");
      this._header.className = "sgb-map-legend-header";

      this._title = document.createElement("div");
      this._title.className = "sgb-map-legend-title";
      this._title.textContent = this._options.title;
      this._header.appendChild(this._title);

      this._panel.appendChild(this._header);
    }

    // panel body
    this._body = document.createElement("div");
    this._body.className = "sgb-map-legend-body";
    this._body.appendChild(this._contentRoot);
    this._panel.appendChild(this._body);

    this._container.appendChild(this._panel);
  }

  private _buildStaticShell(): void {
    if (!this._container) {
      return;
    }

    // optional header (only if title is provided)
    if (this._options.title) {
      this._header = document.createElement("div");
      this._header.className = "sgb-map-legend-header";

      this._title = document.createElement("div");
      this._title.className = "sgb-map-legend-title";
      this._title.textContent = this._options.title;
      this._header.appendChild(this._title);

      this._container.appendChild(this._header);
    }

    // body directly in container
    this._body = document.createElement("div");
    this._body.className = "sgb-map-legend-body";
    this._body.appendChild(this._contentRoot);
    this._container.appendChild(this._body);
  }

  private _observeLayout(): void {
    this._resizeObserver?.disconnect();
    this._resizeObserver = null;
    this._mutationObserver?.disconnect();
    this._mutationObserver = null;

    if (!this._mapContainer) {
      return;
    }

    if (typeof ResizeObserver !== "undefined") {
      this._resizeObserver = new ResizeObserver(() => this._syncMaxHeight());
      this._resizeObserver.observe(this._mapContainer);

      for (const host of this._getRelevantControlHosts()) {
        this._resizeObserver.observe(host);
      }
    }

    if (typeof MutationObserver !== "undefined") {
      this._mutationObserver = new MutationObserver(() => {
        this._observeLayout();
        this._syncMaxHeight();
      });
      this._mutationObserver.observe(this._mapContainer, {
        attributes: true,
        childList: true,
        subtree: true,
      });
    }
  }

  private _syncMaxHeight(): void {
    if (!this._container || !this._mapContainer) {
      return;
    }

    const mapRect = this._mapContainer.getBoundingClientRect();
    const mapHeight = this._mapContainer.clientHeight || mapRect.height;

    if (mapHeight <= 0 || mapRect.height <= 0) {
      this._container.style.removeProperty("--sgb-map-legend-max-height");
      return;
    }

    const legendRect = this._container.getBoundingClientRect();
    const topEdge = mapRect.top + LegendControl.EDGE_MARGIN;
    const bottomEdge = mapRect.bottom - LegendControl.EDGE_MARGIN;
    const sideRects = this._getNeighborRects();
    const isTopAnchored = this._options.position.startsWith("top-");

    let maxHeight = mapHeight - LegendControl.EDGE_MARGIN * 2;

    // when collapsible, the panel hangs off the button so we need to reserve
    // space for the button height + gap between button and panel
    const toggleHeight =
      this._options.collapsible && this._toggleButton ? this._toggleButton.getBoundingClientRect().height || 0 : 0;
    const panelOffset = this._options.collapsible ? toggleHeight + LegendControl.PANEL_GAP : 0;

    if (isTopAnchored) {
      const anchorTop = this._isUsableRect(legendRect)
        ? Math.max(legendRect.top, topEdge)
        : Math.max(
            topEdge,
            ...sideRects
              .filter((rect) => rect.bottom <= bottomEdge)
              .map((rect) => rect.bottom + LegendControl.NEIGHBOR_CLEARANCE),
          );
      const blockersBelow = sideRects.filter((rect) => rect.top >= anchorTop);
      const lowerBound = Math.min(
        bottomEdge,
        ...blockersBelow.map((rect) => rect.top - LegendControl.NEIGHBOR_CLEARANCE),
      );
      maxHeight = lowerBound - anchorTop - panelOffset;
    } else {
      const anchorBottom = this._isUsableRect(legendRect)
        ? Math.min(legendRect.bottom, bottomEdge)
        : Math.min(
            bottomEdge,
            ...sideRects
              .filter((rect) => rect.top >= topEdge)
              .map((rect) => rect.top - LegendControl.NEIGHBOR_CLEARANCE),
          );
      const blockersAbove = sideRects.filter((rect) => rect.bottom <= anchorBottom);
      const upperBound = Math.max(
        topEdge,
        ...blockersAbove.map((rect) => rect.bottom + LegendControl.NEIGHBOR_CLEARANCE),
      );
      maxHeight = anchorBottom - upperBound - panelOffset;
    }

    maxHeight = Math.max(Math.floor(maxHeight), LegendControl.MIN_HEIGHT);
    this._container.style.setProperty("--sgb-map-legend-max-height", `${maxHeight}px`);
  }

  private _getRelevantControlHosts(): HTMLElement[] {
    if (!this._mapContainer) {
      return [];
    }

    const side = this._options.position.endsWith("right") ? "right" : "left";
    const topHost = this._mapContainer.querySelector(`.maplibregl-ctrl-top-${side}`);
    const bottomHost = this._mapContainer.querySelector(`.maplibregl-ctrl-bottom-${side}`);

    return [topHost, bottomHost].filter((host): host is HTMLElement => host instanceof HTMLElement);
  }

  private _getNeighborRects(): DOMRect[] {
    return this._getRelevantControlHosts()
      .flatMap((host) => Array.from(host.children))
      .filter((child): child is HTMLElement => child instanceof HTMLElement && child !== this._container)
      .map((child) => child.getBoundingClientRect())
      .filter((rect) => this._isUsableRect(rect));
  }

  private _isUsableRect(rect: Pick<DOMRect, "width" | "height" | "top" | "bottom">): boolean {
    return Number.isFinite(rect.top) && Number.isFinite(rect.bottom) && rect.width > 0 && rect.height > 0;
  }

  private _buildContainerClassName(): string {
    return [
      "maplibregl-ctrl",
      "sgb-map-legend",
      this._options.collapsible ? "sgb-map-legend-collapsible" : null,
      this._isOpen ? "sgb-map-legend-open" : "sgb-map-legend-closed",
      this._options.className,
    ]
      .filter((value) => value && value.trim().length > 0)
      .join(" ");
  }

  private _setOpen(isOpen: boolean): void {
    this._isOpen = this._options.collapsible ? isOpen : true;

    this._container?.classList.toggle("sgb-map-legend-open", this._isOpen);
    this._container?.classList.toggle("sgb-map-legend-closed", !this._isOpen);

    if (this._panel) {
      this._panel.hidden = !this._isOpen;
    }

    if (this._toggleButton) {
      this._toggleButton.innerHTML = this._isOpen ? CLOSE_ICON_SVG : LEGEND_ICON_SVG;
      this._toggleButton.setAttribute("aria-expanded", this._isOpen ? "true" : "false");
      this._toggleButton.setAttribute("aria-label", this._isOpen ? "Hide legend" : "Show legend");
    }
  }
}
