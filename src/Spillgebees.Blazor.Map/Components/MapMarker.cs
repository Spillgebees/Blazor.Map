using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// A single declarative marker. Must be placed inside <see cref="MapFeatures"/>
/// within a <see cref="SgbMap"/>.
/// </summary>
public sealed class MapMarker : ComponentBase, IAsyncDisposable
{
    private readonly MapOverlayRegistration<Marker> _registration = new();

    [CascadingParameter]
    private IMapFeatureHost? _map { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <summary>Unique marker id.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>Marker position.</summary>
    [Parameter, EditorRequired]
    public Coordinate Position { get; set; }

    /// <summary>Tooltip text shown on hover.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Popup shown when the marker is clicked.</summary>
    [Parameter]
    public PopupOptions? Popup { get; set; }

    /// <summary>Marker color (any CSS color).</summary>
    [Parameter]
    public string? Color { get; set; }

    /// <summary>Aligns the marker's rotation to the map or the viewport.</summary>
    [Parameter]
    public MapAlignment? RotationAlignment { get; set; }

    /// <summary>Aligns the marker's pitch to the map or the viewport.</summary>
    [Parameter]
    public MapAlignment? PitchAlignment { get; set; }

    /// <summary>Registers or updates the marker on the host map.</summary>
    protected override async Task OnParametersSetAsync()
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

        await _registration.RegisterAsync(_map, _sectionContext, nameof(MapMarker), marker, SetOverlayMarkersAsync);
    }

    /// <summary>Removes the marker from the host map.</summary>
    public async ValueTask DisposeAsync() => await _registration.DisposeAsync();

    private static ValueTask SetOverlayMarkersAsync(IMapFeatureHost map, string ownerId, IReadOnlyList<Marker> markers) =>
        map.SetOverlayMarkersAsync(ownerId, markers);
}
