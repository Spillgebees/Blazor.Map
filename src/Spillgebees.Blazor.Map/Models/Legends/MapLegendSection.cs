namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Defines a visual legend section.
/// </summary>
/// <param name="Title">Section title.</param>
/// <param name="Items">Items rendered inside the section.</param>
/// <param name="Description">Optional helper text displayed below the section title.</param>
/// <param name="ClassName">Optional additional CSS class for the section container.</param>
public sealed record MapLegendSection(
    string Title,
    IReadOnlyList<MapLegendItem> Items,
    string? Description = null,
    string? ClassName = null
);
