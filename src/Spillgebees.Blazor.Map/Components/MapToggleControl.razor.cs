using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Renders a first-class styled map toggle button control.
/// </summary>
public partial class MapToggleControl : ComponentBase, IAsyncDisposable
{
    private const string CustomControlKind = "content";
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private readonly string _contentId = $"sgb-map-toggle-control-content-{Guid.NewGuid():N}";
    private ElementReference _placeholderReference;
    private ElementReference _contentReference;
    private bool _controlSyncPending = true;
    private bool _contentSyncPending = true;
    private string? _registeredId;
    private readonly List<string> _pendingRemovalIds = [];

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

    protected override void OnParametersSet()
    {
        ValidateParameters();

        if (Registry is null)
        {
            throw new InvalidOperationException("MapToggleControl must be placed inside a map.");
        }

        RegisterControl();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Registry is null || string.IsNullOrWhiteSpace(_registeredId))
        {
            return;
        }

        var ready = await Registry.WhenReadyAsync();
        if (!ready)
        {
            return;
        }

        await RemovePendingControlsAsync();

        if (_controlSyncPending)
        {
            await Registry.SyncControlsAsync();
            _controlSyncPending = false;
        }

        if (!Enabled)
        {
            await Registry.RemoveControlContentAsync(_registeredId);
            _contentSyncPending = false;
            return;
        }

        if (_contentSyncPending)
        {
            await Registry.SetControlContentAsync(Id, CustomControlKind, _placeholderReference, _contentReference);
            _contentSyncPending = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Registry is null)
        {
            return;
        }

        var controlId = _registeredId;
        var pendingRemovalIds = _pendingRemovalIds.ToArray();
        Registry.UnregisterByOwner(_ownerId);

        try
        {
            if (!Registry.IsReady)
            {
                _pendingRemovalIds.Clear();
                return;
            }

            var removalIds = pendingRemovalIds
                .Append(controlId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var removalId in removalIds)
            {
                await Registry.RemoveControlContentAsync(removalId!);
            }

            if (removalIds.Length > 0)
            {
                await Registry.SyncControlsAsync();
            }
        }
        catch (Exception)
        {
            // disposal may run after JS runtime teardown.
        }
        finally
        {
            _registeredId = null;
            _pendingRemovalIds.Clear();
        }
    }

    private async Task ToggleAsync()
    {
        if (Disabled)
        {
            return;
        }

        await PressedChanged.InvokeAsync(!Pressed);
    }

    private void RegisterControl()
    {
        if (!string.IsNullOrWhiteSpace(_registeredId) && !string.Equals(_registeredId, Id, StringComparison.Ordinal))
        {
            _pendingRemovalIds.Add(_registeredId);
            _controlSyncPending = true;
            _contentSyncPending = true;
            _registeredId = null;
        }

        var changed = Registry!.Register(_ownerId, new ContentMapControl(Id, Enabled, Position, Order));
        _registeredId = Id;
        _controlSyncPending = _controlSyncPending || changed;
        _contentSyncPending = _contentSyncPending || changed;
    }

    private async Task RemovePendingControlsAsync()
    {
        var pendingRemovalIds = _pendingRemovalIds.ToArray();
        _pendingRemovalIds.Clear();

        foreach (var pendingRemovalId in pendingRemovalIds)
        {
            await Registry!.RemoveControlContentAsync(pendingRemovalId);
        }
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
