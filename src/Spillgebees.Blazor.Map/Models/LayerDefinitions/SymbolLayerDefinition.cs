namespace Spillgebees.Blazor.Map;

/// <summary>
/// Parameter-based definition for a MapLibre symbol layer.
/// </summary>
public sealed record SymbolLayerDefinition : MapLayerDefinition
{
    public SymbolLayerDefinition(
        string idSuffix,
        StyleValue<string>? textField = null,
        StyleValue<double>? textSize = null,
        IReadOnlyList<string>? textFont = null,
        SymbolAnchor? textAnchor = null,
        IReadOnlyList<double>? textOffset = null,
        StyleValue<double>? textRotate = null,
        MapAlignment? textPitchAlignment = null,
        MapAlignment? textRotationAlignment = null,
        TextTransform? textTransform = null,
        double? textMaxWidth = null,
        bool textAllowOverlap = false,
        StyleValue<string>? textColor = null,
        StyleValue<string>? textHaloColor = null,
        StyleValue<double>? textHaloWidth = null,
        StyleValue<double>? textOpacity = null,
        StyleValue<string>? iconImage = null,
        StyleValue<double>? iconSize = null,
        StyleValue<double>? iconRotate = null,
        IReadOnlyList<double>? iconOffset = null,
        StyleValue<SymbolAnchor>? iconAnchor = null,
        bool iconAllowOverlap = false,
        IconTextFit? iconTextFit = null,
        IReadOnlyList<double>? iconTextFitPadding = null,
        MapAlignment? rotationAlignment = null,
        StyleValue<double>? iconOpacity = null,
        StyleValue<string>? iconColor = null,
        SymbolPlacement? placement = null,
        double? spacing = null,
        StyleValue<double>? symbolSortKey = null,
        string? key = null,
        object? filter = null,
        double? minZoom = null,
        double? maxZoom = null,
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null
    )
        : base(idSuffix, key, filter, minZoom, maxZoom, beforeLayerId, layerGroup, beforeLayerGroup, afterLayerGroup)
    {
        TextField = textField;
        TextSize = textSize;
        TextFont = textFont?.ToArray();
        TextAnchor = textAnchor;
        TextOffset = textOffset?.ToArray();
        TextRotate = textRotate;
        TextPitchAlignment = textPitchAlignment;
        TextRotationAlignment = textRotationAlignment;
        TextTransform = textTransform;
        TextMaxWidth = textMaxWidth;
        TextAllowOverlap = textAllowOverlap;
        TextColor = textColor;
        TextHaloColor = textHaloColor;
        TextHaloWidth = textHaloWidth;
        TextOpacity = textOpacity;
        IconImage = iconImage;
        IconSize = iconSize;
        IconRotate = iconRotate;
        IconOffset = iconOffset?.ToArray();
        IconAnchor = iconAnchor;
        IconAllowOverlap = iconAllowOverlap;
        IconTextFit = iconTextFit;
        IconTextFitPadding = iconTextFitPadding?.ToArray();
        RotationAlignment = rotationAlignment;
        IconOpacity = iconOpacity;
        IconColor = iconColor;
        Placement = placement;
        Spacing = spacing;
        SymbolSortKey = symbolSortKey;
    }

    /// <inheritdoc />
    public override string Type => "symbol";

    /// <summary>The text field content (literal or expression).</summary>
    public StyleValue<string>? TextField { get; init; }

    /// <summary>The text font size in pixels (literal or expression).</summary>
    public StyleValue<double>? TextSize { get; init; }

    /// <summary>The font stack for text rendering.</summary>
    public IReadOnlyList<string>? TextFont { get; init; }

    /// <summary>The text anchor position.</summary>
    public SymbolAnchor? TextAnchor { get; init; }

    /// <summary>The text offset from the anchor position in ems [x, y].</summary>
    public IReadOnlyList<double>? TextOffset { get; init; }

    /// <summary>The text rotation in degrees (literal or expression).</summary>
    public StyleValue<double>? TextRotate { get; init; }

    /// <summary>The alignment of text when the map is pitched.</summary>
    public MapAlignment? TextPitchAlignment { get; init; }

    /// <summary>The alignment of text when the map is rotated.</summary>
    public MapAlignment? TextRotationAlignment { get; init; }

    /// <summary>The text transform.</summary>
    public TextTransform? TextTransform { get; init; }

    /// <summary>Maximum text width in ems before wrapping.</summary>
    public double? TextMaxWidth { get; init; }

    /// <summary>Whether text can overlap other symbols.</summary>
    public bool TextAllowOverlap { get; init; }

    /// <summary>The text color (CSS color string or expression).</summary>
    public StyleValue<string>? TextColor { get; init; }

    /// <summary>The text halo color (CSS color string or expression).</summary>
    public StyleValue<string>? TextHaloColor { get; init; }

    /// <summary>The text halo width in pixels (literal or expression).</summary>
    public StyleValue<double>? TextHaloWidth { get; init; }

    /// <summary>The text opacity (0.0-1.0, literal or expression).</summary>
    public StyleValue<double>? TextOpacity { get; init; }

    /// <summary>The icon image name from the map's sprite (literal or expression).</summary>
    public StyleValue<string>? IconImage { get; init; }

    /// <summary>The icon size scaling factor (literal or expression).</summary>
    public StyleValue<double>? IconSize { get; init; }

    /// <summary>The icon rotation in degrees (literal or expression).</summary>
    public StyleValue<double>? IconRotate { get; init; }

    /// <summary>The icon offset from the anchor position in pixels [x, y].</summary>
    public IReadOnlyList<double>? IconOffset { get; init; }

    /// <summary>The icon anchor position.</summary>
    public StyleValue<SymbolAnchor>? IconAnchor { get; init; }

    /// <summary>Whether icons can overlap other symbols.</summary>
    public bool IconAllowOverlap { get; init; }

    /// <summary>Scales the icon to fit the text.</summary>
    public IconTextFit? IconTextFit { get; init; }

    /// <summary>Padding around text when icon-text-fit is active [top, right, bottom, left].</summary>
    public IReadOnlyList<double>? IconTextFitPadding { get; init; }

    /// <summary>The alignment of the icon when the map is rotated.</summary>
    public MapAlignment? RotationAlignment { get; init; }

    /// <summary>The icon opacity (0.0-1.0, literal or expression).</summary>
    public StyleValue<double>? IconOpacity { get; init; }

    /// <summary>The icon color tint, only works with SDF icons.</summary>
    public StyleValue<string>? IconColor { get; init; }

    /// <summary>The symbol placement strategy.</summary>
    public SymbolPlacement? Placement { get; init; }

    /// <summary>The distance between symbol instances along a line in pixels.</summary>
    public double? Spacing { get; init; }

    /// <summary>The explicit symbol sort key for render ordering (literal or expression).</summary>
    public StyleValue<double>? SymbolSortKey { get; init; }
}
