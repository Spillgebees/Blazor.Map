using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models.Popups;

/// <summary>
/// Defines how popup content is applied to the MapLibre popup.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PopupContentMode>))]
public enum PopupContentMode
{
    [JsonStringEnumMemberName("text")]
    Text,

    [JsonStringEnumMemberName("rawHtml")]
    RawHtml,
}
