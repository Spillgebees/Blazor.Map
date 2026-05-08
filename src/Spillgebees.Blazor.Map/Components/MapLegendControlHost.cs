using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Interop;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Models.Visibility;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Renders and wires one declarative legend control entry.
/// </summary>
internal sealed class MapLegendControlHost : ComponentBase, IAsyncDisposable
{
    private const string CustomControlKind = "legend";

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapLayerVisibilityState? LayerVisibility { get; set; }

    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter, EditorRequired]
    public LegendMapControl Control { get; set; } = null!;

    private readonly string _contentId = $"sgb-map-legend-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;
    private bool _controlSyncPending = true;
    private bool _registered;
    private string? _registeredControlId;
    private ILogger? _logger;
    private MapLayerVisibilityState? _subscribedLayerVisibility;

    private ILogger Logger => _logger ??= LoggerFactory.CreateLogger<MapLegendControlHost>();

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
            RenderItemCopy(builder, ref sequence, item, "div");
        }

        builder.CloseElement();
    }

    private void RenderToggleableItem(RenderTreeBuilder builder, ref int sequence, MapLegendItem item)
    {
        builder.OpenElement(sequence++, "label");
        builder.AddAttribute(sequence++, "class", "sgb-map-legend-item-toggle");

        RenderItemCopy(builder, ref sequence, item, "span");

        builder.OpenElement(sequence++, "span");
        builder.AddAttribute(sequence++, "class", "sgb-map-legend-item-switch");

        var selected = GetItemVisible(item);

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
        ValidateDefinition();
        UpdateVisibilitySubscription();

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

        if (!Control.Enabled)
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
        if (_subscribedLayerVisibility is not null)
        {
            _subscribedLayerVisibility.Changed -= HandleLayerVisibilityChanged;
            _subscribedLayerVisibility = null;
        }

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

    private void UpdateVisibilitySubscription()
    {
        if (ReferenceEquals(_subscribedLayerVisibility, LayerVisibility))
        {
            return;
        }

        if (_subscribedLayerVisibility is not null)
        {
            _subscribedLayerVisibility.Changed -= HandleLayerVisibilityChanged;
        }

        _subscribedLayerVisibility = LayerVisibility;

        if (_subscribedLayerVisibility is not null)
        {
            _subscribedLayerVisibility.Changed += HandleLayerVisibilityChanged;
        }
    }

    private void HandleLayerVisibilityChanged(object? sender, MapLayerVisibilityChangedEventArgs args) =>
        _ = InvokeAsync(StateHasChanged);

    private static string GetSectionClassName(MapLegendSection section) =>
        new CssBuilder()
            .AddClass("sgb-map-legend-section")
            .AddClass(section.ClassName, !string.IsNullOrWhiteSpace(section.ClassName))
            .Build();

    private string GetItemClassName(MapLegendItem item)
    {
        var isToggleable = IsToggleable(item);
        return new CssBuilder()
            .AddClass("sgb-map-legend-item")
            .AddClass("sgb-map-legend-item-toggleable", isToggleable)
            .AddClass("sgb-map-legend-item-off", isToggleable && !GetItemVisible(item))
            .AddClass(item.ClassName, !string.IsNullOrWhiteSpace(item.ClassName))
            .Build();
    }

    private bool IsToggleable(MapLegendItem item) => item.VisibilityGroupId is not null;

    private bool GetItemVisible(MapLegendItem item) =>
        ResolveVisibilityGroup(item, required: item.VisibilityGroupId is not null)?.IsVisible ?? true;

    private async Task ToggleItemAsync(MapLegendItem item, ChangeEventArgs args)
    {
        var selected = args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => false,
        };

        await SetItemVisibleAsync(item, selected);
    }

    private Task SetItemVisibleAsync(MapLegendItem item, bool selected)
    {
        var group = ResolveVisibilityGroup(item, required: true);
        if (group is not null)
        {
            LayerVisibility!.SetVisible(group.Id, selected);
        }

        return Task.CompletedTask;
    }

    private MapLegendItemTemplateContext BuildTemplateContext(MapLegendItem item)
    {
        var group = ResolveVisibilityGroup(item, required: item.VisibilityGroupId is not null);
        return new(
            item,
            group is not null,
            group?.IsVisible ?? true,
            group,
            selected => SetItemVisibleAsync(item, selected)
        );
    }

    private MapLayerVisibilityGroup? ResolveVisibilityGroup(MapLegendItem item, bool required)
    {
        if (item.VisibilityGroupId is null)
        {
            return null;
        }

        if (LayerVisibility is not null && LayerVisibility.TryGetGroup(item.VisibilityGroupId, out var group))
        {
            return group;
        }

        if (!required)
        {
            return null;
        }

        throw new InvalidOperationException(
            $"Legend item '{item.Id}' references missing layer visibility group '{item.VisibilityGroupId}'."
        );
    }

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
            var missingGroupItem = Control
                .Content.Definition.GetItems()
                .FirstOrDefault(item =>
                    item.VisibilityGroupId is not null
                    && (LayerVisibility is null || !LayerVisibility.Contains(item.VisibilityGroupId))
                );

            if (missingGroupItem is null)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Legend item '{missingGroupItem.Id}' references missing layer visibility group '{missingGroupItem.VisibilityGroupId}'."
            );
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(duplicateId.Key)
                ? "Legend item IDs must be non-empty."
                : $"Legend item IDs must be unique. Duplicate ID: '{duplicateId.Key}'."
        );
    }
}
