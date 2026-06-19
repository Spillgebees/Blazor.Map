using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map;

public partial class SgbMap
{
    private MapFollowCoordinator _follow = null!;

    // Last Follow parameter value observed; the declarative diff keys off this, not the active follow,
    // so a stale parameter does not re-engage a follow the user already broke.
    private MapFollowOptions? _lastSeenFollowParam;

    // What the engine currently holds. Drives Started-vs-Updated and the redundant-clear guard.
    private MapFollowOptions? _appliedFollow;

    /// <summary>
    /// The active camera follow request. The camera tracks the referenced tracked-entity-layer entity
    /// as it moves. Re-applied only when the value changes (record equality); prefer <c>@bind-Follow</c>
    /// so user-initiated clears flow back into app state.
    /// </summary>
    [Parameter]
    public MapFollowOptions? Follow { get; set; }

    /// <summary>
    /// Enables <c>@bind-Follow</c> to keep app state in sync; invoked with null when follow is cleared
    /// map-side. Independent of <see cref="OnFollowChanged"/> and both fire on every transition; use this
    /// one for binding and <see cref="OnFollowChanged"/> when you need the reason.
    /// </summary>
    [Parameter]
    public EventCallback<MapFollowOptions?> FollowChanged { get; set; }

    /// <summary>
    /// Invoked with the reason for every follow state transition (e.g. to distinguish a user-pan clear
    /// from a programmatic one). Pairs with, and is independent of, <see cref="FollowChanged"/>.
    /// </summary>
    [Parameter]
    public EventCallback<MapFollowChangedEventArgs> OnFollowChanged { get; set; }

    /// <summary>Starts (or re-targets) the camera follow imperatively.</summary>
    /// <exception cref="ArgumentException">Thrown when <see cref="MapFollowOptions.LayerId"/> or <see cref="MapFollowOptions.EntityId"/> is empty.</exception>
    public async Task StartFollowAsync(MapFollowOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var reason = _appliedFollow is null ? MapFollowChangeReason.Started : MapFollowChangeReason.Updated;
        await ApplyFollowAsync(options, reason);
    }

    /// <summary>Clears the active camera follow imperatively.</summary>
    public async Task ClearFollowAsync()
    {
        if (_appliedFollow is null)
        {
            return;
        }

        _appliedFollow = null;
        await _follow.ClearAsync();
        await RaiseFollowChangedAsync(null, MapFollowChangeReason.Cleared);
    }

    // Applies the declarative Follow parameter when it changes. Channel buffers until the map is ready,
    // so this is safe to call once the channel exists.
    private async Task SyncFollowAsync()
    {
        if (Follow == _lastSeenFollowParam)
        {
            return;
        }

        _lastSeenFollowParam = Follow;

        if (Follow is null)
        {
            if (_appliedFollow is null)
            {
                return;
            }

            _appliedFollow = null;
            await _follow.ClearAsync();
            await RaiseFollowChangedAsync(null, MapFollowChangeReason.Cleared);
            return;
        }

        var reason = _appliedFollow is null ? MapFollowChangeReason.Started : MapFollowChangeReason.Updated;
        await ApplyFollowAsync(Follow, reason);
    }

    private async Task ApplyFollowAsync(MapFollowOptions options, MapFollowChangeReason reason)
    {
        ValidateFollow(options);

        _appliedFollow = options;
        await _follow.FollowAsync(options);
        await RaiseFollowChangedAsync(options, reason);
    }

    // Routed from the map-event channel ("followcleared"): the engine cleared the follow on its own
    // (a user gesture or the entity going missing). The payload carries only the lowercase reason.
    private Task HandleFollowClearedAsync(JsonElement payload)
    {
        var reason = payload.GetProperty("reason").GetString() switch
        {
            "featuremissing" => MapFollowChangeReason.FeatureMissing,
            _ => MapFollowChangeReason.UserInteraction,
        };

        _appliedFollow = null;
        return RaiseFollowChangedAsync(null, reason);
    }

    private async Task RaiseFollowChangedAsync(MapFollowOptions? value, MapFollowChangeReason reason)
    {
        if (FollowChanged.HasDelegate)
        {
            await FollowChanged.InvokeAsync(value);
        }

        if (OnFollowChanged.HasDelegate)
        {
            await OnFollowChanged.InvokeAsync(new MapFollowChangedEventArgs(value, reason));
        }
    }

    private static void ValidateFollow(MapFollowOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.LayerId))
        {
            throw new ArgumentException("MapFollowOptions.LayerId must be a non-empty string.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.EntityId))
        {
            throw new ArgumentException("MapFollowOptions.EntityId must be a non-empty string.", nameof(options));
        }
    }
}
