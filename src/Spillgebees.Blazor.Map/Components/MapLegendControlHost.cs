using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Interop;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Renders and wires one declarative legend control entry.
/// </summary>
internal sealed class MapLegendControlHost : ComponentBase, IAsyncDisposable
{
    private const string CustomControlKind = "legend";

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter, EditorRequired]
    public LegendMapControl Control { get; set; } = null!;

    private readonly Dictionary<string, bool> _itemSelection = new(StringComparer.Ordinal);
    private readonly string _contentId = $"sgb-map-legend-content-{Guid.NewGuid():N}";
    private readonly HashSet<string> _registeredVisibilityGroupIds = new(StringComparer.Ordinal);
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;
    private bool _controlSyncPending = true;
    private bool _visibilitySyncPending = true;
    private bool _registered;
    private string? _registeredControlId;
    private ILogger? _logger;

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
                    builder.AddContent(
                        sequence++,
                        Control.Content.ItemTemplate(
                            new MapLegendItemTemplateContext(
                                item,
                                GetItemSelected(item.Id),
                                selected => SetItemSelectedAsync(item, selected)
                            )
                        )
                    );

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

        if (item.IsToggleable)
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

        var selected = GetItemSelected(item.Id);

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
        SyncItemSelection();

        _controlSyncPending = true;
        _visibilitySyncPending = true;
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

        if (_controlSyncPending)
        {
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
            }
            else
            {
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
        }

        if (_visibilitySyncPending)
        {
            await SyncVisibilityGroupsAsync();
            _visibilitySyncPending = false;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await UnregisterVisibilityGroupsAsync();

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

    private string GetItemClassName(MapLegendItem item) =>
        new CssBuilder()
            .AddClass("sgb-map-legend-item")
            .AddClass("sgb-map-legend-item-toggleable", item.IsToggleable)
            .AddClass("sgb-map-legend-item-off", item.IsToggleable && !GetItemSelected(item.Id))
            .AddClass(item.ClassName, !string.IsNullOrWhiteSpace(item.ClassName))
            .Build();

    private bool GetItemSelected(string itemId) => _itemSelection.TryGetValue(itemId, out var selected) && selected;

    private async Task ToggleItemAsync(MapLegendItem item, ChangeEventArgs args)
    {
        var selected = args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => false,
        };

        await SetItemSelectedAsync(item, selected);
    }

    private async Task SetItemSelectedAsync(MapLegendItem item, bool selected)
    {
        _itemSelection[item.Id] = selected;

        if (Map is not null && item.IsToggleable)
        {
            await Map.SceneRegistry.RegisterVisibilityGroupAsync(BuildVisibilityGroupDescriptor(item));
            _registeredVisibilityGroupIds.Add(GetVisibilityGroupId(item));
        }

        if (Control.Content.OnItemVisibilityChanged.HasDelegate)
        {
            await Control.Content.OnItemVisibilityChanged.InvokeAsync(
                new MapLegendVisibilityChangedEventArgs(item, selected)
            );
        }
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
            return;
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(duplicateId.Key)
                ? "Legend item IDs must be non-empty."
                : $"Legend item IDs must be unique. Duplicate ID: '{duplicateId.Key}'."
        );
    }

    private void SyncItemSelection()
    {
        var nextSelection = Control
            .Content.Definition.GetItems()
            .ToDictionary(
                item => item.Id,
                item =>
                    _itemSelection.TryGetValue(item.Id, out var currentSelection)
                        ? currentSelection
                        : item.IsVisibleByDefault,
                StringComparer.Ordinal
            );

        _itemSelection.Clear();

        foreach (var pair in nextSelection)
        {
            _itemSelection[pair.Key] = pair.Value;
        }
    }

    private async Task SyncVisibilityGroupsAsync()
    {
        if (Map is null)
        {
            return;
        }

        var activeGroupIds = Control.Enabled
            ? Control
                .Content.Definition.GetItems()
                .Where(item => item.IsToggleable)
                .Select(GetVisibilityGroupId)
                .ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        var removedGroupIds = _registeredVisibilityGroupIds.Except(activeGroupIds, StringComparer.Ordinal).ToArray();
        foreach (var groupId in removedGroupIds)
        {
            await Map.SceneRegistry.UnregisterVisibilityGroupAsync(groupId);
            _registeredVisibilityGroupIds.Remove(groupId);
        }

        if (!Control.Enabled)
        {
            return;
        }

        foreach (var item in Control.Content.Definition.GetItems().Where(item => item.IsToggleable))
        {
            await Map.SceneRegistry.RegisterVisibilityGroupAsync(BuildVisibilityGroupDescriptor(item));
            _registeredVisibilityGroupIds.Add(GetVisibilityGroupId(item));
        }
    }

    private MapVisibilityGroupDescriptor BuildVisibilityGroupDescriptor(MapLegendItem item) =>
        new(
            GetVisibilityGroupId(item),
            GetItemSelected(item.Id),
            item.Targets?.Select(target => new MapVisibilityGroupTargetDescriptor(target.StyleId, [.. target.LayerIds]))
                .ToArray()
                ?? []
        );

    private static string GetVisibilityGroupId(MapLegendItem item) => $"legend:{item.Id}";

    private async Task UnregisterVisibilityGroupsAsync()
    {
        if (Map is null)
        {
            return;
        }

        foreach (var groupId in _registeredVisibilityGroupIds.ToArray())
        {
            await Map.SceneRegistry.UnregisterVisibilityGroupAsync(groupId);
            _registeredVisibilityGroupIds.Remove(groupId);
        }
    }
}
