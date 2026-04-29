import type { DotNet } from "@microsoft/dotnet-js-interop";
import { type GeoJSONSource, type Map as MapLibreMap, Popup } from "maplibre-gl";
import type { IPopupOptions } from "../interfaces/features";
import type { ICoordinate } from "../interfaces/map";
import type {
  LayerEventSubscription,
  RegisteredMapLayer,
  VisibilityGroupRegistration,
} from "../interfaces/spillgebees";
import { buildLayerPlan } from "../ordering";
import {
  getLayerEventStore,
  getSceneLayerStore,
  getSceneSourceStore,
  getVisibilityGroupStore,
} from "../runtime/registry";

function getAnimationStore(map: MapLibreMap): Map<string, AnimationState> {
  const existing = activeAnimations.get(map);
  if (existing) {
    return existing;
  }

  const created = new Map<string, AnimationState>();
  activeAnimations.set(map, created);
  return created;
}

function getLayerEventSubscriptionStore(map: MapLibreMap) {
  return getLayerEventStore(map);
}

function bindLayerEventsForMap(map: MapLibreMap, layerId: string, subscription: LayerEventSubscription): void {
  if (!map.getLayer(layerId)) {
    return;
  }

  if (subscription.click) {
    map.off("click", layerId, subscription.click);
    map.on("click", layerId, subscription.click);
  }

  if (subscription.mouseEnter) {
    map.off("mouseenter", layerId, subscription.mouseEnter);
    map.on("mouseenter", layerId, subscription.mouseEnter);
  }

  if (subscription.mouseLeave) {
    map.off("mouseleave", layerId, subscription.mouseLeave);
    map.on("mouseleave", layerId, subscription.mouseLeave);
  }
}

function unregisterLayerEventsForMap(map: MapLibreMap, layerId: string): void {
  const subscriptions = getLayerEventSubscriptionStore(map);
  const existing = subscriptions.get(layerId);

  if (!existing) {
    return;
  }

  if (existing.click) {
    map.off("click", layerId, existing.click);
  }

  if (existing.mouseEnter) {
    map.off("mouseenter", layerId, existing.mouseEnter);
  }

  if (existing.mouseLeave) {
    map.off("mouseleave", layerId, existing.mouseLeave);
  }

  if (existing.onStyleData) {
    map.off("styledata", existing.onStyleData);
  }

  subscriptions.delete(layerId);
}

export function addMapSource(mapElement: HTMLElement, sourceId: string, sourceSpec: Record<string, unknown>): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const sourceStore = getSceneSourceStore(map);
  sourceStore.set(sourceId, {
    sourceId,
    sourceSpec: structuredClone(sourceSpec),
  });

  // Don't add if already exists (idempotent)
  if (map.getSource(sourceId)) return;

  map.addSource(sourceId, sourceSpec as Parameters<MapLibreMap["addSource"]>[1]);
}

export function removeMapSource(mapElement: HTMLElement, sourceId: string): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const animationStore = activeAnimations.get(map);
  const activeAnimation = animationStore?.get(sourceId);
  if (activeAnimation) {
    cancelAnimationFrame(activeAnimation.animationFrame);
    animationStore?.delete(sourceId);
  }

  const sourceStore = getSceneSourceStore(map);
  sourceStore?.delete(sourceId);

  const layerStore = getSceneLayerStore(map);

  // Remove all layers that reference this source first
  const style = map.getStyle();
  if (style?.layers) {
    for (const layer of style.layers) {
      if ("source" in layer && layer.source === sourceId) {
        unregisterLayerEventsForMap(map, layer.id);
        layerStore?.delete(layer.id);
        map.removeLayer(layer.id);
      }
    }
  }

  if (map.getSource(sourceId)) {
    map.removeSource(sourceId);
  }
}

export function setSourceData(mapElement: HTMLElement, sourceId: string, data: unknown): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const sourceStore = getSceneSourceStore(map);
  const existingSpec = sourceStore?.get(sourceId);
  if (existingSpec) {
    existingSpec.sourceSpec = {
      ...existingSpec.sourceSpec,
      data: structuredClone(data),
    };
  }

  const source = map.getSource(sourceId);
  if (source && "setData" in source) {
    (source as GeoJSONSource).setData(data as GeoJSON.GeoJSON);
  }
}

