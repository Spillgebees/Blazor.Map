// Visibility controller for display groups and overlays — see
// sources/geojson.ts resolveTargets/composeDisplayFilter/applyOverlay):
//
// - Groups (display items) and overlays (with parts) target layers through the
//   VisibilityTarget vocabulary; a layer is visible iff its original visibility AND
//   every group containing it AND every overlay/part containing it are visible.
// - styleLayerFeatures targets never toggle whole layers: each hidden group's filter is
//   negated and ANDed onto the layer's baseline filter.
// - Baseline filters are owned by the engine layer store for runtime layers and
//   captured from the style on first touch for style layers.

import type { OverlayPartConfig, VisibilityTarget } from "./ops";

export interface StyleLayerInfo {
  id: string;
  visible: boolean;
  filter: unknown;
  tags: string[];
}

/** Map surface + engine lookups the controller needs; injectable for tests. */
export interface VisibilityHost {
  getRuntimeLayer(layerId: string): { visible: boolean } | null;
  getRuntimeBaselineFilter(layerId: string): unknown;
  /** Layers of the base style (excluding engine-managed runtime layers). */
  listStyleLayers(): StyleLayerInfo[];
  /** Resolves a composed overlay-style layer to its runtime id, if composed. */
  resolveComposedLayer(styleId: string, layerId: string): { layerId: string; visible: boolean } | null;
  listComposedLayers(styleId: string): { layerId: string; visible: boolean }[];
  setLayerVisibility(layerId: string, visible: boolean): void;
  setLayerFilter(layerId: string, filter: unknown): void;
  hasLayer(layerId: string): boolean;
}

interface GroupState {
  visible: boolean;
  targets: VisibilityTarget[];
}

interface OverlayState {
  visible: boolean;
  targets: VisibilityTarget[];
  parts: OverlayPartConfig[];
}

interface ResolvedLayer {
  layerId: string;
  originalVisible: boolean;
}

export interface VisibilityController {
  setGroup(id: string, visible: boolean, targets: VisibilityTarget[]): void;
  removeGroup(id: string): void;
  setOverlay(id: string, visible: boolean, targets: VisibilityTarget[], parts: OverlayPartConfig[]): void;
  removeOverlay(id: string): void;
  /** Recomposes the filter for a layer whose baseline changed. */
  onBaselineFilterChanged(layerId: string): void;
  /** Applies registrations to a layer that was just added. */
  onLayerAdded(layerId: string): void;
  /** Reapplies everything (after style replay). */
  replay(): void;
}

export function composeDisplayFilter(baseline: unknown, hiddenFilters: unknown[]): unknown {
  if (hiddenFilters.length === 0) {
    return baseline ?? null;
  }

  const negated = hiddenFilters.map((filter) => ["!", filter]);
  return baseline == null ? ["all", ...negated] : ["all", baseline, ...negated];
}

function layerTags(metadata: unknown): string[] {
  const meta = metadata as Record<string, unknown> | null | undefined;
  const tags = meta?.tags ?? meta?.["sgb:tags"];
  return Array.isArray(tags) ? tags.filter((tag): tag is string => typeof tag === "string") : [];
}

export function styleLayerInfo(layer: {
  id: string;
  layout?: { visibility?: string };
  filter?: unknown;
  metadata?: unknown;
}): StyleLayerInfo {
  return {
    id: layer.id,
    visible: layer.layout?.visibility !== "none",
    filter: layer.filter ?? null,
    tags: layerTags(layer.metadata),
  };
}

