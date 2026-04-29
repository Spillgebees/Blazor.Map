using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a geolocate control subcomponent.
/// </summary>
public sealed class MapGeolocateControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    [Parameter]
    public string Id { get; set; } = "geolocate";

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 300;

    [Parameter]
    public bool Enabled { get; set; } = true;

    [Parameter]
    public bool TrackUser { get; set; }

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    protected override void OnParametersSet() =>
        _registration.Register(Registry, SectionContext, nameof(MapGeolocateControl), Id, BuildControl());

    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(Registry);

    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);

    private MapControl BuildControl() => new GeolocateMapControl(Id, Enabled, Position, TrackUser, Order);
}