export function addMapLayer(
  mapElement: HTMLElement,
  layerSpec: Record<string, unknown>,
  beforeLayerId: string | null,
  ordering?: {
    declarationOrder: number;
    layerGroup: string | null;
    beforeLayerGroup: string | null;
    afterLayerGroup: string | null;
  },
): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const layerStore = getSceneLayerStore(map);
  const existing = layerStore.get(layerSpec.id as string);
  layerStore.set(layerSpec.id as string, {
    layerId: layerSpec.id as string,
    layerSpec: structuredClone(layerSpec),
    beforeLayerId,
    imperativeBeforeLayerId: existing?.imperativeBeforeLayerId,
    ordering: ordering ??
      existing?.ordering ?? {
        declarationOrder: Number.MAX_SAFE_INTEGER,
        layerGroup: null,
        beforeLayerGroup: null,
        afterLayerGroup: null,
      },
  });

  const subscription = getLayerEventSubscriptionStore(map).get(layerSpec.id as string);
  if (subscription) {
    bindLayerEventsForMap(map, layerSpec.id as string, subscription);
  }
}

export function removeMapLayer(mapElement: HTMLElement, layerId: string): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  unregisterLayerEventsForMap(map, layerId);
  getSceneLayerStore(map).delete(layerId);

  if (map.getLayer(layerId)) {
    map.removeLayer(layerId);
  }
}

export function moveMapLayer(mapElement: HTMLElement, layerId: string, beforeLayerId: string | null): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  const registration = getSceneLayerStore(map).get(layerId);
  if (!registration) {
    if (map.getLayer(layerId)) {
      map.moveLayer(layerId, beforeLayerId ?? undefined);
    }

    return;
  }

  registration.imperativeBeforeLayerId = beforeLayerId;
  reconcileLayerOrdering(map);
}

export function reconcileLayerOrdering(map: MapLibreMap): void {
  const layerStore = getSceneLayerStore(map);
  if (!layerStore || layerStore.size === 0) {
    return;
  }

  const styleLayerIds = (map.getStyle()?.layers ?? []).map((layer) => layer.id);
  const registrations = Array.from(layerStore.values());
  const plan = buildLayerPlan(registrations, styleLayerIds);

  for (let index = plan.length - 1; index >= 0; index--) {
    const step = plan[index];
    const registration = layerStore.get(step.layerId) as RegisteredMapLayer;
    const exists = map.getLayer(step.layerId);
    if (!exists) {
      map.addLayer(registration.layerSpec as Parameters<MapLibreMap["addLayer"]>[0], step.beforeLayerId ?? undefined);
      continue;
    }

    map.moveLayer(step.layerId, step.beforeLayerId ?? undefined);
  }
}

export function setVisibilityGroup(
  mapElement: HTMLElement,
  groupId: string,
  visible: boolean,
  targets: Array<{ styleId: string; layerIds: string[] }>,
): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  const visibilityGroups = getVisibilityGroupStore(map);
  visibilityGroups.set(groupId, {
    groupId,
    visible,
    targets: structuredClone(targets),
  });

  applyVisibilityGroup(mapElement, {
    groupId,
    visible,
    targets,
  });
}

export function removeVisibilityGroup(mapElement: HTMLElement, groupId: string): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  getVisibilityGroupStore(map).delete(groupId);
}

export function applyVisibilityGroup(mapElement: HTMLElement, group: VisibilityGroupRegistration): void {
  for (const target of group.targets) {
    for (const layerId of target.layerIds) {
      const resolvedLayerId = resolveStyleLayerId(mapElement, target.styleId, layerId);
      if (!resolvedLayerId) {
        continue;
      }

      setLayerVisibility(mapElement, resolvedLayerId, group.visible);
    }
  }
}

function resolveStyleLayerId(mapElement: HTMLElement, styleId: string, layerId: string): string | null {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return null;
  }

  const mapOptions = window.Spillgebees.Map.mapOptions.get(map);
  const baseStyleId = mapOptions?.styles?.[0]?.id ?? mapOptions?.style?.id ?? null;
  if (baseStyleId === styleId) {
    return layerId;
  }

  const composedLayerKey = `${styleId}\u0000${layerId}`;
  const registration = window.Spillgebees.Map.composedStyleLayerIds.get(map)?.get(composedLayerKey);
  return registration?.runtimeLayerId ?? null;
}

