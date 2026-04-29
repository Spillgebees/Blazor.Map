using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Options;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapMarker : ComponentBase, IAsyncDisposable
{
    private readonly MapOverlayRegistration<Marker> _registration = new();

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
        var marker = new Marker(
            Id,
            Position,
            Title,
            Popup,
            Color: Color,
            RotationAlignment: RotationAlignment,
            PitchAlignment: PitchAlignment
        );

        await _registration.RegisterAsync(Map, SectionContext, nameof(MapMarker), marker, SetOverlayMarkersAsync);
    }

    public async ValueTask DisposeAsync() => await _registration.DisposeAsync();

    private static ValueTask SetOverlayMarkersAsync(BaseMap map, string ownerId, IReadOnlyList<Marker> markers) =>
        map.SetOverlayMarkersAsync(ownerId, markers);
}
