namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// <see cref="MapDisplayState"/> items → visibility ops. Only items that changed since the
/// last sync are sent; items removed from the state release their visibility group.
/// </summary>
internal sealed class MapDisplayCoordinator(MapEngineChannel channel)
{
    private readonly Dictionary<string, MapDisplayItem> _synced = new(StringComparer.Ordinal);

    public void Sync(MapDisplayState? display)
    {
        var items = display?.Items ?? [];
        var currentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            currentIds.Add(item.Id);
            if (_synced.TryGetValue(item.Id, out var synced) && synced == item)
            {
                continue;
            }

            _synced[item.Id] = item;
            channel.Queue(
                new VisibilitySetOp(item.Id, item.IsOn, [.. item.Targets.Select(EngineVisibilityTarget.From)])
            );
        }

        foreach (var removedId in _synced.Keys.Where(id => !currentIds.Contains(id)).ToArray())
        {
            _synced.Remove(removedId);
            channel.Queue(new VisibilityRemoveOp(removedId));
        }
    }
}
