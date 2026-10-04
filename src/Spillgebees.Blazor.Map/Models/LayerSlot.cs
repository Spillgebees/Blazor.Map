using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Where a layer paints relative to the base style's labels. Slots hold no matter when a
/// layer is added, and they follow the base style when it changes.
/// </summary>
/// <remarks>
/// Paint order, bottom to top: the base style's ground, tile overlays, composed styles'
/// ground layers, layer components in <see cref="BelowLabels"/>, the base style's labels,
/// composed styles' labels, then layer components in <see cref="AboveLabels"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<LayerSlot>))]
public enum LayerSlot
{
    /// <summary>Paints above the base style's labels.</summary>
    [JsonStringEnumMemberName("above-labels")]
    AboveLabels,

    /// <summary>
    /// Paints above the base style's ground (land, water, roads) and below its labels, so
    /// text stays readable. Use it for 3D buildings and other area drawing.
    /// </summary>
    [JsonStringEnumMemberName("below-labels")]
    BelowLabels,
}
