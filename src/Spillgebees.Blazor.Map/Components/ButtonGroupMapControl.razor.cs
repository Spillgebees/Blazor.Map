using Microsoft.AspNetCore.Components;

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

    /// <summary>Accessible label for the button group (<c>aria-label</c>); must be non-empty.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Buttons rendered inside the group, typically <see cref="MapButton" /> and <see cref="MapToggleButton" /> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private RenderFragment _groupContent =>
        builder =>
        {
            builder.OpenComponent<CascadingValue<MapButtonGroupContext>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<>.Value), _buttonGroupContext);
            builder.AddAttribute(2, nameof(CascadingValue<>.IsFixed), true);
            builder.AddAttribute(3, nameof(CascadingValue<>.ChildContent), ChildContent);
            builder.CloseComponent();
        };

    private const string PlacementErrorMessage = "ButtonGroupMapControl must be placed inside MapControls.";

    private string _groupClass =>
        string.Join(
            " ",
            new[] { "sgb-map-control-button-group", "sgb-map-control-button-group-control", Class }.Where(value =>
                !string.IsNullOrWhiteSpace(value)
            )
        );

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        StyledContentMapControlRegistration.ValidateId(Id);

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }

        _registration.RegisterContent(_registry, _sectionContext, PlacementErrorMessage, Id, Visible, Position, Order);
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration.SyncAfterRenderAsync(_registry, Id, Visible, _placeholderReference, _contentReference);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _registration.DisposeAsync(_registry);
    }
}
