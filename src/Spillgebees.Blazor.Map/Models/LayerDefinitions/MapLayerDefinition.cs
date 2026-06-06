namespace Spillgebees.Blazor.Map;

/// <summary>
/// Base definition for a source-owned MapLibre style layer.
/// </summary>
public abstract record MapLayerDefinition
{
    protected MapLayerDefinition(
        string idSuffix,
        string? key = null,
        object? filter = null,
        double? minZoom = null,
        double? maxZoom = null,
        bool visible = true,
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null
    )
    {
        ValidateIdSuffix(idSuffix);
        ValidateZoomRange(minZoom, maxZoom);

        IdSuffix = idSuffix;
        Key = string.IsNullOrWhiteSpace(key) ? idSuffix : key;
        Filter = filter;
        MinZoom = minZoom;
        MaxZoom = maxZoom;
        Visible = visible;
        BeforeLayerId = beforeLayerId;
        LayerGroup = layerGroup;
        BeforeLayerGroup = beforeLayerGroup;
        AfterLayerGroup = afterLayerGroup;
    }

    /// <summary>
    /// The MapLibre layer type.
    /// </summary>
    public abstract string Type { get; }

    /// <summary>
    /// The suffix appended to the source id when generating a layer id.
    /// </summary>
    public string IdSuffix { get; }

    /// <summary>
    /// A stable logical key for diffing or matching definitions. Defaults to <see cref="IdSuffix" />.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Optional MapLibre layer filter expression.
    /// </summary>
    public object? Filter { get; init; }

    /// <summary>
    /// Optional minimum zoom at which this layer is visible.
    /// </summary>
    public double? MinZoom { get; }

    /// <summary>
    /// Optional maximum zoom at which this layer is visible.
    /// </summary>
    public double? MaxZoom { get; }

    /// <summary>
    /// Whether the generated layer is visible.
    /// </summary>
    public bool Visible { get; init; }

    /// <summary>
    /// Optional concrete layer id before which this layer should be inserted.
    /// </summary>
    public string? BeforeLayerId { get; init; }

    /// <summary>
    /// Optional logical layer group for ordering.
    /// </summary>
    public string? LayerGroup { get; init; }

    /// <summary>
    /// Optional logical layer group before which this layer's group should be ordered.
    /// </summary>
    public string? BeforeLayerGroup { get; init; }

    /// <summary>
    /// Optional logical layer group after which this layer's group should be ordered.
    /// </summary>
    public string? AfterLayerGroup { get; init; }

    /// <summary>
    /// Resolves the concrete MapLibre layer id for a source id.
    /// </summary>
    public string ResolveId(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Source id must not be null, empty, or whitespace.", nameof(sourceId));
        }

        return $"{sourceId}-{IdSuffix}";
    }

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

    private static void ValidateIdSuffix(string idSuffix)
    {
        if (string.IsNullOrWhiteSpace(idSuffix))
        {
            throw new ArgumentException("Layer id suffix must not be null, empty, or whitespace.", nameof(idSuffix));
        }
    }
}
