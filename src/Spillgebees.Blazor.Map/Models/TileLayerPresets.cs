namespace Spillgebees.Blazor.Map.Models;

public static class TileLayerPresets
{
    /// <summary>
    /// OpenStreetMap tile layer - free to use with attribution, optimized for high-DPI displays
    /// </summary>
    public static readonly TileLayer OpenStreetMap = new(
        UrlTemplate: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
        Attribution: "&copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
    );

    /// <summary>
    /// OpenStreetMap France tile layer - free to use with attribution, optimized for high-DPI displays
    /// </summary>
    public static readonly TileLayer OpenStreetMapFrance = new(
        UrlTemplate: "https://{s}.tile.openstreetmap.fr/osmfr/{z}/{x}/{y}.png",
        Attribution: "&copy; OpenStreetMap France | &copy; <a href='https://www.openstreetmap.org/copyright'>OpenStreetMap</a> contributors"
    );
}
