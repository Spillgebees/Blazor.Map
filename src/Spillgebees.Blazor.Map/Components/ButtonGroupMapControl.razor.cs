using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Renders first-class styled grouped map control buttons.
/// </summary>
public partial class ButtonGroupMapControl : ComponentBase, IAsyncDisposable
{
    private readonly MapButtonGroupContext _buttonGroupContext = new();
    private readonly StyledContentMapControlRegistration _registration = new();
    private readonly string _contentId = $"sgb-map-button-group-content-{Guid.NewGuid():N}";
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

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private RenderFragment GroupContent =>
        builder =>
        {
            builder.OpenComponent<CascadingValue<MapButtonGroupContext>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<MapButtonGroupContext>.Value), _buttonGroupContext);
            builder.AddAttribute(2, nameof(CascadingValue<MapButtonGroupContext>.IsFixed), true);
            builder.AddAttribute(3, nameof(CascadingValue<MapButtonGroupContext>.ChildContent), ChildContent);
            builder.CloseComponent();
        };

    private const string PlacementErrorMessage = "ButtonGroupMapControl must be placed inside MapControls.";

    private string GroupClass =>
        string.Join(
            " ",
            new[] { "sgb-map-control-button-group", "sgb-map-control-button-group-control", Class }.Where(value =>
                !string.IsNullOrWhiteSpace(value)
            )
        );

    protected override void OnParametersSet()
    {
        StyledContentMapControlRegistration.ValidateId(Id);

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }

        _registration.RegisterContent(Registry, SectionContext, PlacementErrorMessage, Id, Visible, Position, Order);
    }

    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration.SyncAfterRenderAsync(Registry, Id, Visible, _placeholderReference, _contentReference);

    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);
}
