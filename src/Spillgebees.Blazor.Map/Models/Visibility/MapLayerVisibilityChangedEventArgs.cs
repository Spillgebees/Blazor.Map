namespace Spillgebees.Blazor.Map;

/// <summary>
/// Provides data for a map layer visibility state change.
/// </summary>
public sealed record MapLayerVisibilityChangedEventArgs
{
    private MapLayerVisibilityChangedEventArgs(
        MapLayerVisibilityChangeKind changeKind,
        string? groupId,
        bool? isVisible,
        MapLayerVisibilityGroup? group
    )
    {
        if (group is not null && !string.Equals(groupId, group.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException("GroupId must match Group.Id when Group is provided.", nameof(groupId));
        }

        ChangeKind = changeKind;
        GroupId = groupId;
        IsVisible = isVisible;
        Group = group;
    }

    /// <summary>
    /// Gets the scope of the change.
    /// </summary>
    public MapLayerVisibilityChangeKind ChangeKind { get; }

    /// <summary>
    /// Gets the changed group ID for single-group changes.
    /// </summary>
    public string? GroupId { get; }

    /// <summary>
    /// Gets the changed visibility for single-group changes.
    /// </summary>
    public bool? IsVisible { get; }

    /// <summary>
    /// Gets the changed group for single-group changes.
    /// </summary>
    public MapLayerVisibilityGroup? Group { get; }

    /// <summary>
    /// Creates event data for a change tied to a visibility group.
    /// </summary>
    public static MapLayerVisibilityChangedEventArgs CreateForGroup(
        MapLayerVisibilityChangeKind changeKind,
        MapLayerVisibilityGroup group
    )
    {
        ArgumentNullException.ThrowIfNull(group);
        return new(changeKind, group.Id, group.IsVisible, group);
    }

    /// <summary>
    /// Creates event data for a change that is not tied to a group instance.
    /// </summary>
    public static MapLayerVisibilityChangedEventArgs CreateForStandalone(
        MapLayerVisibilityChangeKind changeKind,
        string? groupId,
        bool? isVisible
    ) => new(changeKind, groupId, isVisible, null);
}
