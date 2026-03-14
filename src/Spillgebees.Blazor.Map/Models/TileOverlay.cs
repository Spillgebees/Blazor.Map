namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// A raster tile layer rendered on top of the base map style (e.g., OpenRailwayMap).
/// </summary>
/// <param name="Id">A unique identifier for the overlay.</param>
/// <param name="UrlTemplate">The tile URL template with <c>{z}</c>, <c>{x}</c>, <c>{y}</c> placeholders.</param>
/// <param name="Attribution">The attribution text to display on the map. Default is empty.</param>
/// <param name="TileSize">The tile size in pixels. Default is 256.</param>
/// <param name="Opacity">The opacity of the overlay (0.0–1.0). Default is 1.0.</param>
public record TileOverlay(
    string Id,
    string UrlTemplate,
    string Attribution = "",
    int TileSize = 256,
    double Opacity = 1.0
);
