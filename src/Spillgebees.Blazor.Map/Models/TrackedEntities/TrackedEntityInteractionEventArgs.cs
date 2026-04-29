using Spillgebees.Blazor.Map.Models.Events;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Typed interaction details for a tracked entity feature.
/// </summary>
public sealed record TrackedEntityInteractionEventArgs<TItem>(
    string EntityId,
    TItem? Item,
    Coordinate Position,
    LayerFeatureEventArgs FeatureEvent,
    string? DecorationId = null,
    IReadOnlyDictionary<string, object?>? Properties = null
);
