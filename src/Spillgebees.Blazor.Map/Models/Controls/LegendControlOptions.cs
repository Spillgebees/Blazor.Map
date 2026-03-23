namespace Spillgebees.Blazor.Map.Models.Controls;

/// <summary>
/// Options for the map legend control shell.
/// </summary>
/// <param name="Enable">Whether to render the legend control shell.</param>
/// <param name="Position">Position of the control on the map.</param>
/// <param name="Title">Optional control title shown in the shell header.</param>
/// <param name="Collapsible">Whether the control can be opened and closed.</param>
/// <param name="InitiallyOpen">Whether the control starts opened.</param>
/// <param name="ClassName">Optional additional CSS class applied to the shell.</param>
public sealed record LegendControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    string? Title = "Legend",
    bool Collapsible = true,
    bool InitiallyOpen = true,
    string? ClassName = null
)
{
    /// <summary>
    /// Default legend control options.
    /// </summary>
    public static LegendControlOptions Default => new();
}
