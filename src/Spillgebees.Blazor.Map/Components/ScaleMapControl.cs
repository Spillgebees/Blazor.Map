using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registers a scale control subcomponent.
/// </summary>
public sealed class ScaleMapControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    /// <summary>Unique control identifier within the map. Defaults to <c>"scale"</c>.</summary>
    [Parameter]
    public string Id { get; set; } = "scale";

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.BottomLeft" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.BottomLeft;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 100.</summary>
    [Parameter]
    public int Order { get; set; } = 100;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>Unit system displayed by the scale. Defaults to <see cref="ScaleUnit.Metric" />.</summary>
    [Parameter]
    public ScaleUnit Unit { get; set; } = ScaleUnit.Metric;

    [CascadingParameter]
    private MapControlRegistryContext? _registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() =>
        _registration.Register(_registry, _sectionContext, nameof(ScaleMapControl), Id, BuildControl());

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(_registry);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _registration.DisposeAsync(_registry);

    private ScaleControlDefinition BuildControl() => new(Id, Visible, Position, Unit, Order);
}
