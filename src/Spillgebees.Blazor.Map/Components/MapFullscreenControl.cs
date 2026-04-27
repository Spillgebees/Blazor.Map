using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a fullscreen control subcomponent.
/// </summary>
public sealed class MapFullscreenControl : MapControlComponentBase
{
    public MapFullscreenControl()
    {
        Id = "fullscreen";
        Order = 200;
    }

    protected override MapControl BuildControl() => new FullscreenMapControl(Id, Enabled, Position, Order);
}
