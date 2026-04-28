using Spillgebees.Blazor.Map.Models.Options;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Builds diff-friendly GeoJSON feature collections for tracked entities.
/// </summary>
public static class TrackedEntityGeoJsonBuilder
{
    /// <summary>
    /// Builds the primary entity feature collection.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> BuildPrimaryFeatureCollection<TItem>(
        IReadOnlyList<TrackedEntity<TItem>> entities
    ) => BuildFeatureCollection(entities.Select(BuildPrimaryFeature));

    /// <summary>
    /// Builds the companion decoration feature collection.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> BuildDecorationFeatureCollection<TItem>(
        IReadOnlyList<TrackedEntity<TItem>> entities
    ) => BuildFeatureCollection(entities.SelectMany(BuildDecorationFeatures));

    private static IReadOnlyDictionary<string, object?> BuildFeatureCollection(
        IEnumerable<Dictionary<string, object?>> features
    ) => new Dictionary<string, object?> { ["type"] = "FeatureCollection", ["features"] = features.ToArray() };

    private static Dictionary<string, object?> BuildPrimaryFeature<TItem>(TrackedEntity<TItem> entity)
    {
        var properties = CreateBaseProperties(entity, TrackedEntityFeatureKind.Primary);
        properties[TrackedEntityFeatureProperties.IconImage] = entity.Symbol.IconImage;
        properties[TrackedEntityFeatureProperties.IconSize] = entity.Symbol.Size;
        properties[TrackedEntityFeatureProperties.IconRotation] = entity.Symbol.Rotation;
        properties[TrackedEntityFeatureProperties.Anchor] = entity.Symbol.Anchor?.ToJsonName();
        properties[TrackedEntityFeatureProperties.Offset] = ToOffsetArray(entity.Symbol.Offset);

        return BuildPointFeature(entity.Id, entity.Position, properties);
    }

    private static IEnumerable<Dictionary<string, object?>> BuildDecorationFeatures<TItem>(TrackedEntity<TItem> entity)
    {
        foreach (var decoration in entity.Decorations)
        {
            if (decoration.Text is null && decoration.IconImage is null)
            {
                throw new InvalidOperationException(
                    $"Tracked entity decoration '{decoration.Id}' must define text, icon, or both."
                );
            }

            var properties = CreateBaseProperties(entity, TrackedEntityFeatureKind.Decoration);
            properties[TrackedEntityFeatureProperties.DecorationId] = decoration.Id;
            properties[TrackedEntityFeatureProperties.Text] = decoration.Text;
            properties[TrackedEntityFeatureProperties.IconImage] = decoration.IconImage;
            properties[TrackedEntityFeatureProperties.Anchor] = decoration.Anchor?.ToJsonName();
            properties[TrackedEntityFeatureProperties.Offset] = ToOffsetArray(decoration.Offset);
            properties[TrackedEntityFeatureProperties.Color] = decoration.Color ?? entity.Color;
            properties[TrackedEntityFeatureProperties.IconSize] = decoration.IconSize;
            properties[TrackedEntityFeatureProperties.TextSize] = decoration.TextSize;
            properties[TrackedEntityFeatureProperties.IconRotation] = decoration.Rotation;
            properties[TrackedEntityFeatureProperties.DisplayMode] = decoration.DisplayMode.ToMapLibreValue();
            properties[TrackedEntityFeatureProperties.RenderOrder] = decoration.RenderOrder ?? entity.RenderOrder;
            properties[TrackedEntityFeatureProperties.HaloColor] = decoration.HaloColor;
            properties[TrackedEntityFeatureProperties.HaloWidth] = decoration.HaloWidth;
            properties[TrackedEntityFeatureProperties.IconColor] = decoration.IconColor;

            yield return BuildPointFeature($"{entity.Id}::{decoration.Id}", entity.Position, properties);
        }
    }

    private static Dictionary<string, object?> CreateBaseProperties<TItem>(
        TrackedEntity<TItem> entity,
        TrackedEntityFeatureKind kind
    )
    {
        var properties = new Dictionary<string, object?>
        {
            [TrackedEntityFeatureProperties.Kind] = kind.ToMapLibreValue(),
            [TrackedEntityFeatureProperties.EntityId] = entity.Id,
            [TrackedEntityFeatureProperties.Color] = entity.Color,
            [TrackedEntityFeatureProperties.HoverScale] = entity.Hover?.Scale,
            [TrackedEntityFeatureProperties.HoverRaise] = entity.Hover?.RaiseToTop,
            [TrackedEntityFeatureProperties.RenderOrder] = entity.RenderOrder,
            [TrackedEntityFeatureProperties.Item] = entity.Item,
        };

        if (entity.Properties is not null)
        {
            foreach (var (key, value) in entity.Properties)
            {
                if (TrackedEntityFeatureProperties.Reserved.Contains(key))
                {
                    throw new InvalidOperationException(
                        $"Tracked entity custom property '{key}' collides with a reserved tracked entity property."
                    );
                }

                properties[key] = value;
            }
        }

        return properties;
    }

    private static Dictionary<string, object?> BuildPointFeature(
        string id,
        Coordinate position,
        Dictionary<string, object?> properties
    ) =>
        new()
        {
            ["type"] = "Feature",
            ["id"] = id,
            ["geometry"] = new Dictionary<string, object?>
            {
                ["type"] = "Point",
                ["coordinates"] = new[] { position.Longitude, position.Latitude },
            },
            ["properties"] = properties.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value),
        };

    private static double[]? ToOffsetArray(PixelPoint? offset) => offset is null ? null : [offset.X, offset.Y];
}
