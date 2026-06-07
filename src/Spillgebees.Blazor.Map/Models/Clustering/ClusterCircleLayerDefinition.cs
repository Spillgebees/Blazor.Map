namespace Spillgebees.Blazor.Map;

/// <summary>
/// Visual definition for a circle layer that renders cluster bubbles.
/// </summary>
public sealed record ClusterCircleLayerDefinition : ClusterLayerDefinition
{
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

    public StyleValue<string>? Color { get; init; }

    public StyleValue<double>? Radius { get; init; }

    public StyleValue<double>? Opacity { get; init; }

    public StyleValue<string>? StrokeColor { get; init; }

    public StyleValue<double>? StrokeWidth { get; init; }
}
