using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registers a scale control subcomponent.
/// </summary>
public sealed class ScaleMapControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    [Parameter]
    public string Id { get; set; } = "scale";

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.BottomLeft;

    [Parameter]
    public int Order { get; set; } = 100;

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public ScaleUnit Unit { get; set; } = ScaleUnit.Metric;

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    protected override void OnParametersSet() =>
        _registration.Register(Registry, SectionContext, nameof(ScaleMapControl), Id, BuildControl());

    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(Registry);

    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);

    private MapControlDefinition BuildControl() => new ScaleControlDefinition(Id, Visible, Position, Unit, Order);
}
