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
    StyleValue<double>? StrokeWidth = null,
    double? MinZoom = null,
    double? MaxZoom = null,
    bool Visible = true,
    string? BeforeLayerId = null,
    string? LayerGroup = null,
    string? BeforeLayerGroup = null,
    string? AfterLayerGroup = null,
    bool Interactive = true
)
    : ClusterLayerDefinition(
        IdSuffix,
        MinZoom,
        MaxZoom,
        Visible,
        BeforeLayerId,
        LayerGroup,
        BeforeLayerGroup,
        AfterLayerGroup,
        Interactive
    );
