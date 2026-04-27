using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a navigation control subcomponent.
/// </summary>
public sealed class MapNavigationControl : MapControlComponentBase
{
    public MapNavigationControl()
    {
        Id = "navigation";
        Position = ControlPosition.TopRight;
    }

    [Parameter]
    public bool ShowCompass { get; set; } = true;

    [Parameter]
    public bool ShowZoom { get; set; } = true;

    protected override MapControl BuildControl() =>
        new NavigationMapControl(Id, Enabled, Position, ShowCompass, ShowZoom, Order);
}
