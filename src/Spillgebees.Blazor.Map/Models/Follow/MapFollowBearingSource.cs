namespace Spillgebees.Blazor.Map;

/// <summary>
/// Where the bearing the follow holds comes from. Whether the user can change it with the rotate-and-tilt
/// gesture is set separately by <see cref="MapFollowCameraOptions.OrientationMode"/>.
/// </summary>
public enum MapFollowBearingSource
{
    /// <summary>Hold the bearing the camera had when the follow engaged.</summary>
    KeepCurrent,

    /// <summary>Hold <see cref="MapFollowCameraOptions.Bearing"/>.</summary>
    Fixed,

    /// <summary>
    /// Hold the entity's heading (the value your <see cref="TrackedEntityLayer{TItem}.Rotation"/>
    /// selector returns), easing toward each new heading so sharp turns do not snap. Falls back to the
    /// current bearing when absent.
    /// </summary>
    MatchHeading,
}
