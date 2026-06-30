using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registers a navigation control subcomponent.
/// </summary>
public sealed class NavigationMapControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    /// <summary>Creates the control with the default id <c>"navigation"</c> placed in the top-right corner.</summary>
    public NavigationMapControl()
    {
        Id = "navigation";
        Position = ControlPosition.TopRight;
    }

    /// <summary>Unique control identifier within the map. Defaults to <c>"navigation"</c>.</summary>
    [Parameter]
    public string Id { get; set; } = string.Empty;

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.TopRight" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 100.</summary>
    [Parameter]
    public int Order { get; set; } = 100;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>Whether the compass button is shown. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool ShowCompass { get; set; } = true;

    /// <summary>Whether the zoom in/out buttons are shown. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool ShowZoom { get; set; } = true;

    [CascadingParameter]
    private MapControlRegistryContext? _registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() =>
        _registration.Register(_registry, _sectionContext, nameof(NavigationMapControl), Id, BuildControl());

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(_registry);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _registration.DisposeAsync(_registry);

    private NavigationControlDefinition BuildControl() => new(Id, Visible, Position, ShowCompass, ShowZoom, Order);
}
