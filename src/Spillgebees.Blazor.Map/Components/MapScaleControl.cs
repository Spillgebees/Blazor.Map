using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a scale control subcomponent.
/// </summary>
public sealed class MapScaleControl : MapControlComponentBase
{
    public MapScaleControl()
    {
        Id = "scale";
        Position = ControlPosition.BottomLeft;
    }

    [Parameter]
    public ScaleUnit Unit { get; set; } = ScaleUnit.Metric;

    protected override MapControl BuildControl() => new ScaleMapControl(Id, Enabled, Position, Unit, Order);
}
