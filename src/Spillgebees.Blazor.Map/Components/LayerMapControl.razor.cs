using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Visibility;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Renders a purpose-built layer visibility control backed by <see cref="MapLayerVisibilityState"/>.
/// </summary>
public partial class LayerMapControl : ComponentBase, IDisposable
{
    private MapLayerVisibilityState? _subscribedLayerVisibility;

    [CascadingParameter]
    private MapLayerVisibilityState? LayerVisibility { get; set; }

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
    public string Label { get; set; } = "Layers";

    [Parameter]
    public string Title { get; set; } = "Layers";

    [Parameter]
    public bool InitiallyOpen { get; set; }

    [Parameter]
    public string? MaxWidth { get; set; }

    [Parameter]
    public string? PanelClass { get; set; }

    [Parameter]
    public IReadOnlyList<string>? GroupIds { get; set; }

    [Parameter]
    public RenderFragment<MapLayerControlItemContext>? ItemTemplate { get; set; }

    private IReadOnlyList<MapLayerVisibilityGroup> Items => ResolveItems();

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

        if (LayerVisibility is null)
        {
            throw new InvalidOperationException("LayerMapControl requires SgbMap.LayerVisibility.");
        }

        if (!ReferenceEquals(_subscribedLayerVisibility, LayerVisibility))
        {
            if (_subscribedLayerVisibility is not null)
            {
                _subscribedLayerVisibility.Changed -= HandleVisibilityChanged;
            }

            _subscribedLayerVisibility = LayerVisibility;
            _subscribedLayerVisibility.Changed += HandleVisibilityChanged;
        }
    }

    public void Dispose()
    {
        if (_subscribedLayerVisibility is not null)
        {
            _subscribedLayerVisibility.Changed -= HandleVisibilityChanged;
            _subscribedLayerVisibility = null;
        }
    }

    private IReadOnlyList<MapLayerVisibilityGroup> ResolveItems()
    {
        if (LayerVisibility is null)
        {
            return [];
        }

        if (GroupIds is null)
        {
            return LayerVisibility.Groups;
        }

        var items = new List<MapLayerVisibilityGroup>(GroupIds.Count);
        foreach (var groupId in GroupIds)
        {
            if (!LayerVisibility.TryGetGroup(groupId, out var group))
            {
                throw new InvalidOperationException($"Layer visibility group '{groupId}' was not found.");
            }

            items.Add(group);
        }

        return items;
    }

    private MapLayerControlItemContext BuildTemplateContext(MapLayerVisibilityGroup item) =>
        new(item, item.IsVisible, visible => SetVisibleAsync(item.Id, visible));

    private Task ToggleItemAsync(MapLayerVisibilityGroup item, ChangeEventArgs args)
    {
        var visible = args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => throw new InvalidOperationException("Layer toggle expected a bool or parseable string value."),
        };

        return SetVisibleAsync(item.Id, visible);
    }

    private Task SetVisibleAsync(string groupId, bool visible)
    {
        LayerVisibility!.SetVisible(groupId, visible);
        return Task.CompletedTask;
    }

    private void HandleVisibilityChanged(object? sender, MapLayerVisibilityChangedEventArgs args) =>
        _ = InvokeAsync(StateHasChanged);

    private static string ResolveLabel(MapLayerVisibilityGroup group) =>
        string.IsNullOrWhiteSpace(group.Label) ? group.Id : group.Label;
}
