namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A tile layer to be used as a base map layer.
/// </summary>
/// <param name="UrlTemplate">
/// The URL template for the tile layer.
/// See <see href="https://leafletjs.com/reference.html#tilelayer">leaflet</see> for details.
/// </param>
/// <param name="Attribution">The attribution text to be displayed on the map.</param>
/// <param name="DetectRetina">
/// Whether to use high-DPI tiles if the browser supports them.
/// Default is <see langword="null" />, which uses the Leaflet default of <see langword="false" />.
/// </param>
/// <param name="TileSize">
/// The size of the tiles in pixels. Default is <see langword="null" />
/// which uses the Leaflet default of 256.
/// </param>
/// <param name="Layers">
/// The WMS layers to request. When this is set, the layer is created via Leaflet's WMS tile layer support.
/// </param>
/// <param name="Format">
/// The image format to request for WMS tiles. Default is <see langword="null" />, which uses the Leaflet default.
/// </param>
/// <param name="Transparent">
/// Whether the WMS tiles should be requested with transparency enabled.
/// Default is <see langword="null" />, which uses the Leaflet default of <see langword="false" />.
/// </param>
/// <param name="Version">
/// The WMS service version to request. Default is <see langword="null" />, which uses the Leaflet default.
/// </param>
/// <param name="Styles">
/// The WMS styles parameter to send. Default is <see langword="null" />, which uses the Leaflet default.
/// </param>
public record TileLayer(
    string UrlTemplate,
    string Attribution,
    bool? DetectRetina = null,
    int? TileSize = null,
    string? Layers = null,
    string? Format = null,
    bool? Transparent = null,
    string? Version = null,
    string? Styles = null
)
{
    /// <summary>
    /// Creates a WMS tile layer with a similar API surface to the regular <see cref="TileLayer" /> record.
    /// </summary>
    public static TileLayer Wms(
        string baseUrl,
        string attribution,
        string layers,
        string? format = null,
        bool? transparent = null,
        string? version = null,
        string? styles = null,
        bool? detectRetina = null,
        int? tileSize = null
    ) =>
        new(
            UrlTemplate: baseUrl,
            Attribution: attribution,
            DetectRetina: detectRetina,
            TileSize: tileSize,
            Layers: layers,
            Format: format,
            Transparent: transparent,
            Version: version,
            Styles: styles
        );

    /// <summary>
    /// OpenStreetMap tile layer, free to use with attribution.
    /// </summary>
    public static readonly TileLayer OpenStreetMap = new(
        UrlTemplate: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
        Attribution: "&copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
    );

    /// <summary>
    /// OpenStreetMap France tile layer, free to use with attribution.
    /// </summary>
    public static readonly TileLayer OpenStreetMapFrance = new(
        UrlTemplate: "https://{s}.tile.openstreetmap.fr/osmfr/{z}/{x}/{y}.png",
        Attribution: "&copy; OpenStreetMap France | &copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
    );

    /// <summary>
    /// Public transport map tile layer, free to use with attribution.
    /// </summary>
    public static readonly TileLayer OpnvKarte = new(
        UrlTemplate: "https://tileserver.memomaps.de/tilegen/{z}/{x}/{y}.png",
        Attribution: "&copy; <a href='memomaps.de'>memomaps.de</a> <a href='https://creativecommons.org/licenses/by-sa/2.0/CC-BY-SA'>CC-BY-SA</a> | &copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
    );

    public static readonly TileLayer OpenDataBaseMap = new(
        UrlTemplate: "https://wmts1.geoportail.lu/opendata/wmts/basemap/GLOBAL_WEBMERCATOR/{z}/{x}/{y}.png",
        Attribution: "&copy;  <a href='https://data.public.lu/en/datasets/carte-de-base-webservices-wms-et-wmts'>OpenData</a> <a href='https://creativecommons.org/publicdomain/zero/1.0/'>CC0</a>/<a href='https://creativecommons.org/licenses/by/4.0/deed.en'>CC-BY</a> | &copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
    );

    /// <summary>
    /// Luxembourg open data WMS base map layer, free to use with attribution.
    /// </summary>
    public static readonly TileLayer OpenDataBaseMapWms = Wms(
        baseUrl: "https://wms.geoportail.lu/geoserver/opendata/wms",
        attribution: "&copy;  <a href='https://data.public.lu/en/datasets/carte-de-base-webservices-wms-et-wmts'>OpenData</a> <a href='https://creativecommons.org/publicdomain/zero/1.0/'>CC0</a>/<a href='https://creativecommons.org/licenses/by/4.0/deed.en'>CC-BY</a> | &copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors",
        layers: "basemap",
        format: "image/png",
        transparent: false,
        version: "1.3.0"
    );
}
