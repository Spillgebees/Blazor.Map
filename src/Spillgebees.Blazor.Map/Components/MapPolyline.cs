using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// A single declarative polyline. Must be placed inside <see cref="MapFeatures"/>
/// within a <see cref="SgbMap"/>.
/// </summary>
public sealed class MapPolyline : ComponentBase, IAsyncDisposable
{
    private readonly MapOverlayRegistration<Polyline> _registration = new();
    private IReadOnlyList<Coordinate>? _cachedCoordinatesSource;
    private ImmutableList<Coordinate> _cachedCoordinates = [];

    [CascadingParameter]
    private IMapFeatureHost? _map { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <summary>Unique polyline id.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>The line's coordinates, in draw order. Defaults to an empty list.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<Coordinate> Coordinates { get; set; } = [];

    /// <summary>Line color (any CSS color).</summary>
    [Parameter]
    public string? Color { get; set; }

    /// <summary>Line width.</summary>
    [Parameter]
    public double? Width { get; set; }

    /// <summary>Popup shown when the polyline is clicked.</summary>
    [Parameter]
    public PopupOptions? Popup { get; set; }

    /// <summary>Registers or updates the polyline on the host map.</summary>
    protected override async Task OnParametersSetAsync()
    {
        var polyline = new Polyline(Id, GetCoordinateSnapshot(), Color, Width, Popup: Popup);

        await _registration.RegisterAsync(_map, _sectionContext, nameof(MapPolyline), polyline, SetOverlayPolylinesAsync);
    }

    /// <summary>Removes the polyline from the host map.</summary>
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
            _cachedCoordinates = [.. Coordinates];
        }

        return _cachedCoordinates;
    }

    private static ValueTask SetOverlayPolylinesAsync(IMapFeatureHost map, string ownerId, IReadOnlyList<Polyline> polylines) =>
        map.SetOverlayPolylinesAsync(ownerId, polylines);
}
