namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Options for smooth position interpolation when GeoJSON source data changes.
/// When applied to a <see cref="Components.Layers.GeoJsonSource"/>, position updates
/// are animated over the specified duration instead of jumping instantly.
/// </summary>
/// <param name="Duration">The animation duration in milliseconds. Default is 2000.</param>
/// <param name="Easing">The easing function for the animation. Default is <see cref="AnimationEasing.Linear"/>.</param>
public record AnimationOptions(int Duration = 2000, AnimationEasing Easing = AnimationEasing.Linear);

/// <summary>
/// Easing functions for GeoJSON source animations.
/// </summary>
public enum AnimationEasing
{
    /// <summary>Constant speed from start to finish.</summary>
    Linear,

    /// <summary>Smooth acceleration and deceleration.</summary>
    EaseInOut,
}
