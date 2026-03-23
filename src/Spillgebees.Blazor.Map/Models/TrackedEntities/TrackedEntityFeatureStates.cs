using Spillgebees.Blazor.Map.Models.Expressions;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Common feature-state keys used with tracked entity layers.
/// </summary>
public static class TrackedEntityFeatureStates
{
    /// <summary>
    /// Hover state for tracked entities.
    /// </summary>
    public static FeatureStateKey<bool> Hover => FeatureState.Bool("hover");

    /// <summary>
    /// Selection or click state for tracked entities.
    /// </summary>
    public static FeatureStateKey<bool> Selected => FeatureState.Bool("selected");

    /// <summary>
    /// Rotation state for tracked-entity decorations.
    /// </summary>
    public static FeatureStateKey<double> Rotation => FeatureState.Number("rotation");
}
