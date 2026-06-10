import type { DotNet } from "@microsoft/dotnet-js-interop";
import type { IControl, Map as MapLibreMap } from "maplibre-gl";
import type { IPanelMapControl } from "../interfaces/controls";

const PANEL_ICON_SVG = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none">
  <path d="M4 7h16M7 12h10M10 17h4" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
</svg>`;

const CLOSE_ICON_SVG = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 14 14" width="14" height="14" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
  <line x1="3" y1="3" x2="11" y2="11"/>
  <line x1="11" y1="3" x2="3" y2="11"/>
</svg>`;

export class PanelControl implements IControl {
  private static readonly EDGE_MARGIN = 10;
  private static readonly PANEL_GAP = 4;
  private static readonly MIN_HEIGHT = 160;

  private _options: IPanelMapControl;
  private readonly _placeholderHost: HTMLElement;
  private readonly _contentRoot: HTMLElement;
  private _container: HTMLDivElement | null = null;
  private _panel: HTMLDivElement | null = null;
  private _body: HTMLDivElement | null = null;
  private _toggleButton: HTMLButtonElement | null = null;
  private _title: HTMLDivElement | null = null;
  private _mapContainer: HTMLElement | null = null;
  private _resizeObserver: ResizeObserver | null = null;
  private _isOpen: boolean;
  private _stateReference: DotNet.DotNetObject | null;

  constructor(
    options: IPanelMapControl,
    placeholderHost: HTMLElement,
    contentRoot: HTMLElement,
    stateReference?: DotNet.DotNetObject | null,
  ) {
    this._options = options;
    this._placeholderHost = placeholderHost;
    this._contentRoot = contentRoot;
    this._isOpen = options.isOpen ?? options.initiallyOpen;
    this._stateReference = stateReference ?? null;
  }

  onAdd(map: MapLibreMap): HTMLElement {
    this._container = document.createElement("div");
    this._mapContainer = typeof map.getContainer === "function" ? map.getContainer() : null;
    this._rebuildShell();
    this._observeLayout();
    queueMicrotask(() => this._syncMaxHeight());
    return this._container;
  }

  onRemove(): void {
    this._resizeObserver?.disconnect();
    this._resizeObserver = null;
    this._mapContainer = null;
    this._placeholderHost.appendChild(this._contentRoot);
    this._contentRoot.hidden = true;
    this._container?.remove();
    this._container = null;
    this._panel = null;
    this._body = null;
    this._toggleButton = null;
    this._title = null;
  }

  update(options: IPanelMapControl, stateReference?: DotNet.DotNetObject | null): void {
    const previousControlId = this._options.controlId;
    const hadTitle = Boolean(this._options.title);
    const hasTitle = Boolean(options.title);
    this._options = options;
    this._stateReference = stateReference ?? this._stateReference;

    if (!this._container) {
      if (this._options.isOpen != null) {
        this._isOpen = this._options.isOpen;
      }
      return;
    }

    this._container.className = this._buildContainerClassName();
    if (this._title) {
      this._title.textContent = this._options.title ?? "";
    }

    if (previousControlId !== options.controlId) {
      this._toggleButton?.setAttribute("aria-controls", this._panelId);
      this._panel?.setAttribute("id", this._panelId);
    }

    if (hadTitle !== hasTitle) {
      this._rebuildShell();
    }

    this._syncMaxWidth();
    this._syncMaxHeight();
    if (this._options.isOpen != null) {
      this._isOpen = this._options.isOpen;
    }
    this._setOpen(this._isOpen);
  }

  private get _panelId(): string {
    return `sgb-map-panel-${this._options.controlId}`;
  }

