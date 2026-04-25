namespace Spillgebees.Blazor.Map.Models.TrackedData;

/// <summary>
/// Behavior options for tracked data rendering.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedDataBehaviorOptions<TItem>(TrackedDataInteractionOptions<TItem> Interaction)
{
    /// <summary>
    /// Creates behavior options with default interaction selectors.
    /// </summary>
    public TrackedDataBehaviorOptions()
        : this(new TrackedDataInteractionOptions<TItem>()) { }
}
