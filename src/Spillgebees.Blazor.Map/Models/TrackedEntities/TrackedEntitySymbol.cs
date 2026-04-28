using Spillgebees.Blazor.Map.Models.Options;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Primary symbol configuration for a tracked entity.
/// </summary>
public sealed record TrackedEntitySymbol
{
    /// <summary>
    /// Creates a new tracked entity symbol.
    /// </summary>
    public TrackedEntitySymbol(
        string iconImage,
        double? size = null,
        double? rotation = null,
        SymbolAnchor? anchor = null,
        Point? offset = null
    )
    {
        if (string.IsNullOrWhiteSpace(iconImage))
        {
            throw new ArgumentException("Tracked entity symbol icon image must not be empty.", nameof(iconImage));
        }

        IconImage = iconImage;
        Size = size;
        Rotation = rotation;
        Anchor = anchor;
        Offset = offset;
    }

    /// <summary>
    /// Registered image name for the entity symbol.
    /// </summary>
    public string IconImage { get; }

    /// <summary>
    /// Optional symbol scale factor.
    /// </summary>
    public double? Size { get; }

    /// <summary>
    /// Optional clockwise rotation in degrees.
    /// </summary>
    public double? Rotation { get; }

    /// <summary>
    /// Optional MapLibre-style symbol anchor.
    /// </summary>
    public SymbolAnchor? Anchor { get; }

    /// <summary>
    /// Optional icon offset in pixels.
    /// </summary>
    public Point? Offset { get; }
}
