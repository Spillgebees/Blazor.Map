using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Projects a collection of items into markers via selector functions. Must be placed
/// inside <see cref="MapFeatures"/> within a <see cref="SgbMap"/>.
/// </summary>
/// <typeparam name="TItem">The item type the selectors operate on.</typeparam>
public sealed class MapMarkers<TItem> : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");

    [CascadingParameter]
    private IMapFeatureHost? _map { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <summary>The items to project into markers. Defaults to an empty list.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Extracts the unique marker id per item.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string>? IdSelector { get; set; }

    /// <summary>Extracts the marker position per item.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, Coordinate>? PositionSelector { get; set; }

    /// <summary>Extracts the hover tooltip text per item.</summary>
    [Parameter]
    public Func<TItem, string?>? TitleSelector { get; set; }

    /// <summary>Extracts the click popup per item.</summary>
    [Parameter]
    public Func<TItem, PopupOptions?>? PopupSelector { get; set; }

    /// <summary>Extracts the marker color (any CSS color) per item.</summary>
    [Parameter]
    public Func<TItem, string?>? ColorSelector { get; set; }

    /// <summary>Re-projects the items and replaces this component's markers on the host map.</summary>
    protected override async Task OnParametersSetAsync()
    {
        ValidatePlacement();
        ValidateSelectors();

        var markers = Items.Select(CreateMarker).ToArray();
        await _map!.SetOverlayMarkersAsync(_ownerId, markers);
    }

    /// <summary>Removes this component's markers from the host map.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_map is not null)
        {
            await _map.RemoveOverlayFeaturesAsync(_ownerId);
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
        if (_map is null)
        {
            throw new InvalidOperationException("MapMarkers must be placed inside SgbMap.");
        }

        if (_sectionContext?.Kind is not MapContentSectionKind.Features)
        {
            throw new InvalidOperationException("MapMarkers must be placed inside MapFeatures.");
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
