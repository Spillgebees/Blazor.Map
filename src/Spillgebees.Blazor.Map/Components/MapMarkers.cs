using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapMarkers<TItem> : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    [Parameter, EditorRequired]
    public Func<TItem, string>? IdSelector { get; set; }

    [Parameter, EditorRequired]
    public Func<TItem, Coordinate>? PositionSelector { get; set; }

    [Parameter]
    public Func<TItem, string?>? TitleSelector { get; set; }

    [Parameter]
    public Func<TItem, PopupOptions?>? PopupSelector { get; set; }

    [Parameter]
    public Func<TItem, string?>? ColorSelector { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        ValidatePlacement();
        ValidateSelectors();

        var markers = Items.Select(CreateMarker).ToArray();
        await Map!.SetOverlayMarkersAsync(_ownerId, markers);
    }

    public async ValueTask DisposeAsync()
    {
        if (Map is not null)
        {
            await Map.RemoveOverlayFeaturesAsync(_ownerId);
        }
    }

    private Marker CreateMarker(TItem item) =>
        new(
            IdSelector!(item),
            PositionSelector!(item),
            TitleSelector?.Invoke(item),
            PopupSelector?.Invoke(item),
            Color: ColorSelector?.Invoke(item)
        );

    private void ValidatePlacement()
    {
        if (Map is null)
        {
            throw new InvalidOperationException("MapMarkers must be placed inside SgbMap.");
        }

        if (SectionContext?.Kind is not MapContentSectionKind.Overlays)
        {
            throw new InvalidOperationException("MapMarkers must be placed inside MapOverlays.");
        }
    }

    private void ValidateSelectors()
    {
        if (IdSelector is null)
        {
            throw new InvalidOperationException("MapMarkers requires IdSelector.");
        }

        if (PositionSelector is null)
        {
            throw new InvalidOperationException("MapMarkers requires PositionSelector.");
        }
    }
}