export function setPaintProperty(mapElement: HTMLElement, layerId: string, name: string, value: unknown): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map?.getLayer(layerId)) return;

  const layerStore = getSceneLayerStore(map);
  const registration = layerStore?.get(layerId);
  if (registration) {
    const currentPaint = (registration.layerSpec.paint as Record<string, unknown> | undefined) ?? {};
    registration.layerSpec.paint = { ...currentPaint, [name]: structuredClone(value) };
  }

  map.setPaintProperty(layerId, name, value);
}

export function setLayoutProperty(mapElement: HTMLElement, layerId: string, name: string, value: unknown): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map?.getLayer(layerId)) return;

  const layerStore = getSceneLayerStore(map);
  const registration = layerStore?.get(layerId);
  if (registration) {
    const currentLayout = (registration.layerSpec.layout as Record<string, unknown> | undefined) ?? {};
    registration.layerSpec.layout = { ...currentLayout, [name]: structuredClone(value) };
  }

  map.setLayoutProperty(layerId, name, value);
}

export function setFilter(mapElement: HTMLElement, layerId: string, filter: unknown): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map?.getLayer(layerId)) return;

  const layerStore = getSceneLayerStore(map);
  const registration = layerStore?.get(layerId);
  if (registration) {
    registration.layerSpec.filter = structuredClone(filter);
  }

  map.setFilter(layerId, filter as Parameters<typeof map.setFilter>[1]);
}

export function setLayerZoomRange(mapElement: HTMLElement, layerId: string, minZoom: number, maxZoom: number): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map?.getLayer(layerId)) return;

  const layerStore = getSceneLayerStore(map);
  const registration = layerStore?.get(layerId);
  if (registration) {
    registration.layerSpec.minzoom = minZoom;
    registration.layerSpec.maxzoom = maxZoom;
  }

  map.setLayerZoomRange(layerId, minZoom, maxZoom);
}

export function setLayerVisibility(mapElement: HTMLElement, layerId: string, visible: boolean): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  if (map.getLayer(layerId)) {
    const layerStore = window.Spillgebees.Map.layerSpecs.get(map);
    const registration = layerStore?.get(layerId);
    if (registration) {
      const currentLayout = (registration.layerSpec.layout as Record<string, unknown> | undefined) ?? {};
      registration.layerSpec.layout = { ...currentLayout, visibility: visible ? "visible" : "none" };
    }

    map.setLayoutProperty(layerId, "visibility", visible ? "visible" : "none");
  }
}

export function wireLayerEvents(
  mapElement: HTMLElement,
  layerId: string,
  dotNetRef: DotNet.DotNetObject,
  onClick: boolean,
  onMouseEnter: boolean,
  onMouseLeave: boolean,
): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  unregisterLayerEventsForMap(map, layerId);

  // biome-ignore lint/security/noSecrets: .NET interop method names, not secrets
  const clickCallback = "OnLayerClickAsync";
  // biome-ignore lint/security/noSecrets: .NET interop method name
  const mouseEnterCallback = "OnLayerMouseEnterAsync";
  // biome-ignore lint/security/noSecrets: .NET interop method name
  const mouseLeaveCallback = "OnLayerMouseLeaveAsync";

  const subscriptions: LayerEventSubscription = {};

  subscriptions.dotNetRef = dotNetRef;

  if (onClick) {
    subscriptions.click = (e) => {
      const feature = e.features?.[0];
      dotNetRef.invokeMethodAsync(clickCallback, e.lngLat.lat, e.lngLat.lng, feature?.properties ?? null);
    };
    map.on("click", layerId, subscriptions.click);
  }

  if (onMouseEnter) {
    subscriptions.mouseEnter = (e) => {
      map.getCanvas().style.cursor = "pointer";
      const feature = e.features?.[0];
      dotNetRef.invokeMethodAsync(mouseEnterCallback, e.lngLat.lat, e.lngLat.lng, feature?.properties ?? null);
    };
    map.on("mouseenter", layerId, subscriptions.mouseEnter);
  }

  if (onMouseLeave) {
    subscriptions.mouseLeave = () => {
      map.getCanvas().style.cursor = "";
      dotNetRef.invokeMethodAsync(mouseLeaveCallback);
    };
    map.on("mouseleave", layerId, subscriptions.mouseLeave);
  }

  subscriptions.onStyleData = () => {
    bindLayerEventsForMap(map, layerId, subscriptions);
  };
  map.on("styledata", subscriptions.onStyleData);

  getLayerEventSubscriptionStore(map).set(layerId, subscriptions);
  bindLayerEventsForMap(map, layerId, subscriptions);
}

