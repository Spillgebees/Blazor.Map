using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map;

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
    private MapControlRegistryContext? Registry { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 500;

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        StyledContentMapControlRegistration.ValidateId(Id);

        _registration.RegisterContent(
            Registry,
            SectionContext,
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
            Registry,
            Id,
            Visible,
            CustomControlKind,
            _placeholderReference,
            _contentReference
        );

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);
}
