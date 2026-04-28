using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Renders a first-class styled map toggle button control.
/// </summary>
public partial class MapToggleControl : StyledContentMapControlBase
{
    private readonly string _contentId = $"sgb-map-toggle-control-content-{Guid.NewGuid():N}";

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
    public RenderFragment? PressedIcon { get; set; }

    [Parameter]
    public MapControlButtonVariant Variant { get; set; } = MapControlButtonVariant.Neutral;

    [Parameter]
    public MapControlButtonSize Size { get; set; } = MapControlButtonSize.Medium;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool Pressed { get; set; }

    [Parameter]
    public EventCallback<bool> PressedChanged { get; set; }

    protected override string PlacementErrorMessage => "MapToggleControl must be placed inside a map.";

    private RenderFragment? CurrentIcon => Pressed && PressedIcon is not null ? PressedIcon : Icon;

    private string? DisplayText => Pressed ? OnText ?? Text : OffText ?? Text;

    private string AriaPressed => Pressed.ToString().ToLowerInvariant();

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
                Pressed ? "sgb-map-control-button-pressed" : "sgb-map-control-button-unpressed",
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

        await PressedChanged.InvokeAsync(!Pressed);
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

    protected override void ValidateParameters()
    {
        base.ValidateParameters();

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }

        if (CurrentIcon is null && string.IsNullOrWhiteSpace(DisplayText))
        {
            throw new InvalidOperationException(
                "MapToggleControl requires visible content for the current pressed state."
            );
        }
    }
}
