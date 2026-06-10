using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// The unit system for the scale control.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ScaleUnit>))]
public enum ScaleUnit
{
    /// <summary>
    /// Metric units (meters/kilometers).
    /// </summary>
    [JsonStringEnumMemberName("metric")]
    Metric,

    /// <summary>
    /// Imperial units (feet/miles).
    /// </summary>
    [JsonStringEnumMemberName("imperial")]
    Imperial,

    /// <summary>
    /// Nautical units (nautical miles).
    /// </summary>
    [JsonStringEnumMemberName("nautical")]
    Nautical,
}
