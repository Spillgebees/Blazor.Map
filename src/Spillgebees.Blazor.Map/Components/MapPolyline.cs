using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapPolyline : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private BaseMap? _registeredMap;
    private IReadOnlyList<Coordinate>? _cachedCoordinatesSource;
    private ImmutableList<Coordinate> _cachedCoordinates = [];
    private Polyline? _currentPolyline;

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public IReadOnlyList<Coordinate> Coordinates { get; set; } = [];

    [Parameter]
    public string? Color { get; set; }

    [Parameter]
    public double? Width { get; set; }

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
        var polyline = new Polyline(Id, GetCoordinateSnapshot(), Color, Width, Popup: Popup);

        if (ReferenceEquals(_registeredMap, Map) && _currentPolyline == polyline)
        {
            return;
        }

        if (_registeredMap is not null && !ReferenceEquals(_registeredMap, Map))
        {
            await RemoveRegisteredOverlayFeaturesAsync();
        }

        _currentPolyline = polyline;
        _registeredMap = Map;
        await Map!.SetOverlayPolylinesAsync(_ownerId, [polyline]);
    }

    private async ValueTask RemoveRegisteredOverlayFeaturesAsync()
    {
        if (_registeredMap is not null)
        {
            await _registeredMap.RemoveOverlayFeaturesAsync(_ownerId);
        }

        _registeredMap = null;
        _currentPolyline = null;
        _cachedCoordinatesSource = null;
        _cachedCoordinates = [];
    }

    private ImmutableList<Coordinate> GetCoordinateSnapshot()
    {
        if (!ReferenceEquals(_cachedCoordinatesSource, Coordinates) || !_cachedCoordinates.SequenceEqual(Coordinates))
        {
            _cachedCoordinatesSource = Coordinates;
            _cachedCoordinates = Coordinates.ToImmutableList();
        }

        return _cachedCoordinates;
    }

    private void ValidatePlacement()
    {
        if (Map is null)
        {
            throw new InvalidOperationException("MapPolyline must be placed inside SgbMap.");
        }

        if (SectionContext?.Kind is not MapContentSectionKind.Overlays)
        {
            throw new InvalidOperationException("MapPolyline must be placed inside MapOverlays.");
        }
    }
}
