using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Renders custom Blazor content inside a collapsible map panel control.
/// </summary>
public partial class PanelMapControl : ComponentBase, IAsyncDisposable
{
    private const string PanelControlKind = "panel";
    private readonly StyledContentMapControlRegistration _registration = new();
    private readonly string _contentId = $"sgb-map-panel-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;
    private DotNetObjectReference<PanelMapControl>? _dotNetReference;

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
    public bool InitiallyOpen { get; set; }

    [Parameter]
    public bool? IsOpen { get; set; }

    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter]
    public string? MaxWidth { get; set; }

    [Parameter]
    public string? PanelClass { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string ContentClass =>
        new CssBuilder()
            .AddClass("sgb-map-panel-content")
            .AddClass(PanelClass, !string.IsNullOrWhiteSpace(PanelClass))
            .Build();

    protected override void OnParametersSet()
    {
        ValidateParameters();

        var control = new PanelControlDefinition(
            Id,
            new MapControlPlacement(Position, Order, Visible),
            new PanelChromeOptions(Label, Title, InitiallyOpen, IsOpen, MaxWidth),
            Class
        );
        _registration.Register(
            Registry,
            SectionContext,
            "PanelMapControl must be placed inside MapControls.",
            Id,
            control
        );
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _registration.SyncAfterRenderAsync(
            Registry,
            Id,
            Visible,
            PanelControlKind,
            _placeholderReference,
            _contentReference,
            GetDotNetReference
        );
    }

    [JSInvokable]
    public Task OnPanelOpenChangedAsync(bool isOpen) =>
        IsOpenChanged.HasDelegate ? IsOpenChanged.InvokeAsync(isOpen) : Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _registration.DisposeAsync(Registry);
        _dotNetReference?.Dispose();
        _dotNetReference = null;
    }

    private DotNetObjectReference<PanelMapControl> GetDotNetReference() =>
        _dotNetReference ??= DotNetObjectReference.Create(this);

    private void ValidateParameters()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("A non-empty Id is required.");
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("A non-empty Label is required.");
        }
    }
}
