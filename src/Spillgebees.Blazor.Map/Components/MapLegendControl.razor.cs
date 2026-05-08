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
    private MapLegendVisibilityBinder? _visibilityBinder;

    private MapLegendVisibilityBinder VisibilityBinder =>
        _visibilityBinder ??= new MapLegendVisibilityBinder(() => InvokeAsync(StateHasChanged));

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
        VisibilityBinder.UpdateVisibilitySubscription(LayerVisibility);
        ValidateDefinition();

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
        _visibilityBinder?.Dispose();
        _visibilityBinder = null;

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
