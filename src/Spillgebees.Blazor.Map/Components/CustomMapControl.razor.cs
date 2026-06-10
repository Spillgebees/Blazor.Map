using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Renders Blazor content inside a MapLibre control shell.
/// </summary>
public partial class CustomMapControl : ComponentBase, IAsyncDisposable
{
    private const string CustomControlKind = "content";
    private readonly StyledContentMapControlRegistration _registration = new();
    private readonly string _contentId = $"sgb-map-custom-control-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;

    [CascadingParameter]
    private MapControlRegistryContext? _registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? _sectionContext { get; set; }

    /// <summary>Unique control identifier within the map.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>Map corner the control is placed in. Defaults to <see cref="ControlPosition.TopRight" />.</summary>
    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    /// <summary>Deterministic ordering among controls at the same corner; lower values render first. Defaults to 500.</summary>
    [Parameter]
    public int Order { get; set; } = 500;

    /// <summary>Whether the control is visible. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>Additional CSS class applied to the control container.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Content rendered inside the control shell.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        StyledContentMapControlRegistration.ValidateId(Id);

        _registration.RegisterContent(
            _registry,
            _sectionContext,
            "CustomMapControl must be placed inside MapControls.",
            Id,
            Visible,
            Position,
            Order,
            Class
        );
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration.SyncAfterRenderAsync(
            _registry,
            Id,
            Visible,
            CustomControlKind,
            _placeholderReference,
            _contentReference
        );

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _registration.DisposeAsync(_registry);
    }
}
