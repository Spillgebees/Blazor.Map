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
  // Permanent and hover popups should never show close buttons
  const showCloseButton = options.trigger === "click" && options.closeButton;

  const popupOptions: PopupOptions = {
    closeButton: showCloseButton,
    closeOnClick: options.trigger === "click",
    closeOnMove: false,
    maxWidth: options.maxWidth ?? "240px",
    className: options.className ?? undefined,
  };

  // Handle anchor — default to "bottom" for hover/click popups so they appear above the marker
  if (options.anchor !== "auto") {
    popupOptions.anchor = options.anchor;
  } else if (options.trigger === "hover" || options.trigger === "click") {
    popupOptions.anchor = "bottom";
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
        // MapLibre positions click popups relative to the marker automatically
        marker.setPopup(popupInstance);
        popup = popupInstance;
        break;

      case "hover": {
        // Attach to marker so MapLibre positions it correctly above the icon.
        // Manually toggle on mouseenter/mouseleave.
        marker.setPopup(popupInstance);
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
        hoverPopup = popupInstance;
        break;
      }

      case "permanent": {
        // Attach to marker and open immediately
        marker.setPopup(popupInstance);
        marker.togglePopup();
        popup = popupInstance;

        // Sync z-index between marker and popup on hover so both rise together
        const markerEl = marker.getElement();
        const popupEl = popupInstance.getElement();
        if (popupEl) {
          markerEl.addEventListener("mouseenter", () => {
            popupEl.style.zIndex = "10";
          });
          markerEl.addEventListener("mouseleave", () => {
            popupEl.style.zIndex = "";
          });
          // Also rise when hovering the popup itself
          popupEl.addEventListener("mouseenter", () => {
            markerEl.style.zIndex = "10";
            popupEl.style.zIndex = "10";
          });
          popupEl.addEventListener("mouseleave", () => {
            markerEl.style.zIndex = "";
            popupEl.style.zIndex = "";
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
        break;
      }
    }
  }

  // Wire marker click event
  marker.getElement().addEventListener("click", (e: Event) => {
    e.stopPropagation(); // prevent map click from firing too
    const lngLat = marker.getLngLat();
    const dotNetHelper = window.Spillgebees.Map.dotNetHelpers.get(map);
    dotNetHelper?.invokeMethodAsync("OnMarkerClickCallbackAsync", {
      markerId: data.id,
      position: { latitude: lngLat.lat, longitude: lngLat.lng },
    });
  });

  // Wire marker drag end event for draggable markers
  if (data.draggable) {
    marker.on("dragend", () => {
      const lngLat = marker.getLngLat();
      const dotNetHelper = window.Spillgebees.Map.dotNetHelpers.get(map);
      // biome-ignore lint/security/noSecrets: C# callback method name, not a secret
      dotNetHelper?.invokeMethodAsync("OnMarkerDragEndCallbackAsync", {
        markerId: data.id,
        position: { latitude: lngLat.lat, longitude: lngLat.lng },
      });
    });
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
