namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Defines the visual appearance of the map (tiles + styling rules).
/// Use factory methods to create instances from URLs, raster tile templates, or WMS endpoints.
/// </summary>
public record MapStyle
{
    private MapStyle(
        string? id,
        string? url,
        ReferrerPolicy? referrerPolicy,
        RasterTileSource? rasterSource,
        WmsTileSource? wmsSource
    )
    {
        Id = id;
        Url = url;
        ReferrerPolicy = referrerPolicy;
        RasterSource = rasterSource;
        WmsSource = wmsSource;
    }

    /// <summary>
    /// Stable identifier for this style when used in multi-style composition.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// The MapLibre style specification URL (for vector or JSON-based styles).
    /// </summary>
    public string? Url { get; }

    /// <summary>
    /// The referrer policy to apply to MapLibre-managed requests for this style.
    /// </summary>
    public ReferrerPolicy? ReferrerPolicy { get; init; }

    /// <summary>
    /// The raster tile source configuration, when using raster tiles directly.
    /// </summary>
    public RasterTileSource? RasterSource { get; init; }

    /// <summary>
    /// The WMS tile source configuration, when using a WMS endpoint.
    /// </summary>
    public WmsTileSource? WmsSource { get; init; }

    /// <summary>
    /// Returns a copy of this style with the given stable identifier.
    /// </summary>
    public MapStyle WithId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Map style ID must not be empty.", nameof(id));
        }

        return this with
        {
            Id = id,
        };
    }

    /// <summary>
    /// Returns a copy of this style with the given referrer policy.
    /// </summary>
    public MapStyle WithReferrerPolicy(ReferrerPolicy? referrerPolicy)
    {
        if (RasterSource is not null)
        {
            return this with { RasterSource = RasterSource with { ReferrerPolicy = referrerPolicy } };
        }

        if (WmsSource is not null)
        {
            return this with { WmsSource = WmsSource with { ReferrerPolicy = referrerPolicy } };
        }

        return this with
        {
            ReferrerPolicy = referrerPolicy,
        };
    }

    /// <summary>
    /// Creates a <see cref="MapStyle"/> from a MapLibre style specification URL.
    /// </summary>
    /// <param name="url">The URL to a MapLibre-compatible style JSON.</param>
    public static MapStyle FromUrl(string url) => new(null, url, null, null, null);

    /// <summary>
    /// Creates a <see cref="MapStyle"/> from a raster tile URL template.
    /// </summary>
    /// <param name="urlTemplate">The tile URL template with <c>{z}</c>, <c>{x}</c>, <c>{y}</c> placeholders.</param>
    /// <param name="attribution">The attribution text to display on the map.</param>
    /// <param name="tileSize">The tile size in pixels. Default is 256.</param>
    /// <param name="referrerPolicy">The referrer policy to apply to tile requests.</param>
    public static MapStyle FromRasterUrl(
        string urlTemplate,
        string attribution,
        int tileSize = 256,
        ReferrerPolicy? referrerPolicy = null
    ) => new(null, null, null, new RasterTileSource(urlTemplate, attribution, tileSize, referrerPolicy), null);

    /// <summary>
    /// Creates a <see cref="MapStyle"/> from a WMS endpoint.
    /// </summary>
    /// <param name="baseUrl">The WMS service base URL.</param>
    /// <param name="layers">The WMS layers to request.</param>
    /// <param name="attribution">The attribution text to display on the map.</param>
    /// <param name="format">The image format to request. Default is <c>"image/png"</c>.</param>
    /// <param name="transparent">Whether to request transparent tiles. Default is <see langword="false"/>.</param>
    /// <param name="version">The WMS service version. Default is <c>"1.1.1"</c>.</param>
    /// <param name="tileSize">The tile size in pixels. Default is 256.</param>
    /// <param name="referrerPolicy">The referrer policy to apply to tile requests.</param>
    public static MapStyle FromWmsUrl(
        string baseUrl,
        string layers,
        string attribution,
        string format = "image/png",
        bool transparent = false,
        string version = "1.1.1",
        int tileSize = 256,
        ReferrerPolicy? referrerPolicy = null
    ) =>
        new(
            null,
            null,
            null,
            null,
            new WmsTileSource(baseUrl, layers, attribution, format, transparent, version, tileSize, referrerPolicy)
        );

    /// <summary>
    /// Creates a <see cref="MapStyle"/> from a WMTS (Web Map Tile Service) endpoint.
    /// This is the standard protocol for ArcGIS cached tile services.
    /// </summary>
    /// <param name="baseUrl">The WMTS base URL (e.g., <c>https://server/arcgis/rest/services/Name/MapServer/WMTS</c>).</param>
    /// <param name="layer">The WMTS layer identifier.</param>
    /// <param name="attribution">The attribution text to display on the map.</param>
    /// <param name="tileMatrixSet">The tile matrix set identifier. Default is <c>"default028mm"</c>.</param>
    /// <param name="style">The WMTS style. Default is <c>"default"</c>.</param>
    /// <param name="format">The image format extension. Default is <c>"png"</c>.</param>
    /// <param name="tileSize">The tile size in pixels. Default is 256.</param>
    public static MapStyle FromWmtsUrl(
        string baseUrl,
        string layer,
        string attribution,
        string tileMatrixSet = "default028mm",
        string style = "default",
        string format = "png",
        int tileSize = 256
    )
    {
        var urlTemplate =
            $"{baseUrl.TrimEnd('/')}/tile/1.0.0/{layer}/{style}/{tileMatrixSet}/{{z}}/{{y}}/{{x}}.{format}";
        return FromRasterUrl(urlTemplate, attribution, tileSize);
    }

    /// <summary>
    /// Creates a <see cref="MapStyle"/> from an ArcGIS MapServer tile endpoint.
    /// </summary>
    /// <param name="mapServerUrl">The MapServer URL (e.g., <c>https://server/arcgis/rest/services/Name/MapServer</c>).</param>
    /// <param name="attribution">The attribution text to display on the map.</param>
    /// <param name="tileSize">The tile size in pixels. Default is 256.</param>
    public static MapStyle FromArcGisMapServer(string mapServerUrl, string attribution, int tileSize = 256)
    {
        var urlTemplate = $"{mapServerUrl.TrimEnd('/')}/tile/{{z}}/{{y}}/{{x}}";
        return FromRasterUrl(urlTemplate, attribution, tileSize);
    }

    /// <summary>
    /// Preset styles from <a href="https://openfreemap.org/">OpenFreeMap</a> — free vector tiles, no API key required.
    /// </summary>
    public static class OpenFreeMap
    {
        /// <summary>
        /// A clean, modern vector style.
        /// </summary>
        public static MapStyle Liberty => FromUrl("https://tiles.openfreemap.org/styles/liberty").WithId("sgb-liberty");

        /// <summary>
        /// A colorful vector style.
        /// </summary>
        public static MapStyle Bright => FromUrl("https://tiles.openfreemap.org/styles/bright").WithId("sgb-bright");

        /// <summary>
        /// A light, minimal vector style — ideal for data visualization.
        /// </summary>
        public static MapStyle Positron =>
            FromUrl("https://tiles.openfreemap.org/styles/positron").WithId("sgb-positron");
    }

    /// <summary>
    /// Preset styles from <a href="https://www.openstreetmap.org/">OpenStreetMap</a>.
    /// </summary>
    public static class OpenStreetMap
    {
        /// <summary>
        /// Classic OpenStreetMap raster tiles.
        /// </summary>
        public static MapStyle Standard =>
            FromRasterUrl(
                    "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
                    "© <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors",
                    referrerPolicy: Models.ReferrerPolicy.Origin
                )
                .WithId("sgb-openstreetmap-standard");
    }
}

