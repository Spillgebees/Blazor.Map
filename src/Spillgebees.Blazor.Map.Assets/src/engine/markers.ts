// DOM marker execution for the ops channel (marker.set / marker.remove). Markers are
// positioned HTML elements, not GL features — there is no faster lane for them by
// construction; this module is the single owner of their lifecycle. Marker click and
// drag-end interactions surface through one callback, mirroring how the engine
// forwards layer events.

import {
  type Map as MapLibreMap,
  Marker as MapLibreMarker,
  Popup as MapLibrePopup,
  type MarkerOptions,
  type PopupOptions,
} from "maplibre-gl";
import type { MarkerData, MarkerIconData, MarkerPopupData } from "./ops";

export type MarkerEventKind = "click" | "dragend";

export interface MarkerController {
  set(data: MarkerData): void;
  remove(id: string): void;
  /** Live position of a marker (camera.fitFeatures resolution). */
  position(id: string): { lng: number; lat: number } | null;
  /** Marker ids currently alive (for tests/diagnostics). */
  ids(): string[];
  dispose(): void;
}

interface MarkerEntry {
  /** Everything except position — position-only updates take the setLngLat fast path. */
  structureKey: string;
  marker: MapLibreMarker;
  popup: MapLibrePopup | null;
}

function createMarkerElement(icon: MarkerIconData): HTMLElement {
  const img = document.createElement("img");
  img.src = icon.url;
  if (icon.size) {
    img.width = icon.size.x;
    img.height = icon.size.y;
  }
  img.style.display = "block"; // prevent inline spacing

  const container = document.createElement("div");
  container.appendChild(img);
  return container;
}

export function createMarkerPopup(options: MarkerPopupData): MapLibrePopup {
  // Permanent and hover popups should never show close buttons
  const showCloseButton = options.trigger === "click" && options.closeButton;

  const popupOptions: PopupOptions = {
    closeButton: showCloseButton,
    closeOnClick: options.trigger === "click",
    closeOnMove: false,
    maxWidth: options.maxWidth ?? "240px",
    className: options.className ?? undefined,
  };

  // Default to "bottom" for hover/click popups so they appear above the marker
  if (options.anchor !== "auto") {
    popupOptions.anchor = options.anchor as PopupOptions["anchor"];
  } else if (options.trigger === "hover" || options.trigger === "click") {
    popupOptions.anchor = "bottom";
  }

  if (options.offset) {
    popupOptions.offset = [options.offset.x, options.offset.y];
  }

  const popup = new MapLibrePopup(popupOptions);
  if (options.contentMode === "rawHtml") {
    popup.setHTML(options.content);
  } else {
    popup.setText(options.content);
  }

  return popup;
}

function markerOptions(data: MarkerData): MarkerOptions {
  const options: MarkerOptions = {};

  if (data.icon) {
    options.element = createMarkerElement(data.icon);
    if (data.icon.anchor) {
      // The anchor point in the icon should align with the geographic coordinate.
      // Set anchor to "top-left" and offset to negate the anchor point so the
      // specified pixel lands on the coordinate.
      options.anchor = "top-left";
      options.offset = [-data.icon.anchor.x, -data.icon.anchor.y];
    }
  } else {
    // Default MapLibre marker pin
    if (data.color) {
      options.color = data.color;
    }
    if (data.scale != null) {
      options.scale = data.scale;
    }
  }

  if (data.rotation != null) {
    options.rotation = data.rotation;
  }
  if (data.rotationAlignment) {
    options.rotationAlignment = data.rotationAlignment as "map" | "viewport" | "auto";
  }
  if (data.pitchAlignment) {
    options.pitchAlignment = data.pitchAlignment as "map" | "viewport" | "auto";
  }
  if (data.draggable) {
    options.draggable = data.draggable;
  }
  if (data.opacity != null) {
    options.opacity = data.opacity;
  }
  if (data.className) {
    options.className = data.className;
  }

  return options;
}

