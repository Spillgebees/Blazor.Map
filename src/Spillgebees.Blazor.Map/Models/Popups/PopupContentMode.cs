using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Defines how popup content is applied to the MapLibre popup.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PopupContentMode>))]
public enum PopupContentMode
{
    /// <summary>
    /// Content is applied as plain text and rendered escaped.
    /// </summary>
    [JsonStringEnumMemberName("text")]
    Text,

    /// <summary>
    /// Content is applied as raw HTML without sanitization.
    /// </summary>
    [JsonStringEnumMemberName("rawHtml")]
    RawHtml,
}
