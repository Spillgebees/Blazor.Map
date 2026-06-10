using System.Text.Json.Nodes;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Shared MapLibre spec builders for engine components: expression literals, pruned
/// objects, paint/layout dictionaries, and generated cluster layers (used by both
/// tracked entity layers and raw GeoJSON sources).
/// </summary>
internal static class EngineSpec
{
    public static JsonArray Expr(params object?[] parts)
    {
        var array = new JsonArray();
        foreach (var part in parts)
        {
            array.Add(
                part switch
                {
                    null => null,
                    JsonNode node => node,
                    string text => JsonValue.Create(text),
                    int number => JsonValue.Create(number),
                    double number => JsonValue.Create(number),
                    bool flag => JsonValue.Create(flag),
                    _ => throw new ArgumentException($"Unsupported expression part type '{part.GetType()}'."),
                }
            );
        }

        return array;
    }

    public static JsonObject Pruned(params (string Key, JsonNode? Value)[] entries)
    {
        var result = new JsonObject();
        foreach (var (key, value) in entries)
        {
            if (value is not null)
            {
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>Converts a layer property dictionary (StyleValue serializables) to JSON, dropping nulls.</summary>
    public static JsonObject FromProperties(Dictionary<string, object?> properties)
    {
        var result = new JsonObject();
        foreach (var (key, value) in properties)
        {
            if (value is not null)
            {
                result[key] = EngineJson.ToNode(value);
            }
        }

        return result;
    }

    /// <summary>MapLibre GeoJSON source options for enabled clustering.</summary>
    public static JsonNode? BuildClusterSourceOptions(ClusterOptions? cluster)
    {
        if (cluster is not { Enabled: true })
        {
            return null;
        }

        var options = new JsonObject { ["cluster"] = true, ["clusterRadius"] = cluster.Radius };
        if (cluster.MaxZoom is { } maxZoom)
        {
            options["clusterMaxZoom"] = maxZoom;
        }

        if (cluster.MinPoints is { } minPoints)
        {
            options["clusterMinPoints"] = minPoints;
        }

        if (cluster.Properties is { Count: > 0 } properties)
        {
            var clusterProperties = new JsonObject();
            foreach (var (name, expression) in properties)
            {
                clusterProperties[name] = EngineJson.ToNode(expression);
            }

            options["clusterProperties"] = clusterProperties;
        }

        return options;
    }

    public static JsonObject BuildClusterLayerSpec(string layerId, string sourceId, ClusterLayerDefinition definition)
    {
        var spec = new JsonObject
        {
            ["id"] = layerId,
            ["source"] = sourceId,
            ["filter"] = Expr("has", "point_count"),
        };

        if (definition.MinZoom is { } minZoom)
        {
            spec["minzoom"] = minZoom;
        }

        if (definition.MaxZoom is { } maxZoom)
        {
            spec["maxzoom"] = maxZoom;
        }

        switch (definition)
        {
            case ClusterCircleLayerDefinition circle:
                spec["type"] = "circle";
                spec["paint"] = Pruned(
                    ("circle-color", EngineJson.ToNode(circle.Color)),
                    ("circle-radius", EngineJson.ToNode(circle.Radius)),
                    ("circle-opacity", EngineJson.ToNode(circle.Opacity)),
                    ("circle-stroke-color", EngineJson.ToNode(circle.StrokeColor)),
                    ("circle-stroke-width", EngineJson.ToNode(circle.StrokeWidth))
                );
                break;
            case ClusterSymbolLayerDefinition symbol:
                spec["type"] = "symbol";
                spec["layout"] = Pruned(
                    ("text-field", EngineJson.ToNode(symbol.TextField)),
                    ("text-size", EngineJson.ToNode(symbol.TextSize)),
                    ("text-allow-overlap", JsonValue.Create(true))
                );
                spec["paint"] = Pruned(("text-color", EngineJson.ToNode(symbol.TextColor)));
                break;
            default:
                throw new NotSupportedException($"Unsupported cluster layer definition '{definition.GetType()}'.");
        }

        return spec;
    }
}
