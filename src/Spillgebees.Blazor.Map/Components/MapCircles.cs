using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Projects a collection of items into circles via selector functions. Must be placed
/// inside <see cref="MapFeatures"/> within a <see cref="SgbMap"/>.
/// </summary>
/// <typeparam name="TItem">The item type the selectors operate on.</typeparam>
public sealed class MapCircles<TItem> : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");

    [CascadingParameter]
    private IMapFeatureHost? _map { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <summary>The items to project into circles. Defaults to an empty list.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Extracts the unique circle id per item.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string>? IdSelector { get; set; }

    /// <summary>Extracts the circle center position per item.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, Coordinate>? PositionSelector { get; set; }

    /// <summary>Extracts the circle radius per item; 8 when not set.</summary>
    [Parameter]
    public Func<TItem, int>? RadiusSelector { get; set; }

    /// <summary>Extracts the circle color (any CSS color) per item.</summary>
    [Parameter]
    public Func<TItem, string?>? ColorSelector { get; set; }

    /// <summary>Extracts the circle stroke color (any CSS color) per item.</summary>
    [Parameter]
    public Func<TItem, string?>? StrokeColorSelector { get; set; }

    /// <summary>Extracts the circle stroke width per item.</summary>
    [Parameter]
    public Func<TItem, double?>? StrokeWidthSelector { get; set; }

    /// <summary>Extracts the click popup per item.</summary>
    [Parameter]
    public Func<TItem, PopupOptions?>? PopupSelector { get; set; }

    /// <summary>Re-projects the items and replaces this component's circles on the host map.</summary>
    protected override async Task OnParametersSetAsync()
    {
        ValidatePlacement();
        ValidateSelectors();

        var circles = (Items ?? []).Select(CreateCircle).ToArray();
        await _map!.SetOverlayCirclesAsync(_ownerId, circles);
    }

    /// <summary>Removes this component's circles from the host map.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_map is not null)
        {
            await _map.RemoveOverlayFeaturesAsync(_ownerId);
        }
    }

    private Circle CreateCircle(TItem item) =>
        new(
            IdSelector!(item),
            PositionSelector!(item),
            RadiusSelector?.Invoke(item) ?? 8,
            ColorSelector?.Invoke(item),
            StrokeColor: StrokeColorSelector?.Invoke(item),
            StrokeWidth: StrokeWidthSelector?.Invoke(item),
            Popup: PopupSelector?.Invoke(item)
        );

    private void ValidatePlacement()
    {
        if (_map is null)
        {
            throw new InvalidOperationException("MapCircles must be placed inside SgbMap.");
        }

        if (_sectionContext?.Kind is not MapContentSectionKind.Features)
        {
            throw new InvalidOperationException("MapCircles must be placed inside MapFeatures.");
        }
    }

    private void ValidateSelectors()
    {
        if (IdSelector is null)
        {
            throw new InvalidOperationException("MapCircles requires IdSelector.");
        }

        if (PositionSelector is null)
        {
            throw new InvalidOperationException("MapCircles requires PositionSelector.");
        }
    }
}
