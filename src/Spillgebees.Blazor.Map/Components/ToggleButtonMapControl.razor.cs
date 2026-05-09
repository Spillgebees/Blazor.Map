using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

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
    public string? Title { get; set; }

    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public string? OnText { get; set; }

    [Parameter]
    public string? OffText { get; set; }

    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public RenderFragment? OnIcon { get; set; }

    [Parameter]
    public RenderFragment? OffIcon { get; set; }

    [Parameter]
    public MapButtonVariant Variant { get; set; } = MapButtonVariant.Neutral;

    [Parameter]
    public MapButtonSize Size { get; set; } = MapButtonSize.Medium;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool IsOn { get; set; }

    [Parameter]
    public EventCallback<bool> IsOnChanged { get; set; }

    private const string PlacementErrorMessage = "ToggleButtonMapControl must be placed inside MapControls.";

    private string EffectiveTitle => string.IsNullOrWhiteSpace(Title) ? Label : Title;

    private RenderFragment? CurrentIcon => IsOn ? OnIcon ?? Icon : OffIcon ?? Icon;

    private string? DisplayText => IsOn ? OnText ?? Text : OffText ?? Text;

    private string AriaIsOn => IsOn.ToString().ToLowerInvariant();

    private string GroupClass =>
        string.Join(
            " ",
            new[] { "sgb-map-control-button-group", "sgb-map-toggle-control", Class }.Where(value =>
                !string.IsNullOrWhiteSpace(value)
            )
        );

    private string ButtonClass =>
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
        if (CurrentIcon is not null && !string.IsNullOrWhiteSpace(DisplayText))
        {
            return "sgb-map-control-button-with-icon-text";
        }

        if (CurrentIcon is not null)
        {
            return "sgb-map-control-button-icon-only";
        }

        return "sgb-map-control-button-text-only";
    }

    protected override void OnParametersSet()
    {
        StyledContentMapControlRegistration.ValidateId(Id);

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }

        if (CurrentIcon is null && string.IsNullOrWhiteSpace(DisplayText))
        {
            throw new InvalidOperationException(
                "ToggleButtonMapControl requires visible content for the current pressed state."
            );
        }

        _registration.RegisterContent(Registry, SectionContext, PlacementErrorMessage, Id, Visible, Position, Order);
    }

    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration.SyncAfterRenderAsync(Registry, Id, Visible, _placeholderReference, _contentReference);

    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);
}
