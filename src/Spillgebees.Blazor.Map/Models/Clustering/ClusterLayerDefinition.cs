namespace Spillgebees.Blazor.Map;

/// <summary>
/// Base definition for a generated visual layer that renders clustered features.
/// </summary>
public abstract record ClusterLayerDefinition
{
    protected ClusterLayerDefinition(
        string idSuffix,
        double? minZoom = null,
        double? maxZoom = null,
        bool visible = true,
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null
    )
    {
        if (string.IsNullOrWhiteSpace(idSuffix))
        {
            throw new ArgumentException(
                "Cluster layer id suffix must not be null, empty, or whitespace.",
                nameof(idSuffix)
            );
        }

        IdSuffix = idSuffix;
        MinZoom = minZoom;
        MaxZoom = maxZoom;
        Visible = visible;
        BeforeLayerId = beforeLayerId;
        LayerGroup = layerGroup;
        BeforeLayerGroup = beforeLayerGroup;
        AfterLayerGroup = afterLayerGroup;
    }

    /// <summary>
    /// The suffix appended to the source id when generating the layer id.
    /// </summary>
    public string IdSuffix { get; }

    public double? MinZoom { get; }

    public double? MaxZoom { get; }

    public bool Visible { get; }

    public string? BeforeLayerId { get; }

    public string? LayerGroup { get; }

    public string? BeforeLayerGroup { get; }

    public string? AfterLayerGroup { get; }

    /// <summary>
    /// Creates a circle layer definition for cluster bubbles.
    /// </summary>
    public static ClusterCircleLayerDefinition Circle(
        string idSuffix,
        StyleValue<string>? color = null,
        StyleValue<double>? radius = null,
        StyleValue<double>? opacity = null,
        StyleValue<string>? strokeColor = null,
        StyleValue<double>? strokeWidth = null,
        double? minZoom = null,
        double? maxZoom = null,
        bool visible = true,
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null
    ) =>
        new(
            idSuffix,
            color,
            radius,
            opacity,
            strokeColor,
            strokeWidth,
            minZoom,
            maxZoom,
            visible,
            beforeLayerId,
            layerGroup,
            beforeLayerGroup,
            afterLayerGroup
        );

    /// <summary>
    /// Creates a symbol layer definition for cluster labels.
    /// </summary>
    public static ClusterSymbolLayerDefinition Symbol(
        string idSuffix,
        StyleValue<string>? textField = null,
        StyleValue<double>? textSize = null,
        StyleValue<string>? textColor = null,
        double? minZoom = null,
        double? maxZoom = null,
        bool visible = true,
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null
    ) =>
        new(
            idSuffix,
            textField,
            textSize,
            textColor,
            minZoom,
            maxZoom,
            visible,
            beforeLayerId,
            layerGroup,
            beforeLayerGroup,
            afterLayerGroup
        );
}
