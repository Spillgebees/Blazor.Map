namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Feature role produced by the tracked entity GeoJSON builder.
/// </summary>
public enum TrackedEntityFeatureKind
{
    /// <summary>
    /// Primary entity feature.
    /// </summary>
    Primary,

    /// <summary>
    /// Companion decoration feature.
    /// </summary>
    Decoration,
}
