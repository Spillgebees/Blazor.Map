using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map;
using Spillgebees.Blazor.Map.Interop;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Renders and wires one declarative legend control entry.
/// </summary>
internal sealed class LegendMapControlHost : ComponentBase, IAsyncDisposable
{
    private const string CustomControlKind = "legend";

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapDisplayState? Display { get; set; }

    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter, EditorRequired]
    public LegendControlDefinition Control { get; set; } = null!;

    private readonly string _contentId = $"sgb-map-legend-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;
    private bool _controlSyncPending = true;
    private bool _registered;
    private string? _registeredControlId;
    private ILogger? _logger;
    private MapLegendDisplayBinder? _displayBinder;

    private ILogger Logger => _logger ??= LoggerFactory.CreateLogger<LegendMapControlHost>();
    private MapLegendDisplayBinder DisplayBinder =>
        _displayBinder ??= new MapLegendDisplayBinder(() => InvokeAsync(StateHasChanged));

    private string ContentClassName =>
        new CssBuilder()
            .AddClass("sgb-map-legend-content")
            .AddClass(
                Control.Content.Definition.ClassName,
                !string.IsNullOrWhiteSpace(Control.Content.Definition.ClassName)
            )
            .Build();

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var sequence = 0;

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "sgb-map-legend-placeholder");
        builder.AddAttribute(sequence++, "hidden", true);
        builder.AddElementReferenceCapture(sequence++, elementReference => _placeholderReference = elementReference);

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "id", _contentId);
        builder.AddAttribute(sequence++, "class", ContentClassName);
        builder.AddAttribute(sequence++, "hidden", true);
        builder.AddElementReferenceCapture(sequence++, elementReference => _contentReference = elementReference);

        foreach (var legendSection in Control.Content.Definition.Sections)
        {
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", GetSectionClassName(legendSection));

            builder.OpenElement(sequence++, "header");
            builder.AddAttribute(sequence++, "class", "sgb-map-legend-section-header");

            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "sgb-map-legend-section-title");
            builder.AddContent(sequence++, legendSection.Title);
            builder.CloseElement();

            if (!string.IsNullOrWhiteSpace(legendSection.Description))
            {
                builder.OpenElement(sequence++, "p");
                builder.AddAttribute(sequence++, "class", "sgb-map-legend-section-description");
                builder.AddContent(sequence++, legendSection.Description);
                builder.CloseElement();
            }

            builder.CloseElement();

            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "sgb-map-legend-items");

            foreach (var item in legendSection.Items)
            {
                if (Control.Content.ItemTemplate is not null)
                {
                    builder.AddContent(sequence++, Control.Content.ItemTemplate(BuildTemplateContext(item)));
                    continue;
                }

                RenderDefaultItem(builder, ref sequence, item);
            }

            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();
    }

    private void RenderDefaultItem(RenderTreeBuilder builder, ref int sequence, MapLegendItem item)
    {
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", GetItemClassName(item));

        if (IsToggleable(item))
        {
            RenderToggleableItem(builder, ref sequence, item);
        }
        else
        {
            RenderLegendSymbol(builder, ref sequence, item);
            RenderItemCopy(builder, ref sequence, item, "div");
        }

        builder.CloseElement();
    }

    private void RenderToggleableItem(RenderTreeBuilder builder, ref int sequence, MapLegendItem item)
    {
        builder.OpenElement(sequence++, "label");
        builder.AddAttribute(sequence++, "class", "sgb-map-legend-item-toggle");

        RenderLegendSymbol(builder, ref sequence, item);
        RenderItemCopy(builder, ref sequence, item, "span");

        builder.OpenElement(sequence++, "span");
        builder.AddAttribute(sequence++, "class", "sgb-map-legend-item-switch");

        var selected = GetItemOn(item);

        builder.OpenElement(sequence++, "input");
        builder.AddAttribute(sequence++, "type", "checkbox");
        builder.AddAttribute(sequence++, "role", "switch");
        builder.AddAttribute(sequence++, "aria-checked", selected.ToString().ToLowerInvariant());
        builder.AddAttribute(sequence++, "data-testid", $"map-legend-toggle-{item.Id}");
        builder.AddAttribute(sequence++, "checked", selected);
        builder.AddAttribute(
            sequence++,
            "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, args => ToggleItemAsync(item, args))
        );
        builder.CloseElement();

        builder.OpenElement(sequence++, "span");
        builder.AddAttribute(sequence++, "class", "sgb-map-legend-item-switch-track");
        builder.CloseElement();

        builder.CloseElement();
        builder.CloseElement();
    }

    private static void RenderLegendSymbol(RenderTreeBuilder builder, ref int sequence, MapLegendItem item)
    {
        var symbol = item.ResolvedSymbol;
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
                var sanitizedCssClass = SanitizeCssClass(icon.CssClass);
                builder.AddAttribute(sequence++, "class", BuildSymbolClassName(sanitizedCssClass));
                builder.AddAttribute(sequence++, "data-symbol", "icon");
                break;
        }

        builder.CloseElement();
    }

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

    private static void RenderItemCopy(
        RenderTreeBuilder builder,
        ref int sequence,
        MapLegendItem item,
        string elementName
    )
    {
        builder.OpenElement(sequence++, elementName);
        builder.AddAttribute(sequence++, "class", "sgb-map-legend-item-copy");

        builder.OpenElement(sequence++, "span");
        builder.AddAttribute(sequence++, "class", "sgb-map-legend-item-label");
        builder.AddContent(sequence++, item.Label);
        builder.CloseElement();

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "sgb-map-legend-item-description");
            builder.AddContent(sequence++, item.Description);
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        ValidateControl();
        DisplayBinder.UpdateDisplaySubscription(Display);
        ValidateDefinition();

        _controlSyncPending = true;
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Map is null)
        {
            return;
        }

        var mapReady = await Map.WhenReadyAsync();
        if (!mapReady)
        {
            return;
        }

        if (!_controlSyncPending)
        {
            return;
        }

        if (
            _registered
            && !string.IsNullOrWhiteSpace(_registeredControlId)
            && !string.Equals(_registeredControlId, Control.ControlId, StringComparison.Ordinal)
        )
        {
            await MapJs.RemoveControlContentAsync(JsRuntime, Logger, Map.MapReference, _registeredControlId);
            _registered = false;
            _registeredControlId = null;
        }

        if (!Control.Visible)
        {
            if (_registered)
            {
                await MapJs.RemoveControlContentAsync(
                    JsRuntime,
                    Logger,
                    Map.MapReference,
                    _registeredControlId ?? Control.ControlId
                );
                _registered = false;
                _registeredControlId = null;
            }

            _controlSyncPending = false;
            return;
        }

        await MapJs.SetControlContentAsync(
            JsRuntime,
            Logger,
            Map.MapReference,
            Control.ControlId,
            CustomControlKind,
            _placeholderReference,
            _contentReference
        );

        _registered = true;
        _registeredControlId = Control.ControlId;
        _controlSyncPending = false;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _displayBinder?.Dispose();
        _displayBinder = null;

        if (Map is null || !_registered)
        {
            return;
        }

        try
        {
            await MapJs.RemoveControlContentAsync(
                JsRuntime,
                Logger,
                Map.MapReference,
                _registeredControlId ?? Control.ControlId
            );
        }
        catch (Exception exception)
        {
            Logger.LogTrace(exception, "Legend control removal skipped during disposal.");
        }
        finally
        {
            _registered = false;
            _registeredControlId = null;
        }
    }

    private static string GetSectionClassName(MapLegendSection section) =>
        new CssBuilder()
            .AddClass("sgb-map-legend-section")
            .AddClass(section.ClassName, !string.IsNullOrWhiteSpace(section.ClassName))
            .Build();

    private string GetItemClassName(MapLegendItem item) => DisplayBinder.GetItemClassName(item);

    private static bool IsToggleable(MapLegendItem item) => MapLegendDisplayBinder.IsToggleable(item);

    private bool GetItemOn(MapLegendItem item) => DisplayBinder.GetItemOn(item);

    private Task ToggleItemAsync(MapLegendItem item, ChangeEventArgs args) => DisplayBinder.ToggleItemAsync(item, args);

    private MapLegendItemTemplateContext BuildTemplateContext(MapLegendItem item) =>
        DisplayBinder.BuildTemplateContext(item);

    private void ValidateControl()
    {
        if (Control is null)
        {
            throw new InvalidOperationException("A legend control entry is required.");
        }

        if (string.IsNullOrWhiteSpace(Control.ControlId))
        {
            throw new InvalidOperationException("A non-empty ControlId is required.");
        }
    }

    private void ValidateDefinition()
    {
        var duplicateId = Control
            .Content.Definition.GetItems()
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);

        if (duplicateId is null)
        {
            DisplayBinder.ValidateDisplayItems(Control.Content.Definition.GetItems());
            return;
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(duplicateId.Key)
                ? "Legend item IDs must be non-empty."
                : $"Legend item IDs must be unique. Duplicate ID: '{duplicateId.Key}'."
        );
    }
}
