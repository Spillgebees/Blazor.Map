using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapPolylines<TItem> : ComponentBase, IAsyncDisposable
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
    public Func<TItem, IReadOnlyList<Coordinate>>? CoordinatesSelector { get; set; }

    [Parameter]
    public Func<TItem, string?>? ColorSelector { get; set; }

    [Parameter]
    public Func<TItem, double?>? WidthSelector { get; set; }

    [Parameter]
    public Func<TItem, PopupOptions?>? PopupSelector { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        ValidatePlacement();
        ValidateSelectors();

        var polylines = Items.Select(CreatePolyline).ToArray();
        await Map!.SetOverlayPolylinesAsync(_ownerId, polylines);
    }

    public async ValueTask DisposeAsync()
    {
        if (Map is not null)
        {
            await Map.RemoveOverlayFeaturesAsync(_ownerId);
        }
    }

    private Polyline CreatePolyline(TItem item) =>
        new(
            IdSelector!(item),
            CoordinatesSelector!(item).ToImmutableList(),
            ColorSelector?.Invoke(item),
            WidthSelector?.Invoke(item),
            Popup: PopupSelector?.Invoke(item)
        );

    private void ValidatePlacement()
    {
        if (Map is null)
        {
            throw new InvalidOperationException("MapPolylines must be placed inside SgbMap.");
        }

        if (SectionContext?.Kind is not MapContentSectionKind.Overlays)
        {
            throw new InvalidOperationException("MapPolylines must be placed inside MapOverlays.");
        }
    }

    private void ValidateSelectors()
    {
        if (IdSelector is null)
        {
            throw new InvalidOperationException("MapPolylines requires IdSelector.");
        }

        if (CoordinatesSelector is null)
        {
            throw new InvalidOperationException("MapPolylines requires CoordinatesSelector.");
        }
    }
}
