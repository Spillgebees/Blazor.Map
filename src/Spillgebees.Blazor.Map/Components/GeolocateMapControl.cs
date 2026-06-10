using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registers a geolocate control subcomponent.
/// </summary>
public sealed class GeolocateMapControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    /// <summary>Unique control identifier within the map. Defaults to <c>"geolocate"</c>.</summary>
    [Parameter]
    public string Id { get; set; } = "geolocate";

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.TopRight" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 300.</summary>
    [Parameter]
    public int Order { get; set; } = 300;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>Whether the control continuously tracks the user's location instead of locating once.</summary>
    [Parameter]
    public bool TrackUser { get; set; }

    [CascadingParameter]
    private MapControlRegistryContext? _registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() =>
        _registration.Register(_registry, _sectionContext, nameof(GeolocateMapControl), Id, BuildControl());

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(_registry);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _registration.DisposeAsync(_registry);

    private GeolocateControlDefinition BuildControl() =>
        new(Id, Visible, Position, TrackUser, Order);
}
