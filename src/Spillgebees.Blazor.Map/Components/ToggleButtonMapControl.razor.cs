using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Renders a first-class styled map toggle button control.
/// </summary>
public partial class ToggleButtonMapControl : ComponentBase, IAsyncDisposable
{
    private readonly StyledContentMapControlRegistration _registration = new();
    private readonly string _contentId = $"sgb-map-toggle-control-content-{Guid.NewGuid():N}";
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

    /// <summary>Accessible label for the toggle button (<c>aria-label</c>); must be non-empty.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Tooltip text; falls back to <see cref="Label" /> when not set.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Text displayed in both toggle states unless overridden by <see cref="OnText" /> or <see cref="OffText" />.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>Text displayed while toggled on; falls back to <see cref="Text" />.</summary>
    [Parameter]
    public string? OnText { get; set; }

    /// <summary>Text displayed while toggled off; falls back to <see cref="Text" />.</summary>
    [Parameter]
    public string? OffText { get; set; }

    /// <summary>Icon displayed in both toggle states unless overridden by <see cref="OnIcon" /> or <see cref="OffIcon" />.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>Icon displayed while toggled on; falls back to <see cref="Icon" />.</summary>
    [Parameter]
    public RenderFragment? OnIcon { get; set; }

    /// <summary>Icon displayed while toggled off; falls back to <see cref="Icon" />.</summary>
    [Parameter]
    public RenderFragment? OffIcon { get; set; }

    /// <summary>Visual variant of the button. Defaults to <see cref="MapButtonVariant.Neutral" />.</summary>
    [Parameter]
    public MapButtonVariant Variant { get; set; } = MapButtonVariant.Neutral;

    /// <summary>Size of the button. Defaults to <see cref="MapButtonSize.Medium" />.</summary>
    [Parameter]
    public MapButtonSize Size { get; set; } = MapButtonSize.Medium;

    /// <summary>Whether the button is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the toggle is on; supports two-way binding via <see cref="IsOnChanged" />.</summary>
    [Parameter]
    public bool IsOn { get; set; }

    /// <summary>Fires when the user toggles the button, carrying the requested on state.</summary>
    [Parameter]
    public EventCallback<bool> IsOnChanged { get; set; }

    private const string PlacementErrorMessage = "ToggleButtonMapControl must be placed inside MapControls.";

    private string _effectiveTitle => string.IsNullOrWhiteSpace(Title) ? Label : Title;

    private RenderFragment? _currentIcon => IsOn ? OnIcon ?? Icon : OffIcon ?? Icon;

    private string? _displayText => IsOn ? OnText ?? Text : OffText ?? Text;

    private string _ariaIsOn => IsOn.ToString().ToLowerInvariant();

    private string _groupClass =>
        string.Join(
            " ",
            new[] { "sgb-map-control-button-group", "sgb-map-toggle-control", Class }.Where(value =>
                !string.IsNullOrWhiteSpace(value)
            )
        );

    private string _buttonClass =>
        string.Join(
            " ",
            new[]
            {
                "sgb-map-control-button",
                "sgb-map-toggle-control-button",
                IsOn ? "sgb-map-control-button-pressed" : "sgb-map-control-button-unpressed",
                GetLayoutClass(),
                $"sgb-map-control-button-{Variant.ToString().ToLowerInvariant()}",
                $"sgb-map-control-button-{Size.ToString().ToLowerInvariant()}",
            }
        );

    private async Task ToggleAsync()
    {
        if (Disabled)
        {
            return;
        }

        await IsOnChanged.InvokeAsync(!IsOn);
    }

    private string GetLayoutClass()
    {
        if (_currentIcon is not null && !string.IsNullOrWhiteSpace(_displayText))
        {
            return "sgb-map-control-button-with-icon-text";
        }

        if (_currentIcon is not null)
        {
            return "sgb-map-control-button-icon-only";
        }

        return "sgb-map-control-button-text-only";
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        StyledContentMapControlRegistration.ValidateId(Id);

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }

        if (_currentIcon is null && string.IsNullOrWhiteSpace(_displayText))
        {
            throw new InvalidOperationException(
                "ToggleButtonMapControl requires visible content for the current on state."
            );
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
