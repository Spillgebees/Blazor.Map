using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Renders a purpose-built overlay control that toggles the visibility of the map's overlays and their parts.
/// </summary>
public partial class OverlayMapControl : ComponentBase, IDisposable
{
    [CascadingParameter]
    private IMapOverlayHost? _map { get; set; }

    /// <summary>Unique control identifier within the map.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.TopRight" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 450.</summary>
    [Parameter]
    public int Order { get; set; } = 450;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>Additional CSS class applied to the control container.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Accessible label for the control's toggle button; must be non-empty. Defaults to <c>"Overlays"</c>.</summary>
    [Parameter]
    public string Label { get; set; } = "Overlays";

    /// <summary>Title shown in the panel header. Defaults to <c>"Overlays"</c>.</summary>
    [Parameter]
    public string Title { get; set; } = "Overlays";

    /// <summary>Whether the panel starts open.</summary>
    [Parameter]
    public bool InitiallyOpen { get; set; }

    /// <summary>Maximum width of the panel as a CSS length value.</summary>
    [Parameter]
    public string? MaxWidth { get; set; }

    /// <summary>Additional CSS class applied to the panel content container.</summary>
    [Parameter]
    public string? PanelClass { get; set; }

    /// <summary>Overlay ids to show; shows all overlays when not set.</summary>
    [Parameter]
    public IReadOnlyList<string>? OverlayIds { get; set; }

    /// <summary>Optional template used to render each overlay item.</summary>
    [Parameter]
    public RenderFragment<MapOverlayControlItemContext>? ItemTemplate { get; set; }

    /// <summary>Optional template used to render each overlay part.</summary>
    [Parameter]
    public RenderFragment<MapOverlayPartControlItemContext>? PartTemplate { get; set; }

    private IReadOnlyList<MapOverlayItem> _items => ResolveItems();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("A non-empty Id is required.");
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }

        if (_map is null)
        {
            throw new InvalidOperationException("OverlayMapControl must be placed inside SgbMap.");
        }

        _map.OverlayChanged -= HandleOverlayChanged;
        _map.OverlayChanged += HandleOverlayChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _map?.OverlayChanged -= HandleOverlayChanged;
        GC.SuppressFinalize(this);
    }

    private IReadOnlyList<MapOverlayItem> ResolveItems()
    {
        if (_map is null)
        {
            return [];
        }

        var items = _map.GetOverlayItems();
        if (OverlayIds is null)
        {
            return items;
        }

        var selectedIds = OverlayIds.ToHashSet(StringComparer.Ordinal);
        return [.. items.Where(item => selectedIds.Contains(item.Id))];
    }

    private MapOverlayControlItemContext BuildTemplateContext(MapOverlayItem overlay) =>
        new(
            overlay,
            visible => SetOverlayVisibleAsync(overlay.Id, visible),
            (partId, visible) => SetPartVisibleAsync(overlay.Id, partId, visible)
        );

    private MapOverlayPartControlItemContext BuildPartTemplateContext(
        MapOverlayItem overlay,
        MapOverlayPartItem part
    ) => new(overlay, part, visible => SetPartVisibleAsync(overlay.Id, part.Id, visible));

    private Task ToggleOverlayAsync(MapOverlayItem overlay, ChangeEventArgs args) =>
        SetOverlayVisibleAsync(overlay.Id, ResolveToggleValue(args));

    private Task TogglePartAsync(MapOverlayItem overlay, MapOverlayPartItem part, ChangeEventArgs args) =>
        SetPartVisibleAsync(overlay.Id, part.Id, ResolveToggleValue(args));

    private Task SetOverlayVisibleAsync(string overlayId, bool visible)
    {
        _map!.SetOverlayVisible(overlayId, visible);
        return Task.CompletedTask;
    }

    private Task SetPartVisibleAsync(string overlayId, string partId, bool visible)
    {
        _map!.SetOverlayPartVisible(overlayId, partId, visible);
        return Task.CompletedTask;
    }

    private void HandleOverlayChanged(object? sender, MapOverlayChangedEventArgs args) =>
        _ = InvokeAsync(StateHasChanged);

    private static bool ResolveToggleValue(ChangeEventArgs args) =>
        args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => throw new InvalidOperationException("Overlay toggle expected a bool or parseable string value."),
        };

    private static RenderFragment RenderLegendSymbol(MapLegendSymbol? symbol) =>
        builder =>
        {
            if (symbol is null or MapLegendSymbol.NoneSymbol)
            {
                return;
            }

            var (finalClass, finalStyle, finalDataSymbol) = symbol switch
            {
                MapLegendSymbol.ColorSwatchSymbol swatch => (
                    "sgb-map-legend-symbol",
                    $"--sgb-map-legend-symbol-color:{swatch.Color}",
                    "swatch"
                ),
                MapLegendSymbol.LineSymbol line => (
                    "sgb-map-legend-symbol",
                    $"--sgb-map-legend-symbol-color:{line.Color};--sgb-map-legend-symbol-width:{line.Width}px",
                    line.Dashed ? "dashed-line" : "line"
                ),
                MapLegendSymbol.CircleSymbol circle => (
                    "sgb-map-legend-symbol",
                    $"--sgb-map-legend-symbol-color:{circle.Color};--sgb-map-legend-symbol-stroke:{circle.StrokeColor ?? "transparent"}",
                    "circle"
                ),
                MapLegendSymbol.IconSymbol icon => (
                    BuildSymbolClassName(SanitizeCssClass(icon.CssClass)),
                    null,
                    "icon"
                ),
                _ => ("sgb-map-legend-symbol sgb-map-legend-symbol-empty", null, null),
            };

            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", finalClass);
            if (finalStyle is not null)
            {
                builder.AddAttribute(2, "style", finalStyle);
            }
            if (finalDataSymbol is not null)
            {
                builder.AddAttribute(3, "data-symbol", finalDataSymbol);
            }
            builder.AddAttribute(4, "aria-hidden", "true");
            builder.CloseElement();
        };

    private static string BuildSymbolClassName(string? cssClass) =>
        string.IsNullOrWhiteSpace(cssClass) ? "sgb-map-legend-symbol" : $"sgb-map-legend-symbol {cssClass}";

    private static string? SanitizeCssClass(string? cssClass)
    {
        if (string.IsNullOrWhiteSpace(cssClass))
        {
            return null;
        }

        var sanitized = new string(
            [.. cssClass.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_' or ' ')]
        );

        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized.Trim();
    }
}
