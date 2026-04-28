using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a center control subcomponent.
/// </summary>
public sealed class MapCenterControl : MapControlComponentBase
{
    public MapCenterControl()
    {
        Id = "center";
        Position = ControlPosition.TopLeft;
    }

    protected override MapControl BuildControl() => new CenterMapControl(Id, Enabled, Position, Order);
}
