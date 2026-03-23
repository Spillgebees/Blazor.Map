namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Options for the map.
/// </summary>
/// <param name="Center">The coordinate to initially center the map to. If <see cref="FitBoundsOptions"/> is provided, this value has no effect.</param>
/// <param name="Zoom">The zoom level to initially apply to the map. If <see cref="FitBoundsOptions"/> is provided, this value may be ignored.</param>
/// <param name="Style">
/// The map style to use. When <see langword="null"/>, defaults to <see cref="MapStyle.OpenFreeMap.Liberty"/>.
/// This is a convenience for single-style maps. For multi-style composition, use <see cref="Styles"/> instead.
/// When both <see cref="Style"/> and <see cref="Styles"/> are provided, <see cref="Styles"/> takes precedence.
/// </param>
/// <param name="Styles">
/// A list of styles to compose. The first style is the base map, and subsequent styles are overlaid on top.
/// Each overlay style's sources and layers are merged into the map after the base style loads.
/// See <see cref="ComposedGlyphsUrl"/> for controlling the glyph endpoint used by composed maps.
/// </param>
/// <param name="ComposedGlyphsUrl">
/// Overrides the effective <c>glyphs</c> URL for composed maps when using <see cref="Styles"/>.
/// When set, MapLibre uses this URL as the single glyph endpoint for the composed style.
/// When <see langword="null"/>, the component validates that all composed styles share the same glyph endpoint.
/// The endpoint must serve all font stacks required by both base and overlay styles.
/// This option has no effect on single-style maps.
/// </param>
/// <param name="Pitch">Tilt angle in degrees (0–85). Default is 0.</param>
/// <param name="Bearing">Rotation angle in degrees (0–360). Default is 0.</param>
/// <param name="Projection">The map projection to use. Default is <see cref="MapProjection.Mercator"/>.</param>
/// <param name="Terrain">Whether to enable 3D terrain. Default is <see langword="false"/>.</param>
/// <param name="TerrainExaggeration">Exaggeration factor for 3D terrain. Default is 1.0.</param>
/// <param name="FitBoundsOptions">Options for centering/fitting the bounds of the map on specific features. Overrides <see cref="Center"/> and <see cref="Zoom"/>.</param>
/// <param name="MinZoom">The minimum zoom level. Default is <see langword="null"/> (no minimum).</param>
/// <param name="MaxZoom">The maximum zoom level. Default is <see langword="null"/> (no maximum).</param>
/// <param name="MaxBounds">
/// Restricts the map viewport to the given geographic bounds.
/// The map cannot be panned or zoomed outside these bounds.
/// Specified as (Southwest, Northeast) corners.
/// Default is <see langword="null"/> (no restriction).
/// </param>
/// <param name="Interactive">Whether the map supports user interaction. Default is <see langword="true"/>.</param>
/// <param name="CooperativeGestures">Whether to require Ctrl+scroll to zoom. Default is <see langword="false"/>.</param>
/// <param name="WebFonts">
/// A list of web font specifications to preload before the map renders.
/// Each entry is a CSS font specification (e.g., "24px 'DM Sans'").
/// The fonts must also be loaded via CSS <c>@@import</c> or <c>@@font-face</c> in your application.
/// Once loaded, they can be referenced by name in SymbolLayer's TextFont property.
/// </param>
public record MapOptions(
    Coordinate Center,
    int Zoom = 0,
    MapStyle? Style = null,
    IReadOnlyList<MapStyle>? Styles = null,
    string? ComposedGlyphsUrl = null,
    double Pitch = 0,
    double Bearing = 0,
    MapProjection Projection = MapProjection.Mercator,
    bool Terrain = false,
    double TerrainExaggeration = 1.0,
    FitBoundsOptions? FitBoundsOptions = null,
    int? MinZoom = null,
    int? MaxZoom = null,
    MapBounds? MaxBounds = null,
    bool Interactive = true,
    bool CooperativeGestures = false,
    IReadOnlyList<string>? WebFonts = null
)
{
    /// <summary>
    /// Default map options centered at (0, 0) with zoom level 0.
    /// </summary>
    public static MapOptions Default => new(new Coordinate(0, 0));
}