export function unregisterLayerEvents(mapElement: HTMLElement, layerId: string): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  unregisterLayerEventsForMap(map, layerId);
}

export function rebindLayerEvents(mapElement: HTMLElement, layerId: string): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) {
    return;
  }

  const subscription = getLayerEventSubscriptionStore(map).get(layerId);
  if (!subscription) {
    return;
  }

  bindLayerEventsForMap(map, layerId, subscription);
}

// --- Image registration ---

export async function addImage(
  mapElement: HTMLElement,
  name: string,
  url: string,
  width: number,
  height: number,
  pixelRatio: number,
  sdf: boolean = false,
  shouldAbort?: () => boolean,
): Promise<void> {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  if (shouldAbort?.()) {
    return;
  }

  if (map.hasImage(name)) {
    map.removeImage(name);
  }

  // render at native device resolution for crisp icons on HiDPI displays
  const dpr = window.devicePixelRatio || 1;
  const renderWidth = Math.round(width * dpr);
  const renderHeight = Math.round(height * dpr);

  // render the image (supports SVG data URIs, HTTP URLs, PNGs, etc.)
  // via an HTMLImageElement + OffscreenCanvas, since map.loadImage()
  // doesn't support SVG data URIs
  const img = new Image(renderWidth, renderHeight);
  img.crossOrigin = "anonymous";
  await new Promise<void>((resolve, reject) => {
    img.onload = () => resolve();
    img.onerror = () => reject(new Error(`Failed to load image: ${name}`));
    img.src = url;
  });

  if (shouldAbort?.()) {
    return;
  }

  const canvas = new OffscreenCanvas(renderWidth, renderHeight);
  const ctx = canvas.getContext("2d");
  if (!ctx) return;
  ctx.drawImage(img, 0, 0, renderWidth, renderHeight);
  const imageData = ctx.getImageData(0, 0, renderWidth, renderHeight);

  if (shouldAbort?.()) {
    return;
  }

  // pixelRatio tells MapLibre the image is rendered at DPR scale —
  // it displays at width x height CSS pixels using the hi-res data
  map.addImage(name, imageData, { pixelRatio: pixelRatio * dpr, sdf });
}

// --- Programmatic popup ---

// One active popup per map — calling showPopup replaces the previous one
const activePopups = new WeakMap<MapLibreMap, InstanceType<typeof Popup>>();
interface ComponentPopupRegistration {
  popup: InstanceType<typeof Popup>;
  placeholder: HTMLElement;
  content: HTMLElement;
  suppressCloseCallback: boolean;
}

const componentPopups = new WeakMap<MapLibreMap, Map<string, ComponentPopupRegistration>>();

function setPopupContent(popup: InstanceType<typeof Popup>, options: IPopupOptions): void {
  if (options.contentMode === "rawHtml") {
    popup.setHTML(options.content);
  } else {
    popup.setText(options.content);
  }
}

function createPopupOptions(options?: IPopupOptions | null): ConstructorParameters<typeof Popup>[0] {
  return {
    closeButton: options?.closeButton ?? true,
    maxWidth: options?.maxWidth ?? "300px",
    className: options?.className ?? undefined,
    anchor:
      options?.anchor !== "auto" ? (options?.anchor as "top" | "bottom" | "left" | "right" | undefined) : undefined,
    offset: options?.offset ? [options.offset.x, options.offset.y] : undefined,
  };
}

function getComponentPopupStore(map: MapLibreMap): Map<string, ComponentPopupRegistration> {
  const existing = componentPopups.get(map);
  if (existing) {
    return existing;
  }

  const created = new Map<string, ComponentPopupRegistration>();
  componentPopups.set(map, created);
  return created;
}

