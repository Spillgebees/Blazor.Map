using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
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
public partial class MapLegendControlHost : ComponentBase, IAsyncDisposable
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

    private ILogger Logger => LoggerFactory.CreateLogger<MapLegendControlHost>();

    private string ContentClassName =>
        new CssBuilder()
            .AddClass("sgb-map-legend-content")
            .AddClass(
                Control.Content.Definition.ClassName,
                !string.IsNullOrWhiteSpace(Control.Content.Definition.ClassName)
            )
            .Build();

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

            if (Control.Enabled)
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

    private static string GetSectionClassName(MapLegendSectionDefinition section) =>
        new CssBuilder()
            .AddClass("sgb-map-legend-section")
            .AddClass(section.ClassName, !string.IsNullOrWhiteSpace(section.ClassName))
            .Build();

    private string GetItemClassName(MapLegendItemDefinition item) =>
        new CssBuilder()
            .AddClass("sgb-map-legend-item")
            .AddClass("sgb-map-legend-item-toggleable", item.IsToggleable)
            .AddClass("sgb-map-legend-item-off", item.IsToggleable && !GetItemSelected(item.Id))
            .AddClass(item.ClassName, !string.IsNullOrWhiteSpace(item.ClassName))
            .Build();

    private bool GetItemSelected(string itemId) => _itemSelection.TryGetValue(itemId, out var selected) && selected;

    private async Task ToggleItemAsync(MapLegendItemDefinition item, ChangeEventArgs args)
    {
        var selected = args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => false,
        };

        await SetItemSelectedAsync(item, selected);
    }

    private async Task SetItemSelectedAsync(MapLegendItemDefinition item, bool selected)
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

        if (Control.Content.Definition is null)
        {
            throw new InvalidOperationException("A legend definition is required.");
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

    private MapVisibilityGroupDescriptor BuildVisibilityGroupDescriptor(MapLegendItemDefinition item) =>
        new(
            GetVisibilityGroupId(item),
            GetItemSelected(item.Id),
            item.Targets?.Select(target => new MapVisibilityGroupTargetDescriptor(target.StyleId, [.. target.LayerIds]))
                .ToArray()
                ?? []
        );

    private static string GetVisibilityGroupId(MapLegendItemDefinition item) => $"legend:{item.Id}";

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
