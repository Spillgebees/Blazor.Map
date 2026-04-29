using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Options;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapMarker : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private BaseMap? _registeredMap;
    private Marker? _currentMarker;

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public Coordinate Position { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public PopupOptions? Popup { get; set; }

    [Parameter]
    public string? Color { get; set; }

    [Parameter]
    public MapAlignment? RotationAlignment { get; set; }

    [Parameter]
    public MapAlignment? PitchAlignment { get; set; }

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
        var marker = new Marker(
            Id,
            Position,
            Title,
            Popup,
            Color: Color,
            RotationAlignment: RotationAlignment,
            PitchAlignment: PitchAlignment
        );

        if (ReferenceEquals(_registeredMap, Map) && _currentMarker == marker)
        {
            return;
        }

        if (_registeredMap is not null && !ReferenceEquals(_registeredMap, Map))
        {
            await RemoveRegisteredOverlayFeaturesAsync();
        }

        _currentMarker = marker;
        _registeredMap = Map;
        await Map!.SetOverlayMarkersAsync(_ownerId, [marker]);
    }

    private async ValueTask RemoveRegisteredOverlayFeaturesAsync()
    {
        if (_registeredMap is not null)
        {
            await _registeredMap.RemoveOverlayFeaturesAsync(_ownerId);
        }

        _registeredMap = null;
        _currentMarker = null;
    }

    private void ValidatePlacement()
    {
        if (Map is null)
        {
            throw new InvalidOperationException("MapMarker must be placed inside SgbMap.");
        }

        if (SectionContext?.Kind is not MapContentSectionKind.Overlays)
        {
            throw new InvalidOperationException("MapMarker must be placed inside MapOverlays.");
        }
    }
}
