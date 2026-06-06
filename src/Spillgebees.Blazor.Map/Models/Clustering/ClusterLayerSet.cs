namespace Spillgebees.Blazor.Map;

/// <summary>
/// Describes the generated visual layers used to render clustered source features.
/// </summary>
public sealed record ClusterLayerSet
{
    private ClusterLayerSet(bool enabled, IReadOnlyList<ClusterLayerDefinition> layers)
    {
        Enabled = enabled;
        Layers = layers;
    }

    /// <summary>
    /// Disables generated cluster visual layers.
    /// </summary>
    public static ClusterLayerSet None { get; } = new(false, []);

    /// <summary>
    /// Uses the built-in cluster bubble and count label layers.
    /// </summary>
    public static ClusterLayerSet Default { get; } =
        new(
            true,
            [
                ClusterLayerDefinition.Circle(
                    "clusters",
                    color: "#2563eb",
                    radius: Expr.Step("point_count", 22, 10, 28, 50, 36, 100, 44),
                    opacity: 0.9,
                    strokeColor: "#dbeafe",
                    strokeWidth: 2
                ),
                ClusterLayerDefinition.Symbol(
                    "cluster-count",
                    textField: Expr.Get("point_count_abbreviated"),
                    textSize: 14,
                    textColor: "#ffffff"
                ),
            ]
        );

    /// <summary>
    /// Whether generated cluster visual layers should be rendered.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// The generated cluster layer definitions.
    /// </summary>
    public IReadOnlyList<ClusterLayerDefinition> Layers { get; }

    /// <summary>
    /// Creates a custom generated cluster visual layer set.
    /// </summary>
    public static ClusterLayerSet Custom(params ClusterLayerDefinition[] layers)
    {
        ArgumentNullException.ThrowIfNull(layers);

        if (layers.Length == 0)
        {
            throw new ArgumentException("A custom cluster layer set must include at least one layer.", nameof(layers));
        }

        if (layers.Any(layer => layer is null))
        {
            throw new ArgumentException("A custom cluster layer set must not contain null layers.", nameof(layers));
        }

        var duplicateIdSuffix = layers
            .GroupBy(layer => layer.IdSuffix, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateIdSuffix is not null)
        {
            throw new ArgumentException(
                $"A custom cluster layer set must not contain duplicate id suffix '{duplicateIdSuffix}'.",
                nameof(layers)
            );
        }

        return new ClusterLayerSet(true, layers.ToArray());
    }
}
