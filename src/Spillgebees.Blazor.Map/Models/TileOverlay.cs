namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// A raster tile layer rendered on top of the base map style.
/// Supports XYZ tile URLs, WMS endpoints, and WMTS services.
/// </summary>
/// <param name="Id">A unique identifier for the overlay.</param>
/// <param name="UrlTemplate">
/// The tile URL template. For XYZ tiles, use <c>{z}</c>, <c>{x}</c>, <c>{y}</c> placeholders.
/// For WMS/WMTS, use the factory methods to construct the URL automatically.
/// </param>
/// <param name="Attribution">The attribution text to display on the map. Default is empty.</param>
/// <param name="TileSize">The tile size in pixels. Default is 256.</param>
/// <param name="Opacity">The opacity of the overlay (0.0–1.0). Default is 1.0.</param>
public record TileOverlay(
    string Id,
    string UrlTemplate,
    string Attribution = "",
    int TileSize = 256,
    double Opacity = 1.0
)
{
    /// <summary>
    /// Creates a tile overlay from a WMS endpoint.
    /// The WMS GetMap URL is constructed automatically with the correct parameters.
    /// </summary>
    /// <param name="id">A unique identifier for the overlay.</param>
    /// <param name="baseUrl">The WMS service base URL.</param>
    /// <param name="layers">The WMS layers to request.</param>
    /// <param name="attribution">The attribution text. Default is empty.</param>
    /// <param name="format">The image format. Default is <c>"image/png"</c>.</param>
    /// <param name="transparent">Whether to request transparent tiles. Default is <see langword="true"/>.</param>
    /// <param name="version">The WMS service version. Default is <c>"1.1.1"</c>.</param>
    /// <param name="tileSize">The tile size in pixels. Default is 256.</param>
    /// <param name="opacity">The overlay opacity. Default is 1.0.</param>
    public static TileOverlay FromWms(
        string id,
        string baseUrl,
        string layers,
        string attribution = "",
        string format = "image/png",
        bool transparent = true,
        string version = "1.1.1",
        int tileSize = 256,
        double opacity = 1.0
    )
    {
        // WMS 1.3.0 uses CRS; earlier versions use SRS
        var crsParam = version == "1.3.0" ? "CRS" : "SRS";
        var url =
            $"{baseUrl}?SERVICE=WMS&VERSION={version}&REQUEST=GetMap"
            + $"&LAYERS={layers}&FORMAT={format}&TRANSPARENT={transparent.ToString().ToLowerInvariant()}"
            + $"&{crsParam}=EPSG:3857&STYLES=&WIDTH={tileSize}&HEIGHT={tileSize}"
            + "&BBOX={bbox-epsg-3857}";

        return new TileOverlay(id, url, attribution, tileSize, opacity);
    }

    /// <summary>
    /// Creates a tile overlay from a WMTS endpoint using the RESTful URL pattern.
    /// This is the standard protocol for ArcGIS cached tile services.
    /// </summary>
    /// <param name="id">A unique identifier for the overlay.</param>
    /// <param name="baseUrl">
    /// The WMTS base URL. For ArcGIS, this is typically:
    /// <c>https://server/arcgis/rest/services/ServiceName/MapServer/WMTS</c>
    /// </param>
    /// <param name="layer">The WMTS layer identifier.</param>
    /// <param name="attribution">The attribution text. Default is empty.</param>
    /// <param name="tileMatrixSet">The tile matrix set identifier. Default is <c>"default028mm"</c> (ArcGIS standard).</param>
    /// <param name="style">The WMTS style identifier. Default is <c>"default"</c>.</param>
    /// <param name="format">The image format extension. Default is <c>"png"</c>.</param>
    /// <param name="tileSize">The tile size in pixels. Default is 256.</param>
    /// <param name="opacity">The overlay opacity. Default is 1.0.</param>
    public static TileOverlay FromWmts(
        string id,
        string baseUrl,
        string layer,
        string attribution = "",
        string tileMatrixSet = "default028mm",
        string style = "default",
        string format = "png",
        int tileSize = 256,
        double opacity = 1.0
    )
    {
        // WMTS RESTful tile URL pattern:
        // {baseUrl}/tile/1.0.0/{layer}/{style}/{tileMatrixSet}/{z}/{y}/{x}.{format}
        var url = $"{baseUrl.TrimEnd('/')}/tile/1.0.0/{layer}/{style}/{tileMatrixSet}/{{z}}/{{y}}/{{x}}.{format}";

        return new TileOverlay(id, url, attribution, tileSize, opacity);
    }

    /// <summary>
    /// Creates a tile overlay from an ArcGIS MapServer tile endpoint.
    /// This is the Esri-proprietary tile URL pattern used by ArcGIS Server and ArcGIS Online.
    /// </summary>
    /// <param name="id">A unique identifier for the overlay.</param>
    /// <param name="mapServerUrl">
    /// The ArcGIS MapServer URL, e.g.:
    /// <c>https://server/arcgis/rest/services/ServiceName/MapServer</c>
    /// </param>
    /// <param name="attribution">The attribution text. Default is empty.</param>
    /// <param name="tileSize">The tile size in pixels. Default is 256.</param>
    /// <param name="opacity">The overlay opacity. Default is 1.0.</param>
    public static TileOverlay FromArcGisMapServer(
        string id,
        string mapServerUrl,
        string attribution = "",
        int tileSize = 256,
        double opacity = 1.0
    )
    {
        // ArcGIS MapServer tile URL pattern:
        // {mapServerUrl}/tile/{z}/{y}/{x}
        var url = $"{mapServerUrl.TrimEnd('/')}/tile/{{z}}/{{y}}/{{x}}";

        return new TileOverlay(id, url, attribution, tileSize, opacity);
    }
}