  private _rebuildShell(): void {
    if (!this._container) {
      return;
    }

    this._container.replaceChildren();
    this._container.className = this._buildContainerClassName();

    const toggleWrapper = document.createElement("div");
    toggleWrapper.className = "sgb-map-ctrl-group";

    this._toggleButton = document.createElement("button");
    this._toggleButton.type = "button";
    this._toggleButton.className = "sgb-map-panel-toggle";
    this._toggleButton.setAttribute("aria-controls", this._panelId);
    this._toggleButton.addEventListener("click", () => this._requestOpenChange(!this._isOpen));
    this._toggleButton.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && this._isOpen) {
        this._requestOpenChange(false);
        event.stopPropagation();
      }
    });
    toggleWrapper.appendChild(this._toggleButton);
    this._container.appendChild(toggleWrapper);

    this._panel = document.createElement("div");
    this._panel.id = this._panelId;
    this._panel.className = "sgb-map-panel";
    this._panel.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && !event.defaultPrevented && event.target === event.currentTarget) {
        this._requestOpenChange(false);
        this._toggleButton?.focus();
        event.stopPropagation();
      }
    });

    if (this._options.title) {
      const header = document.createElement("div");
      header.className = "sgb-map-panel-header";
      this._title = document.createElement("div");
      this._title.className = "sgb-map-panel-title";
      this._title.textContent = this._options.title;
      header.appendChild(this._title);
      this._panel.appendChild(header);
    }

    this._body = document.createElement("div");
    this._body.className = "sgb-map-panel-body";
    this._contentRoot.hidden = false;
    this._body.appendChild(this._contentRoot);
    this._panel.appendChild(this._body);
    this._container.appendChild(this._panel);

    this._syncMaxWidth();
    this._setOpen(this._isOpen);
  }

  private _observeLayout(): void {
    this._resizeObserver?.disconnect();
    this._resizeObserver = null;
    if (!this._mapContainer || typeof ResizeObserver === "undefined") {
      return;
    }

    this._resizeObserver = new ResizeObserver(() => this._syncMaxHeight());
    this._resizeObserver.observe(this._mapContainer);
  }

  private _syncMaxWidth(): void {
    if (!this._panel) {
      return;
    }

    if (this._options.maxWidth && this._options.maxWidth.trim().length > 0) {
      this._panel.style.setProperty("--sgb-map-panel-max-width", this._options.maxWidth);
    } else {
      this._panel.style.removeProperty("--sgb-map-panel-max-width");
    }
  }

  private _syncMaxHeight(): void {
    if (!this._container || !this._mapContainer) {
      return;
    }

    const mapRect = this._mapContainer.getBoundingClientRect();
    const mapHeight = this._mapContainer.clientHeight || mapRect.height;
    if (mapHeight <= 0 || mapRect.height <= 0) {
      this._container.style.removeProperty("--sgb-map-panel-max-height");
      return;
    }

    const toggleHeight = this._toggleButton?.getBoundingClientRect().height || 0;
    const maxHeight = Math.max(
      Math.floor(mapHeight - PanelControl.EDGE_MARGIN * 2 - toggleHeight - PanelControl.PANEL_GAP),
      PanelControl.MIN_HEIGHT,
    );
    this._container.style.setProperty("--sgb-map-panel-max-height", `${maxHeight}px`);
  }

  private _requestOpenChange(isOpen: boolean): void {
    if (this._options.isOpen == null) {
      this._setOpen(isOpen);
    }

    // biome-ignore lint/security/noSecrets: JSInvokable method identifier, not a secret
    const openChangedTask = this._stateReference?.invokeMethodAsync("OnPanelOpenChangedAsync", isOpen);
    void openChangedTask?.catch((error: unknown) => {
      // biome-ignore lint/suspicious/noConsole: explicit diagnostics for async .NET callback failures
      console.error(
        `[Spillgebees.Map] panel control '${this._options.controlId}' failed to report open state '${isOpen}'.`,
        error,
      );
    });
  }

  private _buildContainerClassName(): string {
    return [
      "maplibregl-ctrl",
      "sgb-map-panel-control",
      this._isOpen ? "sgb-map-panel-open" : "sgb-map-panel-closed",
      this._options.className,
    ]
      .filter((value) => value && value.trim().length > 0)
      .join(" ");
  }

  private _setOpen(isOpen: boolean): void {
    this._isOpen = isOpen;
    this._container?.classList.toggle("sgb-map-panel-open", this._isOpen);
    this._container?.classList.toggle("sgb-map-panel-closed", !this._isOpen);

    if (this._panel) {
      this._panel.hidden = !this._isOpen;
    }

    if (this._toggleButton) {
      const buttonLabel = this._isOpen ? `Close ${this._options.label}` : this._options.label;
      this._toggleButton.innerHTML = this._isOpen ? CLOSE_ICON_SVG : PANEL_ICON_SVG;
      this._toggleButton.setAttribute("aria-expanded", this._isOpen ? "true" : "false");
      this._toggleButton.setAttribute("aria-label", buttonLabel);
      this._toggleButton.title = buttonLabel;
    }
  }
}
