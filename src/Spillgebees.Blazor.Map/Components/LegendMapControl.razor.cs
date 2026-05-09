using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Models.Visibility;

namespace Spillgebees.Blazor.Map.Components;

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
    private MapLegendVisibilityBinder? _visibilityBinder;

    private MapLegendVisibilityBinder VisibilityBinder =>
        _visibilityBinder ??= new MapLegendVisibilityBinder(() => InvokeAsync(StateHasChanged));

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    [CascadingParameter]
    private MapLayerVisibilityState? LayerVisibility { get; set; }

    [Parameter]
    public string Id { get; set; } = "legend";

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 500;

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public bool Collapsible { get; set; } = true;

    [Parameter]
    public bool InitiallyOpen { get; set; } = true;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public MapLegend Definition { get; set; } = new([]);

    [Parameter]
    public RenderFragment<MapLegendItemTemplateContext>? ItemTemplate { get; set; }

    private string ContentClassName =>
        new CssBuilder()
            .AddClass("sgb-map-legend-content")
            .AddClass(Definition.ClassName, !string.IsNullOrWhiteSpace(Definition.ClassName))
            .Build();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ValidateControl();
        VisibilityBinder.UpdateVisibilitySubscription(LayerVisibility);
        ValidateDefinition();

        _registration.Register(
            Registry,
            SectionContext,
            "LegendMapControl must be placed inside MapControls.",
            Id,
            BuildControl()
        );
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration.SyncAfterRenderAsync(
            Registry,
            Id,
            Visible,
            CustomControlKind,
            _placeholderReference,
            _contentReference
        );

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _visibilityBinder?.Dispose();
        _visibilityBinder = null;
        await _registration.DisposeAsync(Registry);
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

    private string GetItemClassName(MapLegendItem item) => VisibilityBinder.GetItemClassName(item);

    private static bool IsToggleable(MapLegendItem item) => MapLegendVisibilityBinder.IsToggleable(item);

    private bool GetItemVisible(MapLegendItem item) => VisibilityBinder.GetItemVisible(item);

    private Task ToggleItemAsync(MapLegendItem item, ChangeEventArgs args) =>
        VisibilityBinder.ToggleItemAsync(item, args);

    private MapLegendItemTemplateContext BuildTemplateContext(MapLegendItem item) =>
        VisibilityBinder.BuildTemplateContext(item);

    private static RenderFragment RenderLegendSymbol(MapLegendItem item) =>
        builder =>
        {
            var symbol = item.ResolvedSymbol;
            var sequence = 0;
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(
                sequence++,
                "class",
                symbol is MapLegendSymbol.NoneSymbol
                    ? "sgb-map-legend-symbol sgb-map-legend-symbol-empty"
                    : "sgb-map-legend-symbol"
            );
            builder.AddAttribute(sequence++, "aria-hidden", "true");

            switch (symbol)
            {
                case MapLegendSymbol.ColorSwatchSymbol swatch:
                    builder.AddAttribute(sequence++, "style", $"--sgb-map-legend-symbol-color:{swatch.Color}");
                    builder.AddAttribute(sequence++, "data-symbol", "swatch");
                    break;
                case MapLegendSymbol.LineSymbol line:
                    builder.AddAttribute(
                        sequence++,
                        "style",
                        $"--sgb-map-legend-symbol-color:{line.Color};--sgb-map-legend-symbol-width:{line.Width}px"
                    );
                    builder.AddAttribute(sequence++, "data-symbol", line.Dashed ? "dashed-line" : "line");
                    break;
                case MapLegendSymbol.CircleSymbol circle:
                    builder.AddAttribute(
                        sequence++,
                        "style",
                        $"--sgb-map-legend-symbol-color:{circle.Color};--sgb-map-legend-symbol-stroke:{circle.StrokeColor ?? "transparent"}"
                    );
                    builder.AddAttribute(sequence++, "data-symbol", "circle");
                    break;
                case MapLegendSymbol.IconSymbol icon:
                    builder.AddAttribute(sequence++, "class", $"sgb-map-legend-symbol {icon.CssClass}");
                    builder.AddAttribute(sequence++, "data-symbol", "icon");
                    break;
            }

            builder.CloseElement();
        };

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
            VisibilityBinder.ValidateVisibilityGroups(Definition.GetItems());
            return;
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(duplicateId.Key)
                ? "Legend item IDs must be non-empty."
                : $"Legend item IDs must be unique. Duplicate ID: '{duplicateId.Key}'."
        );
    }
}
