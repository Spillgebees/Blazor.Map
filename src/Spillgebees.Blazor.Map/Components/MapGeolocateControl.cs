using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a geolocate control subcomponent.
/// </summary>
public sealed class MapGeolocateControl : MapControlComponentBase
{
    public MapGeolocateControl()
    {
        Id = "geolocate";
        Order = 300;
    }

    [Parameter]
    public bool TrackUser { get; set; }

    protected override MapControl BuildControl() => new GeolocateMapControl(Id, Enabled, Position, TrackUser, Order);
}
