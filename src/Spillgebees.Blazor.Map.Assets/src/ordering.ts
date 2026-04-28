import type { RegisteredMapLayer } from "./interfaces/spillgebees";

type OrderedNode = {
  id: string;
  rank: number;
  kind: "custom" | "native";
};

type LayerPlan = {
  layerId: string;
  beforeLayerId: string | null;
};

export function resolveLayerOrder(layers: RegisteredMapLayer[], styleLayerIds: string[]): string[] {
  const fullOrder = resolveFullOrder(layers, styleLayerIds);

  return fullOrder.filter((node) => node.kind === "custom").map((node) => node.id);
}

export function buildLayerPlan(layers: RegisteredMapLayer[], styleLayerIds: string[]): LayerPlan[] {
  const fullOrder = resolveFullOrder(layers, styleLayerIds);
  const customNodeIndexes = new Map<string, number>();
  const opaqueAnchors = new Map<string, string>();

  for (let index = 0; index < fullOrder.length; index++) {
    const node = fullOrder[index];
    if (node.kind === "custom") {
      customNodeIndexes.set(node.id, index);
    }
  }

  for (const layer of layers) {
    const effectiveBeforeLayerId = layer.imperativeBeforeLayerId ?? layer.beforeLayerId;
    if (!effectiveBeforeLayerId) {
      continue;
    }

    const isKnownCustom = layers.some((candidate) => candidate.layerId === effectiveBeforeLayerId);
    const isKnownNative = styleLayerIds.includes(effectiveBeforeLayerId);
    if (!isKnownCustom && !isKnownNative) {
      opaqueAnchors.set(layer.layerId, effectiveBeforeLayerId);
    }
  }

  return layers
    .map((layer) => {
      const layerIndex = customNodeIndexes.get(layer.layerId);
      if (layerIndex === undefined) {
        throw new Error(`Layer '${layer.layerId}' was not included in the resolved order.`);
      }

      return {
        layerId: layer.layerId,
        beforeLayerId: opaqueAnchors.get(layer.layerId) ?? fullOrder[layerIndex + 1]?.id ?? null,
      };
    })
    .sort((left, right) => {
      return (customNodeIndexes.get(left.layerId) ?? 0) - (customNodeIndexes.get(right.layerId) ?? 0);
    });
}

