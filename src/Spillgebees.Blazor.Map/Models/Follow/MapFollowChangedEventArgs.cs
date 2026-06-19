namespace Spillgebees.Blazor.Map;

/// <summary>Describes a follow state transition raised through <see cref="SgbMap.OnFollowChanged"/>.</summary>
/// <param name="Follow">The active follow after the transition, or null when follow was cleared.</param>
/// <param name="Reason">Why the follow state changed.</param>
public sealed record MapFollowChangedEventArgs(MapFollowOptions? Follow, MapFollowChangeReason Reason);
