using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Controls how the MapLibre canvas pixel ratio is resolved for a map instance.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MapPixelRatioMode>))]
public enum MapPixelRatioMode
{
    /// <summary>
    /// Preserve MapLibre's browser default behavior.
    /// </summary>
    [JsonStringEnumMemberName("browserDefault")]
    BrowserDefault,

    /// <summary>
    /// Use the next whole-number device pixel ratio, with a minimum of 1.
    /// </summary>
    [JsonStringEnumMemberName("roundedUpDevicePixelRatio")]
    RoundedUpDevicePixelRatio,
}
