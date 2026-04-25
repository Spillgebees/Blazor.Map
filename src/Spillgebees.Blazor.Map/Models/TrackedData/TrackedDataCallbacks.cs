using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Models.TrackedData;

/// <summary>
/// Callback delegates for tracked data interactions.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedDataCallbacks<TItem>(
    Func<TrackedEntityInteractionEventArgs<TItem>, Task>? OnItemClick,
    Func<TrackedEntityInteractionEventArgs<TItem>, Task>? OnItemMouseEnter,
    Func<Task>? OnItemMouseLeave,
    Func<Task>? BeforeShowPopupAsync
)
{
    /// <summary>
    /// Creates callback options with no handlers.
    /// </summary>
    public TrackedDataCallbacks()
        : this(null, null, null, null) { }
}
