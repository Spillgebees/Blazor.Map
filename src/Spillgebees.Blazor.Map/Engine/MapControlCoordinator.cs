namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Control definitions → ops. Components register typed definitions per owner; Sync
/// diffs them by record equality into control.set/control.remove ops. Custom-shell
/// content binds via control.content ops — the engine resolves the DOM by convention,
/// and panel open-state callbacks ride engine event handler ids.
/// </summary>
internal sealed class MapControlCoordinator(
    MapEngineChannel channel,
    MapEngineEventRouter router,
    Func<Task> onCenterRequestedAsync
)
{
    private int? _centerClickHandlerId;

    private sealed class RegisteredControl(string ownerId, MapControlDefinition control)
    {
        public string OwnerId { get; } = ownerId;
        public MapControlDefinition Control { get; set; } = control;
    }

    private readonly List<RegisteredControl> _registeredControls = [];
    private readonly Dictionary<string, EngineControl> _syncedControls = [];
    private readonly Dictionary<string, int> _panelOpenHandlers = [];

    public bool Register(string ownerId, MapControlDefinition control)
    {
        var existing = _registeredControls.FirstOrDefault(entry => entry.OwnerId == ownerId);
        if (existing is not null)
        {
            if (existing.Control == control)
            {
                return false;
            }

            existing.Control = control;
            return true;
        }

        _registeredControls.Add(new RegisteredControl(ownerId, control));
        return true;
    }

    public bool Unregister(string controlId)
    {
        var index = _registeredControls.FindIndex(entry =>
            string.Equals(entry.Control.ControlId, controlId, StringComparison.Ordinal)
        );
        if (index < 0)
        {
            return false;
        }

        _registeredControls.RemoveAt(index);
        return true;
    }

    public bool UnregisterByOwner(string ownerId)
    {
        var index = _registeredControls.FindIndex(entry => entry.OwnerId == ownerId);
        if (index < 0)
        {
            return false;
        }

        _registeredControls.RemoveAt(index);
        return true;
    }

    public void Sync()
    {
        var desired = new Dictionary<string, EngineControl>();
        foreach (var entry in _registeredControls)
        {
            if (entry.Control is CenterControlDefinition)
            {
                _centerClickHandlerId ??= router.Register(_ => onCenterRequestedAsync());
            }

            desired[entry.Control.ControlId] = EngineControl.From(entry.Control, _centerClickHandlerId);
        }

        List<string>? removedIds = null;
        foreach (var id in _syncedControls.Keys)
        {
            if (!desired.ContainsKey(id))
            {
                (removedIds ??= []).Add(id);
            }
        }

        foreach (var id in removedIds ?? [])
        {
            _syncedControls.Remove(id);
            channel.Queue(new ControlRemoveOp(id));
        }

        foreach (var (id, control) in desired)
        {
            if (!_syncedControls.TryGetValue(id, out var synced) || synced != control)
            {
                _syncedControls[id] = control;
                channel.Queue(new ControlSetOp(control));
            }
        }
    }

    public void SetContent(string controlId, Func<bool, Task>? onPanelOpenChangedAsync)
    {
        EngineControlEvents? events = null;
        if (onPanelOpenChangedAsync is not null)
        {
            if (_panelOpenHandlers.Remove(controlId, out var previousHandlerId))
            {
                router.Unregister(previousHandlerId);
            }

            var handlerId = router.Register(payload =>
                onPanelOpenChangedAsync(payload.GetProperty("open").GetBoolean())
            );
            _panelOpenHandlers[controlId] = handlerId;
            events = new EngineControlEvents(OpenChanged: handlerId);
        }

        channel.Queue(new ControlContentOp(controlId, events));
    }

    public void RemoveContent(string controlId)
    {
        if (_panelOpenHandlers.Remove(controlId, out var handlerId))
        {
            router.Unregister(handlerId);
        }

        channel.Queue(new ControlRemoveContentOp(controlId));
    }
}
