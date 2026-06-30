using System.Diagnostics.CodeAnalysis;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Stores shared map-level display items for toggling layers and feature subsets.
/// </summary>
public sealed class MapDisplayState
{
    private readonly Dictionary<string, MapDisplayItem> _items = new(StringComparer.Ordinal);
    private readonly List<string> _itemIds = [];

    /// <summary>Initializes a new map display state.</summary>
    public MapDisplayState(IEnumerable<MapDisplayItem> items)
    {
        ReplaceCore(items);
    }

    /// <summary>Raised when an item changes or the collection is replaced.</summary>
    public event EventHandler<MapDisplayChangedEventArgs>? Changed;

    /// <summary>Gets current display items.</summary>
    [AllowNull]
    public IReadOnlyList<MapDisplayItem> Items
    {
        get => field ??= Array.AsReadOnly(_itemIds.Select(id => _items[id]).ToArray());
        private set;
    }

    /// <summary>Returns whether an item exists.</summary>
    public bool Contains(string itemId) => _items.ContainsKey(itemId);

    /// <summary>Attempts to get a display item.</summary>
    public bool TryGetItem(string itemId, [MaybeNullWhen(false)] out MapDisplayItem item) =>
        _items.TryGetValue(itemId, out item);

    /// <summary>Gets whether an item is on.</summary>
    public bool IsOn(string itemId) => GetItem(itemId).IsOn;

    /// <summary>Sets whether an item is on.</summary>
    public void SetOn(string itemId, bool on)
    {
        var item = GetItem(itemId);
        if (item.IsOn == on)
        {
            return;
        }

        Upsert(item with { IsOn = on });
    }

    /// <summary>Toggles a display item.</summary>
    public void Toggle(string itemId) => SetOn(itemId, !IsOn(itemId));

    /// <summary>Adds or replaces a display item.</summary>
    public void Upsert(MapDisplayItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id))
        {
            _itemIds.Add(item.Id);
        }

        _items[item.Id] = item;
        Items = null;
        Changed?.Invoke(this, new MapDisplayChangedEventArgs(item.Id, item, false));
    }

    /// <summary>Replaces all display items.</summary>
    public void Replace(IEnumerable<MapDisplayItem> items)
    {
        ReplaceCore(items);
        Changed?.Invoke(this, new MapDisplayChangedEventArgs(null, null, true));
    }

    private MapDisplayItem GetItem(string itemId)
    {
        if (!_items.TryGetValue(itemId, out var item))
        {
            throw new KeyNotFoundException($"Display item '{itemId}' was not found.");
        }

        return item;
    }

    private void ReplaceCore(IEnumerable<MapDisplayItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var next = items.ToArray();
        var duplicate = next.GroupBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Display item IDs must be unique. Duplicate ID: '{duplicate.Key}'.",
                nameof(items)
            );
        }

        _items.Clear();
        _itemIds.Clear();
        foreach (var item in next)
        {
            _items[item.Id] = item;
            _itemIds.Add(item.Id);
        }

        Items = null;
    }
}
