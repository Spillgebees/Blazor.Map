using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Renders a purpose-built display control backed by <see cref="MapDisplayState" />.
/// </summary>
public partial class DisplayMapControl : ComponentBase, IDisposable
{
    private MapDisplayState? _subscribedDisplay;

    [CascadingParameter]
    private MapDisplayState? Display { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 450;

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string Label { get; set; } = "Display";

    [Parameter]
    public string Title { get; set; } = "Display";

    [Parameter]
    public bool InitiallyOpen { get; set; }

    [Parameter]
    public string? MaxWidth { get; set; }

    [Parameter]
    public string? PanelClass { get; set; }

    [Parameter]
    public IReadOnlyList<string>? ItemIds { get; set; }

    [Parameter]
    public RenderFragment<MapDisplayControlItemContext>? ItemTemplate { get; set; }

    private IReadOnlyList<MapDisplayItem> Items => ResolveItems();

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

        if (Display is null)
        {
            throw new InvalidOperationException("DisplayMapControl requires SgbMap.Display.");
        }

        if (ReferenceEquals(_subscribedDisplay, Display))
        {
            return;
        }

        if (_subscribedDisplay is not null)
        {
            _subscribedDisplay.Changed -= HandleDisplayChanged;
        }

        _subscribedDisplay = Display;
        _subscribedDisplay.Changed += HandleDisplayChanged;
    }

    public void Dispose()
    {
        if (_subscribedDisplay is not null)
        {
            _subscribedDisplay.Changed -= HandleDisplayChanged;
            _subscribedDisplay = null;
        }
    }

    private IReadOnlyList<MapDisplayItem> ResolveItems()
    {
        if (Display is null)
        {
            return [];
        }

        if (ItemIds is null)
        {
            return Display.Items;
        }

        var items = new List<MapDisplayItem>(ItemIds.Count);
        foreach (var itemId in ItemIds)
        {
            if (!Display.TryGetItem(itemId, out var item))
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
        Display!.SetOn(itemId, on);
        return Task.CompletedTask;
    }

    private void HandleDisplayChanged(object? sender, MapDisplayChangedEventArgs args) =>
        _ = InvokeAsync(StateHasChanged);

    private static string ResolveLabel(MapDisplayItem item) =>
        string.IsNullOrWhiteSpace(item.Label) ? item.Id : item.Label;
}
