namespace Spillgebees.Blazor.Map;

/// <summary>
/// Visual definition for a symbol layer that renders cluster labels.
/// </summary>
public sealed record ClusterSymbolLayerDefinition : ClusterLayerDefinition
{
    /// <summary>
    /// Creates a symbol layer definition for cluster labels.
    /// </summary>
    public ClusterSymbolLayerDefinition(
        string idSuffix,
        StyleValue<string>? textField = null,
        StyleValue<double>? textSize = null,
        StyleValue<string>? textColor = null,
        double? minZoom = null,
        double? maxZoom = null,
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null,
        bool interactive = true
    )
        : base(idSuffix, minZoom, maxZoom, beforeLayerId, layerGroup, beforeLayerGroup, afterLayerGroup, interactive)
    {
        TextField = textField;
        TextSize = textSize;
        TextColor = textColor;
    }

    /// <summary>
    /// The label text content.
    /// </summary>
    public StyleValue<string>? TextField { get; init; }

    /// <summary>
    /// The label text size in pixels.
    /// </summary>
    public StyleValue<double>? TextSize { get; init; }

    /// <summary>
    /// The label text color.
    /// </summary>
    public StyleValue<string>? TextColor { get; init; }
}
