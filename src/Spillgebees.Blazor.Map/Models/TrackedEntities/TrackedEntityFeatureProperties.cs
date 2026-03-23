namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Canonical tracked entity GeoJSON property names for MapLibre expressions.
/// </summary>
public static class TrackedEntityFeatureProperties
{
    public const string Kind = "kind";
    public const string EntityId = "entityId";
    public const string DecorationId = "decorationId";
    public const string Color = "color";
    public const string IconImage = "iconImage";
    public const string IconSize = "iconSize";
    public const string IconRotation = "iconRotation";
    public const string Anchor = "anchor";
    public const string Offset = "offset";
    public const string HoverScale = "hoverScale";
    public const string HoverRaise = "hoverRaise";
    public const string RenderOrder = "renderOrder";
    public const string Text = "text";
    public const string TextSize = "textSize";
    public const string DisplayMode = "displayMode";
    public const string HaloColor = "haloColor";
    public const string HaloWidth = "haloWidth";
    public const string IconColor = "iconColor";
    public const string Metadata = "metadata";

    internal static readonly ISet<string> Reserved = new HashSet<string>(StringComparer.Ordinal)
    {
        Kind,
        EntityId,
        DecorationId,
        Color,
        IconImage,
        IconSize,
        IconRotation,
        Anchor,
        Offset,
        HoverScale,
        HoverRaise,
        RenderOrder,
        Text,
        TextSize,
        DisplayMode,
        HaloColor,
        HaloWidth,
        IconColor,
        Metadata,
    };
}
