using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Registration plumbing for custom-shell controls (legend/panel/content): registers
/// the definition, then binds the component's rendered DOM as control content after
/// render, tracking pending syncs and removals across re-renders.
/// </summary>
internal sealed class StyledContentMapControlRegistration
{
    private const string CustomControlKind = "content";
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private readonly List<string> _pendingRemovalIds = [];
    private bool _controlSyncPending = true;
    private bool _contentSyncPending = true;
    private bool _contentRegistered;
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
        MapSectionContext? sectionContext,
        string placementErrorMessage,
        string id,
        MapControlDefinition control
    )
    {
        ValidateId(id);

        if (registry is null)
        {
            throw new InvalidOperationException(placementErrorMessage);
        }

        if (sectionContext?.Kind is not MapContentSectionKind.Controls)
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

        var changed = registry.Register(_ownerId, control);
        _registeredId = id;
        _controlSyncPending = _controlSyncPending || changed;
        _contentSyncPending = _contentSyncPending || changed;
    }

    public void RegisterContent(
        MapControlRegistryContext? registry,
        MapSectionContext? sectionContext,
        string placementErrorMessage,
        string id,
        bool visible,
        ControlPosition position,
        int order,
        string? className = null
    ) =>
        Register(
            registry,
            sectionContext,
            placementErrorMessage,
            id,
            new ContentControlDefinition(id, visible, position, order, className)
        );

    public Task SyncAfterRenderAsync(
        MapControlRegistryContext? registry,
        string id,
        bool visible,
        ElementReference placeholderReference,
        ElementReference contentReference
    ) => SyncAfterRenderAsync(registry, id, visible, CustomControlKind, placeholderReference, contentReference);

    public async Task SyncAfterRenderAsync(
        MapControlRegistryContext? registry,
        string id,
        bool visible,
        string kind,
        ElementReference placeholderReference,
        ElementReference contentReference,
        Func<bool, Task>? onPanelOpenChangedAsync = null
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

        if (!visible)
        {
            if (_contentRegistered)
            {
                await registry.RemoveControlContentAsync(_registeredId);
                _contentRegistered = false;
            }

            _contentSyncPending = false;
            return;
        }

        if (_contentSyncPending)
        {
            await registry.SetControlContentAsync(
                id,
                kind,
                placeholderReference,
                contentReference,
                onPanelOpenChangedAsync
            );
            _contentRegistered = true;
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
            _contentRegistered = false;
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

        if (pendingRemovalIds.Length > 0)
        {
            _contentRegistered = false;
        }
    }
}
