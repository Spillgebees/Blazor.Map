using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Callback delegates for tracked entity interactions.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedEntityCallbacks<TItem>(
    Func<TrackedEntityInteractionEventArgs<TItem>, Task>? OnItemClick,
    Func<TrackedEntityInteractionEventArgs<TItem>, Task>? OnItemMouseEnter,
    Func<Task>? OnItemMouseLeave,
    Func<Task>? OnBeforeShowPopup
)
{
    /// <summary>
    /// Creates callback options with no handlers.
    /// </summary>
    public TrackedEntityCallbacks()
        : this(null, null, null, null) { }
}
