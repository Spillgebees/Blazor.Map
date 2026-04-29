using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapPolyline : ComponentBase, IAsyncDisposable
{
    private readonly MapOverlayRegistration<Polyline> _registration = new();
    private IReadOnlyList<Coordinate>? _cachedCoordinatesSource;
    private ImmutableList<Coordinate> _cachedCoordinates = [];

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
        var polyline = new Polyline(Id, GetCoordinateSnapshot(), Color, Width, Popup: Popup);

        await _registration.RegisterAsync(Map, SectionContext, nameof(MapPolyline), polyline, SetOverlayPolylinesAsync);
    }

    public async ValueTask DisposeAsync()
    {
        await _registration.DisposeAsync();
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

    private static ValueTask SetOverlayPolylinesAsync(BaseMap map, string ownerId, IReadOnlyList<Polyline> polylines) =>
        map.SetOverlayPolylinesAsync(ownerId, polylines);
}
