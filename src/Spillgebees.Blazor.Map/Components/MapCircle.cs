using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// A single declarative circle. Must be placed inside <see cref="MapFeatures"/>
/// within a <see cref="SgbMap"/>.
/// </summary>
public sealed class MapCircle : ComponentBase, IAsyncDisposable
{
    private readonly MapOverlayRegistration<Circle> _registration = new();

    [CascadingParameter]
    private IMapFeatureHost? _map { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <summary>Unique circle id.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>Circle center position.</summary>
    [Parameter, EditorRequired]
    public Coordinate Position { get; set; }

    /// <summary>Circle radius. Defaults to 8.</summary>
    [Parameter]
    public int Radius { get; set; } = 8;

    /// <summary>Circle color (any CSS color).</summary>
    [Parameter]
    public string? Color { get; set; }

    /// <summary>Circle stroke color (any CSS color).</summary>
    [Parameter]
    public string? StrokeColor { get; set; }

    /// <summary>Circle stroke width.</summary>
    [Parameter]
    public double? StrokeWidth { get; set; }

    /// <summary>Popup shown when the circle is clicked.</summary>
    [Parameter]
    public PopupOptions? Popup { get; set; }

    /// <summary>Registers or updates the circle on the host map.</summary>
    protected override async Task OnParametersSetAsync()
    {
        var circle = new Circle(
            Id,
            Position,
            Radius,
            Color,
            StrokeColor: StrokeColor,
            StrokeWidth: StrokeWidth,
            Popup: Popup
        );

        await _registration.RegisterAsync(_map, _sectionContext, nameof(MapCircle), circle, SetOverlayCirclesAsync);
    }

    /// <summary>Removes the circle from the host map.</summary>
    public async ValueTask DisposeAsync() => await _registration.DisposeAsync();

    private static ValueTask SetOverlayCirclesAsync(
        IMapFeatureHost map,
        string ownerId,
        IReadOnlyList<Circle> circles
    ) => map.SetOverlayCirclesAsync(ownerId, circles);
}
