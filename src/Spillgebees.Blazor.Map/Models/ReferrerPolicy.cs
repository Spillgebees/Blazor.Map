using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Controls the referrer information sent with network requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReferrerPolicy>))]
public enum ReferrerPolicy
{
    [JsonStringEnumMemberName("no-referrer")]
    NoReferrer,

    [JsonStringEnumMemberName("no-referrer-when-downgrade")]
    NoReferrerWhenDowngrade,

    [JsonStringEnumMemberName("origin")]
    Origin,

    [JsonStringEnumMemberName("origin-when-cross-origin")]
    OriginWhenCrossOrigin,

    [JsonStringEnumMemberName("same-origin")]
    SameOrigin,

    [JsonStringEnumMemberName("strict-origin")]
    StrictOrigin,

    [JsonStringEnumMemberName("strict-origin-when-cross-origin")]
    StrictOriginWhenCrossOrigin,

    [JsonStringEnumMemberName("unsafe-url")]
    UnsafeUrl,
}
