namespace Spillgebees.Blazor.Map.Models.Visibility;

/// <summary>
/// Stores shared layer visibility groups for a map.
/// </summary>
public sealed class MapLayerVisibilityState
{
    private readonly Dictionary<string, MapLayerVisibilityGroup> _groups = new(StringComparer.Ordinal);
    private IReadOnlyList<MapLayerVisibilityGroup> _snapshot = Array.AsReadOnly(Array.Empty<MapLayerVisibilityGroup>());

    /// <summary>
    /// Initializes a new layer visibility state.
    /// </summary>
    /// <param name="groups">Initial visibility groups.</param>
    public MapLayerVisibilityState(IEnumerable<MapLayerVisibilityGroup> groups)
    {
        ReplaceCore(groups);
    }

    /// <summary>
    /// Raised when a single group changes or when the group collection is replaced.
    /// </summary>
    public event EventHandler<MapLayerVisibilityChangedEventArgs>? Changed;

    /// <summary>
    /// Gets the current visibility groups.
    /// </summary>
    public IReadOnlyList<MapLayerVisibilityGroup> Groups => _snapshot;

    /// <summary>
    /// Returns whether a group exists.
    /// </summary>
    public bool Contains(string groupId) => _groups.ContainsKey(groupId);

    /// <summary>
    /// Attempts to get a visibility group.
    /// </summary>
    public bool TryGetGroup(string groupId, out MapLayerVisibilityGroup group) =>
        _groups.TryGetValue(groupId, out group!);

    /// <summary>
    /// Gets the current visibility value for a group.
    /// </summary>
    public bool IsVisible(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group))
        {
            throw new KeyNotFoundException($"Layer visibility group '{groupId}' was not found.");
        }

        return group.IsVisible;
    }

    /// <summary>
    /// Sets the visibility value for a group.
    /// </summary>
    public void SetVisible(string groupId, bool visible)
    {
        if (!_groups.TryGetValue(groupId, out var group))
        {
            throw new KeyNotFoundException($"Layer visibility group '{groupId}' was not found.");
        }

        if (group.IsVisible == visible)
        {
            return;
        }

        var updated = group with { IsVisible = visible };
        _groups[groupId] = updated;
        _snapshot = Array.AsReadOnly(_groups.Values.ToArray());
        Changed?.Invoke(
            this,
            new MapLayerVisibilityChangedEventArgs(
                MapLayerVisibilityChangeKind.GroupChanged,
                groupId,
                visible,
                updated
            )
        );
    }

    /// <summary>
    /// Toggles the visibility value for a group.
    /// </summary>
    public void Toggle(string groupId) => SetVisible(groupId, !IsVisible(groupId));

    /// <summary>
    /// Replaces all visibility groups.
    /// </summary>
    public void Replace(IEnumerable<MapLayerVisibilityGroup> groups)
    {
        ReplaceCore(groups);
        Changed?.Invoke(
            this,
            new MapLayerVisibilityChangedEventArgs(MapLayerVisibilityChangeKind.GroupsReplaced, null, null, null)
        );
    }

    private void ReplaceCore(IEnumerable<MapLayerVisibilityGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        var next = groups.ToArray();
        var duplicate = next
            .GroupBy(group => group.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Layer visibility group IDs must be unique. Duplicate ID: '{duplicate.Key}'.",
                nameof(groups)
            );
        }

        _groups.Clear();

        foreach (var group in next)
        {
            _groups[group.Id] = group;
        }

        _snapshot = Array.AsReadOnly(next);
    }
}
