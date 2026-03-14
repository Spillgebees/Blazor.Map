using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models.Controls;

/// <summary>
/// Position of a control on the map.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ControlPosition>))]
public enum ControlPosition
{
    /// <summary>
    /// Top-left corner of the map.
    /// </summary>
    [JsonStringEnumMemberName("top-left")]
    TopLeft,

    /// <summary>
    /// Top-right corner of the map.
    /// </summary>
    [JsonStringEnumMemberName("top-right")]
    TopRight,

    /// <summary>
    /// Bottom-left corner of the map.
    /// </summary>
    [JsonStringEnumMemberName("bottom-left")]
    BottomLeft,

    /// <summary>
    /// Bottom-right corner of the map.
    /// </summary>
    [JsonStringEnumMemberName("bottom-right")]
    BottomRight,
}
