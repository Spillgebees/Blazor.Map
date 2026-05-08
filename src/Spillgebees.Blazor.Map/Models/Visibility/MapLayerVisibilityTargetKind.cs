using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models.Visibility;

/// <summary>
/// Identifies how a layer visibility target should be resolved.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MapLayerVisibilityTargetKind>))]
public enum MapLayerVisibilityTargetKind
{
    /// <summary>
    /// Targets original layer IDs within a composed map style.
    /// </summary>
    [JsonStringEnumMemberName("styleLayer")]
    StyleLayer,

    /// <summary>
    /// Targets runtime MapLibre layer IDs directly.
    /// </summary>
    [JsonStringEnumMemberName("runtimeLayer")]
    RuntimeLayer,
}
