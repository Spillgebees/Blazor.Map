using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;

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

    /// <summary>Accessible label for the panel toggle button; must be non-empty.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Title text shown in the panel header.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Whether the panel starts open.</summary>
    [Parameter]
    public bool InitiallyOpen { get; set; }

    /// <summary>Explicit open state; when set, controls the panel and supports two-way binding via <see cref="IsOpenChanged" />.</summary>
    [Parameter]
    public bool? IsOpen { get; set; }

    /// <summary>Fires when the panel is opened or closed.</summary>
    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Maximum width of the panel as a CSS length value.</summary>
    [Parameter]
    public string? MaxWidth { get; set; }

    /// <summary>Additional CSS class applied to the panel content container.</summary>
    [Parameter]
    public string? PanelClass { get; set; }

    /// <summary>Content rendered inside the panel.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string _contentClass =>
        new CssBuilder()
            .AddClass("sgb-map-panel-content")
            .AddClass(PanelClass, !string.IsNullOrWhiteSpace(PanelClass))
            .Build();

    /// <inheritdoc />
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
            _registry,
            _sectionContext,
            "PanelMapControl must be placed inside MapControls.",
            Id,
            control
        );
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _registration.SyncAfterRenderAsync(
            _registry,
            Id,
            Visible,
            PanelControlKind,
            _placeholderReference,
            _contentReference,
            OnPanelOpenChangedAsync
        );
    }

    internal Task OnPanelOpenChangedAsync(bool isOpen) =>
        IsOpenChanged.HasDelegate ? IsOpenChanged.InvokeAsync(isOpen) : Task.CompletedTask;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await _registration.DisposeAsync(_registry);
    }

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
