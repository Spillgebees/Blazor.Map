namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Default legend item template context.
/// </summary>
/// <param name="Item">The legend item definition.</param>
/// <param name="Selected">The current item selection state.</param>
/// <param name="SetSelectedAsync">Supported toggle callback for custom templates.</param>
public sealed record MapLegendItemTemplateContext(
    MapLegendItemDefinition Item,
    bool Selected,
    Func<bool, Task> SetSelectedAsync
);
