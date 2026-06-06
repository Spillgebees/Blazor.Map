using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registers a center control subcomponent.
/// </summary>
public sealed class CenterMapControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    [Parameter]
    public string Id { get; set; } = "center";

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopLeft;

    [Parameter]
    public int Order { get; set; } = 100;

    [Parameter]
    public bool Visible { get; set; } = true;

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    protected override void OnParametersSet() =>
        _registration.Register(Registry, SectionContext, nameof(CenterMapControl), Id, BuildControl());

    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(Registry);

    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);

    private MapControlDefinition BuildControl() => new CenterControlDefinition(Id, Visible, Position, Order);
}
