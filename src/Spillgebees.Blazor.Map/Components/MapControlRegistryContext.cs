using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

internal sealed class MapControlRegistryContext(BaseMap map)
{
    public bool Register(string ownerId, MapControl control) => map.RegisterControl(ownerId, control);

    public bool Unregister(string controlId) => map.UnregisterControl(controlId);

    public bool UnregisterByOwner(string ownerId) => map.UnregisterControlByOwner(ownerId);

    public bool IsReady => map.RuntimeIsReady;

    public Task<bool> WhenReadyAsync() => map.WhenReadyAsync();

    public ValueTask SyncControlsAsync() => map.SyncControlsAsync();

    public ValueTask SetControlContentAsync(
        string controlId,
        string kind,
        ElementReference placeholderReference,
        ElementReference contentReference
    ) => map.SetControlContentAsync(controlId, kind, placeholderReference, contentReference);

    public ValueTask RemoveControlContentAsync(string controlId) => map.RemoveControlContentAsync(controlId);
}
