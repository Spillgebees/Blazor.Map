using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>Engine-backed symbol layer.</summary>
public sealed class SymbolLayer : LayerBase
{
    /// <summary>Text to display (MapLibre <c>text-field</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? TextField { get; set; }

    /// <summary>Text size in pixels (MapLibre <c>text-size</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? TextSize { get; set; }

    /// <summary>Font stack for the text (MapLibre <c>text-font</c>, style glyph names).</summary>
    [Parameter]
    public string[]? TextFont { get; set; }

    /// <summary>Part of the text placed closest to the anchor point (MapLibre <c>text-anchor</c>).</summary>
    [Parameter]
    public SymbolAnchor? TextAnchor { get; set; }

    /// <summary>Text offset as <c>[x, y]</c> in ems (MapLibre <c>text-offset</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double[]>? TextOffset { get; set; }

    /// <summary>Text rotation in degrees clockwise (MapLibre <c>text-rotate</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? TextRotate { get; set; }

    /// <summary>Orientation of text when the map is pitched (MapLibre <c>text-pitch-alignment</c>).</summary>
    [Parameter]
    public MapAlignment? TextPitchAlignment { get; set; }

    /// <summary>Rotation behavior of text when the map is rotated (MapLibre <c>text-rotation-alignment</c>).</summary>
    [Parameter]
    public MapAlignment? TextRotationAlignment { get; set; }

    /// <summary>Capitalization transform applied to the text (MapLibre <c>text-transform</c>).</summary>
    [Parameter]
    public TextTransform? TextTransform { get; set; }

    /// <summary>Maximum text line width in ems before wrapping (MapLibre <c>text-max-width</c>).</summary>
    [Parameter]
    public double? TextMaxWidth { get; set; }

    /// <summary>Shows the text even when it collides with other symbols (MapLibre <c>text-allow-overlap</c>).</summary>
    [Parameter]
    public bool TextAllowOverlap { get; set; }

    /// <summary>Text color (MapLibre <c>text-color</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? TextColor { get; set; }

    /// <summary>Text halo color (MapLibre <c>text-halo-color</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? TextHaloColor { get; set; }

    /// <summary>Text halo width in pixels (MapLibre <c>text-halo-width</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? TextHaloWidth { get; set; }

    /// <summary>Text opacity (MapLibre <c>text-opacity</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? TextOpacity { get; set; }

    /// <summary>Icon image id from the map's image registry (MapLibre <c>icon-image</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? IconImage { get; set; }

    /// <summary>Icon scale factor relative to the image's native size (MapLibre <c>icon-size</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? IconSize { get; set; }

    /// <summary>Icon rotation in degrees clockwise (MapLibre <c>icon-rotate</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? IconRotate { get; set; }

    /// <summary>Icon offset as <c>[x, y]</c> in pixels (MapLibre <c>icon-offset</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double[]>? IconOffset { get; set; }

    /// <summary>Part of the icon placed closest to the anchor point (MapLibre <c>icon-anchor</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<SymbolAnchor>? IconAnchor { get; set; }

    /// <summary>Shows the icon even when it collides with other symbols (MapLibre <c>icon-allow-overlap</c>).</summary>
    [Parameter]
    public bool IconAllowOverlap { get; set; }

    /// <summary>Scales the icon to fit the text (MapLibre <c>icon-text-fit</c>).</summary>
    [Parameter]
    public IconTextFit? IconTextFit { get; set; }

    /// <summary>Padding as <c>[top, right, bottom, left]</c> pixels added when fitting the icon to text (MapLibre <c>icon-text-fit-padding</c>).</summary>
    [Parameter]
    public double[]? IconTextFitPadding { get; set; }

    /// <summary>Rotation behavior of icons when the map is rotated (MapLibre <c>icon-rotation-alignment</c>).</summary>
    [Parameter]
    public MapAlignment? RotationAlignment { get; set; }

    /// <summary>Icon opacity (MapLibre <c>icon-opacity</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? IconOpacity { get; set; }

    /// <summary>Icon tint color, applied to SDF icons (MapLibre <c>icon-color</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<string>? IconColor { get; set; }

    /// <summary>Symbol placement relative to the geometry (MapLibre <c>symbol-placement</c>).</summary>
    [Parameter]
    public SymbolPlacement? Placement { get; set; }

    /// <summary>Distance between symbols placed along a line, in pixels (MapLibre <c>symbol-spacing</c>).</summary>
    [Parameter]
    public double? Spacing { get; set; }

    /// <summary>Render order of symbols; lower values render first (MapLibre <c>symbol-sort-key</c>). Accepts a literal or an expression.</summary>
    [Parameter]
    public StyleValue<double>? SymbolSortKey { get; set; }

    internal override string LayerType => "symbol";

    internal override Dictionary<string, object?> GetPaintProperties() =>
        new()
        {
            ["text-color"] = TextColor?.ToSerializable(),
            ["text-halo-color"] = TextHaloColor?.ToSerializable(),
            ["text-halo-width"] = TextHaloWidth?.ToSerializable(),
            ["text-opacity"] = TextOpacity?.ToSerializable(),
            ["icon-opacity"] = IconOpacity?.ToSerializable(),
            ["icon-color"] = IconColor?.ToSerializable(),
        };

    internal override Dictionary<string, object?> GetLayoutProperties() =>
        new()
        {
            ["text-field"] = TextField?.ToSerializable(),
            ["text-size"] = TextSize?.ToSerializable(),
            ["text-font"] = TextFont,
            ["text-anchor"] = TextAnchor?.ToJsonName(),
            ["text-offset"] = TextOffset?.ToSerializable(),
            ["text-rotate"] = TextRotate?.ToSerializable(),
            ["text-pitch-alignment"] = TextPitchAlignment?.ToJsonName(),
            ["text-rotation-alignment"] = TextRotationAlignment?.ToJsonName(),
            ["text-transform"] = TextTransform?.ToJsonName(),
            ["text-max-width"] = TextMaxWidth,
            ["text-allow-overlap"] = TextAllowOverlap ? (object)true : null,
            ["icon-image"] = IconImage?.ToSerializable(),
            ["icon-size"] = IconSize?.ToSerializable(),
            ["icon-rotate"] = IconRotate?.ToSerializable(),
            ["icon-offset"] = IconOffset?.ToSerializable(),
            ["icon-anchor"] = IconAnchor?.ToSerializable(),
            ["icon-allow-overlap"] = IconAllowOverlap ? (object)true : null,
            ["icon-text-fit"] = IconTextFit?.ToJsonName(),
            ["icon-text-fit-padding"] = IconTextFitPadding,
            ["icon-rotation-alignment"] = RotationAlignment?.ToJsonName(),
            ["symbol-placement"] = Placement?.ToJsonName(),
            ["symbol-spacing"] = Spacing,
            ["symbol-sort-key"] = SymbolSortKey?.ToSerializable(),
        };
}
