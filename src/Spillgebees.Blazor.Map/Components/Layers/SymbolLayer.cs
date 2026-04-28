using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models.Expressions;
using Spillgebees.Blazor.Map.Models.Options;

namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// A MapLibre symbol layer that renders text and/or icon labels from a GeoJSON source.
/// </summary>
public class SymbolLayer : LayerBase
{
    // Text layout

    /// <summary>The text field content (literal or expression).</summary>
    [Parameter]
    public StyleValue<string>? TextField { get; set; }

    /// <summary>The text font size in pixels (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? TextSize { get; set; }

    /// <summary>The font stack for text rendering.</summary>
    [Parameter]
    public string[]? TextFont { get; set; }

    /// <summary>The text anchor position. Values are defined by <see cref="SymbolAnchor"/>.</summary>
    [Parameter]
    public SymbolAnchor? TextAnchor { get; set; }

    /// <summary>The text offset from the anchor position in ems [x, y].</summary>
    [Parameter]
    public StyleValue<double[]>? TextOffset { get; set; }

    /// <summary>The text rotation in degrees (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? TextRotate { get; set; }

    /// <summary>The alignment of text when the map is pitched. Values are defined by <see cref="MapAlignment"/>.</summary>
    [Parameter]
    public MapAlignment? TextPitchAlignment { get; set; }

    /// <summary>The alignment of text when the map is rotated. Values are defined by <see cref="MapAlignment"/>.</summary>
    [Parameter]
    public MapAlignment? TextRotationAlignment { get; set; }

    /// <summary>The text transform. Values are defined by <see cref="TextTransform"/>.</summary>
    [Parameter]
    public TextTransform? TextTransform { get; set; }

    /// <summary>Maximum text width in ems before wrapping. Default is 10.</summary>
    [Parameter]
    public double? TextMaxWidth { get; set; }

    /// <summary>Whether text can overlap other symbols.</summary>
    [Parameter]
    public bool TextAllowOverlap { get; set; }

    // Text paint

    /// <summary>The text color (CSS color string or expression).</summary>
    [Parameter]
    public StyleValue<string>? TextColor { get; set; }

    /// <summary>The text halo color (CSS color string or expression).</summary>
    [Parameter]
    public StyleValue<string>? TextHaloColor { get; set; }

    /// <summary>The text halo width in pixels (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? TextHaloWidth { get; set; }

    /// <summary>The text opacity (0.0–1.0, literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? TextOpacity { get; set; }

    // Icon layout

    /// <summary>The icon image name from the map's sprite (literal or expression).</summary>
    [Parameter]
    public StyleValue<string>? IconImage { get; set; }

    /// <summary>The icon size scaling factor (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? IconSize { get; set; }

    /// <summary>The icon rotation in degrees (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? IconRotate { get; set; }

    /// <summary>The icon offset from the anchor position in pixels [x, y].</summary>
    [Parameter]
    public StyleValue<double[]>? IconOffset { get; set; }

    /// <summary>The icon anchor position. Values are defined by <see cref="SymbolAnchor"/>.</summary>
    [Parameter]
    public StyleValue<SymbolAnchor>? IconAnchor { get; set; }

    /// <summary>Whether icons can overlap other symbols.</summary>
    [Parameter]
    public bool IconAllowOverlap { get; set; }

    /// <summary>Scales the icon to fit the text. Values are defined by <see cref="IconTextFit"/>.</summary>
    [Parameter]
    public IconTextFit? IconTextFit { get; set; }

    /// <summary>Padding around the text when icon-text-fit is active [top, right, bottom, left].</summary>
    [Parameter]
    public double[]? IconTextFitPadding { get; set; }

    /// <summary>
    /// The alignment of the icon when the map is rotated. Values are defined by <see cref="MapAlignment"/>.
    /// When set to <see cref="MapAlignment.Map"/>, the icon rotates with the map. Default is <see cref="MapAlignment.Auto"/>.
    /// </summary>
    [Parameter]
    public MapAlignment? RotationAlignment { get; set; }

    // Icon paint

    /// <summary>The icon opacity (0.0–1.0, literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? IconOpacity { get; set; }

    /// <summary>The icon color tint, only works with SDF icons (CSS color string or expression).</summary>
    [Parameter]
    public StyleValue<string>? IconColor { get; set; }

    // Symbol layout

    /// <summary>The symbol placement strategy. Values are defined by <see cref="SymbolPlacement"/>.</summary>
    [Parameter]
    public SymbolPlacement? Placement { get; set; }

    /// <summary>The distance between symbol instances along a line in pixels.</summary>
    [Parameter]
    public double? Spacing { get; set; }

    /// <summary>The explicit symbol sort key for render ordering (literal or expression).</summary>
    [Parameter]
    public StyleValue<double>? SymbolSortKey { get; set; }

    internal override string _layerType => "symbol";

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
