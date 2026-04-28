using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Renders first-class styled grouped map control buttons.
/// </summary>
public partial class MapControlButtonGroup : StyledContentMapControlBase
{
    private readonly string _contentId = $"sgb-map-button-group-content-{Guid.NewGuid():N}";

    [Parameter]
    public string? Class { get; set; }

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string PlacementErrorMessage => "MapControlButtonGroup must be placed inside a map.";

    private string GroupClass =>
        string.Join(
            " ",
            new[] { "sgb-map-control-button-group", "sgb-map-control-button-group-control", Class }.Where(value =>
                !string.IsNullOrWhiteSpace(value)
            )
        );

    protected override void ValidateParameters()
    {
        base.ValidateParameters();

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }
    }
}
