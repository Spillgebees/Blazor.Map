using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Runtime.Scene;

[JsonConverter(typeof(JsonStringEnumConverter<MapVisibilityGroupTargetKind>))]
internal enum MapVisibilityGroupTargetKind
{
    [JsonStringEnumMemberName("styleLayer")]
    StyleLayer,

    [JsonStringEnumMemberName("runtimeLayer")]
    RuntimeLayer,
}
