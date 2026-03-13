namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// Options that apply specifically to WMS tile layer requests.
/// </summary>
/// <param name="Layers">The WMS layers to request.</param>
/// <param name="Format">
/// The image format to request. Default is <see langword="null" />, which uses the Leaflet default.
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
public record WmsLayerOptions(
    string Layers,
    string? Format = null,
    bool? Transparent = null,
    string? Version = null,
    string? Styles = null
);
