namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// <see cref="MapDisplayState"/> items → visibility ops. Items removed from the state
/// release their visibility group.
/// </summary>
internal sealed class MapDisplayCoordinator(MapEngineChannel channel)
{
    private readonly HashSet<string> _syncedItemIds = [];

    public void Sync(MapDisplayState? display)
    {
        var currentIds = new HashSet<string>();
        foreach (var item in display?.Items ?? [])
        {
            currentIds.Add(item.Id);
            channel.Queue(
                new VisibilitySetOp(item.Id, item.IsOn, [.. item.Targets.Select(EngineVisibilityTarget.From)])
            );
        }

        foreach (var removedId in _syncedItemIds.Where(id => !currentIds.Contains(id)).ToArray())
        {
            channel.Queue(new VisibilityRemoveOp(removedId));
        }

        _syncedItemIds.Clear();
        _syncedItemIds.UnionWith(currentIds);
    }
}
