namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Display mode intent for a tracked entity decoration.
/// </summary>
public enum TrackedEntityDecorationDisplayMode
{
    /// <summary>
    /// Always visible.
    /// </summary>
    Always,

    /// <summary>
    /// Visible while hovered.
    /// </summary>
    Hover,

    /// <summary>
    /// Visible after click or selection.
    /// </summary>
    Click,

    /// <summary>
    /// Visible while hovered or selected.
    /// </summary>
    HoverOrSelected,
}
