using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registers a legend control and owns its Blazor content host.
/// </summary>
public partial class LegendMapControl : ComponentBase, IAsyncDisposable
{
    private const string CustomControlKind = "legend";
    private readonly StyledContentMapControlRegistration _registration = new();
    private readonly string _contentId = $"sgb-map-legend-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;
    private readonly MapLegendDisplayBinder _displayBinder;

    /// <summary>Initializes the display binder bound to this component's re-render callback.</summary>
    public LegendMapControl()
    {
        _displayBinder = new MapLegendDisplayBinder(() => InvokeAsync(StateHasChanged));
    }

    [CascadingParameter]
    private MapControlRegistryContext? _registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    [CascadingParameter]
    private MapDisplayState? _display { get; set; }

    /// <summary>Unique control identifier within the map. Defaults to <c>"legend"</c>.</summary>
    [Parameter]
    public string Id { get; set; } = "legend";

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.TopRight" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 500.</summary>
    [Parameter]
    public int Order { get; set; } = 500;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>Title shown in the legend header.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Whether the legend can be collapsed. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Collapsible { get; set; } = true;

    /// <summary>Whether the legend starts open. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool InitiallyOpen { get; set; } = true;

    /// <summary>Additional CSS class applied to the control container.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Legend definition describing its sections and items. Defaults to an empty legend.</summary>
    [Parameter]
    public MapLegend Definition { get; set; } = new([]);

    /// <summary>Optional template used to render each legend item.</summary>
    [Parameter]
    public RenderFragment<MapLegendItemTemplateContext>? ItemTemplate { get; set; }

    private string _contentClassName =>
        new CssBuilder()
            .AddClass("sgb-map-legend-content")
            .AddClass(Definition.ClassName, !string.IsNullOrWhiteSpace(Definition.ClassName))
            .Build();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ValidateControl();
        _displayBinder.UpdateDisplaySubscription(_display);
        ValidateDefinition();

        _registration.Register(
            _registry,
            _sectionContext,
            "LegendMapControl must be placed inside MapControls.",
            Id,
            BuildControl()
        );
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration.SyncAfterRenderAsync(
            _registry,
            Id,
            Visible,
            CustomControlKind,
            _placeholderReference,
            _contentReference
        );

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        _displayBinder.Dispose();
        await _registration.DisposeAsync(_registry);
    }

    private LegendControlDefinition BuildControl() =>
        new(
            Id,
            new MapControlPlacement(Position, Order, Visible),
            new LegendChromeOptions(Title, Collapsible, InitiallyOpen, Class),
            new LegendContentOptions(Definition, ItemTemplate)
        );

    private static string GetSectionClassName(MapLegendSection section) =>
        new CssBuilder()
            .AddClass("sgb-map-legend-section")
            .AddClass(section.ClassName, !string.IsNullOrWhiteSpace(section.ClassName))
            .Build();

    private string GetItemClassName(MapLegendItem item) => _displayBinder.GetItemClassName(item);

    private static bool IsToggleable(MapLegendItem item) => MapLegendDisplayBinder.IsToggleable(item);

    private bool GetItemOn(MapLegendItem item) => _displayBinder.GetItemOn(item);

    private Task ToggleItemAsync(MapLegendItem item, ChangeEventArgs args) => _displayBinder.ToggleItemAsync(item, args);

    private MapLegendItemTemplateContext BuildTemplateContext(MapLegendItem item) =>
        _displayBinder.BuildTemplateContext(item);

    private static RenderFragment RenderLegendSymbol(MapLegendItem item) =>
        builder =>
        {
            var (finalClass, finalStyle, finalDataSymbol) = item.ResolvedSymbol switch
            {
                MapLegendSymbol.NoneSymbol => ("sgb-map-legend-symbol sgb-map-legend-symbol-empty", null, null),
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

    private void ValidateControl()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("A non-empty Id is required.");
        }
    }

    private void ValidateDefinition()
    {
        var duplicateId = Definition
            .GetItems()
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);

        if (duplicateId is null)
        {
            _displayBinder.ValidateDisplayItems(Definition.GetItems());
            return;
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(duplicateId.Key)
                ? "Legend item IDs must be non-empty."
                : $"Legend item IDs must be unique. Duplicate ID: '{duplicateId.Key}'."
        );
    }
}
