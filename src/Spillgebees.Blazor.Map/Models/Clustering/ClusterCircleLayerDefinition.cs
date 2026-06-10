namespace Spillgebees.Blazor.Map;

/// <summary>
/// Visual definition for a circle layer that renders cluster bubbles.
/// </summary>
public sealed record ClusterCircleLayerDefinition : ClusterLayerDefinition
{
    /// <summary>
    /// Creates a circle layer definition for cluster bubbles.
    /// </summary>
    public ClusterCircleLayerDefinition(
        string idSuffix,
        StyleValue<string>? color = null,
        StyleValue<double>? radius = null,
        StyleValue<double>? opacity = null,
        StyleValue<string>? strokeColor = null,
        StyleValue<double>? strokeWidth = null,
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
        Color = color;
        Radius = radius;
        Opacity = opacity;
        StrokeColor = strokeColor;
        StrokeWidth = strokeWidth;
    }

    /// <summary>
    /// The circle fill color.
    /// </summary>
    public StyleValue<string>? Color { get; init; }

    /// <summary>
    /// The circle radius in pixels.
    /// </summary>
    public StyleValue<double>? Radius { get; init; }

    /// <summary>
    /// The circle fill opacity.
    /// </summary>
    public StyleValue<double>? Opacity { get; init; }

    /// <summary>
    /// The circle stroke color.
    /// </summary>
    public StyleValue<string>? StrokeColor { get; init; }

    /// <summary>
    /// The circle stroke width in pixels.
    /// </summary>
    public StyleValue<double>? StrokeWidth { get; init; }
}
