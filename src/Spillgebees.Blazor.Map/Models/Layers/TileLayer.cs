namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// A tile layer to be used as a base map layer.
/// </summary>
/// <param name="UrlTemplate">
/// The URL template for the tile layer.
/// See <see href="https://leafletjs.com/reference.html#tilelayer">leaflet</see> for details.
/// </param>
/// <param name="Attribution">The attribution text to be displayed on the map.</param>
/// <param name="ReferrerPolicyOptions">Options for defining the <c>Referer</c> header.</param>
/// <param name="DetectRetina">
/// Whether to use high-DPI tiles if the browser supports them.
/// Default is <see langword="null" />.
/// </param>
/// <param name="TileSize">
/// The size of the tiles in pixels. Default is <see langword="null" />
/// which uses the Leaflet default of 256.
/// </param>
public record TileLayer(
    string UrlTemplate,
    string Attribution,
    ReferrerPolicyOptions? ReferrerPolicyOptions = null,
    bool? DetectRetina = null,
    int? TileSize = null)
{
    /// <summary>
    /// OpenStreetMap tile layer, free to use with attribution.
    /// </summary>
    public static readonly TileLayer OpenStreetMap = new(
        UrlTemplate: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
        Attribution: "&copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors",
        ReferrerPolicyOptions: new ReferrerPolicyOptions(true, ReferrerPolicy.Origin));

    /// <summary>
    /// OpenStreetMap France tile layer, free to use with attribution.
    /// </summary>
    public static readonly TileLayer OpenStreetMapFrance = new(
        UrlTemplate: "https://{s}.tile.openstreetmap.fr/osmfr/{z}/{x}/{y}.png",
        Attribution: "&copy; OpenStreetMap France | &copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors",
        ReferrerPolicyOptions: new ReferrerPolicyOptions(true, ReferrerPolicy.Origin));

    /// <summary>
    /// Public transport map tile layer, free to use with attribution.
    /// </summary>
    public static readonly TileLayer OpnvKarte = new(
        UrlTemplate: "https://tileserver.memomaps.de/tilegen/{z}/{x}/{y}.png",
        Attribution: "&copy; <a href='memomaps.de'>memomaps.de</a> <a href='https://creativecommons.org/licenses/by-sa/2.0/CC-BY-SA'>CC-BY-SA</a> | &copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
        );

    public static readonly TileLayer OpenDataBaseMap = new(
        UrlTemplate: "https://wmts1.geoportail.lu/opendata/wmts/basemap/GLOBAL_WEBMERCATOR/{z}/{x}/{y}.png",
        Attribution: "&copy;  <a href='https://data.public.lu/en/datasets/carte-de-base-webservices-wms-et-wmts'>OpenData</a> <a href='https://creativecommons.org/publicdomain/zero/1.0/'>CC0</a>/<a href='https://creativecommons.org/licenses/by/4.0/deed.en'>CC-BY</a> | &copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors");
}

/// <summary>
/// The referrer policy to use for the tile layer.
/// </summary>
/// <param name="ActivateReferrerPolicy">Whether to activate the referrer policy. When this is <c>true</c>, the <see cref="ReferrerPolicy"/> value is used.</param>
/// <param name="ReferrerPolicy">The referrer policy to use. Defaults to <see cref="ReferrerPolicy.Origin"/>.</param>
public record ReferrerPolicyOptions(bool ActivateReferrerPolicy, ReferrerPolicy ReferrerPolicy);

/// <summary>
/// See <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/img#referrerpolicy">the mozilla docs</see> for a description of each policy.
/// </summary>
public enum ReferrerPolicy
{
    NoReferrer,
    NoReferrerWhenDowngrade,
    Origin,
    OriginWhenCrossOrigin,
    SameOrigin,
    StrictOrigin,
    StrictOriginWhenCrossOrigin,
    UnsafeUrl
}
