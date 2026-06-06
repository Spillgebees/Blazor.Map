namespace Spillgebees.Blazor.Map;

/// <summary>
/// Visual definition for a circle layer that renders cluster bubbles.
/// </summary>
public sealed record ClusterCircleLayerDefinition(
    string IdSuffix,
    StyleValue<string>? Color = null,
    StyleValue<double>? Radius = null,
    StyleValue<double>? Opacity = null,
    StyleValue<string>? StrokeColor = null,
    StyleValue<double>? StrokeWidth = null
) : ClusterLayerDefinition(IdSuffix);
