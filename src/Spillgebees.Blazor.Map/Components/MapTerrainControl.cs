using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a terrain control subcomponent.
/// </summary>
public sealed class MapTerrainControl : MapControlComponentBase
{
    public MapTerrainControl()
    {
        Id = "terrain";
        Order = 400;
    }

    protected override MapControl BuildControl() => new TerrainMapControl(Id, Enabled, Position, Order);
}
