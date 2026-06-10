import type { IControl, Map as MapLibreMap } from "maplibre-gl";

export class CenterControl implements IControl {
  private _container: HTMLDivElement | null = null;
  private readonly _onClick: (() => void) | null;

  constructor(onClick?: () => void) {
    this._onClick = onClick ?? null;
  }

  onAdd(_map: MapLibreMap): HTMLElement {
    this._container = document.createElement("div");
    this._container.className = "maplibregl-ctrl sgb-map-ctrl-group sgb-map-center-control";

    const button = document.createElement("button");
    button.type = "button";
    button.className = "sgb-map-center-control-button";
    button.title = "Re-center map";
    button.setAttribute("aria-label", "Re-center map");

    button.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <circle cx="12" cy="12" r="10"/>
      <line x1="22" y1="12" x2="18" y2="12"/>
      <line x1="6" y1="12" x2="2" y2="12"/>
      <line x1="12" y1="6" x2="12" y2="2"/>
      <line x1="12" y1="22" x2="12" y2="18"/>
    </svg>`;

    button.addEventListener("click", () => this._handleClick());
    this._container.appendChild(button);

    return this._container;
  }

  onRemove(): void {
    this._container?.remove();
    this._container = null;
  }

  private _handleClick(): void {
    // the host owns the home view (center/zoom/fit) — surface the intent and let it decide
    this._onClick?.();
  }
}
