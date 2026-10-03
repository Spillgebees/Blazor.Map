using System.Text.Json.Nodes;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Builds the style options JSON consumed by <c>Engine.createMap</c>/<c>Engine.setStyles</c>.
/// </summary>
internal static class EngineStyleJson
{
    /// <summary>
    /// Resolves the effective style configuration: <paramref name="styles"/> wins over
    /// <paramref name="style"/> wins over <paramref name="styleSpec"/>.
    /// </summary>
    public static JsonObject BuildStylesNode(
        IReadOnlyList<MapStyle>? styles,
        MapStyle? style,
        string? styleSpec,
        string? composedGlyphsUrl
    )
    {
        var node = new JsonObject();
        var effectiveStyles = styles ?? (style is null ? null : (IReadOnlyList<MapStyle>)[style]);
        if (effectiveStyles is { Count: > 0 })
        {
            var array = new JsonArray();
            foreach (var entry in effectiveStyles)
            {
                array.Add(StyleToNode(entry));
            }

            node["styles"] = array;
        }
        else if (styleSpec is { Length: > 0 })
        {
            node["style"] = styleSpec.TrimStart().StartsWith('{') ? JsonNode.Parse(styleSpec) : styleSpec;
        }

        if (composedGlyphsUrl is not null)
        {
            node["composedGlyphsUrl"] = composedGlyphsUrl;
        }

        return node;
    }

    public static string ThemeName(MapTheme theme) => theme == MapTheme.Dark ? "dark" : "light";

    private static JsonObject StyleToNode(MapStyle style) =>
        new()
        {
            ["id"] = style.Id,
            ["url"] = style.Url,
            ["referrerPolicy"] = style.ReferrerPolicy is { } policy ? EnumJsonName.Get(policy) : null,
            ["rasterSource"] = style.RasterSource is { } raster
                ? new JsonObject
                {
                    ["urlTemplate"] = raster.UrlTemplate,
                    ["attribution"] = raster.Attribution,
                    ["tileSize"] = raster.TileSize,
                    ["referrerPolicy"] = raster.ReferrerPolicy is { } rasterPolicy
                        ? EnumJsonName.Get(rasterPolicy)
                        : null,
                }
                : null,
            ["wmsSource"] = style.WmsSource is { } wms
                ? new JsonObject
                {
                    ["baseUrl"] = wms.BaseUrl,
                    ["layers"] = wms.Layers,
                    ["attribution"] = wms.Attribution,
                    ["format"] = wms.Format,
                    ["transparent"] = wms.Transparent,
                    ["version"] = wms.Version,
                    ["tileSize"] = wms.TileSize,
                    ["referrerPolicy"] = wms.ReferrerPolicy is { } wmsPolicy ? EnumJsonName.Get(wmsPolicy) : null,
                }
                : null,
        };
}
