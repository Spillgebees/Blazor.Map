using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

internal sealed class MapLegendDisplayBinder : IDisposable
{
    private readonly Func<Task> _requestRender;
    private MapDisplayState? _display;

    public MapLegendDisplayBinder(Func<Task> requestRender)
    {
        _requestRender = requestRender;
    }

    public void UpdateDisplaySubscription(MapDisplayState? display)
    {
        if (ReferenceEquals(_display, display))
        {
            return;
        }

        if (_display is not null)
        {
            _display.Changed -= HandleDisplayChanged;
        }

        _display = display;

        if (_display is not null)
        {
            _display.Changed += HandleDisplayChanged;
        }
    }

    public string GetItemClassName(MapLegendItem item)
    {
        var isToggleable = IsToggleable(item);
        return new CssBuilder()
            .AddClass("sgb-map-legend-item")
            .AddClass("sgb-map-legend-item-toggleable", isToggleable)
            .AddClass("sgb-map-legend-item-off", isToggleable && !GetItemOn(item))
            .AddClass(item.ClassName, !string.IsNullOrWhiteSpace(item.ClassName))
            .Build();
    }

    public static bool IsToggleable(MapLegendItem item) => item.DisplayItemId is not null;

    public bool GetItemOn(MapLegendItem item) =>
        ResolveDisplayItem(item, required: item.DisplayItemId is not null)?.IsOn ?? true;

    public async Task ToggleItemAsync(MapLegendItem item, ChangeEventArgs args)
    {
        var selected = args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => throw new InvalidOperationException(
                "Legend display toggle expected a bool or parseable string value."
            ),
        };

        await SetItemOnAsync(item, selected);
    }

    public Task SetItemOnAsync(MapLegendItem item, bool selected)
    {
        var displayItem = ResolveDisplayItem(item, required: true);
        if (displayItem is not null)
        {
            _display!.SetOn(displayItem.Id, selected);
        }

        return Task.CompletedTask;
    }

    public MapLegendItemTemplateContext BuildTemplateContext(MapLegendItem item)
    {
        var displayItem = ResolveDisplayItem(item, required: item.DisplayItemId is not null);
        return new(
            item,
            displayItem is not null,
            displayItem?.IsOn ?? true,
            displayItem,
            selected => SetItemOnAsync(item, selected)
        );
    }

    public MapDisplayItem? ResolveDisplayItem(MapLegendItem item, bool required)
    {
        if (item.DisplayItemId is null)
        {
            return null;
        }

        if (_display is not null && _display.TryGetItem(item.DisplayItemId, out var displayItem))
        {
            return displayItem;
        }

        if (!required)
        {
            return null;
        }

        throw new InvalidOperationException(
            $"Legend item '{item.Id}' references missing display item '{item.DisplayItemId}'."
        );
    }

    public void ValidateDisplayItems(IEnumerable<MapLegendItem> items)
    {
        var missingItem = items.FirstOrDefault(item =>
            item.DisplayItemId is not null && (_display is null || !_display.Contains(item.DisplayItemId))
        );
        if (missingItem is null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Legend item '{missingItem.Id}' references missing display item '{missingItem.DisplayItemId}'."
        );
    }

    public void Dispose()
    {
        if (_display is not null)
        {
            _display.Changed -= HandleDisplayChanged;
            _display = null;
        }
    }

    private void HandleDisplayChanged(object? sender, MapDisplayChangedEventArgs args) => _ = _requestRender();
}
