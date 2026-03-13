using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A tile layer to be used as a base map layer.
/// </summary>
/// <param name="UrlTemplate">
/// The URL template for the tile layer.
/// See <see href="https://leafletjs.com/reference.html#tilelayer">leaflet</see> for details.
/// </param>
/// <param name="Attribution">The attribution text to be displayed on the map.</param>
/// <param name="Tile">Options that apply to regular tile requests.</param>
/// <param name="Wms">Options that apply to WMS requests.</param>
public record TileLayer(string UrlTemplate, string Attribution, TileLayerOptions? Tile = null, WmsLayerOptions? Wms = null)
{
    public TileLayer(string urlTemplate, string attribution, bool? detectRetina = null, int? tileSize = null)
        : this(urlTemplate, attribution, CreateTileOptions(detectRetina, tileSize))
    {
    }

    [JsonIgnore]
    public bool? DetectRetina => Tile?.DetectRetina;

    [JsonIgnore]
    public int? TileSize => Tile?.TileSize;

    [JsonIgnore]
    public string? Layers => Wms?.Layers;

    [JsonIgnore]
    public string? Format => Wms?.Format;

    [JsonIgnore]
    public bool? Transparent => Wms?.Transparent;

    [JsonIgnore]
    public string? Version => Wms?.Version;

    [JsonIgnore]
    public string? Styles => Wms?.Styles;

    /// <summary>
    /// Creates a WMS tile layer with a similar API surface to the regular <see cref="TileLayer" /> record.
    /// </summary>
    public static TileLayer CreateWms(
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
            Tile: CreateTileOptions(detectRetina, tileSize),
            Wms: new WmsLayerOptions(
                Layers: layers,
                Format: format,
                Transparent: transparent,
                Version: version,
                Styles: styles
            )
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
    public static readonly TileLayer OpenDataBaseMapWms = CreateWms(
        baseUrl: "https://wms.geoportail.lu/geoserver/opendata/wms",
        attribution: "&copy;  <a href='https://data.public.lu/en/datasets/carte-de-base-webservices-wms-et-wmts'>OpenData</a> <a href='https://creativecommons.org/publicdomain/zero/1.0/'>CC0</a>/<a href='https://creativecommons.org/licenses/by/4.0/deed.en'>CC-BY</a> | &copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors",
        layers: "basemap",
        format: "image/png",
        transparent: false,
        version: "1.3.0"
    );

    private static TileLayerOptions? CreateTileOptions(bool? detectRetina, int? tileSize) =>
        detectRetina is null && tileSize is null ? null : new TileLayerOptions(detectRetina, tileSize);
}
