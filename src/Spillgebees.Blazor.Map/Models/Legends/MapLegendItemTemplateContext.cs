namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Default legend item template context.
/// </summary>
/// <param name="Item">The legend item definition.</param>
/// <param name="Selected">The current item selection state.</param>
/// <param name="SetSelectedAsync">
/// Callback invoked by templates to set the selection state for this specific item.
/// Pass <see langword="true"/> to mark the item selected, <see langword="false"/> to clear selection.
/// </param>
public sealed record MapLegendItemTemplateContext(MapLegendItem Item, bool Selected, Func<bool, Task> SetSelectedAsync);
