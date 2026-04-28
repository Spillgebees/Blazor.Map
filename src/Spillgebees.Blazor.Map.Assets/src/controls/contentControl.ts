import type { IControl, Map as MapLibreMap } from "maplibre-gl";
import type { IContentMapControl } from "../interfaces/controls";

export class ContentControl implements IControl {
  private _options: IContentMapControl;
  private readonly _placeholderHost: HTMLElement;
  private readonly _contentRoot: HTMLElement;
  private _container: HTMLDivElement | null = null;

  constructor(options: IContentMapControl, placeholderHost: HTMLElement, contentRoot: HTMLElement) {
    this._options = options;
    this._placeholderHost = placeholderHost;
    this._contentRoot = contentRoot;
  }

  onAdd(_map: MapLibreMap): HTMLElement {
    this._container = document.createElement("div");
    this._container.className = this._buildContainerClassName();
    this._contentRoot.hidden = false;
    this._container.appendChild(this._contentRoot);

    return this._container;
  }

  onRemove(): void {
    this._placeholderHost.appendChild(this._contentRoot);
    this._contentRoot.hidden = true;
    this._container?.remove();
    this._container = null;
  }

  update(options: IContentMapControl): void {
    this._options = options;

    if (!this._container) {
      return;
    }

    this._container.className = this._buildContainerClassName();
  }

  private _buildContainerClassName(): string {
    return ["maplibregl-ctrl", "sgb-map-custom-control", this._options.className]
      .filter((className): className is string => Boolean(className))
      .join(" ");
  }
}
