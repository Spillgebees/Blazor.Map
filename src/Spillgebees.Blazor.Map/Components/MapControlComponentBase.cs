using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Base component for map controls registered through map context.
/// </summary>
public abstract class MapControlComponentBase : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private bool _syncPending = true;
    private string? _registeredId;

    [CascadingParameter]
    private MapControlRegistryContext? Registry { get; set; }

    [Parameter]
    public string Id { get; set; } = string.Empty;

    [Parameter]
    public ControlPosition Position { get; set; } = ControlPosition.TopRight;

    [Parameter]
    public int Order { get; set; } = 100;

    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Registry is null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside a map.");
        }

        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("A non-empty Id is required.");
        }

        var changed = Registry.Register(_ownerId, BuildControl());
        _registeredId = Id;
        _syncPending = _syncPending || changed;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Registry is null || !_syncPending)
        {
            return;
        }

        var ready = await Registry.WhenReadyAsync();
        if (!ready)
        {
            return;
        }

        await Registry.SyncControlsAsync();
        _syncPending = false;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Registry is null || string.IsNullOrWhiteSpace(_registeredId))
        {
            return;
        }

        Registry.UnregisterByOwner(_ownerId);

        if (!Registry.IsReady)
        {
            _registeredId = null;
            return;
        }

        try
        {
            await Registry.SyncControlsAsync();
        }
        catch (Exception)
        {
            // disposal may run after JS runtime teardown.
        }
        finally
        {
            _registeredId = null;
        }
    }

    protected abstract MapControl BuildControl();
}
