using System.Text.Json;
using Microsoft.JSInterop;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// The single JS → .NET entry point per map.
/// UI events arrive with an integer handler id registered by components; map lifecycle
/// events (load, moveend, …) arrive by kind.
/// </summary>
internal sealed class MapEngineEventRouter : IDisposable
{
    private readonly Dictionary<int, Func<JsonElement, Task>> _handlers = [];
    private int _nextHandlerId = 1;

    public MapEngineEventRouter()
    {
        Reference = DotNetObjectReference.Create(this);
    }

    public DotNetObjectReference<MapEngineEventRouter> Reference { get; }

    /// <summary>Raised for map lifecycle events: "load", "moveend", "zoomend", "click".</summary>
    public event Func<string, JsonElement, Task>? MapEvent;

    /// <summary>Marker interactions; markers share the router instead of owning per-marker .NET object references.</summary>
    public event Func<MarkerClickEventArgs, Task>? MarkerClick;

    public event Func<MarkerDragEventArgs, Task>? MarkerDragEnd;

    /// <summary>Raised when the engine clears an active follow (user interaction or missing entity).</summary>
    public event Func<MapFollowChangedEventArgs, Task>? FollowChanged;

    public int Register(Func<JsonElement, Task> handler)
    {
        var handlerId = _nextHandlerId++;
        _handlers[handlerId] = handler;
        return handlerId;
    }

    public void Unregister(int handlerId) => _handlers.Remove(handlerId);

    [JSInvokable]
    public Task OnEvent(int handlerId, JsonElement payload) =>
        _handlers.TryGetValue(handlerId, out var handler) ? handler(payload) : Task.CompletedTask;

    [JSInvokable]
    public Task OnMapEvent(string kind, JsonElement payload) => MapEvent?.Invoke(kind, payload) ?? Task.CompletedTask;

    [JSInvokable]
    public Task OnMarkerClickCallbackAsync(MarkerClickEventArgs args) =>
        MarkerClick?.Invoke(args) ?? Task.CompletedTask;

    [JSInvokable]
    public Task OnMarkerDragEndCallbackAsync(MarkerDragEventArgs args) =>
        MarkerDragEnd?.Invoke(args) ?? Task.CompletedTask;

    [JSInvokable]
    public Task OnFollowChangedCallbackAsync(MapFollowChangedEventArgs args) =>
        FollowChanged?.Invoke(args) ?? Task.CompletedTask;

    public void Dispose()
    {
        _handlers.Clear();
        Reference.Dispose();
    }
}
