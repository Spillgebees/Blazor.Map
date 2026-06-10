// Component popup execution for the ops channel (popup.set / popup.remove). The popup
// chrome comes from the op; the content is Blazor-rendered DOM resolved by convention:
// a `data-sgb-popup-placeholder="<popupId>"` element inside the map container whose
// first element child is the content root. Close interactions surface through an
// engine event handler id.

import type { Map as MapLibreMap, PopupOptions as MapLibrePopupOptions } from "maplibre-gl";
import { Popup } from "maplibre-gl";
import type { MarkerPopupData, PopupData } from "./ops";

export interface PopupController {
  set(popup: PopupData): void;
  remove(id: string): void;
  /** Open popup ids (for tests/diagnostics). */
  ids(): string[];
  dispose(): void;
}

function findByAttribute(container: HTMLElement, attribute: string, id: string): Element | null {
  for (const element of container.querySelectorAll(`[${attribute}]`)) {
    if (element.getAttribute(attribute) === id) {
      return element;
    }
  }

  return null;
}

interface PopupEntry {
  popup: Popup;
  placeholder: HTMLElement;
  content: HTMLElement;
  suppressCloseCallback: boolean;
}

function componentPopupOptions(options: MarkerPopupData): MapLibrePopupOptions {
  return {
    closeButton: options.closeButton ?? true,
    maxWidth: options.maxWidth ?? "300px",
    className: options.className ?? undefined,
    anchor: options.anchor !== "auto" ? (options.anchor as MapLibrePopupOptions["anchor"]) : undefined,
    offset: options.offset ? [options.offset.x, options.offset.y] : undefined,
  };
}

export function createPopupController(
  map: MapLibreMap,
  container: HTMLElement,
  emit: (handlerId: number, payload: unknown) => void,
): PopupController {
  const entries = new Map<string, PopupEntry>();

  function detach(id: string, entry: PopupEntry): void {
    // return the Blazor-owned content to its hidden placeholder
    entry.placeholder.appendChild(entry.content);
    entries.delete(id);
  }

  return {
    set(data) {
      const existing = entries.get(data.id);
      if (existing) {
        existing.suppressCloseCallback = true;
        existing.popup.remove();
        detach(data.id, existing);
      }

      const placeholder = findByAttribute(container, "data-sgb-popup-placeholder", data.id);
      const content = placeholder?.firstElementChild;
      if (!(placeholder instanceof HTMLElement) || !(content instanceof HTMLElement)) {
        // biome-ignore lint/suspicious/noConsole: explicit popup diagnostics for interop mismatches
        console.warn(`[Spillgebees.Map] popup content for '${data.id}' was not found in the map container.`);
        return;
      }

      const entry: PopupEntry = {
        popup: new Popup(componentPopupOptions(data.options))
          .setLngLat([data.position.longitude, data.position.latitude])
          .setDOMContent(content),
        placeholder,
        content,
        suppressCloseCallback: false,
      };

      const closedHandlerId = data.events?.closed;
      entry.popup.on("close", () => {
        if (entry.suppressCloseCallback || entries.get(data.id) !== entry) {
          return;
        }

        detach(data.id, entry);
        if (closedHandlerId != null) {
          emit(closedHandlerId, {});
        }
      });

      entry.popup.addTo(map);
      entries.set(data.id, entry);
    },
    remove(id) {
      const entry = entries.get(id);
      if (!entry) {
        return;
      }

      entry.suppressCloseCallback = true;
      entry.popup.remove();
      detach(id, entry);
    },
    ids: () => [...entries.keys()],
    dispose() {
      for (const [id, entry] of [...entries]) {
        entry.suppressCloseCallback = true;
        entry.popup.remove();
        detach(id, entry);
      }
    },
  };
}
