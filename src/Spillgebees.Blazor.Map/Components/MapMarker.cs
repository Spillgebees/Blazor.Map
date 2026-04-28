using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapMarker : MapOverlayComponentBase
{
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

    protected override ValueTask SetOverlayFeaturesAsync() =>
        Map!.SetOverlayMarkersAsync(
            OwnerId,
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