function detachPopupDomContent(
  store: Map<string, ComponentPopupRegistration>,
  popupId: string,
  registration: ComponentPopupRegistration,
): void {
  registration.placeholder.appendChild(registration.content);
  store.delete(popupId);
}

export function showPopup(mapElement: HTMLElement, position: ICoordinate, options: IPopupOptions): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  // Close previous
  activePopups.get(map)?.remove();

  const popup = new Popup(createPopupOptions(options)).setLngLat([position.longitude, position.latitude]);
  setPopupContent(popup, options);
  popup.addTo(map);

  activePopups.set(map, popup);
}

export function setPopupDomContent(
  mapElement: HTMLElement,
  popupId: string,
  position: ICoordinate,
  options: IPopupOptions,
  placeholder: HTMLElement,
  content: HTMLElement,
  dotNetRef: DotNet.DotNetObject,
): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const store = getComponentPopupStore(map);
  const existing = store.get(popupId);
  if (existing) {
    existing.suppressCloseCallback = true;
    existing.popup.remove();
    detachPopupDomContent(store, popupId, existing);
  }

  const registration = {
    popup: new Popup(createPopupOptions(options))
      .setLngLat([position.longitude, position.latitude])
      .setDOMContent(content),
    placeholder,
    content,
    suppressCloseCallback: false,
  };

  registration.popup.on("close", () => {
    if (registration.suppressCloseCallback || store.get(popupId) !== registration) {
      return;
    }

    detachPopupDomContent(store, popupId, registration);

    // biome-ignore lint/security/noSecrets: .NET interop method name, not a secret
    dotNetRef.invokeMethodAsync("OnPopupClosedAsync").catch(() => undefined);
  });

  registration.popup.addTo(map);
  store.set(popupId, registration);
}

export function removePopupDomContent(mapElement: HTMLElement, popupId: string): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const store = componentPopups.get(map);
  if (!store) {
    return;
  }

  const registration = store.get(popupId);
  if (!registration) {
    return;
  }

  registration.suppressCloseCallback = true;
  registration.popup.remove();
  detachPopupDomContent(store, popupId, registration);
}

export function closePopup(mapElement: HTMLElement): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  activePopups.get(map)?.remove();
  activePopups.delete(map);
}

// --- Feature state ---

export function setFeatureState(
  mapElement: HTMLElement,
  sourceId: string,
  featureId: unknown,
  state: Record<string, unknown>,
  sourceLayer?: string | null,
): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  if (typeof featureId !== "string" && typeof featureId !== "number") {
    return;
  }

  const target: { source: string; id: string | number; sourceLayer?: string } = {
    source: sourceId,
    id: featureId,
  };

  if (sourceLayer) {
    target.sourceLayer = sourceLayer;
  }

  map.setFeatureState(target, state);
}

// --- Cluster interaction ---

export async function getClusterExpansionZoom(
  mapElement: HTMLElement,
  sourceId: string,
  clusterId: number,
): Promise<number> {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return 0;

  const source = map.getSource(sourceId) as GeoJSONSource;
  if (!source) return 0;

  return await source.getClusterExpansionZoom(clusterId);
}

// --- Animated source data updates ---

interface AnimationState {
  startTime: number;
  duration: number;
  easing: string;
  fromFeatures: Map<string | number, { lng: number; lat: number; bearing?: number }>;
  toFeatures: Map<string | number, { lng: number; lat: number; bearing?: number }>;
  baseData: GeoJSON.GeoJSON;
  animationFrame: number;
}

const activeAnimations = new WeakMap<MapLibreMap, Map<string, AnimationState>>();

