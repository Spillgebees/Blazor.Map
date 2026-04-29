using System.Collections.ObjectModel;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// A high-frequency tracked entity with a stable ID, primary symbol, and optional companion decorations.
/// </summary>
/// <typeparam name="TItem">Optional strongly typed domain item associated with the entity.</typeparam>
internal sealed record TrackedEntity<TItem>
{
    /// <summary>
    /// Creates a new tracked entity.
    /// </summary>
    public TrackedEntity(
        string id,
        Coordinate position,
        TrackedEntitySymbol symbol,
        string? color = null,
        TrackedEntityHoverIntent? hover = null,
        double? renderOrder = null,
        IReadOnlyList<TrackedEntityDecoration>? decorations = null,
        TItem? item = default,
        IReadOnlyDictionary<string, object?>? properties = null
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Tracked entity ID must not be empty.", nameof(id));
        }

        Id = id;
        Position = position;
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Color = color;
        Hover = hover;
        RenderOrder = renderOrder;
        Decorations = new ReadOnlyCollection<TrackedEntityDecoration>((decorations ?? []).ToList());
        Item = item;
        Properties = properties is null ? null : new ReadOnlyDictionary<string, object?>(properties.ToDictionary());
    }

    /// <summary>
    /// Stable entity ID used for diffing and feature identity.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Current geographic position.
    /// </summary>
    public Coordinate Position { get; }

    /// <summary>
    /// Primary symbol definition.
    /// </summary>
    public TrackedEntitySymbol Symbol { get; }

    /// <summary>
    /// Optional entity color used by style expressions.
    /// </summary>
    public string? Color { get; }

    /// <summary>
    /// Hover interaction intent for future renderer behavior.
    /// </summary>
    public TrackedEntityHoverIntent? Hover { get; }

    /// <summary>
    /// Optional explicit render ordering hint.
    /// </summary>
    public double? RenderOrder { get; }

    /// <summary>
    /// Companion decorations that follow the entity.
    /// </summary>
    public IReadOnlyList<TrackedEntityDecoration> Decorations { get; }

    /// <summary>
    /// Optional strongly typed domain item payload.
    /// </summary>
    public TItem? Item { get; }

    /// <summary>
    /// Additional flat GeoJSON properties for raw interop and custom styling.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Properties { get; }
}
