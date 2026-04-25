using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Legends;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Legacy component kept only for migration guidance.
/// </summary>
[Obsolete("MapLegend is no longer used. Configure legend content directly on LegendMapControl.Content.")]
public partial class MapLegend : ComponentBase
{
    [Parameter, EditorRequired]
    public MapLegendDefinition Definition { get; set; } = null!;

    [Parameter, EditorRequired]
    public string ControlId { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment<MapLegendItemTemplateContext>? ItemTemplate { get; set; }

    [Parameter]
    public EventCallback<MapLegendVisibilityChangedEventArgs> OnItemVisibilityChanged { get; set; }

    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(ControlId))
        {
            throw new InvalidOperationException(
                "MapLegend is no longer supported. Configure legend content on LegendMapControl.Content instead."
            );
        }

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
}