function resolveFullOrder(layers: RegisteredMapLayer[], styleLayerIds: string[]): OrderedNode[] {
  const customIds = new Set(layers.map((layer) => layer.layerId));
  const nativeLayerIds = styleLayerIds.filter((layerId) => !customIds.has(layerId));
  const nativeLayerIdSet = new Set(nativeLayerIds);
  const nodes = new Map<string, OrderedNode>();
  const adjacency = new Map<string, Set<string>>();
  const indegree = new Map<string, number>();
  const layersById = new Map(layers.map((layer) => [layer.layerId, layer]));
  const layerGroups = new Map<string, RegisteredMapLayer[]>();

  function ensureNode(node: OrderedNode): void {
    if (nodes.has(node.id)) {
      return;
    }

    nodes.set(node.id, node);
    adjacency.set(node.id, new Set());
    indegree.set(node.id, 0);
  }

  function addEdge(fromId: string, toId: string): void {
    if (fromId === toId) {
      throw new Error(`Layer ordering cannot reference itself: '${fromId}'.`);
    }

    const targets = adjacency.get(fromId);
    if (!targets || targets.has(toId)) {
      return;
    }

    targets.add(toId);
    indegree.set(toId, (indegree.get(toId) ?? 0) + 1);
  }

  for (let index = 0; index < nativeLayerIds.length; index++) {
    ensureNode({
      id: nativeLayerIds[index],
      rank: index,
      kind: "native",
    });
  }

  for (let index = 1; index < nativeLayerIds.length; index++) {
    addEdge(nativeLayerIds[index - 1], nativeLayerIds[index]);
  }

  for (const layer of layers) {
    ensureNode({
      id: layer.layerId,
      rank: nativeLayerIds.length + layer.ordering.declarationOrder,
      kind: "custom",
    });

    const layerGroup = layer.ordering.layerGroup;
    if (!layerGroup) {
      continue;
    }

    const groupedLayers = layerGroups.get(layerGroup) ?? [];
    groupedLayers.push(layer);
    layerGroups.set(layerGroup, groupedLayers);
  }

  for (const groupedLayers of layerGroups.values()) {
    groupedLayers.sort((left, right) => left.ordering.declarationOrder - right.ordering.declarationOrder);

    for (let index = 1; index < groupedLayers.length; index++) {
      addEdge(groupedLayers[index - 1].layerId, groupedLayers[index].layerId);
    }
  }

  for (const layer of layers) {
    const effectiveBeforeLayerId = layer.imperativeBeforeLayerId ?? layer.beforeLayerId;
    const respectsLayerGroupAnchors = effectiveBeforeLayerId == null;

    if (respectsLayerGroupAnchors && layer.ordering.beforeLayerGroup) {
      const targetGroupedLayers = layerGroups.get(layer.ordering.beforeLayerGroup);
      if (targetGroupedLayers && targetGroupedLayers.length > 0) {
        for (const target of targetGroupedLayers) {
          addEdge(layer.layerId, target.layerId);
        }
      }
    }

    if (respectsLayerGroupAnchors && layer.ordering.afterLayerGroup) {
      const targetGroupedLayers = layerGroups.get(layer.ordering.afterLayerGroup);
      if (targetGroupedLayers && targetGroupedLayers.length > 0) {
        for (const target of targetGroupedLayers) {
          addEdge(target.layerId, layer.layerId);
        }
      }
    }

    if (effectiveBeforeLayerId === undefined || effectiveBeforeLayerId === null) {
      const lastNativeLayerId = nativeLayerIds.at(-1);
      if (lastNativeLayerId) {
        addEdge(lastNativeLayerId, layer.layerId);
      }

      continue;
    }

    if (layersById.has(effectiveBeforeLayerId)) {
      addEdge(layer.layerId, effectiveBeforeLayerId);
      continue;
    }

    if (!nativeLayerIdSet.has(effectiveBeforeLayerId)) {
      continue;
    }

    const nativeIndex = nativeLayerIds.indexOf(effectiveBeforeLayerId);
    const previousNativeLayerId = nativeIndex > 0 ? nativeLayerIds[nativeIndex - 1] : null;

    if (previousNativeLayerId) {
      addEdge(previousNativeLayerId, layer.layerId);
    }

    addEdge(layer.layerId, effectiveBeforeLayerId);
  }

  const queue = Array.from(nodes.values())
    .filter((node) => (indegree.get(node.id) ?? 0) === 0)
    .sort(compareNodes);
  const ordered: OrderedNode[] = [];

  while (queue.length > 0) {
    const node = queue.shift();
    if (!node) {
      break;
    }

    ordered.push(node);

    for (const targetId of adjacency.get(node.id) ?? []) {
      const nextIndegree = (indegree.get(targetId) ?? 0) - 1;
      indegree.set(targetId, nextIndegree);

      if (nextIndegree === 0) {
        const targetNode = nodes.get(targetId);
        if (targetNode) {
          queue.push(targetNode);
          queue.sort(compareNodes);
        }
      }
    }
  }

  if (ordered.length !== nodes.size) {
    const unresolvedCustomLayers = layers
      .filter((layer) => !ordered.some((node) => node.id === layer.layerId))
      .map((layer) => layer.layerId);

    throw new Error(`Cyclic layer ordering detected: ${unresolvedCustomLayers.join(", ")}.`);
  }

  return ordered;
}

function compareNodes(left: OrderedNode, right: OrderedNode): number {
  return left.rank - right.rank || left.id.localeCompare(right.id);
}
