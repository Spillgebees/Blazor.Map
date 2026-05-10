using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Components;

public sealed class StyleOverlay : ComponentBase, IDisposable
{
    private BaseMap? _registeredMap;
    private string? _registeredOverlayId;

    [CascadingParameter]
    private MapOverlayContext? Overlay { get; set; }

    [Parameter, EditorRequired]
    public MapStyle Style { get; set; } = null!;

    protected override void OnParametersSet()
    {
        if (Overlay is null)
        {
            throw new InvalidOperationException("StyleOverlay must be placed inside MapOverlay.");
        }

        ArgumentNullException.ThrowIfNull(Style);

        var style = string.IsNullOrWhiteSpace(Style.Id) ? Style.WithId(Overlay.OverlayId) : Style;
        if (
            _registeredMap is not null
            && _registeredOverlayId is not null
            && (
                !ReferenceEquals(_registeredMap, Overlay.Map)
                || !string.Equals(_registeredOverlayId, Overlay.OverlayId, StringComparison.Ordinal)
            )
        )
        {
            _registeredMap.UnregisterOverlayStyle(_registeredOverlayId);
        }

        Overlay.Map.RegisterOverlayStyle(Overlay.OverlayId, style);
        _registeredMap = Overlay.Map;
        _registeredOverlayId = Overlay.OverlayId;
    }

    public void Dispose()
    {
        if (_registeredMap is not null && _registeredOverlayId is not null)
        {
            _registeredMap.UnregisterOverlayStyle(_registeredOverlayId);
        }
    }
}
