using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registers a fullscreen control subcomponent.
/// </summary>
public sealed class FullscreenMapControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    /// <summary>Unique control identifier within the map. Defaults to <c>"fullscreen"</c>.</summary>
    [Parameter]
    public string Id { get; set; } = "fullscreen";

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.TopRight" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 200.</summary>
    [Parameter]
    public int Order { get; set; } = 200;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Optional custom SVG markup for the "enter fullscreen" glyph. Trusted markup; do not pass
    /// user-supplied content. Defaults to the built-in glyph when null.
    /// </summary>
    [Parameter]
    public string? EnterIcon { get; set; }

    /// <summary>
    /// Optional custom SVG markup for the "exit fullscreen" glyph. Trusted markup; do not pass
    /// user-supplied content. Defaults to the built-in glyph when null.
    /// </summary>
    [Parameter]
    public string? ExitIcon { get; set; }

    /// <summary>Optional accessible label/tooltip shown while collapsed. Defaults to "Enter fullscreen".</summary>
    [Parameter]
    public string? EnterTitle { get; set; }

    /// <summary>Optional accessible label/tooltip shown while expanded. Defaults to "Exit fullscreen".</summary>
    [Parameter]
    public string? ExitTitle { get; set; }

    [CascadingParameter]
    private MapControlRegistryContext? _registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() =>
        _registration.Register(_registry, _sectionContext, nameof(FullscreenMapControl), Id, BuildControl());

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(_registry);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _registration.DisposeAsync(_registry);

    private FullscreenControlDefinition BuildControl() =>
        new(Id, Visible, Position, Order, EnterIcon, ExitIcon, EnterTitle, ExitTitle);
}
