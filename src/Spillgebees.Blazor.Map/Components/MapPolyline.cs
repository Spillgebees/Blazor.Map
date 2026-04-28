using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapPolyline : MapOverlayComponentBase
{
    private IReadOnlyList<Coordinate>? _cachedCoordinatesSource;
    private ImmutableList<Coordinate> _cachedCoordinates = [];

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

    protected override ValueTask SetOverlayFeaturesAsync()
    {
        return Map!.SetOverlayPolylinesAsync(
            OwnerId,
            [new Polyline(Id, GetCoordinateSnapshot(), Color, Width, Popup: Popup)]
        );
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
}