export function setSourceDataAnimated(
  mapElement: HTMLElement,
  sourceId: string,
  data: GeoJSON.GeoJSON,
  duration: number,
  easing: string,
): void {
  const map = window.Spillgebees.Map.maps.get(mapElement);
  if (!map) return;

  const source = map.getSource(sourceId) as GeoJSONSource;
  if (!source) return;

  const sourceStore = window.Spillgebees.Map.sourceSpecs.get(map);
  const existingSpec = sourceStore?.get(sourceId);
  const previousData = existingSpec?.sourceSpec.data as GeoJSON.GeoJSON | undefined;
  if (existingSpec) {
    existingSpec.sourceSpec = {
      ...existingSpec.sourceSpec,
      data: structuredClone(data),
    };
  }

  const animationStore = getAnimationStore(map);

  // cancel any existing animation for this source
  const existing = animationStore.get(sourceId);
  if (existing) {
    cancelAnimationFrame(existing.animationFrame);
  }

  // capture current positions from the source's current data
  const fromFeatures = new Map<string | number, { lng: number; lat: number; bearing?: number }>();

  // try to get current feature positions from the currently rendered data
  const currentFeatures = map.querySourceFeatures(sourceId);
  for (const feature of currentFeatures) {
    if (feature.geometry?.type === "Point" && feature.id != null) {
      const coords = (feature.geometry as GeoJSON.Point).coordinates;
      fromFeatures.set(feature.id, {
        lng: coords[0],
        lat: coords[1],
        bearing: feature.properties?.bearing as number | undefined,
      });
    }
  }

  if (fromFeatures.size === 0 && previousData) {
    const existingData = previousData;
    if (existingData && "features" in existingData) {
      for (const feature of (existingData as GeoJSON.FeatureCollection).features) {
        if (feature.geometry?.type === "Point" && feature.id != null) {
          const coords = (feature.geometry as GeoJSON.Point).coordinates;
          fromFeatures.set(feature.id, {
            lng: coords[0],
            lat: coords[1],
            bearing: feature.properties?.bearing as number | undefined,
          });
        }
      }
    }
  }

  // if we couldn't get features from query (first load), just set data directly
  if (fromFeatures.size === 0) {
    source.setData(data);
    return;
  }

  // capture target positions from the new data
  const toFeatures = new Map<string | number, { lng: number; lat: number; bearing?: number }>();
  if (data && "features" in data) {
    for (const feature of (data as GeoJSON.FeatureCollection).features) {
      if (feature.geometry?.type === "Point" && feature.id != null) {
        const coords = (feature.geometry as GeoJSON.Point).coordinates;
        toFeatures.set(feature.id, {
          lng: coords[0],
          lat: coords[1],
          bearing: feature.properties?.bearing as number | undefined,
        });
      }
    }
  }

  const state: AnimationState = {
    startTime: performance.now(),
    duration,
    easing,
    fromFeatures,
    toFeatures,
    baseData: data,
    animationFrame: 0,
  };

  const easingFn = easing === "easeinout" ? easeInOut : linear;

  function animate(now: number) {
    const elapsed = now - state.startTime;
    const t = Math.min(elapsed / state.duration, 1);
    const easedT = easingFn(t);

    // interpolate all point features
    if (state.baseData && "features" in state.baseData) {
      const interpolatedFeatures = (state.baseData as GeoJSON.FeatureCollection).features.map((feature) => {
        if (feature.geometry?.type !== "Point" || feature.id == null) return feature;

        const from = state.fromFeatures.get(feature.id);
        const to = state.toFeatures.get(feature.id);
        if (!from || !to) return feature;

        const lng = from.lng + (to.lng - from.lng) * easedT;
        const lat = from.lat + (to.lat - from.lat) * easedT;

        const interpolated = {
          ...feature,
          geometry: { type: "Point" as const, coordinates: [lng, lat] },
        };

        // interpolate bearing if both have it
        if (from.bearing != null && to.bearing != null && interpolated.properties) {
          // handle bearing wraparound (e.g., 350° → 10° should go through 360/0, not backwards)
          let diff = to.bearing - from.bearing;
          if (diff > 180) diff -= 360;
          if (diff < -180) diff += 360;
          interpolated.properties = {
            ...interpolated.properties,
            bearing: from.bearing + diff * easedT,
          };
        }

        return interpolated;
      });

      source.setData({
        type: "FeatureCollection",
        features: interpolatedFeatures,
      });
    }

    if (t < 1) {
      state.animationFrame = requestAnimationFrame(animate);
    } else {
      // animation complete — set final data exactly
      source.setData(state.baseData);
      animationStore.delete(sourceId);
    }
  }

  state.animationFrame = requestAnimationFrame(animate);
  animationStore.set(sourceId, state);
}

function linear(t: number): number {
  return t;
}

function easeInOut(t: number): number {
  return t < 0.5 ? 2 * t * t : 1 - (-2 * t + 2) ** 2 / 2;
}
