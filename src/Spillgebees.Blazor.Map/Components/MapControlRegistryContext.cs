using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// The cascaded facade control components use to talk to their hosting map's control
/// registry without referencing the map type directly.
/// </summary>
internal sealed class MapControlRegistryContext(IMapControlHost map)
{
    public bool Register(string ownerId, MapControlDefinition control) => map.RegisterControl(ownerId, control);

    public bool Unregister(string controlId) => map.UnregisterControl(controlId);

    public bool UnregisterByOwner(string ownerId) => map.UnregisterControlByOwner(ownerId);

    public bool IsReady => map.RuntimeIsReady;

    public Task<bool> WhenReadyAsync() => map.WhenReadyAsync();

    public ValueTask SyncControlsAsync() => map.SyncControlsAsync();

    public ValueTask SetControlContentAsync(
        string controlId,
        string kind,
        ElementReference placeholderReference,
        ElementReference contentReference,
        Func<bool, Task>? onPanelOpenChangedAsync = null
    ) => map.SetControlContentAsync(controlId, kind, placeholderReference, contentReference, onPanelOpenChangedAsync);

    public ValueTask RemoveControlContentAsync(string controlId) => map.RemoveControlContentAsync(controlId);
}
