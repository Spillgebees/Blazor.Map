using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Identifies how a map display target should be resolved.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MapDisplayTargetKind>))]
public enum MapDisplayTargetKind
{
    /// <summary>
    /// Targets runtime MapLibre layer IDs registered by Blazor layer components.
    /// </summary>
    RuntimeLayer,

    /// <summary>
    /// Targets original layer IDs within a composed MapLibre style.
    /// </summary>
    StyleLayer,

    /// <summary>
    /// Targets matching features within composed MapLibre style layers by composing a display filter.
    /// </summary>
    StyleLayerFeatures,

    /// <summary>
    /// Targets layers in a composed MapLibre style by tags.
    /// </summary>
    StyleLayerTag,
}
