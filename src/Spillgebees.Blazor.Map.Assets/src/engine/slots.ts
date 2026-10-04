// Slots are fixed positions in the paint order, relative to the base style's labels.
// Each slot is a hidden background layer (an anchor); a layer inserted before an
// anchor paints below everything after it, no matter when it was added.

/** Prefix for the hidden anchor layers that make up the slots. */
export const SLOT_LAYER_PREFIX = "sgb-slot:";

/**
 * Prefix for the sources and layers of composed styles (styles/composition.ts). The
 * value predates the "composed style" naming and stays as is, since runtime layer
 * ids show up in queried features.
 */
export const COMPOSED_STYLE_PREFIX = "sgb-overlay-style";

/** A slot that layers can opt into, as written on the wire and in style metadata. */
export type LayerSlot = "above-labels" | "below-labels";

/** Tile overlays: right above the base style's ground, below every vector layer we add. */
export const TILE_OVERLAYS_SLOT = "sgb:tile-overlays";

/** Composed styles' ground layers (tracks, route lines, platform fills). */
export const COMPOSED_GROUND_SLOT = "sgb:composed-ground";

/** Layer components with `LayerSlot.BelowLabels`, such as 3D buildings. */
export const BELOW_LABELS_SLOT = "below-labels";

/** Composed styles' label and icon layers: above the base labels, below layer components. */
export const COMPOSED_LABELS_SLOT = "sgb:composed-labels";

/** How an anchor's position is derived from the base style. */
export type SlotAnchor = "first-label" | "top";

/** The slots, bottom to top. Anchors with the same position stack in this order. */
export const SLOTS: readonly { id: string; anchor: SlotAnchor }[] = [
  { id: TILE_OVERLAYS_SLOT, anchor: "first-label" },
  { id: COMPOSED_GROUND_SLOT, anchor: "first-label" },
  { id: BELOW_LABELS_SLOT, anchor: "first-label" },
  { id: COMPOSED_LABELS_SLOT, anchor: "top" },
];

const slotIds = new Set(SLOTS.map((slot) => slot.id));

export function isSlotId(slotId: string): boolean {
  return slotIds.has(slotId);
}

export function slotAnchorLayerId(slotId: string): string {
  return `${SLOT_LAYER_PREFIX}${slotId}`;
}

/** Labels are symbol layers that draw text; icon-only symbols (one-way arrows) sit among the roads. */
export function isLabelLayer(layer: { type?: string; layout?: Record<string, unknown> }): boolean {
  return layer.type === "symbol" && layer.layout?.["text-field"] !== undefined;
}
