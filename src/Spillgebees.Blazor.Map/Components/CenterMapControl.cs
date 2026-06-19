using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registers a center control subcomponent.
/// </summary>
public sealed class CenterMapControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    /// <summary>Unique control identifier within the map. Defaults to <c>"center"</c>.</summary>
    [Parameter]
    public string Id { get; set; } = "center";

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.TopLeft" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopLeft;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 100.</summary>
    [Parameter]
    public int Order { get; set; } = 100;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Optional custom SVG markup for the control glyph. Trusted markup; do not pass user-supplied
    /// content. Defaults to the built-in glyph when null.
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    [CascadingParameter]
    private MapControlRegistryContext? _registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() =>
        _registration.Register(_registry, _sectionContext, nameof(CenterMapControl), Id, BuildControl());

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(_registry);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _registration.DisposeAsync(_registry);

    private CenterControlDefinition BuildControl() => new(Id, Visible, Position, Order, Icon);
}
