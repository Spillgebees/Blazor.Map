namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Defines the visual appearance of the map (tiles + styling rules).
/// Use factory methods to create instances from URLs, raster tile templates, or WMS endpoints.
/// </summary>
public record MapStyle
{
    private MapStyle(string? url, RasterTileSource? rasterSource, WmsTileSource? wmsSource)
    {
        Url = url;
        RasterSource = rasterSource;
        WmsSource = wmsSource;
    }

    /// <summary>
    /// The MapLibre style specification URL (for vector or JSON-based styles).
    /// </summary>
    internal string? Url { get; }

    /// <summary>
    /// The raster tile source configuration, when using raster tiles directly.
    /// </summary>
    internal RasterTileSource? RasterSource { get; }

    /// <summary>
    /// The WMS tile source configuration, when using a WMS endpoint.
    /// </summary>
    internal WmsTileSource? WmsSource { get; }

    /// <summary>
    /// Creates a <see cref="MapStyle"/> from a MapLibre style specification URL.
    /// </summary>
    /// <param name="url">The URL to a MapLibre-compatible style JSON.</param>
    public static MapStyle FromUrl(string url) => new(url, null, null);

    /// <summary>
    /// Creates a <see cref="MapStyle"/> from a raster tile URL template.
    /// </summary>
    /// <param name="urlTemplate">The tile URL template with <c>{z}</c>, <c>{x}</c>, <c>{y}</c> placeholders.</param>
    /// <param name="attribution">The attribution text to display on the map.</param>
    /// <param name="tileSize">The tile size in pixels. Default is 256.</param>
    public static MapStyle FromRasterUrl(string urlTemplate, string attribution, int tileSize = 256) =>
        new(null, new RasterTileSource(urlTemplate, attribution, tileSize), null);

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
    public static MapStyle FromWmsUrl(
        string baseUrl,
        string layers,
        string attribution,
        string format = "image/png",
        bool transparent = false,
        string version = "1.1.1",
        int tileSize = 256
    ) => new(null, null, new WmsTileSource(baseUrl, layers, attribution, format, transparent, version, tileSize));

    /// <summary>
    /// Preset styles from <a href="https://openfreemap.org/">OpenFreeMap</a> — free vector tiles, no API key required.
    /// </summary>
    public static class OpenFreeMap
    {
        /// <summary>
        /// A clean, modern vector style.
        /// </summary>
        public static MapStyle Liberty => FromUrl("https://tiles.openfreemap.org/styles/liberty");

        /// <summary>
        /// A colorful vector style.
        /// </summary>
        public static MapStyle Bright => FromUrl("https://tiles.openfreemap.org/styles/bright");

        /// <summary>
        /// A light, minimal vector style — ideal for data visualization.
        /// </summary>
        public static MapStyle Positron => FromUrl("https://tiles.openfreemap.org/styles/positron");
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
                "© <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors"
            );
    }
}

/// <summary>
/// Configuration for a raster tile source.
/// </summary>
/// <param name="UrlTemplate">The tile URL template with <c>{z}</c>, <c>{x}</c>, <c>{y}</c> placeholders.</param>
/// <param name="Attribution">The attribution text to display on the map.</param>
/// <param name="TileSize">The tile size in pixels.</param>
internal sealed record RasterTileSource(string UrlTemplate, string Attribution, int TileSize);

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
internal sealed record WmsTileSource(
    string BaseUrl,
    string Layers,
    string Attribution,
    string Format,
    bool Transparent,
    string Version,
    int TileSize
);
