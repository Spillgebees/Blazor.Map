namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// A decoration that follows a tracked entity using the same position with relative offsets.
/// </summary>
public sealed record TrackedEntityDecoration
{
    /// <summary>
    /// Creates a new tracked entity decoration.
    /// </summary>
    public TrackedEntityDecoration(
        string id,
        string? text = null,
        string? iconImage = null,
        Point? offset = null,
        string? anchor = null,
        TrackedEntityDecorationDisplayMode displayMode = TrackedEntityDecorationDisplayMode.Always,
        string? color = null,
        double? textSize = null,
        double? iconSize = null,
        double? rotation = null,
        double? renderOrder = null,
        string? haloColor = null,
        double? haloWidth = null,
        string? iconColor = null
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Tracked entity decoration ID must not be empty.", nameof(id));
        }

        Id = id;
        Text = text;
        IconImage = iconImage;
        Offset = offset;
        Anchor = anchor;
        DisplayMode = displayMode;
        Color = color;
        TextSize = textSize;
        IconSize = iconSize;
        Rotation = rotation;
        RenderOrder = renderOrder;
        HaloColor = haloColor;
        HaloWidth = haloWidth;
        IconColor = iconColor;
    }

    /// <summary>
    /// Stable decoration ID unique within the entity.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Optional GPU-rendered text.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Optional registered image name.
    /// </summary>
    public string? IconImage { get; }

    /// <summary>
    /// Optional relative offset.
    /// </summary>
    public Point? Offset { get; }

    /// <summary>
    /// Optional MapLibre-style anchor.
    /// </summary>
    public string? Anchor { get; }

    /// <summary>
    /// Display intent for the decoration.
    /// </summary>
    public TrackedEntityDecorationDisplayMode DisplayMode { get; }

    /// <summary>
    /// Optional shared color hint.
    /// </summary>
    public string? Color { get; }

    /// <summary>
    /// Optional text size hint.
    /// </summary>
    public double? TextSize { get; }

    /// <summary>
    /// Optional icon size hint.
    /// </summary>
    public double? IconSize { get; }

    /// <summary>
    /// Optional clockwise rotation in degrees.
    /// </summary>
    public double? Rotation { get; }

    /// <summary>
    /// Optional explicit sort key hint.
    /// </summary>
    public double? RenderOrder { get; }

    /// <summary>
    /// Optional text halo color.
    /// </summary>
    public string? HaloColor { get; }

    /// <summary>
    /// Optional text halo width in pixels.
    /// </summary>
    public double? HaloWidth { get; }

    /// <summary>
    /// Optional icon color tint (for SDF icons).
    /// </summary>
    public string? IconColor { get; }
}
