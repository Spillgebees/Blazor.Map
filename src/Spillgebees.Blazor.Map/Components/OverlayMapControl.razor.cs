using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

public partial class OverlayMapControl : ComponentBase, IDisposable
{
    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 450;

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string Label { get; set; } = "Overlays";

    [Parameter]
    public string Title { get; set; } = "Overlays";

    [Parameter]
    public bool InitiallyOpen { get; set; }

    [Parameter]
    public string? MaxWidth { get; set; }

    [Parameter]
    public string? PanelClass { get; set; }

    [Parameter]
    public IReadOnlyList<string>? OverlayIds { get; set; }

    [Parameter]
    public RenderFragment<MapOverlayControlItemContext>? ItemTemplate { get; set; }

    [Parameter]
    public RenderFragment<MapOverlayPartControlItemContext>? PartTemplate { get; set; }

    private IReadOnlyList<MapOverlayItem> Items => ResolveItems();

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

        if (Map is null)
        {
            throw new InvalidOperationException("OverlayMapControl must be placed inside SgbMap.");
        }

        Map.OverlayChanged -= HandleOverlayChanged;
        Map.OverlayChanged += HandleOverlayChanged;
    }

    public void Dispose()
    {
        if (Map is not null)
        {
            Map.OverlayChanged -= HandleOverlayChanged;
        }
    }

    private IReadOnlyList<MapOverlayItem> ResolveItems()
    {
        if (Map is null)
        {
            return [];
        }

        var items = Map.GetOverlayItems();
        if (OverlayIds is null)
        {
            return items;
        }

        var selectedIds = OverlayIds.ToHashSet(StringComparer.Ordinal);
        return items.Where(item => selectedIds.Contains(item.Id)).ToArray();
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
        Map!.SetOverlayVisible(overlayId, visible);
        return Task.CompletedTask;
    }

    private Task SetPartVisibleAsync(string overlayId, string partId, bool visible)
    {
        Map!.SetOverlayPartVisible(overlayId, partId, visible);
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
            cssClass.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_' or ' ').ToArray()
        );

        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized.Trim();
    }
}
