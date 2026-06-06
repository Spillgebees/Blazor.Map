namespace Spillgebees.Blazor.Map;

/// <summary>
/// Defines a named set of layers controlled by one visibility value.
/// </summary>
public sealed record MapLayerVisibilityGroup
{
    /// <summary>
    /// Initializes a new layer visibility group.
    /// </summary>
    /// <param name="Id">Stable group identifier.</param>
    /// <param name="Targets">The layer targets controlled by the group.</param>
    /// <param name="IsVisible">Initial visibility value.</param>
    /// <param name="Label">Optional display label.</param>
    /// <param name="Description">Optional display description.</param>
    public MapLayerVisibilityGroup(
        string Id,
        IReadOnlyList<MapLayerVisibilityTarget> Targets,
        bool IsVisible = true,
        string? Label = null,
        string? Description = null
    )
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new ArgumentException("Layer visibility group IDs must be non-empty.", nameof(Id));
        }

        ArgumentNullException.ThrowIfNull(Targets);

        if (Targets.Count == 0)
        {
            throw new ArgumentException("Visibility groups must declare at least one target.", nameof(Targets));
        }

        this.Id = Id;
        this.Targets = Array.AsReadOnly(Targets.ToArray());
        this.IsVisible = IsVisible;
        this.Label = Label;
        this.Description = Description;
    }

    /// <summary>
    /// Gets the stable group identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the layer targets controlled by the group.
    /// </summary>
    public IReadOnlyList<MapLayerVisibilityTarget> Targets { get; }

    /// <summary>
    /// Gets whether the group is currently visible.
    /// </summary>
    public bool IsVisible { get; init; }

    /// <summary>
    /// Gets an optional display label.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Gets an optional display description.
    /// </summary>
    public string? Description { get; }
}
