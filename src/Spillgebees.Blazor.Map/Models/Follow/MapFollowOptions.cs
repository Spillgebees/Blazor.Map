namespace Spillgebees.Blazor.Map;

/// <summary>
/// Describes a map-level camera follow target: the camera tracks one tracked-entity-layer entity as
/// it moves. <paramref name="LayerId"/> is the <see cref="TrackedEntityLayer{TItem}"/>'s id and
/// <paramref name="EntityId"/> is the entity's id. Prefer <c>@bind-Follow</c> so user-initiated
/// clears flow back into app state.
/// </summary>
/// <param name="LayerId">The <see cref="TrackedEntityLayer{TItem}"/>'s <c>Id</c> to follow within. Required.</param>
/// <param name="EntityId">
/// The id of the entity to follow, matching the value your <see cref="TrackedEntityLayer{TItem}"/>'s
/// <c>ItemId</c> selector returns (not the internal numeric index). Required.
/// </param>
/// <param name="Camera">Camera behaviour while following. <c>null</c> preserves current zoom, pitch, and bearing.</param>
/// <param name="Animation">
/// Animation for discrete camera moves (engage and non-animated updates). When <c>null</c>, defaults to a
/// 500ms ease-in-out engage; when set, only <see cref="AnimationOptions.Duration"/> and
/// <see cref="AnimationOptions.Easing"/> apply. While the entity is interpolating, the camera rides
/// the motion frames and this is unused.
/// </param>
/// <param name="Interaction">Which user interactions clear the follow. <c>null</c> uses the defaults.</param>
public sealed record MapFollowOptions(
    string LayerId,
    string EntityId,
    MapFollowCameraOptions? Camera = null,
    AnimationOptions? Animation = null,
    MapFollowInteractionOptions? Interaction = null
);
