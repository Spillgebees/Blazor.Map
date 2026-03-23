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
/// Renders typed legend content into a real map control shell.
/// </summary>
public partial class MapLegend : ComponentBase, IAsyncDisposable
{
    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter, EditorRequired]
    public MapLegendDefinition Definition { get; set; } = null!;

    [Parameter]
    public LegendControlOptions ControlOptions { get; set; } = LegendControlOptions.Default;

    [Parameter]
    public RenderFragment<MapLegendItemTemplateContext>? ItemTemplate { get; set; }

    [Parameter]
    public EventCallback<MapLegendVisibilityChangedEventArgs> OnItemVisibilityChanged { get; set; }

    private readonly Dictionary<string, bool> _itemVisibility = new(StringComparer.Ordinal);
    private readonly string _contentId = $"sgb-map-legend-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;
    private bool _controlSyncPending = true;
    private bool _visibilitySyncPending = true;
    private bool _registered;
    private LegendControlOptions? _previousControlOptions;
    private readonly HashSet<string> _registeredVisibilityGroupIds = new(StringComparer.Ordinal);

    private ILogger Logger => LoggerFactory.CreateLogger<MapLegend>();

    private string ContentClassName =>
        new CssBuilder()
            .AddClass("sgb-map-legend-content")
            .AddClass(Definition.ClassName, !string.IsNullOrWhiteSpace(Definition.ClassName))
            .Build();

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        ValidateDefinition();
        SyncItemVisibility();
        _visibilitySyncPending = true;

        var shouldResyncShell = _previousControlOptions != ControlOptions;
        _controlSyncPending = _controlSyncPending || !_registered || shouldResyncShell;
        _previousControlOptions = ControlOptions;
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
            if (!ControlOptions.Enable)
            {
                if (_registered)
                {
                    await MapJs.RemoveLegendControlAsync(JsRuntime, Logger, Map.MapReference);
                    _registered = false;
                }

                _controlSyncPending = false;
                return;
            }

            await MapJs.SetLegendControlAsync(
                JsRuntime,
                Logger,
                Map.MapReference,
                ControlOptions,
                _placeholderReference,
                _contentReference
            );

            _registered = ControlOptions.Enable;
            _controlSyncPending = false;
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
            await MapJs.RemoveLegendControlAsync(JsRuntime, Logger, Map.MapReference);
        }
        catch (Exception exception)
        {
            Logger.LogTrace(exception, "Legend control removal skipped during disposal.");
        }
    }

    private async Task SyncVisibilityGroupsAsync()
    {
        if (Map is null)
        {
            return;
        }

        var activeGroupIds = Definition
            .GetItems()
            .Where(item => item.IsToggleable)
            .Select(GetVisibilityGroupId)
            .ToHashSet(StringComparer.Ordinal);

        var removedGroupIds = _registeredVisibilityGroupIds.Except(activeGroupIds, StringComparer.Ordinal).ToArray();
        foreach (var groupId in removedGroupIds)
        {
            await Map.SceneRegistry.UnregisterVisibilityGroupAsync(groupId);
            _registeredVisibilityGroupIds.Remove(groupId);
        }

        foreach (var item in Definition.GetItems().Where(item => item.IsToggleable))
        {
            await Map.SceneRegistry.RegisterVisibilityGroupAsync(BuildVisibilityGroupDescriptor(item));
            _registeredVisibilityGroupIds.Add(GetVisibilityGroupId(item));
        }
    }

    private void ValidateDefinition()
    {
        if (Definition is null)
        {
            throw new InvalidOperationException("A legend definition is required.");
        }

        var duplicateId = Definition
            .GetItems()
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

    private void SyncItemVisibility()
    {
        var nextVisibility = Definition
            .GetItems()
            .ToDictionary(
                item => item.Id,
                item =>
                    _itemVisibility.TryGetValue(item.Id, out var currentVisibility)
                        ? currentVisibility
                        : item.IsVisibleByDefault,
                StringComparer.Ordinal
            );

        _itemVisibility.Clear();

        foreach (var pair in nextVisibility)
        {
            _itemVisibility[pair.Key] = pair.Value;
        }
    }

    private bool GetItemVisibility(string itemId) => _itemVisibility.TryGetValue(itemId, out var visible) && visible;

    private static string GetSectionClassName(MapLegendSectionDefinition section) =>
        new CssBuilder()
            .AddClass("sgb-map-legend-section")
            .AddClass(section.ClassName, !string.IsNullOrWhiteSpace(section.ClassName))
            .Build();

    private string GetItemClassName(MapLegendItemDefinition item) =>
        new CssBuilder()
            .AddClass("sgb-map-legend-item")
            .AddClass("sgb-map-legend-item-toggleable", item.IsToggleable)
            .AddClass("sgb-map-legend-item-off", item.IsToggleable && !GetItemVisibility(item.Id))
            .AddClass(item.ClassName, !string.IsNullOrWhiteSpace(item.ClassName))
            .Build();

    private async Task ToggleItemAsync(MapLegendItemDefinition item, ChangeEventArgs args)
    {
        var visible = args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => false,
        };

        await SetItemVisibilityAsync(item, visible);
    }

    private async Task SetItemVisibilityAsync(MapLegendItemDefinition item, bool visible)
    {
        _itemVisibility[item.Id] = visible;

        if (Map is not null && item.IsToggleable)
        {
            await Map.SceneRegistry.RegisterVisibilityGroupAsync(BuildVisibilityGroupDescriptor(item));
            _registeredVisibilityGroupIds.Add(GetVisibilityGroupId(item));
        }

        if (OnItemVisibilityChanged.HasDelegate)
        {
            await OnItemVisibilityChanged.InvokeAsync(new MapLegendVisibilityChangedEventArgs(item, visible));
        }
    }

    private MapVisibilityGroupDescriptor BuildVisibilityGroupDescriptor(MapLegendItemDefinition item) =>
        new(
            GetVisibilityGroupId(item),
            GetItemVisibility(item.Id),
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

/// <summary>
/// Default legend item template context.
/// </summary>
/// <param name="Item">The legend item definition.</param>
/// <param name="Visible">The current item visibility.</param>
/// <param name="SetVisibilityAsync">Supported toggle callback for custom templates.</param>
public sealed record MapLegendItemTemplateContext(
    MapLegendItemDefinition Item,
    bool Visible,
    Func<bool, Task> SetVisibilityAsync
);
