namespace Spillgebees.Blazor.Map.Engine;

/// <summary>Slot ids on the wire, matching <c>engine/slots.ts</c>.</summary>
internal static class EngineSlots
{
    public const string BelowLabels = "below-labels";
    public const string TileOverlays = "sgb:tile-overlays";

    /// <summary>Layer components above labels paint at the top of the map, which needs no slot.</summary>
    public static string? ForLayer(LayerSlot slot) => slot is LayerSlot.BelowLabels ? BelowLabels : null;

    /// <summary>Tile overlays below labels paint under every vector layer the map adds.</summary>
    public static string? ForTileOverlay(LayerSlot slot) => slot is LayerSlot.BelowLabels ? TileOverlays : null;
}
