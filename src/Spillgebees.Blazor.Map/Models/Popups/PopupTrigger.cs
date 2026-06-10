using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Determines how a popup is triggered.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PopupTrigger>))]
public enum PopupTrigger
{
    /// <summary>
    /// Show on click, dismiss via close button or clicking elsewhere.
    /// </summary>
    [JsonStringEnumMemberName("click")]
    Click,

    /// <summary>
    /// Show on mouse enter, hide on mouse leave.
    /// </summary>
    [JsonStringEnumMemberName("hover")]
    Hover,

    /// <summary>
    /// Always visible — ideal for labels.
    /// </summary>
    [JsonStringEnumMemberName("permanent")]
    Permanent,
}