/// <summary>
/// Configuration for a raster tile source.
/// </summary>
/// <param name="UrlTemplate">The tile URL template with <c>{z}</c>, <c>{x}</c>, <c>{y}</c> placeholders.</param>
/// <param name="Attribution">The attribution text to display on the map.</param>
/// <param name="TileSize">The tile size in pixels.</param>
/// <param name="ReferrerPolicy">The referrer policy to apply to tile requests.</param>
public sealed record RasterTileSource(
    string UrlTemplate,
    string Attribution,
    int TileSize,
    ReferrerPolicy? ReferrerPolicy = null
);

/// <summary>
/// Configuration for a WMS tile source.
/// </summary>
/// <param name="BaseUrl">The WMS service base URL.</param>
/// <param name="Layers">The WMS layers to request.</param>
/// <param name="Attribution">The attribution text to display on the map.</param>
/// <param name="Format">The image format to request.</param>
/// <param name="Transparent">Whether to request transparent tiles.</param>
/// <param name="Version">The WMS service version.</param>
/// <param name="TileSize">The tile size in pixels.</param>
/// <param name="ReferrerPolicy">The referrer policy to apply to tile requests.</param>
public sealed record WmsTileSource(
    string BaseUrl,
    string Layers,
    string Attribution,
    string Format,
    bool Transparent,
    string Version,
    int TileSize,
    ReferrerPolicy? ReferrerPolicy = null
);
