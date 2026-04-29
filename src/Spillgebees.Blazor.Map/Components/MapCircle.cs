using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapCircle : ComponentBase, IAsyncDisposable
{
    private readonly MapOverlayRegistration<Circle> _registration = new();

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

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

    protected override async Task OnParametersSetAsync()
    {
        var circle = new Circle(Id, Position, Radius, Color, Popup: Popup);

        await _registration.RegisterAsync(Map, SectionContext, nameof(MapCircle), circle, SetOverlayCirclesAsync);
    }

    public async ValueTask DisposeAsync() => await _registration.DisposeAsync();

    private static ValueTask SetOverlayCirclesAsync(BaseMap map, string ownerId, IReadOnlyList<Circle> circles) =>
        map.SetOverlayCirclesAsync(ownerId, circles);
}
