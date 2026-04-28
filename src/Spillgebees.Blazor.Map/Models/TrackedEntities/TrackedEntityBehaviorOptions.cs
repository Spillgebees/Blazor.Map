namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Behavior options for tracked entity rendering.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedEntityBehaviorOptions<TItem>(TrackedEntityInteractionOptions<TItem> Interaction)
{
    /// <summary>
    /// Creates behavior options with default interaction selectors.
    /// </summary>
    public TrackedEntityBehaviorOptions()
        : this(new TrackedEntityInteractionOptions<TItem>()) { }
}
