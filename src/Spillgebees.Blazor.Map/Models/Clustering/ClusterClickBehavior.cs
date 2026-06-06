namespace Spillgebees.Blazor.Map;

/// <summary>
/// Built-in behavior for clicking generated cluster layers.
/// </summary>
public enum ClusterClickBehavior
{
    /// <summary>
    /// No built-in click handling.
    /// </summary>
    None,

    /// <summary>
    /// Zoom until the clicked cluster dissolves into child features.
    /// </summary>
    ZoomToDissolve,
}
