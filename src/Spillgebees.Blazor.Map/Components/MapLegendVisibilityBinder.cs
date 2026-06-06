using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

internal sealed class MapLegendVisibilityBinder : IDisposable
{
    private readonly Func<Task> _requestRender;
    private MapLayerVisibilityState? _layerVisibility;

    public MapLegendVisibilityBinder(Func<Task> requestRender)
    {
        _requestRender = requestRender;
    }

    public void UpdateVisibilitySubscription(MapLayerVisibilityState? layerVisibility)
    {
        if (ReferenceEquals(_layerVisibility, layerVisibility))
        {
            return;
        }

        if (_layerVisibility is not null)
        {
            _layerVisibility.Changed -= HandleLayerVisibilityChanged;
        }

        _layerVisibility = layerVisibility;

        if (_layerVisibility is not null)
        {
            _layerVisibility.Changed += HandleLayerVisibilityChanged;
        }
    }

    public string GetItemClassName(MapLegendItem item)
    {
        var isToggleable = IsToggleable(item);
        return new CssBuilder()
            .AddClass("sgb-map-legend-item")
            .AddClass("sgb-map-legend-item-toggleable", isToggleable)
            .AddClass("sgb-map-legend-item-off", isToggleable && !GetItemVisible(item))
            .AddClass(item.ClassName, !string.IsNullOrWhiteSpace(item.ClassName))
            .Build();
    }

    public static bool IsToggleable(MapLegendItem item) => item.VisibilityGroupId is not null;

    public bool GetItemVisible(MapLegendItem item) =>
        ResolveVisibilityGroup(item, required: item.VisibilityGroupId is not null)?.IsVisible ?? true;

    public async Task ToggleItemAsync(MapLegendItem item, ChangeEventArgs args)
    {
        var selected = args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => false,
        };

        await SetItemVisibleAsync(item, selected);
    }

    public Task SetItemVisibleAsync(MapLegendItem item, bool selected)
    {
        var group = ResolveVisibilityGroup(item, required: true);
        if (group is not null)
        {
            _layerVisibility!.SetVisible(group.Id, selected);
        }

        return Task.CompletedTask;
    }

    public MapLegendItemTemplateContext BuildTemplateContext(MapLegendItem item)
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

    public MapLayerVisibilityGroup? ResolveVisibilityGroup(MapLegendItem item, bool required)
    {
        if (item.VisibilityGroupId is null)
        {
            return null;
        }

        if (_layerVisibility is not null && _layerVisibility.TryGetGroup(item.VisibilityGroupId, out var group))
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

    public void ValidateVisibilityGroups(IEnumerable<MapLegendItem> items)
    {
        var missingGroupItem = items.FirstOrDefault(item =>
            item.VisibilityGroupId is not null
            && (_layerVisibility is null || !_layerVisibility.Contains(item.VisibilityGroupId))
        );

        if (missingGroupItem is null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Legend item '{missingGroupItem.Id}' references missing layer visibility group '{missingGroupItem.VisibilityGroupId}'."
        );
    }

    public void Dispose()
    {
        if (_layerVisibility is not null)
        {
            _layerVisibility.Changed -= HandleLayerVisibilityChanged;
            _layerVisibility = null;
        }
    }

    private void HandleLayerVisibilityChanged(object? sender, MapLayerVisibilityChangedEventArgs args) =>
        _ = _requestRender();
}
