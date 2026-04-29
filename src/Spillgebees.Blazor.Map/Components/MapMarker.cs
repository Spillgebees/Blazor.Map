using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Options;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapMarker : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");

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
        if (SectionContext?.Kind is not MapContentSectionKind.Overlays)
        {
            throw new InvalidOperationException("MapMarker must be placed inside MapOverlays.");
        }

        await SetOverlayFeaturesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Map is not null)
        {
            await Map.RemoveOverlayFeaturesAsync(_ownerId);
        }
    }

    private ValueTask SetOverlayFeaturesAsync() =>
        Map!.SetOverlayMarkersAsync(
            _ownerId,
            [
                new Marker(
                    Id,
                    Position,
                    Title,
                    Popup,
                    Color: Color,
                    RotationAlignment: RotationAlignment,
                    PitchAlignment: PitchAlignment
                ),
            ]
        );
}
