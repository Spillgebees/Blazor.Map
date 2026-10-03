using System.Diagnostics.CodeAnalysis;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Stores display items for toggling layers and feature subsets. One state can drive
/// several maps: switching an item applies to every map, while items that were never
/// switched follow each map's own style.
/// </summary>
public sealed class MapDisplayState
{
    private readonly Dictionary<string, MapDisplayItem> _items = new(StringComparer.Ordinal);
    private readonly List<string> _itemIds = [];

    // style defaults of unset items, reported per bound map
    private readonly Dictionary<object, IReadOnlyDictionary<string, bool>> _styleDefaults = new(
        ReferenceEqualityComparer.Instance
    );

    /// <summary>Initializes a new map display state.</summary>
    public MapDisplayState(IEnumerable<MapDisplayItem> items)
    {
        ReplaceCore(items);
    }

    /// <summary>
    /// Raised when an item changes, the collection is replaced, or a map reports the style
    /// defaults of unset items (<see cref="MapDisplayChangedEventArgs.ItemId"/> is null).
    /// </summary>
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

    /// <summary>
    /// Gets whether an item is on: its <see cref="MapDisplayItem.IsOn"/> when set;
    /// otherwise the style default (on when any bound map shows the item's layers), and
    /// on while no map has reported yet.
    /// </summary>
    public bool IsOn(string itemId) => GetItem(itemId).IsOn ?? AnyMapDefault(itemId) ?? true;

    /// <summary>Sets whether an item is on; the value then applies to every bound map.</summary>
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

    /// <summary>Whether an item is on as seen from one map: its own style default wins for unset items.</summary>
    internal bool IsOn(string itemId, object map) =>
        GetItem(itemId).IsOn ?? MapDefault(map, itemId) ?? AnyMapDefault(itemId) ?? true;

    /// <summary>Whether an unset item still waits for <paramref name="map"/> to report its style default.</summary>
    internal bool IsPending(string itemId, object map) =>
        GetItem(itemId).IsOn is null && !_styleDefaults.ContainsKey(map);

    /// <summary>Records the style defaults a map reported for its unset items.</summary>
    internal void SetStyleDefaults(object map, IReadOnlyDictionary<string, bool> defaults)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(defaults);
        if (
            _styleDefaults.TryGetValue(map, out var current)
            && current.Count == defaults.Count
            && defaults.All(entry => current.TryGetValue(entry.Key, out var value) && value == entry.Value)
        )
        {
            return;
        }

        _styleDefaults[map] = defaults;
        Changed?.Invoke(this, new MapDisplayChangedEventArgs(null, null, false));
    }

    /// <summary>Forgets a map's style defaults (map disposed or bound to another state).</summary>
    internal void RemoveStyleDefaults(object map)
    {
        if (_styleDefaults.Remove(map))
        {
            Changed?.Invoke(this, new MapDisplayChangedEventArgs(null, null, false));
        }
    }

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

    private bool? MapDefault(object map, string itemId) =>
        _styleDefaults.TryGetValue(map, out var defaults) && defaults.TryGetValue(itemId, out var on) ? on : null;

    private bool? AnyMapDefault(string itemId)
    {
        bool? result = null;
        foreach (var defaults in _styleDefaults.Values)
        {
            if (defaults.TryGetValue(itemId, out var on))
            {
                if (on)
                {
                    return true;
                }

                result = false;
            }
        }

        return result;
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
