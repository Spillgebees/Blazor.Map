// Visibility controller for display items (C# MapDisplayItem → visibility.set ops).
//
// Rule: layers start as their style (or layer spec) has them. An item that is off hides
// every layer it targets; an item that is on shows the layers it names (by id or tag),
// even ones the style hides; off wins. Unset items (visible === null) leave their layers
// alone and only report the style's default back to C#. Whole-style targets can hide but
// never show. Filtered targets never toggle whole layers: while their item is off, the
// filter is negated and ANDed onto the layer's baseline filter.
//
// Originals: runtime layers read the engine layer store, composed overlay layers read
// their style JSON (composition registry), and base-style layers read a snapshot taken
// before the controller first touches the style (re-taken after a base style change), so
// the controller never mistakes its own writes for the style's defaults.

import type { VisibilityTarget } from "./ops";

export interface StyleLayerInfo {
  id: string;
  visible: boolean;
  filter: unknown;
  tags: string[];
}

export interface ComposedLayerInfo {
  /** Runtime (prefixed) layer id. */
  layerId: string;
  /** Layer id in the overlay style JSON. */
  originalLayerId: string;
  visible: boolean;
  filter: unknown;
  tags: string[];
}

export interface RuntimeLayerInfo {
  id: string;
  /** Id of the component that created the layer (entity layers, tile overlays, clusters). */
  owner: string | null;
  visible: boolean;
  filter: unknown;
}

/** Map surface + engine lookups the controller needs; injectable for tests. */
export interface VisibilityHost {
  listRuntimeLayers(): Iterable<RuntimeLayerInfo>;
  getRuntimeLayer(layerId: string): RuntimeLayerInfo | null;
  /** Live layers of the base style (excluding engine-managed and composed layers). */
  listBaseStyleLayers(): StyleLayerInfo[];
  /** Id of the base style, when the consumer gave it one. */
  baseStyleId(): string | null;
  /** Layers of a composed overlay style, or null when no such style is composed. */
  composedStyleLayers(styleId: string): ComposedLayerInfo[] | null;
  /** Whether the map was configured with a style of this id (base or overlay). */
  isKnownStyle(styleId: string): boolean;
  setLayerVisibility(layerId: string, visible: boolean): void;
  setLayerFilter(layerId: string, filter: unknown): void;
  hasLayer(layerId: string): boolean;
  warn?(message: string): void;
}

export interface VisibilityController {
  setGroup(id: string, visible: boolean | null, targets: VisibilityTarget[]): void;
  removeGroup(id: string): void;
  /** Recomposes the filter for a runtime layer whose baseline changed. */
  onBaselineFilterChanged(layerId: string): void;
  /** Applies registrations to a runtime layer that was just added. */
  onLayerAdded(layerId: string): void;
  onLayerRemoved(layerId: string): void;
  /** Reapplies everything; `styleChanged` re-snapshots the base style's originals. */
  replay(styleChanged: boolean): void;
  /** Style defaults of unset items ({ itemId: on }), or null when unchanged since the last call. */
  takeDefaults(): Record<string, boolean> | null;
}

interface GroupState {
  visible: boolean | null;
  targets: VisibilityTarget[];
}

interface ResolvedEntry {
  layerId: string;
  /** Whether the target names the layer (id/tag/component), i.e. may show it. */
  names: boolean;
  /** Feature filter for filtered targets; undefined for whole-layer targets. */
  filter: unknown;
}

interface Registration {
  group: GroupState;
  entry: ResolvedEntry;
}

export function composeDisplayFilter(baseline: unknown, hiddenFilters: unknown[]): unknown {
  if (hiddenFilters.length === 0) {
    return baseline ?? null;
  }

  const negated = hiddenFilters.map((filter) => ["!", filter]);
  return baseline == null ? ["all", ...negated] : ["all", baseline, ...negated];
}

