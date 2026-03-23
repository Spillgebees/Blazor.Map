namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Intent for cluster click handling.
/// </summary>
public enum TrackedEntityClusterClickBehavior
{
    /// <summary>
    /// No built-in click intent.
    /// </summary>
    None,

    /// <summary>
    /// Zoom until the clicked cluster dissolves.
    /// </summary>
    ZoomToDissolve,
}
