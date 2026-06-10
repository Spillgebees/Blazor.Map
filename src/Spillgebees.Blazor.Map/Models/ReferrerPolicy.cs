using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Controls the referrer information sent with network requests.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReferrerPolicy>))]
public enum ReferrerPolicy
{
    /// <summary>
    /// Omits the Referer header entirely.
    /// </summary>
    [JsonStringEnumMemberName("no-referrer")]
    NoReferrer,

    /// <summary>
    /// Sends the full URL except when navigating from HTTPS to HTTP.
    /// </summary>
    [JsonStringEnumMemberName("no-referrer-when-downgrade")]
    NoReferrerWhenDowngrade,

    /// <summary>
    /// Sends only the origin for all requests.
    /// </summary>
    [JsonStringEnumMemberName("origin")]
    Origin,

    /// <summary>
    /// Sends the full URL for same-origin requests and only the origin for cross-origin requests.
    /// </summary>
    [JsonStringEnumMemberName("origin-when-cross-origin")]
    OriginWhenCrossOrigin,

    /// <summary>
    /// Sends the full URL for same-origin requests and no referrer for cross-origin requests.
    /// </summary>
    [JsonStringEnumMemberName("same-origin")]
    SameOrigin,

    /// <summary>
    /// Sends only the origin, and nothing when navigating from HTTPS to HTTP.
    /// </summary>
    [JsonStringEnumMemberName("strict-origin")]
    StrictOrigin,

    /// <summary>
    /// Sends the full URL for same-origin requests, only the origin for cross-origin requests,
    /// and nothing when navigating from HTTPS to HTTP.
    /// </summary>
    [JsonStringEnumMemberName("strict-origin-when-cross-origin")]
    StrictOriginWhenCrossOrigin,

    /// <summary>
    /// Sends the full URL for all requests regardless of security.
    /// </summary>
    [JsonStringEnumMemberName("unsafe-url")]
    UnsafeUrl,
}