function wirePermanentPopupHover(map: MapLibreMap, marker: MapLibreMarker, popup: MapLibrePopup): void {
  // Sync z-index + hover class between marker and popup
  const markerEl = marker.getElement();
  const popupEl = popup.getElement();
  if (!popupEl) {
    return;
  }

  markerEl.addEventListener("mouseenter", () => {
    popupEl.style.zIndex = "10";
    popupEl.classList.add("sgb-popup-hover");
  });
  markerEl.addEventListener("mouseleave", () => {
    popupEl.style.zIndex = "";
    popupEl.classList.remove("sgb-popup-hover");
  });
  // Also rise when hovering the popup itself
  popupEl.addEventListener("mouseenter", () => {
    markerEl.style.zIndex = "10";
    popupEl.style.zIndex = "10";
    popupEl.classList.add("sgb-popup-hover");
  });
  popupEl.addEventListener("mouseleave", () => {
    markerEl.style.zIndex = "";
    popupEl.style.zIndex = "";
    popupEl.classList.remove("sgb-popup-hover");
  });
  // Prevent scroll/wheel events on the popup from scrolling the page.
  // Instead, forward them to the map canvas for proper zoom handling.
  popupEl.addEventListener(
    "wheel",
    (e) => {
      e.preventDefault();
      e.stopPropagation();
      map.getCanvas().dispatchEvent(new WheelEvent("wheel", e));
    },
    { passive: false },
  );
}

export function createMarkerController(
  map: MapLibreMap,
  onMarkerEvent: (kind: MarkerEventKind, markerId: string, lng: number, lat: number) => void,
): MarkerController {
  const entries = new Map<string, MarkerEntry>();

  function createEntry(data: MarkerData, structureKey: string): MarkerEntry {
    const marker = new MapLibreMarker(markerOptions(data))
      .setLngLat([data.position.longitude, data.position.latitude])
      .addTo(map);

    if (data.title) {
      marker.getElement().title = data.title;
    }

    let popup: MapLibrePopup | null = null;
    if (data.popup) {
      popup = createMarkerPopup(data.popup);
      // MapLibre positions marker popups automatically for all triggers
      marker.setPopup(popup);

      switch (data.popup.trigger) {
        case "hover": {
          // Manually toggle on mouseenter/mouseleave.
          const markerEl = marker.getElement();
          let isHovering = false;
          markerEl.addEventListener("mouseenter", () => {
            if (!isHovering) {
              isHovering = true;
              marker.togglePopup();
            }
          });
          markerEl.addEventListener("mouseleave", () => {
            if (isHovering) {
              isHovering = false;
              marker.togglePopup();
            }
          });
          break;
        }
        case "permanent":
          marker.togglePopup(); // open immediately
          wirePermanentPopupHover(map, marker, popup);
          break;
      }
    }

    marker.getElement().addEventListener("click", (e: Event) => {
      e.stopPropagation(); // prevent map click from firing too

      // stopPropagation prevents the event from reaching the map, which is where
      // MapLibre's Marker._onMapClick listens to toggle popups. Manually toggle
      // when a click popup is attached so the popup still opens.
      if (data.popup?.trigger === "click") {
        marker.togglePopup();
      }

      const lngLat = marker.getLngLat();
      onMarkerEvent("click", data.id, lngLat.lng, lngLat.lat);
    });

    if (data.draggable) {
      marker.on("dragend", () => {
        const lngLat = marker.getLngLat();
        onMarkerEvent("dragend", data.id, lngLat.lng, lngLat.lat);
      });
    }

    return { structureKey, marker, popup };
  }

  function removeEntry(entry: MarkerEntry): void {
    entry.popup?.remove();
    entry.marker.remove();
  }

  return {
    set(data) {
      const structureKey = JSON.stringify({ ...data, position: null });
      const existing = entries.get(data.id);
      if (existing) {
        if (existing.structureKey === structureKey) {
          // only the position changed — move the live element instead of recreating
          existing.marker.setLngLat([data.position.longitude, data.position.latitude]);
          return;
        }

        removeEntry(existing);
      }

      entries.set(data.id, createEntry(data, structureKey));
    },
    remove(id) {
      const entry = entries.get(id);
      if (entry) {
        removeEntry(entry);
        entries.delete(id);
      }
    },
    position(id) {
      const entry = entries.get(id);
      if (!entry) {
        return null;
      }

      const lngLat = entry.marker.getLngLat();
      return { lng: lngLat.lng, lat: lngLat.lat };
    },
    ids: () => [...entries.keys()],
    dispose() {
      for (const entry of entries.values()) {
        removeEntry(entry);
      }
      entries.clear();
    },
  };
}
