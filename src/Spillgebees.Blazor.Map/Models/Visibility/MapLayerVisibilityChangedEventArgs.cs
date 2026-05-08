namespace Spillgebees.Blazor.Map.Models.Visibility;

/// <summary>
/// Provides data for a map layer visibility state change.
/// </summary>
/// <param name="ChangeKind">The scope of the change.</param>
/// <param name="GroupId">The changed group ID for single-group changes.</param>
/// <param name="IsVisible">The changed visibility for single-group changes.</param>
/// <param name="Group">The changed group for single-group changes.</param>
public sealed record MapLayerVisibilityChangedEventArgs(
    MapLayerVisibilityChangeKind ChangeKind,
    string? GroupId,
    bool? IsVisible,
    MapLayerVisibilityGroup? Group
);
