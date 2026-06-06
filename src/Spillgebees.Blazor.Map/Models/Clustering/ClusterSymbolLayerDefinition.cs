namespace Spillgebees.Blazor.Map;

/// <summary>
/// Visual definition for a symbol layer that renders cluster labels.
/// </summary>
public sealed record ClusterSymbolLayerDefinition(
    string IdSuffix,
    StyleValue<string>? TextField = null,
    StyleValue<double>? TextSize = null,
    StyleValue<string>? TextColor = null
) : ClusterLayerDefinition(IdSuffix);
