using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapCircle : MapOverlayComponentBase
{
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

    protected override ValueTask SetOverlayFeaturesAsync() =>
        Map!.SetOverlayCirclesAsync(OwnerId, [new Circle(Id, Position, Radius, Color, Popup: Popup)]);
}
