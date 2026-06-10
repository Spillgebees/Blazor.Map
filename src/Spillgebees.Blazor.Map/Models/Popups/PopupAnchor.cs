using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// The anchor position of a popup relative to its feature.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PopupAnchor>))]
public enum PopupAnchor
{
    /// <summary>
    /// MapLibre chooses the position based on available space.
    /// </summary>
    [JsonStringEnumMemberName("auto")]
    Auto,

    /// <summary>
    /// Popup appears above the feature.
    /// </summary>
    [JsonStringEnumMemberName("top")]
    Top,

    /// <summary>
    /// Popup appears below the feature.
    /// </summary>
    [JsonStringEnumMemberName("bottom")]
    Bottom,

    /// <summary>
    /// Popup appears to the left of the feature.
    /// </summary>
    [JsonStringEnumMemberName("left")]
    Left,

    /// <summary>
    /// Popup appears to the right of the feature.
    /// </summary>
    [JsonStringEnumMemberName("right")]
    Right,
}
