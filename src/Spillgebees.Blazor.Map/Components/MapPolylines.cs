using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Projects a collection of items into polylines via selector functions. Must be placed
/// inside <see cref="MapFeatures"/> within a <see cref="SgbMap"/>.
/// </summary>
/// <typeparam name="TItem">The item type the selectors operate on.</typeparam>
public sealed class MapPolylines<TItem> : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");

    [CascadingParameter]
    private IMapFeatureHost? _map { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <summary>The items to project into polylines. Defaults to an empty list.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Extracts the unique polyline id per item.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string>? IdSelector { get; set; }

    /// <summary>Extracts the line's coordinates, in draw order, per item.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, IReadOnlyList<Coordinate>>? CoordinatesSelector { get; set; }

    /// <summary>Extracts the line color (any CSS color) per item.</summary>
    [Parameter]
    public Func<TItem, string?>? ColorSelector { get; set; }

    /// <summary>Extracts the line width per item.</summary>
    [Parameter]
    public Func<TItem, double?>? WidthSelector { get; set; }

    /// <summary>Extracts the click popup per item.</summary>
    [Parameter]
    public Func<TItem, PopupOptions?>? PopupSelector { get; set; }

    /// <summary>Re-projects the items and replaces this component's polylines on the host map.</summary>
    protected override async Task OnParametersSetAsync()
    {
        ValidatePlacement();
        ValidateSelectors();

        var polylines = Items.Select(CreatePolyline).ToArray();
        await _map!.SetOverlayPolylinesAsync(_ownerId, polylines);
    }

    /// <summary>Removes this component's polylines from the host map.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_map is not null)
        {
            await _map.RemoveOverlayFeaturesAsync(_ownerId);
        }
    }

    private Polyline CreatePolyline(TItem item) =>
        new(
            IdSelector!(item),
            [.. CoordinatesSelector!(item)],
            ColorSelector?.Invoke(item),
            WidthSelector?.Invoke(item),
            Popup: PopupSelector?.Invoke(item)
        );

    private void ValidatePlacement()
    {
        if (_map is null)
        {
            throw new InvalidOperationException("MapPolylines must be placed inside SgbMap.");
        }

        if (_sectionContext?.Kind is not MapContentSectionKind.Features)
        {
            throw new InvalidOperationException("MapPolylines must be placed inside MapFeatures.");
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
