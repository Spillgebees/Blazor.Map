namespace Spillgebees.Blazor.Map;

/// <summary>
/// How a camera gesture (zoom, or rotate-and-tilt) behaves while following an entity. Programmatic
/// camera moves, including the follow's own, are never affected.
/// </summary>
public enum MapFollowGestureMode
{
    /// <summary>The follow leaves this gesture to the user.</summary>
    Free,

    /// <summary>
    /// The follow holds its target: the gesture is allowed, but the camera returns to the target after it.
    /// </summary>
    Anchored,

    /// <summary>The follow holds its target and disables the gesture so the user cannot change it.</summary>
    Locked,

    /// <summary>Using the gesture clears the follow.</summary>
    Clear,
}
