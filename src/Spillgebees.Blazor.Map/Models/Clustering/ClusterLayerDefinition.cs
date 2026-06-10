namespace Spillgebees.Blazor.Map;

/// <summary>
/// Base definition for a generated visual layer that renders clustered features.
/// </summary>
public abstract record ClusterLayerDefinition
{
    /// <summary>
    /// Initializes the shared cluster layer placement values.
    /// </summary>
    protected ClusterLayerDefinition(
        string idSuffix,
        double? minZoom = null,
        double? maxZoom = null,
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null,
        bool interactive = true
    )
    {
        if (string.IsNullOrWhiteSpace(idSuffix))
        {
            throw new ArgumentException(
                "Cluster layer id suffix must not be null, empty, or whitespace.",
                nameof(idSuffix)
            );
        }

        ValidateZoomRange(minZoom, maxZoom);

        IdSuffix = idSuffix;
        MinZoom = minZoom;
        MaxZoom = maxZoom;
        BeforeLayerId = beforeLayerId;
        LayerGroup = layerGroup;
        BeforeLayerGroup = beforeLayerGroup;
        AfterLayerGroup = afterLayerGroup;
        Interactive = interactive;
    }

    /// <summary>
    /// The suffix appended to the source id when generating the layer id.
    /// </summary>
    public string IdSuffix { get; }

    /// <summary>
    /// The minimum zoom level at which the layer is visible.
    /// </summary>
    public double? MinZoom { get; }

    /// <summary>
    /// The maximum zoom level at which the layer is visible.
    /// </summary>
    public double? MaxZoom { get; }

    /// <summary>
    /// The id of an existing layer to insert the generated layer before.
    /// </summary>
    public string? BeforeLayerId { get; }

    /// <summary>
    /// The layer group the generated layer belongs to.
    /// </summary>
    public string? LayerGroup { get; }

    /// <summary>
    /// The layer group the generated layer is inserted before.
    /// </summary>
    public string? BeforeLayerGroup { get; }

    /// <summary>
    /// The layer group the generated layer is inserted after.
    /// </summary>
    public string? AfterLayerGroup { get; }

    /// <summary>
    /// Whether the generated layer raises pointer events. Default is true.
    /// </summary>
    public bool Interactive { get; }

    /// <summary>
    /// Validates that both zoom values are within 0-24 and that the minimum does not exceed the maximum.
    /// </summary>
    protected static void ValidateZoomRange(double? minZoom, double? maxZoom)
    {
        ValidateMinZoom(minZoom);
        ValidateMaxZoom(maxZoom);

        if (minZoom.HasValue && maxZoom.HasValue && minZoom.Value > maxZoom.Value)
        {
            throw new ArgumentException("Minimum zoom must be less than or equal to maximum zoom.", nameof(minZoom));
        }
    }

    private static void ValidateMinZoom(double? minZoom)
    {
        if (minZoom is < 0 or > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(minZoom), "Minimum zoom must be between 0 and 24.");
        }
    }

    private static void ValidateMaxZoom(double? maxZoom)
    {
        if (maxZoom is < 0 or > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(maxZoom), "Maximum zoom must be between 0 and 24.");
        }
    }

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
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null,
        bool interactive = true
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
            beforeLayerId,
            layerGroup,
            beforeLayerGroup,
            afterLayerGroup,
            interactive
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
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null,
        bool interactive = true
    ) =>
        new(
            idSuffix,
            textField,
            textSize,
            textColor,
            minZoom,
            maxZoom,
            beforeLayerId,
            layerGroup,
            beforeLayerGroup,
            afterLayerGroup,
            interactive
        );
}
