using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a legend control and owns its Blazor content host.
/// </summary>
public partial class MapLegendControl : ComponentBase, IAsyncDisposable
{
    private const string CustomControlKind = "legend";
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private readonly Dictionary<string, bool> _itemSelection = new(StringComparer.Ordinal);
    private readonly string _contentId = $"sgb-map-legend-content-{Guid.NewGuid():N}";
    private readonly HashSet<string> _registeredVisibilityGroupIds = new(StringComparer.Ordinal);
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;
    private bool _controlSyncPending = true;
    private bool _contentSyncPending = true;
    private bool _visibilitySyncPending = true;
    private string? _registeredControlId;
    private readonly List<string> _pendingRemovalIds = [];

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

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
    public MapLegendDefinition Definition { get; set; } = new([]);

    [Parameter]
    public RenderFragment<MapLegendItemTemplateContext>? ItemTemplate { get; set; }

    [Parameter]
    public EventCallback<MapLegendVisibilityChangedEventArgs> OnItemVisibilityChanged { get; set; }

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
        SyncItemSelection();

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
        _visibilitySyncPending = true;
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

        foreach (var pendingRemovalId in _pendingRemovalIds.ToArray())
        {
            await Registry.RemoveControlContentAsync(pendingRemovalId);
            _pendingRemovalIds.Remove(pendingRemovalId);
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

        if (_visibilitySyncPending)
        {
            await SyncVisibilityGroupsAsync();
            _visibilitySyncPending = false;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await UnregisterVisibilityGroupsAsync();

        if (Registry is null || string.IsNullOrWhiteSpace(_registeredControlId))
        {
            return;
        }

        var controlId = _registeredControlId;
        Registry.UnregisterByOwner(_ownerId);

        if (!Registry.IsReady)
        {
            _registeredControlId = null;
            return;
        }

        try
        {
            await Registry.RemoveControlContentAsync(controlId);
            await Registry.SyncControlsAsync();
        }
        catch (Exception)
        {
            // disposal may run after JS runtime teardown.
        }
        finally
        {
            _registeredControlId = null;
        }
    }

    private LegendMapControl BuildControl() =>
        new(
            Id,
            new MapControlPlacement(Position, Order, Enabled),
            new LegendChromeOptions(Title, Collapsible, InitiallyOpen, Class),
            new LegendContentOptions(Definition, ItemTemplate, OnItemVisibilityChanged)
        );

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

        if (OnItemVisibilityChanged.HasDelegate)
        {
            await OnItemVisibilityChanged.InvokeAsync(new MapLegendVisibilityChangedEventArgs(item, selected));
        }
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
        var nextSelection = Definition
            .GetItems()
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

        var activeGroupIds = Enabled
            ? Definition
                .GetItems()
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

        if (!Enabled)
        {
            return;
        }

        foreach (var item in Definition.GetItems().Where(item => item.IsToggleable))
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
