using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Components;

internal sealed class StyledContentMapControlRegistration
{
    private const string CustomControlKind = "content";
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private readonly List<string> _pendingRemovalIds = [];
    private bool _controlSyncPending = true;
    private bool _contentSyncPending = true;
    private string? _registeredId;

    public static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("A non-empty Id is required.");
        }
    }

    public void Register(
        MapControlRegistryContext? registry,
        string placementErrorMessage,
        string id,
        bool enabled,
        ControlPosition position,
        int order
    )
    {
        ValidateId(id);

        if (registry is null)
        {
            throw new InvalidOperationException(placementErrorMessage);
        }

        if (!string.IsNullOrWhiteSpace(_registeredId) && !string.Equals(_registeredId, id, StringComparison.Ordinal))
        {
            _pendingRemovalIds.Add(_registeredId);
            _controlSyncPending = true;
            _contentSyncPending = true;
            _registeredId = null;
        }

        var changed = registry.Register(_ownerId, new ContentMapControl(id, enabled, position, order));
        _registeredId = id;
        _controlSyncPending = _controlSyncPending || changed;
        _contentSyncPending = _contentSyncPending || changed;
    }

    public async Task SyncAfterRenderAsync(
        MapControlRegistryContext? registry,
        string id,
        bool enabled,
        ElementReference placeholderReference,
        ElementReference contentReference
    )
    {
        if (registry is null || string.IsNullOrWhiteSpace(_registeredId))
        {
            return;
        }

        var ready = await registry.WhenReadyAsync();
        if (!ready)
        {
            return;
        }

        await RemovePendingControlsAsync(registry);

        if (_controlSyncPending)
        {
            await registry.SyncControlsAsync();
            _controlSyncPending = false;
        }

        if (!enabled)
        {
            await registry.RemoveControlContentAsync(_registeredId);
            _contentSyncPending = false;
            return;
        }

        if (_contentSyncPending)
        {
            await registry.SetControlContentAsync(id, CustomControlKind, placeholderReference, contentReference);
            _contentSyncPending = false;
        }
    }

    public async ValueTask DisposeAsync(MapControlRegistryContext? registry)
    {
        if (registry is null)
        {
            return;
        }

        var controlId = _registeredId;
        var pendingRemovalIds = _pendingRemovalIds.ToArray();
        registry.UnregisterByOwner(_ownerId);

        try
        {
            if (!registry.IsReady)
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
                await registry.RemoveControlContentAsync(removalId!);
            }

            if (removalIds.Length > 0)
            {
                await registry.SyncControlsAsync();
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

    private async Task RemovePendingControlsAsync(MapControlRegistryContext registry)
    {
        var pendingRemovalIds = _pendingRemovalIds.ToArray();
        _pendingRemovalIds.Clear();

        foreach (var pendingRemovalId in pendingRemovalIds)
        {
            await registry.RemoveControlContentAsync(pendingRemovalId);
        }
    }
}
