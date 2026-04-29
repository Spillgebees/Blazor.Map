using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Renders a first-class styled map action button control.
/// </summary>
public partial class MapActionControl : ComponentBase, IAsyncDisposable
{
    private readonly StyledContentMapControlRegistration _registration = new();
    private readonly string _contentId = $"sgb-map-action-control-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 500;

    [Parameter]
    public bool Enabled { get; set; } = true;

    [Parameter]
    public string? Class { get; set; }

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public MapControlButtonVariant Variant { get; set; } = MapControlButtonVariant.Neutral;

    [Parameter]
    public MapControlButtonSize Size { get; set; } = MapControlButtonSize.Medium;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private const string PlacementErrorMessage = "MapActionControl must be placed inside a map.";

    private string GroupClass =>
        string.Join(
            " ",
            new[] { "sgb-map-control-button-group", "sgb-map-action-control", Class }.Where(value =>
                !string.IsNullOrWhiteSpace(value)
            )
        );

    private string ButtonClass =>
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

    protected override void OnParametersSet()
    {
        StyledContentMapControlRegistration.ValidateId(Id);

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }

        if (Icon is null && string.IsNullOrWhiteSpace(Text))
        {
            throw new InvalidOperationException("MapActionControl requires non-empty Text or Icon.");
        }

        _registration.Register(Registry, PlacementErrorMessage, Id, Enabled, Position, Order);
    }

    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration.SyncAfterRenderAsync(Registry, Id, Enabled, _placeholderReference, _contentReference);

    public ValueTask DisposeAsync() => _registration.DisposeAsync(Registry);
}
