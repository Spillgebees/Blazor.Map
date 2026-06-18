using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Why the follow state changed.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MapFollowChangeReason>))]
public enum MapFollowChangeReason
{
    /// <summary>
    /// A new follow target was requested. The camera engages as soon as the target resolves; if the
    /// entity is not in the layer yet, engage is deferred until it first appears.
    /// </summary>
    Started,

    /// <summary>The active follow was re-targeted or its options changed.</summary>
    Updated,

    /// <summary>Follow was cleared explicitly (parameter nulled or <see cref="SgbMap.ClearFollowAsync"/>).</summary>
    Cleared,

    /// <summary>Follow was cleared by a configured user interaction.</summary>
    UserInteraction,

    /// <summary>
    /// The followed entity disappeared and
    /// <see cref="MapFollowInteractionOptions.ClearWhenFeatureMissing"/> is enabled.
    /// </summary>
    FeatureMissing,
}
