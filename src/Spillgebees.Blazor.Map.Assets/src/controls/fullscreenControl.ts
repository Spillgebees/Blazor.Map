import type { IControl, Map as MapLibreMap } from "maplibre-gl";
import type { FullscreenController } from "../engine/fullscreen";
import { applyControlIcon } from "./icon";

// A first-class fullscreen control we fully own (modelled on CenterControl), so its icons are
// trivially overridable and it shares the one FullscreenController with the imperative API.
// The default glyphs match MapLibre's enter/shrink icons so an un-customised control looks
// unchanged. Icon swaps happen only on the controller's change event — never per frame.

export const DEFAULT_FULLSCREEN_ENTER_ICON = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">
  <path d="M4 9V4h5" />
  <path d="M20 9V4h-5" />
  <path d="M4 15v5h5" />
  <path d="M20 15v5h-5" />
</svg>`;

export const DEFAULT_FULLSCREEN_EXIT_ICON = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">
  <path d="M9 4v5H4" />
  <path d="M15 4v5h5" />
  <path d="M9 20v-5H4" />
  <path d="M15 20v-5h5" />
</svg>`;

export interface FullscreenControlIcons {
  enterIcon?: string | null;
  exitIcon?: string | null;
  enterTitle?: string | null;
  exitTitle?: string | null;
}

export class FullscreenControl implements IControl {
  private readonly _controller: FullscreenController;
  private readonly _enterIcon: string;
  private readonly _exitIcon: string;
  private readonly _enterTitle: string;
  private readonly _exitTitle: string;
  private _container: HTMLDivElement | null = null;
  private _button: HTMLButtonElement | null = null;
  private _unsubscribe: (() => void) | null = null;

  constructor(controller: FullscreenController, icons?: FullscreenControlIcons) {
    this._controller = controller;
    this._enterIcon = icons?.enterIcon || DEFAULT_FULLSCREEN_ENTER_ICON;
    this._exitIcon = icons?.exitIcon || DEFAULT_FULLSCREEN_EXIT_ICON;
    this._enterTitle = icons?.enterTitle || "Enter fullscreen";
    this._exitTitle = icons?.exitTitle || "Exit fullscreen";
  }

  onAdd(_map: MapLibreMap): HTMLElement {
    this._container = document.createElement("div");
    this._container.className = "maplibregl-ctrl sgb-map-ctrl-group sgb-map-fullscreen-control";

    const button = document.createElement("button");
    button.type = "button";
    button.className = "sgb-map-fullscreen-control-button";
    button.addEventListener("click", () => void this._controller.toggle());
    this._button = button;
    this._container.appendChild(button);

    this._render(this._controller.isFullscreen());
    this._unsubscribe = this._controller.onChange((isFullscreen) => this._render(isFullscreen));

    return this._container;
  }

  onRemove(): void {
    this._unsubscribe?.();
    this._unsubscribe = null;
    this._container?.remove();
    this._container = null;
    this._button = null;
  }

  private _render(isFullscreen: boolean): void {
    if (!this._button) {
      return;
    }

    // the accessible name describes the action the button performs next (Enter/Exit), matching
    // MapLibre's own control; a separate aria-pressed alongside a changing label would be redundant
    const title = isFullscreen ? this._exitTitle : this._enterTitle;
    applyControlIcon(this._button, isFullscreen ? this._exitIcon : this._enterIcon);
    this._button.title = title;
    this._button.setAttribute("aria-label", title);
  }
}
