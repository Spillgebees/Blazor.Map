namespace Spillgebees.Blazor.Map;

/// <summary>
/// Visual definition for a symbol layer that renders cluster labels.
/// </summary>
public sealed record ClusterSymbolLayerDefinition(
    string IdSuffix,
    StyleValue<string>? TextField = null,
    StyleValue<double>? TextSize = null,
    StyleValue<string>? TextColor = null,
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