export function layerTags(metadata: unknown): string[] {
  const meta = metadata as Record<string, unknown> | null | undefined;
  const tags = meta?.["sgb:tags"] ?? meta?.tags;
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
  /** Per-group resolved layers; cleared whenever the set of layers may have changed. */
  const resolved = new Map<string, ResolvedEntry[]>();
  let byLayer: Map<string, Registration[]> | null = null;
  /** Base-style originals, captured before the controller writes to the style. */
  let baseSnapshot: Map<string, StyleLayerInfo> | null = null;
  /** Originals of resolved style layers (base and composed), keyed by runtime layer id. */
  const styleOriginals = new Map<string, { visible: boolean; filter: unknown }>();
  /** Layers whose visibility the controller has set. */
  const touched = new Set<string>();
  /** Layers currently carrying a composed (baseline + hidden) filter. */
  const filteredLayers = new Set<string>();
  const warned = new Set<string>();
  let defaultsDirty = false;
  let lastDefaults: string | null = null;

  function warnOnce(key: string, message: string): void {
    if (warned.has(key)) {
      return;
    }

    warned.add(key);
    // biome-ignore lint/suspicious/noConsole: library warning for developers
    (host.warn ?? ((text: string) => console.warn(text)))(message);
  }

  function baseLayers(): Map<string, StyleLayerInfo> {
    if (!baseSnapshot) {
      baseSnapshot = new Map(host.listBaseStyleLayers().map((layer) => [layer.id, layer]));
    }

    return baseSnapshot;
  }

  /** Layers a style target resolves against, or null when the style isn't on the map. */
  function styleLayersOf(styleId: string): { layerId: string; originalLayerId: string; info: StyleLayerInfo }[] | null {
    const composed = host.composedStyleLayers(styleId);
    if (composed) {
      return composed.map((layer) => ({
        layerId: layer.layerId,
        originalLayerId: layer.originalLayerId,
        info: { id: layer.layerId, visible: layer.visible, filter: layer.filter ?? null, tags: layer.tags },
      }));
    }

    const baseId = host.baseStyleId();
    if (baseId !== null && baseId !== styleId) {
      return null;
    }

    return [...baseLayers().values()].map((info) => ({ layerId: info.id, originalLayerId: info.id, info }));
  }

  function isStyleSettled(styleId: string): boolean {
    return host.composedStyleLayers(styleId) !== null || host.baseStyleId() === styleId;
  }

  function resolveTarget(groupId: string, target: VisibilityTarget): ResolvedEntry[] {
    const filter = target.filter ?? undefined;
    if (target.kind === "layers") {
      const entries: ResolvedEntry[] = [];
      for (const layer of host.listRuntimeLayers()) {
        if (target.ids.includes(layer.id) || (layer.owner !== null && target.ids.includes(layer.owner))) {
          entries.push({ layerId: layer.id, names: true, filter });
        }
      }

      return entries;
    }

    const layers = styleLayersOf(target.styleId);
    if (!layers) {
      if (!host.isKnownStyle(target.styleId)) {
        warnOnce(
          `${groupId}\u0000${target.styleId}`,
          `[Spillgebees.Map] Display item '${groupId}' targets style '${target.styleId}', which is not one of the map's styles.`,
        );
      }

      return [];
    }

    const remember = (layer: { layerId: string; info: StyleLayerInfo }) =>
      styleOriginals.set(layer.layerId, { visible: layer.info.visible, filter: layer.info.filter });

    switch (target.kind) {
      case "style":
        return layers.map((layer) => {
          remember(layer);
          return { layerId: layer.layerId, names: false, filter };
        });
      case "styleTags":
        return layers
          .filter((layer) => layer.info.tags.some((tag) => target.tags.includes(tag)))
          .map((layer) => {
            remember(layer);
            return { layerId: layer.layerId, names: true, filter };
          });
      case "styleLayers": {
        const byOriginalId = new Map(layers.map((layer) => [layer.originalLayerId, layer]));
        return target.layerIds.flatMap((layerId) => {
          const layer = byOriginalId.get(layerId);
          if (!layer) {
            if (isStyleSettled(target.styleId)) {
              warnOnce(
                `${groupId}\u0000${target.styleId}\u0000${layerId}`,
                `[Spillgebees.Map] Display item '${groupId}' targets layer '${layerId}', which style '${target.styleId}' does not contain.`,
              );
            }

            return [];
          }

          remember(layer);
          return [{ layerId: layer.layerId, names: true, filter }];
        });
      }
    }
  }

  function resolveGroup(id: string): ResolvedEntry[] {
    const cached = resolved.get(id);
    if (cached) {
      return cached;
    }

    const group = groups.get(id);
    const entries = group ? group.targets.flatMap((target) => resolveTarget(id, target)) : [];
    resolved.set(id, entries);
    return entries;
  }

  function layerIndex(): Map<string, Registration[]> {
    if (byLayer) {
      return byLayer;
    }

    byLayer = new Map();
    for (const [id, group] of groups) {
      for (const entry of resolveGroup(id)) {
        const registrations = byLayer.get(entry.layerId);
        if (registrations) {
          registrations.push({ group, entry });
        } else {
          byLayer.set(entry.layerId, [{ group, entry }]);
        }
      }
    }

    return byLayer;
  }

  function invalidate(): void {
    resolved.clear();
    byLayer = null;
    defaultsDirty = true;
  }

  function originalOf(layerId: string): { visible: boolean; filter: unknown } {
    const runtime = host.getRuntimeLayer(layerId);
    if (runtime) {
      return { visible: runtime.visible, filter: runtime.filter };
    }

    return styleOriginals.get(layerId) ?? { visible: true, filter: null };
  }

  function applyLayer(layerId: string): void {
    if (!host.hasLayer(layerId)) {
      touched.delete(layerId);
      filteredLayers.delete(layerId);
      return;
    }

    const original = originalOf(layerId);
    let wholeLayer = false;
    let hide = false;
    let show = false;
    const hiddenFilters: unknown[] = [];
    for (const { group, entry } of layerIndex().get(layerId) ?? []) {
      if (entry.filter !== undefined) {
        if (group.visible === false) {
          hiddenFilters.push(entry.filter);
        }

        continue;
      }

      wholeLayer = true;
      if (group.visible === false) {
        hide = true;
      } else if (group.visible === true && entry.names) {
        show = true;
      }
    }

    if (wholeLayer) {
      touched.add(layerId);
      host.setLayerVisibility(layerId, !hide && (original.visible || show));
    } else if (touched.delete(layerId)) {
      host.setLayerVisibility(layerId, original.visible);
    }

    if (hiddenFilters.length > 0) {
      filteredLayers.add(layerId);
      host.setLayerFilter(layerId, composeDisplayFilter(original.filter, hiddenFilters));
    } else if (filteredLayers.delete(layerId)) {
      host.setLayerFilter(layerId, original.filter ?? null);
    }
  }

  function applyLayers(layerIds: Iterable<string>): void {
    for (const layerId of new Set(layerIds)) {
      applyLayer(layerId);
    }
  }

  return {
    setGroup(id, visible, targets) {
      const previous = groups.has(id) ? resolveGroup(id).map((entry) => entry.layerId) : [];
      groups.set(id, { visible, targets });
      resolved.delete(id);
      byLayer = null;
      defaultsDirty = true;
      applyLayers([...previous, ...resolveGroup(id).map((entry) => entry.layerId)]);
    },
    removeGroup(id) {
      if (!groups.has(id)) {
        return;
      }

      const previous = resolveGroup(id).map((entry) => entry.layerId);
      groups.delete(id);
      resolved.delete(id);
      byLayer = null;
      defaultsDirty = true;
      applyLayers(previous);
    },
    onBaselineFilterChanged(layerId) {
      if (filteredLayers.has(layerId)) {
        applyLayer(layerId);
        return;
      }

      // no display filters active: apply the baseline directly
      host.setLayerFilter(layerId, host.getRuntimeLayer(layerId)?.filter ?? null);
    },
    onLayerAdded(layerId) {
      if (groups.size === 0) {
        return;
      }

      invalidate();
      if (layerIndex().has(layerId)) {
        applyLayer(layerId);
      }
    },
    onLayerRemoved(layerId) {
      touched.delete(layerId);
      filteredLayers.delete(layerId);
      if (groups.size > 0) {
        invalidate();
      }
    },
    replay(styleChanged) {
      if (styleChanged) {
        // the new style starts fresh: nothing carries controller writes yet
        baseSnapshot = null;
        styleOriginals.clear();
        touched.clear();
        filteredLayers.clear();
      }

      invalidate();
      applyLayers([...layerIndex().keys(), ...touched, ...filteredLayers]);
    },
    takeDefaults() {
      if (!defaultsDirty) {
        return null;
      }

      defaultsDirty = false;
      const defaults: Record<string, boolean> = {};
      for (const [id, group] of groups) {
        if (group.visible !== null) {
          continue;
        }

        const layerIds = resolveGroup(id)
          .filter((entry) => entry.filter === undefined)
          .map((entry) => entry.layerId);
        if (layerIds.length > 0) {
          defaults[id] = layerIds.some((layerId) => originalOf(layerId).visible);
        }
      }

      const key = JSON.stringify(defaults);
      if (key === lastDefaults) {
        return null;
      }

      lastDefaults = key;
      return defaults;
    },
  };
}
