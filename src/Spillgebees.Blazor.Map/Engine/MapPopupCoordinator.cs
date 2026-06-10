namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Component popups → ops. Content binds via DOM convention
/// (<c>data-sgb-popup-placeholder</c>); close interactions ride engine event handler
/// ids on the router.
/// </summary>
internal sealed class MapPopupCoordinator(MapEngineChannel channel, MapEngineEventRouter router)
{
    private readonly Dictionary<string, int> _closedHandlers = [];

    public void SetContent(string popupId, Coordinate position, PopupOptions options, Func<Task> onClosedAsync)
    {
        if (_closedHandlers.Remove(popupId, out var previousHandlerId))
        {
            router.Unregister(previousHandlerId);
        }

        var handlerId = router.Register(_ => onClosedAsync());
        _closedHandlers[popupId] = handlerId;
        channel.Queue(new PopupSetOp(new EnginePopup(popupId, position, options, new EnginePopupEvents(handlerId))));
    }

    public void RemoveContent(string popupId)
    {
        if (_closedHandlers.Remove(popupId, out var handlerId))
        {
            router.Unregister(handlerId);
        }

        channel.Queue(new PopupRemoveOp(popupId));
    }
}
