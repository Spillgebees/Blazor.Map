namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Defines the content rendered inside a map legend control.
/// </summary>
/// <param name="Sections">Legend sections rendered in order.</param>
/// <param name="ClassName">Optional additional CSS class for the legend content root.</param>
public sealed record MapLegendDefinition(IReadOnlyList<MapLegendSectionDefinition> Sections, string? ClassName = null)
{
    /// <summary>
    /// Returns all legend items in declaration order.
    /// </summary>
    public IReadOnlyList<MapLegendItemDefinition> GetItems() => Sections.SelectMany(section => section.Items).ToArray();
}
