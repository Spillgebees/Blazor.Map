import {
  type Map as MapLibreMap,
  Marker as MapLibreMarker,
  Popup as MapLibrePopup,
  type MarkerOptions,
  type PopupOptions,
} from "maplibre-gl";
import type { IMarker, IMarkerIcon, IPopupOptions } from "../interfaces/features";
import type { FeatureStorage, MarkerEntry } from "../types/feature-storage";

function createMarkerElement(icon: IMarkerIcon): HTMLElement {
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

function createPopup(options: IPopupOptions): MapLibrePopup {
  const popupOptions: PopupOptions = {
    closeButton: options.trigger === "permanent" ? false : options.closeButton,
    closeOnClick: options.trigger !== "permanent",
    closeOnMove: false,
    maxWidth: options.maxWidth ?? "240px",
    className: options.className ?? undefined,
  };

  // Handle anchor
  if (options.anchor !== "auto") {
    popupOptions.anchor = options.anchor;
  }

  // Handle offset
  if (options.offset) {
    popupOptions.offset = [options.offset.x, options.offset.y];
  }

  const popup = new MapLibrePopup(popupOptions);
  popup.setHTML(options.content);

  return popup;
}

function createMarkerEntry(map: MapLibreMap, data: IMarker): MarkerEntry {
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
    // Default MapLibre marker
    if (data.color) {
      options.color = data.color;
    }
    if (data.scale !== null) {
      options.scale = data.scale;
    }
  }

  if (data.rotation !== null) {
    options.rotation = data.rotation;
  }
  if (data.draggable) {
    options.draggable = data.draggable;
  }
  if (data.opacity !== null) {
    options.opacity = data.opacity;
  }
  if (data.className) {
    options.className = data.className;
  }

  const marker = new MapLibreMarker(options).setLngLat([data.position.longitude, data.position.latitude]).addTo(map);

  // Handle popups based on trigger
  let popup: MapLibrePopup | null = null;
  let hoverPopup: MapLibrePopup | null = null;

  if (data.popup) {
    const popupInstance = createPopup(data.popup);

    switch (data.popup.trigger) {
      case "click":
        marker.setPopup(popupInstance);
        popup = popupInstance;
        break;

      case "hover": {
        // Show on mouseenter, hide on mouseleave
        const markerEl = marker.getElement();
        markerEl.addEventListener("mouseenter", () => {
          popupInstance.setLngLat([data.position.longitude, data.position.latitude]).addTo(map);
        });
        markerEl.addEventListener("mouseleave", () => {
          popupInstance.remove();
        });
        hoverPopup = popupInstance;
        break;
      }

      case "permanent":
        // Always visible — add immediately and don't attach to marker
        popupInstance.setLngLat([data.position.longitude, data.position.latitude]).addTo(map);
        popup = popupInstance;
        break;
    }
  }

  return { marker, popup, hoverPopup };
}

function removeMarkerEntry(entry: MarkerEntry): void {
  entry.popup?.remove();
  entry.hoverPopup?.remove();
  entry.marker.remove();
}

export function addMarkers(map: MapLibreMap, markers: IMarker[], storage: FeatureStorage): void {
  for (const markerData of markers) {
    const entry = createMarkerEntry(map, markerData);
    storage.markers.set(markerData.id, entry);
  }
}

export function updateMarkers(map: MapLibreMap, markers: IMarker[], storage: FeatureStorage): void {
  for (const markerData of markers) {
    const existing = storage.markers.get(markerData.id);
    if (!existing) {
      continue;
    }

    // Remove old marker and recreate (simpler than diffing every property).
    // Full recreation is safer and MapLibre marker creation is cheap.
    removeMarkerEntry(existing);

    const entry = createMarkerEntry(map, markerData);
    storage.markers.set(markerData.id, entry);
  }
}

export function removeMarkers(markerIds: string[], storage: FeatureStorage): void {
  for (const id of markerIds) {
    const entry = storage.markers.get(id);
    if (!entry) {
      continue;
    }

    removeMarkerEntry(entry);
    storage.markers.delete(id);
  }
}
