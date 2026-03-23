import type { RegisteredMapLayer } from "./interfaces/spillgebees";

type OrderedNode = {
  id: string;
  rank: number;
  kind: "custom" | "native";
};

type LayerPlan = {
  layerId: string;
  beforeId: string | null;
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
    const effectiveBeforeId = layer.imperativeBeforeId ?? layer.beforeId;
    if (!effectiveBeforeId) {
      continue;
    }

    const isKnownCustom = layers.some((candidate) => candidate.layerId === effectiveBeforeId);
    const isKnownNative = styleLayerIds.includes(effectiveBeforeId);
    if (!isKnownCustom && !isKnownNative) {
      opaqueAnchors.set(layer.layerId, effectiveBeforeId);
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
        beforeId: opaqueAnchors.get(layer.layerId) ?? fullOrder[layerIndex + 1]?.id ?? null,
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
  const stacks = new Map<string, RegisteredMapLayer[]>();

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

    const stack = layer.ordering.stack;
    if (!stack) {
      continue;
    }

    const stackLayers = stacks.get(stack) ?? [];
    stackLayers.push(layer);
    stacks.set(stack, stackLayers);
  }

  for (const stackLayers of stacks.values()) {
    stackLayers.sort((left, right) => left.ordering.declarationOrder - right.ordering.declarationOrder);

    for (let index = 1; index < stackLayers.length; index++) {
      addEdge(stackLayers[index - 1].layerId, stackLayers[index].layerId);
    }
  }

  for (const layer of layers) {
    const effectiveBeforeId = layer.imperativeBeforeId ?? layer.beforeId;
    const respectsStackAnchors = effectiveBeforeId == null;

    if (respectsStackAnchors && layer.ordering.beforeStack) {
      const targetStackLayers = stacks.get(layer.ordering.beforeStack);
      if (targetStackLayers && targetStackLayers.length > 0) {
        for (const target of targetStackLayers) {
          addEdge(layer.layerId, target.layerId);
        }
      }
    }

    if (respectsStackAnchors && layer.ordering.afterStack) {
      const targetStackLayers = stacks.get(layer.ordering.afterStack);
      if (targetStackLayers && targetStackLayers.length > 0) {
        for (const target of targetStackLayers) {
          addEdge(target.layerId, layer.layerId);
        }
      }
    }

    if (effectiveBeforeId === undefined || effectiveBeforeId === null) {
      const lastNativeLayerId = nativeLayerIds.at(-1);
      if (lastNativeLayerId) {
        addEdge(lastNativeLayerId, layer.layerId);
      }

      continue;
    }

    if (layersById.has(effectiveBeforeId)) {
      addEdge(layer.layerId, effectiveBeforeId);
      continue;
    }

    if (!nativeLayerIdSet.has(effectiveBeforeId)) {
      continue;
    }

    const nativeIndex = nativeLayerIds.indexOf(effectiveBeforeId);
    const previousNativeLayerId = nativeIndex > 0 ? nativeLayerIds[nativeIndex - 1] : null;

    if (previousNativeLayerId) {
      addEdge(previousNativeLayerId, layer.layerId);
    }

    addEdge(layer.layerId, effectiveBeforeId);
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
