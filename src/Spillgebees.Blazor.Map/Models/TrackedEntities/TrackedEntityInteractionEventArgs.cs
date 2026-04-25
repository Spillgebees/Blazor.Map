using Spillgebees.Blazor.Map.Models.Events;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Typed interaction details for a tracked entity feature.
/// </summary>
public sealed record TrackedEntityInteractionEventArgs<TItem>(
    TrackedEntity<TItem> Entity,
    LayerFeatureEventArgs FeatureEvent,
    string? DecorationId = null
)
{
    /// <summary>
    /// The tracked entity ID.
    /// </summary>
    public string EntityId => Entity.Id;
}
