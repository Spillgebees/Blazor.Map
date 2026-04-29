using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

internal abstract class StyledContentMapControlBase : ComponentBase, IAsyncDisposable
{
    private const string CustomControlKind = "content";
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private bool _controlSyncPending = true;
    private bool _contentSyncPending = true;
    private string? _registeredId;
    private readonly List<string> _pendingRemovalIds = [];

    protected ElementReference _placeholderReference;
    protected ElementReference _contentReference;

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

    protected abstract string PlacementErrorMessage { get; }

    protected virtual void ValidateParameters()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("A non-empty Id is required.");
        }
    }

    protected override void OnParametersSet()
    {
        ValidateParameters();

        if (Registry is null)
        {
            throw new InvalidOperationException(PlacementErrorMessage);
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
}
