using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registers a terrain control subcomponent.
/// </summary>
public sealed class TerrainMapControl : ComponentBase, IAsyncDisposable
{
    private readonly MapControlComponentRegistration _registration = new();

    /// <summary>Unique control identifier within the map. Defaults to <c>"terrain"</c>.</summary>
    [Parameter]
    public string Id { get; set; } = "terrain";

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.TopRight" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 400.</summary>
    [Parameter]
    public int Order { get; set; } = 400;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>Id of the raster-DEM source used for terrain elevation. Defaults to <c>"terrain"</c>.</summary>
    [Parameter]
    public string SourceId { get; set; } = "terrain";

    [CascadingParameter]
    private MapControlRegistryContext? _registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() =>
        _registration.Register(_registry, _sectionContext, nameof(TerrainMapControl), Id, BuildControl());

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) => _registration.SyncAfterRenderAsync(_registry);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _registration.DisposeAsync(_registry);

    private TerrainControlDefinition BuildControl() => new(Id, Visible, Position, Order, SourceId);
}
