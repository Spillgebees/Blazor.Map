namespace Spillgebees.Blazor.Map.Models.Visibility;

/// <summary>
/// Describes the scope of a layer visibility state change.
/// </summary>
public enum MapLayerVisibilityChangeKind
{
    /// <summary>
    /// A single visibility group changed.
    /// </summary>
    GroupChanged,

    /// <summary>
    /// The visibility group collection was replaced and consumers should reconcile the full state.
    /// </summary>
    GroupsReplaced,
}
