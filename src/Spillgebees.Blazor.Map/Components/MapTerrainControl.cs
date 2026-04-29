using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a terrain control subcomponent.
/// </summary>
public sealed class MapTerrainControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    public MapTerrainControl()
    {
        Id = "terrain";
        Order = 400;
    }

    [Parameter]
    public string Id { get; set; } = string.Empty;

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 100;

    [Parameter]
    public bool Enabled { get; set; } = true;

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    protected override void OnParametersSet() =>
        _registration.Register(Registry, SectionContext, nameof(MapTerrainControl), Id, BuildControl());

    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(Registry);

    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);

    private MapControl BuildControl() => new TerrainMapControl(Id, Enabled, Position, Order);
}
