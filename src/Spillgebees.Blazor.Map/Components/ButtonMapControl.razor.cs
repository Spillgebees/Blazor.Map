using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Renders a first-class styled map action button control.
/// </summary>
public partial class ButtonMapControl : ComponentBase, IAsyncDisposable
{
    private readonly StyledContentMapControlRegistration _registration = new();
    private readonly string _contentId = $"sgb-map-action-control-content-{Guid.NewGuid():N}";
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

    /// <summary>Accessible label for the button (<c>aria-label</c>); must be non-empty.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Tooltip text; falls back to <see cref="Label" /> when not set.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Text displayed inside the button; at least one of <see cref="Text" /> or <see cref="Icon" /> is required.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>Icon content displayed inside the button.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>Visual variant of the button. Defaults to <see cref="MapButtonVariant.Neutral" />.</summary>
    [Parameter]
    public MapButtonVariant Variant { get; set; } = MapButtonVariant.Neutral;

    /// <summary>Size of the button. Defaults to <see cref="MapButtonSize.Medium" />.</summary>
    [Parameter]
    public MapButtonSize Size { get; set; } = MapButtonSize.Medium;

    /// <summary>Whether the button is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Fires when the button is clicked.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private const string PlacementErrorMessage = "ButtonMapControl must be placed inside MapControls.";

    private string _effectiveTitle => string.IsNullOrWhiteSpace(Title) ? Label : Title;

    private string _groupClass =>
        string.Join(
            " ",
            new[] { "sgb-map-control-button-group", "sgb-map-action-control", Class }.Where(value =>
                !string.IsNullOrWhiteSpace(value)
            )
        );

    private string _buttonClass =>
        string.Join(
            " ",
            new[]
            {
                "sgb-map-control-button",
                "sgb-map-action-control-button",
                GetLayoutClass(),
                $"sgb-map-control-button-{Variant.ToString().ToLowerInvariant()}",
                $"sgb-map-control-button-{Size.ToString().ToLowerInvariant()}",
            }
        );

    private string GetLayoutClass()
    {
        if (Icon is not null && !string.IsNullOrWhiteSpace(Text))
        {
            return "sgb-map-control-button-with-icon-text";
        }

        if (Icon is not null)
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

        if (Icon is null && string.IsNullOrWhiteSpace(Text))
        {
            throw new InvalidOperationException("ButtonMapControl requires non-empty Text or Icon.");
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
