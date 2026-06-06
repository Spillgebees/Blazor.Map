namespace Spillgebees.Blazor.Map;

/// <summary>
/// Base definition for a generated visual layer that renders clustered features.
/// </summary>
public abstract record ClusterLayerDefinition
{
    protected ClusterLayerDefinition(string idSuffix)
    {
        if (string.IsNullOrWhiteSpace(idSuffix))
        {
            throw new ArgumentException(
                "Cluster layer id suffix must not be null, empty, or whitespace.",
                nameof(idSuffix)
            );
        }

        IdSuffix = idSuffix;
    }

    /// <summary>
    /// The suffix appended to the source id when generating the layer id.
    /// </summary>
    public string IdSuffix { get; }

    /// <summary>
    /// Creates a circle layer definition for cluster bubbles.
    /// </summary>
    public static ClusterCircleLayerDefinition Circle(
        string idSuffix,
        StyleValue<string>? color = null,
        StyleValue<double>? radius = null,
        StyleValue<double>? opacity = null,
        StyleValue<string>? strokeColor = null,
        StyleValue<double>? strokeWidth = null
    ) => new(idSuffix, color, radius, opacity, strokeColor, strokeWidth);

    /// <summary>
    /// Creates a symbol layer definition for cluster labels.
    /// </summary>
    public static ClusterSymbolLayerDefinition Symbol(
        string idSuffix,
        StyleValue<string>? textField = null,
        StyleValue<double>? textSize = null,
        StyleValue<string>? textColor = null
    ) => new(idSuffix, textField, textSize, textColor);
}
