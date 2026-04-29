using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapCircle : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private BaseMap? _registeredMap;
    private Circle? _currentCircle;

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public Coordinate Position { get; set; }

    [Parameter]
    public int Radius { get; set; } = 8;

    [Parameter]
    public string? Color { get; set; }

    [Parameter]
    public PopupOptions? Popup { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        ValidatePlacement();

        await SetOverlayFeaturesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await RemoveRegisteredOverlayFeaturesAsync();
    }

    private async ValueTask SetOverlayFeaturesAsync()
    {
        var circle = new Circle(Id, Position, Radius, Color, Popup: Popup);

        if (ReferenceEquals(_registeredMap, Map) && _currentCircle == circle)
        {
            return;
        }

        if (_registeredMap is not null && !ReferenceEquals(_registeredMap, Map))
        {
            await RemoveRegisteredOverlayFeaturesAsync();
        }

        _currentCircle = circle;
        _registeredMap = Map;
        await Map!.SetOverlayCirclesAsync(_ownerId, [circle]);
    }

    private async ValueTask RemoveRegisteredOverlayFeaturesAsync()
    {
        if (_registeredMap is not null)
        {
            await _registeredMap.RemoveOverlayFeaturesAsync(_ownerId);
        }

        _registeredMap = null;
        _currentCircle = null;
    }

    private void ValidatePlacement()
    {
        if (Map is null)
        {
            throw new InvalidOperationException("MapCircle must be placed inside SgbMap.");
        }

        if (SectionContext?.Kind is not MapContentSectionKind.Overlays)
        {
            throw new InvalidOperationException("MapCircle must be placed inside MapOverlays.");
        }
    }
}
