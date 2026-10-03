namespace Spillgebees.Blazor.Map;

/// <summary>
/// A named toggle for map content (layers, whole styles, or feature subsets), typically
/// shown as a legend or display-control switch.
/// </summary>
public sealed record MapDisplayItem
{
    /// <summary>
    /// Initializes a new map display item.
    /// </summary>
    public MapDisplayItem(
        string Id,
        IReadOnlyList<MapDisplayTarget> Targets,
        bool? IsOn = null,
        string? Label = null,
        string? Description = null
    )
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new ArgumentException("Display item IDs must be non-empty.", nameof(Id));
        }

        ArgumentNullException.ThrowIfNull(Targets);
        if (Targets.Count == 0)
        {
            throw new ArgumentException("Display items must declare at least one target.", nameof(Targets));
        }

        this.Id = Id;
        this.Targets = Array.AsReadOnly(Targets.ToArray());
        this.IsOn = IsOn;
        this.Label = Label;
        this.Description = Description;
    }

    /// <summary>Gets the stable display item ID.</summary>
    public string Id { get; }

    /// <summary>Gets display targets controlled by the item.</summary>
    public IReadOnlyList<MapDisplayTarget> Targets { get; }

    /// <summary>
    /// Gets whether the item is on: <c>true</c> shows the layers it names (even ones the
    /// style ships hidden), <c>false</c> hides everything it targets, and <c>null</c>
    /// (the default) leaves its layers as the style has them until the item is switched.
    /// Use <see cref="MapDisplayState.IsOn(string)"/> for the effective state.
    /// </summary>
    public bool? IsOn { get; init; }

    /// <summary>Gets the human-readable display label.</summary>
    public string? Label { get; init; }

    /// <summary>Gets optional descriptive text for controls.</summary>
    public string? Description { get; init; }
}
