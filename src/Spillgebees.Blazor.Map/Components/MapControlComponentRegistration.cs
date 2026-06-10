namespace Spillgebees.Blazor.Map;

/// <summary>
/// Shared registration plumbing for native control components: registers the typed
/// definition with the host's registry on parameter changes and syncs after render
/// once the map is ready.
/// </summary>
internal sealed class MapControlComponentRegistration
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private bool _syncPending = true;
    private string? _registeredId;

    public void Register(
        MapControlRegistryContext? registry,
        MapSectionContext? sectionContext,
        string componentName,
        string id,
        MapControlDefinition control
    )
    {
        if (registry is null)
        {
            throw new InvalidOperationException($"{componentName} must be placed inside a map.");
        }

        if (sectionContext?.Kind is not MapContentSectionKind.Controls)
        {
            throw new InvalidOperationException($"{componentName} must be placed inside MapControls.");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("A non-empty Id is required.");
        }

        var changed = registry.Register(_ownerId, control);
        _registeredId = id;
        _syncPending = _syncPending || changed;
    }

    public async Task SyncAfterRenderAsync(MapControlRegistryContext? registry)
    {
        if (registry is null || !_syncPending)
        {
            return;
        }

        var ready = await registry.WhenReadyAsync();
        if (!ready)
        {
            return;
        }

        await registry.SyncControlsAsync();
        _syncPending = false;
    }

    public async ValueTask DisposeAsync(MapControlRegistryContext? registry)
    {
        if (registry is null || string.IsNullOrWhiteSpace(_registeredId))
        {
            return;
        }

        registry.UnregisterByOwner(_ownerId);

        if (!registry.IsReady)
        {
            _registeredId = null;
            return;
        }

        try
        {
            await registry.SyncControlsAsync();
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
}
