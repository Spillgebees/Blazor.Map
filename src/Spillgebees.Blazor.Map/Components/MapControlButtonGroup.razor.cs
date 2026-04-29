using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Renders first-class styled grouped map control buttons.
/// </summary>
public partial class MapControlButtonGroup : ComponentBase, IAsyncDisposable
{
    private readonly StyledContentMapControlRegistration _registration = new();
    private readonly string _contentId = $"sgb-map-button-group-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter]
    public Spillgebees.Blazor.Map.Models.Controls.ControlPosition Position { get; set; } =
        Spillgebees.Blazor.Map.Models.Controls.ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 500;

    [Parameter]
    public bool Enabled { get; set; } = true;

    [Parameter]
    public string? Class { get; set; }

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private const string PlacementErrorMessage = "MapControlButtonGroup must be placed inside a map.";

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

        _registration.Register(Registry, PlacementErrorMessage, Id, Enabled, Position, Order);
    }

    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration.SyncAfterRenderAsync(Registry, Id, Enabled, _placeholderReference, _contentReference);

    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);
}
