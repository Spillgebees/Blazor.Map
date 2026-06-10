using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// The map projection to use for rendering.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MapProjection>))]
public enum MapProjection
{
    /// <summary>
    /// Standard Web Mercator projection (flat map).
    /// </summary>
    [JsonStringEnumMemberName("mercator")]
    Mercator,

    /// <summary>
    /// Globe projection (3D sphere) — available at low zoom levels.
    /// </summary>
    [JsonStringEnumMemberName("globe")]
    Globe,
}