export function createVisibilityController(host: VisibilityHost): VisibilityController {
  const groups = new Map<string, GroupState>();
  const overlays = new Map<string, OverlayState>();
  /** Original filters of style layers, captured before display filters compose onto them. */
  const styleBaselineFilters = new Map<string, unknown>();
  /** Original visibility of style layers, captured on first resolution. */
  const styleOriginalVisible = new Map<string, boolean>();
  /** Original visibility of composed overlay layers, captured on first resolution. */
  const composedOriginalVisible = new Map<string, boolean>();
  /** Layers currently carrying a composed (baseline + hidden) filter. */
  const composedFilterLayers = new Set<string>();

  function resolveStyleLayer(styleId: string, layerId: string): ResolvedLayer | null {
    const composed = host.resolveComposedLayer(styleId, layerId);
    if (composed) {
      rememberComposedLayer(composed);
      return { layerId: composed.layerId, originalVisible: composedOriginalVisible.get(composed.layerId) ?? true };
    }

    const styleLayer = host.listStyleLayers().find((layer) => layer.id === layerId);
    if (!styleLayer) {
      return null;
    }

    rememberStyleLayer(styleLayer);
    return { layerId: styleLayer.id, originalVisible: styleOriginalVisible.get(styleLayer.id) ?? true };
  }

  function rememberStyleLayer(layer: StyleLayerInfo): void {
    if (!styleOriginalVisible.has(layer.id)) {
      styleOriginalVisible.set(layer.id, layer.visible);
    }

    if (!styleBaselineFilters.has(layer.id)) {
      styleBaselineFilters.set(layer.id, layer.filter);
    }
  }

  function rememberComposedLayer(layer: { layerId: string; visible: boolean }): void {
    if (!composedOriginalVisible.has(layer.layerId)) {
      composedOriginalVisible.set(layer.layerId, layer.visible);
    }
  }

  function resolveTarget(target: VisibilityTarget): ResolvedLayer[] {
    switch (target.kind) {
      case "runtimeLayer":
        return target.layerIds.flatMap((layerId) => {
          const layer = host.getRuntimeLayer(layerId);
          return layer ? [{ layerId, originalVisible: layer.visible }] : [];
        });
      case "styleLayer": {
        if (target.layerIds.length === 0) {
          const composed = host.listComposedLayers(target.styleId);
          if (composed.length > 0) {
            return composed.map((layer) => {
              rememberComposedLayer(layer);
              return { layerId: layer.layerId, originalVisible: composedOriginalVisible.get(layer.layerId) ?? true };
            });
          }

          return host.listStyleLayers().map((layer) => {
            rememberStyleLayer(layer);
            return { layerId: layer.id, originalVisible: styleOriginalVisible.get(layer.id) ?? true };
          });
        }

        return target.layerIds.flatMap((layerId) => {
          const resolved = resolveStyleLayer(target.styleId, layerId);
          return resolved ? [resolved] : [];
        });
      }
      case "styleLayerTag": {
        const composed = host.listComposedLayers(target.styleId);
        if (composed.length > 0) {
          // composed layers cannot expose metadata through the registry; tags resolve
          // against the base style only (composed overlay layers resolve separately).
        }

        return host
          .listStyleLayers()
          .filter((layer) => layer.tags.some((tag) => target.tags.includes(tag)))
          .map((layer) => {
            rememberStyleLayer(layer);
            return { layerId: layer.id, originalVisible: styleOriginalVisible.get(layer.id) ?? true };
          });
      }
      case "styleLayerFeatures":
        // feature targets never toggle whole layers — handled by the filter pass
        return [];
    }
  }

  function visibilityRegistrationsFor(layerId: string): { hidden: boolean } {
    let hidden = false;

    for (const group of groups.values()) {
      if (!group.visible && group.targets.some((target) => resolveTarget(target).some((r) => r.layerId === layerId))) {
        hidden = true;
      }
    }

    for (const overlay of overlays.values()) {
      const inOverlayTargets = overlay.targets.some((target) =>
        resolveTarget(target).some((r) => r.layerId === layerId),
      );
      const containingParts = overlay.parts.filter((part) =>
        part.targets.some((target) => resolveTarget(target).some((r) => r.layerId === layerId)),
      );

      if (!inOverlayTargets && containingParts.length === 0) {
        continue;
      }

      if (!overlay.visible) {
        hidden = true;
      }

      if (containingParts.length > 0 && containingParts.every((part) => !part.visible)) {
        hidden = true;
      }
    }

    return { hidden };
  }

  function originalVisibleOf(layerId: string): boolean {
    const runtime = host.getRuntimeLayer(layerId);
    if (runtime) {
      return runtime.visible;
    }

    return composedOriginalVisible.get(layerId) ?? styleOriginalVisible.get(layerId) ?? true;
  }

  function applyVisibilityFor(layerId: string): void {
    if (!host.hasLayer(layerId)) {
      return;
    }

    const { hidden } = visibilityRegistrationsFor(layerId);
    host.setLayerVisibility(layerId, originalVisibleOf(layerId) && !hidden);
  }

  function hiddenFeatureFiltersFor(layerId: string): unknown[] {
    const filters: unknown[] = [];
    const collect = (visible: boolean, targets: VisibilityTarget[]) => {
      if (visible) {
        return;
      }

      for (const target of targets) {
        if (target.kind !== "styleLayerFeatures") {
          continue;
        }

        const matches = target.layerIds.some((id) => {
          if (host.getRuntimeLayer(id) && id === layerId) {
            return true;
          }

          return resolveStyleLayer(target.styleId, id)?.layerId === layerId;
        });
        if (matches) {
          filters.push(target.filter);
        }
      }
    };

    for (const group of groups.values()) {
      collect(group.visible, group.targets);
    }

    for (const overlay of overlays.values()) {
      collect(overlay.visible, overlay.targets);
      for (const part of overlay.parts) {
        collect(overlay.visible && part.visible, part.targets);
      }
    }

    return filters;
  }

  function applyFilterFor(layerId: string): void {
    if (!host.hasLayer(layerId)) {
      return;
    }

    const hidden = hiddenFeatureFiltersFor(layerId);
    if (hidden.length === 0 && !composedFilterLayers.has(layerId)) {
      // never composed onto this layer — leave its filter untouched
      return;
    }

    const baseline = host.getRuntimeLayer(layerId)
      ? host.getRuntimeBaselineFilter(layerId)
      : styleBaselineFilters.get(layerId);
    if (hidden.length === 0) {
      composedFilterLayers.delete(layerId);
      host.setLayerFilter(layerId, baseline ?? null);
      return;
    }

    composedFilterLayers.add(layerId);
    host.setLayerFilter(layerId, composeDisplayFilter(baseline, hidden));
  }

  function featureTargetLayerIds(targets: VisibilityTarget[]): string[] {
    return targets
      .filter((target): target is Extract<VisibilityTarget, { kind: "styleLayerFeatures" }> => {
        return target.kind === "styleLayerFeatures";
      })
      .flatMap((target) =>
        target.layerIds.map((id) =>
          host.getRuntimeLayer(id) ? id : (resolveStyleLayer(target.styleId, id)?.layerId ?? id),
        ),
      );
  }

  function affectedLayerIds(targets: VisibilityTarget[]): string[] {
    const layerIds = new Set<string>();
    for (const target of targets) {
      for (const resolved of resolveTarget(target)) {
        layerIds.add(resolved.layerId);
      }
    }

    for (const layerId of featureTargetLayerIds(targets)) {
      layerIds.add(layerId);
    }

    return [...layerIds];
  }

  function applyTargets(targets: VisibilityTarget[]): void {
    for (const layerId of affectedLayerIds(targets)) {
      applyVisibilityFor(layerId);
      applyFilterFor(layerId);
    }
  }

  function allRegisteredTargets(): VisibilityTarget[] {
    const targets: VisibilityTarget[] = [];
    for (const group of groups.values()) {
      targets.push(...group.targets);
    }

    for (const overlay of overlays.values()) {
      targets.push(...overlay.targets);
      for (const part of overlay.parts) {
        targets.push(...part.targets);
      }
    }

    return targets;
  }

  return {
    setGroup(id, visible, targets) {
      const previous = groups.get(id);
      groups.set(id, { visible, targets });
      applyTargets(previous ? [...previous.targets, ...targets] : targets);
    },
    removeGroup(id) {
      const previous = groups.get(id);
      groups.delete(id);
      if (previous) {
        applyTargets(previous.targets);
      }
    },
    setOverlay(id, visible, targets, parts) {
      const previous = overlays.get(id);
      overlays.set(id, { visible, targets, parts });
      const previousTargets = previous ? [...previous.targets, ...previous.parts.flatMap((p) => p.targets)] : [];
      applyTargets([...previousTargets, ...targets, ...parts.flatMap((part) => part.targets)]);
    },
    removeOverlay(id) {
      const previous = overlays.get(id);
      overlays.delete(id);
      if (previous) {
        applyTargets([...previous.targets, ...previous.parts.flatMap((part) => part.targets)]);
      }
    },
    onBaselineFilterChanged(layerId) {
      if (composedFilterLayers.has(layerId)) {
        applyFilterFor(layerId);
        return;
      }

      // no display filters active: apply the baseline directly
      host.setLayerFilter(layerId, host.getRuntimeBaselineFilter(layerId) ?? null);
    },
    onLayerAdded(layerId) {
      const targets = allRegisteredTargets();
      const affects =
        targets.some((target) => resolveTarget(target).some((resolved) => resolved.layerId === layerId)) ||
        featureTargetLayerIds(targets).includes(layerId);
      if (!affects) {
        return;
      }

      applyVisibilityFor(layerId);
      applyFilterFor(layerId);
    },
    replay() {
      // the new style starts fresh: recapture originals lazily
      styleBaselineFilters.clear();
      styleOriginalVisible.clear();
      composedOriginalVisible.clear();
      composedFilterLayers.clear();
      applyTargets(allRegisteredTargets());
    },
  };
}
