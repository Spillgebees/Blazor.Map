using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Models.Visibility;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a legend control and owns its Blazor content host.
/// </summary>
public partial class MapLegendControl : ComponentBase, IAsyncDisposable
{
    private const string CustomControlKind = "legend";
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private readonly string _contentId = $"sgb-map-legend-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;
    private bool _controlSyncPending = true;
    private bool _contentSyncPending = true;
    private string? _registeredControlId;
    private readonly List<string> _pendingRemovalIds = [];
    private MapLayerVisibilityState? _subscribedLayerVisibility;

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [CascadingParameter]
    private MapLayerVisibilityState? LayerVisibility { get; set; }

    [Parameter]
    public string Id { get; set; } = "legend";

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 500;

    [Parameter]
    public bool Enabled { get; set; } = true;

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
        if (Registry is null)
        {
            throw new InvalidOperationException("MapLegendControl must be placed inside a map.");
        }

        ValidateControl();
        ValidateDefinition();
        UpdateVisibilitySubscription();

        if (
            !string.IsNullOrWhiteSpace(_registeredControlId)
            && !string.Equals(_registeredControlId, Id, StringComparison.Ordinal)
        )
        {
            _pendingRemovalIds.Add(_registeredControlId);
            _registeredControlId = null;
            _contentSyncPending = true;
        }

        var changed = Registry.Register(_ownerId, BuildControl());
        _registeredControlId = Id;
        _controlSyncPending = _controlSyncPending || changed;
        _contentSyncPending = _contentSyncPending || changed;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Registry is null || Map is null || string.IsNullOrWhiteSpace(_registeredControlId))
        {
            return;
        }

        var ready = await Registry.WhenReadyAsync();
        if (!ready)
        {
            return;
        }

        var pendingRemovalIds = _pendingRemovalIds.ToArray();
        _pendingRemovalIds.Clear();

        foreach (var pendingRemovalId in pendingRemovalIds)
        {
            await Registry.RemoveControlContentAsync(pendingRemovalId);
        }

        if (_controlSyncPending)
        {
            await Registry.SyncControlsAsync();
            _controlSyncPending = false;
        }

        if (!Enabled)
        {
            await Registry.RemoveControlContentAsync(_registeredControlId);
            _contentSyncPending = false;
        }
        else if (_contentSyncPending)
        {
            await Registry.SetControlContentAsync(Id, CustomControlKind, _placeholderReference, _contentReference);
            _contentSyncPending = false;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_subscribedLayerVisibility is not null)
        {
            _subscribedLayerVisibility.Changed -= HandleLayerVisibilityChanged;
            _subscribedLayerVisibility = null;
        }

        if (Registry is null)
        {
            return;
        }

        var controlId = _registeredControlId;
        var pendingRemovalIds = _pendingRemovalIds.ToArray();
        Registry.UnregisterByOwner(_ownerId);

        try
        {
            if (!Registry.IsReady)
            {
                _pendingRemovalIds.Clear();
                return;
            }

            var removalIds = pendingRemovalIds
                .Append(controlId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var removalId in removalIds)
            {
                await Registry.RemoveControlContentAsync(removalId!);
            }

            if (removalIds.Length > 0)
            {
                await Registry.SyncControlsAsync();
            }
        }
        catch (Exception)
        {
            // disposal may run after JS runtime teardown.
        }
        finally
        {
            _registeredControlId = null;
            _pendingRemovalIds.Clear();
        }
    }

    private LegendMapControl BuildControl() =>
        new(
            Id,
            new MapControlPlacement(Position, Order, Enabled),
            new LegendChromeOptions(Title, Collapsible, InitiallyOpen, Class),
            new LegendContentOptions(Definition, ItemTemplate)
        );

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

    private static bool IsToggleable(MapLegendItem item) => item.VisibilityGroupId is not null;

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
            var missingGroupItem = Definition
                .GetItems()
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
