namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// Options that apply to regular tile layer requests.
/// </summary>
/// <param name="DetectRetina">
/// Whether to use high-DPI tiles if the browser supports them.
/// Default is <see langword="null" />, which uses the Leaflet default of <see langword="false" />.
/// </param>
/// <param name="TileSize">
/// The size of the tiles in pixels. Default is <see langword="null" />, which uses the Leaflet default of 256.
/// </param>
public record TileLayerOptions(bool? DetectRetina = null, int? TileSize = null);
