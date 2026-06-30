using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Renders a purpose-built display control backed by <see cref="MapDisplayState" />.
/// </summary>
public partial class DisplayMapControl : ComponentBase, IDisposable
{
    private MapDisplayState? _subscribedDisplay;

    [CascadingParameter]
    private MapDisplayState? _display { get; set; }

    /// <summary>Unique control identifier within the map.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.TopRight" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 450.</summary>
    [Parameter]
    public int Order { get; set; } = 450;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>Additional CSS class applied to the control container.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Accessible label for the control's toggle button; must be non-empty. Defaults to <c>"Display"</c>.</summary>
    [Parameter]
    public string Label { get; set; } = "Display";

    /// <summary>Title shown in the panel header. Defaults to <c>"Display"</c>.</summary>
    [Parameter]
    public string Title { get; set; } = "Display";

    /// <summary>Whether the panel starts open.</summary>
    [Parameter]
    public bool InitiallyOpen { get; set; }

    /// <summary>Maximum width of the panel as a CSS length value.</summary>
    [Parameter]
    public string? MaxWidth { get; set; }

    /// <summary>Additional CSS class applied to the panel content container.</summary>
    [Parameter]
    public string? PanelClass { get; set; }

    /// <summary>Display item ids to show, in order; shows all items when not set.</summary>
    [Parameter]
    public IReadOnlyList<string>? ItemIds { get; set; }

    /// <summary>Optional template used to render each display item.</summary>
    [Parameter]
    public RenderFragment<MapDisplayControlItemContext>? ItemTemplate { get; set; }

    private IReadOnlyList<MapDisplayItem> _items => ResolveItems();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("A non-empty Id is required.");
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }

        if (_display is null)
        {
            throw new InvalidOperationException(
                "DisplayMapControl requires a cascading MapDisplayState (SgbMap Display parameter)."
            );
        }

        if (ReferenceEquals(_subscribedDisplay, _display))
        {
            return;
        }

        _subscribedDisplay?.Changed -= HandleDisplayChanged;

        _subscribedDisplay = _display;
        _subscribedDisplay.Changed += HandleDisplayChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _subscribedDisplay?.Changed -= HandleDisplayChanged;
        _subscribedDisplay = null;
        GC.SuppressFinalize(this);
    }

    private IReadOnlyList<MapDisplayItem> ResolveItems()
    {
        if (_display is null)
        {
            return [];
        }

        if (ItemIds is null)
        {
            return _display.Items;
        }

        var items = new List<MapDisplayItem>(ItemIds.Count);
        foreach (var itemId in ItemIds)
        {
            if (!_display.TryGetItem(itemId, out var item))
            {
                throw new InvalidOperationException($"Display item '{itemId}' was not found.");
            }

            items.Add(item);
        }

        return items;
    }

    private MapDisplayControlItemContext BuildTemplateContext(MapDisplayItem item) =>
        new(item, item.IsOn, on => SetOnAsync(item.Id, on));

    private Task ToggleItemAsync(MapDisplayItem item, ChangeEventArgs args)
    {
        var on = args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => throw new InvalidOperationException("Display toggle expected a bool or parseable string value."),
        };

        return SetOnAsync(item.Id, on);
    }

    private Task SetOnAsync(string itemId, bool on)
    {
        _display!.SetOn(itemId, on);
        return Task.CompletedTask;
    }

    private void HandleDisplayChanged(object? sender, MapDisplayChangedEventArgs args) =>
        _ = InvokeAsync(StateHasChanged);

    private static string ResolveLabel(MapDisplayItem item) =>
        string.IsNullOrWhiteSpace(item.Label) ? item.Id : item.Label;
}
