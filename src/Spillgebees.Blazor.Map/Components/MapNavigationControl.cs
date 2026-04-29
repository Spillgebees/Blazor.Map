using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Registers a navigation control subcomponent.
/// </summary>
public sealed class MapNavigationControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    public MapNavigationControl()
    {
        Id = "navigation";
        Position = ControlPosition.TopRight;
    }

    [Parameter]
    public string Id { get; set; } = string.Empty;

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 100;

    [Parameter]
    public bool Enabled { get; set; } = true;

    [Parameter]
    public bool ShowCompass { get; set; } = true;

    [Parameter]
    public bool ShowZoom { get; set; } = true;

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    protected override void OnParametersSet() =>
        _registration.Register(Registry, SectionContext, nameof(MapNavigationControl), Id, BuildControl());

    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(Registry);

    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);

    private MapControl BuildControl() => new NavigationMapControl(Id, Enabled, Position, ShowCompass, ShowZoom, Order);
}
