using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Controls the visual theme of UI controls, popups, and attribution.
/// This does NOT affect the map tiles — use the map style for that.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MapTheme>))]
public enum MapTheme
{
    /// <summary>
    /// Light theme for UI controls and popups.
    /// </summary>
    [JsonStringEnumMemberName("light")]
    Light,

    /// <summary>
    /// Dark theme for UI controls and popups.
    /// </summary>
    [JsonStringEnumMemberName("dark")]
    Dark,
}
