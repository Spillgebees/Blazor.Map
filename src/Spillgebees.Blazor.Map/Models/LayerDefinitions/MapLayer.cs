namespace Spillgebees.Blazor.Map;

/// <summary>
/// Factory helpers for parameter-based MapLibre layer definitions.
/// </summary>
public static class MapLayer
{
    /// <summary>
    /// Creates a circle layer definition.
    /// </summary>
    public static CircleLayerDefinition Circle(
        string idSuffix,
        StyleValue<string>? color = null,
        StyleValue<double>? radius = null,
        StyleValue<double>? opacity = null,
        StyleValue<double>? strokeWidth = null,
        StyleValue<string>? strokeColor = null,
        StyleValue<double>? strokeOpacity = null,
        CirclePitchAlignment? pitchAlignment = null,
        string? key = null,
        object? filter = null,
        double? minZoom = null,
        double? maxZoom = null,
        bool visible = true,
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null
    ) =>
        new(
            idSuffix,
            color,
            radius,
            opacity,
            strokeWidth,
            strokeColor,
            strokeOpacity,
            pitchAlignment,
            key,
            filter,
            minZoom,
            maxZoom,
            visible,
            beforeLayerId,
            layerGroup,
            beforeLayerGroup,
            afterLayerGroup
        );

    /// <summary>
    /// Creates a symbol layer definition.
    /// </summary>
    public static SymbolLayerDefinition Symbol(
        string idSuffix,
        StyleValue<string>? textField = null,
        StyleValue<double>? textSize = null,
        IEnumerable<string>? textFont = null,
        SymbolAnchor? textAnchor = null,
        IEnumerable<double>? textOffset = null,
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
        IEnumerable<double>? iconOffset = null,
        StyleValue<SymbolAnchor>? iconAnchor = null,
        bool iconAllowOverlap = false,
        IconTextFit? iconTextFit = null,
        IEnumerable<double>? iconTextFitPadding = null,
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
        bool visible = true,
        string? beforeLayerId = null,
        string? layerGroup = null,
        string? beforeLayerGroup = null,
        string? afterLayerGroup = null
    ) =>
        new(
            idSuffix,
            textField,
            textSize,
            textFont?.ToArray(),
            textAnchor,
            textOffset?.ToArray(),
            textRotate,
            textPitchAlignment,
            textRotationAlignment,
            textTransform,
            textMaxWidth,
            textAllowOverlap,
            textColor,
            textHaloColor,
            textHaloWidth,
            textOpacity,
            iconImage,
            iconSize,
            iconRotate,
            iconOffset?.ToArray(),
            iconAnchor,
            iconAllowOverlap,
            iconTextFit,
            iconTextFitPadding?.ToArray(),
            rotationAlignment,
            iconOpacity,
            iconColor,
            placement,
            spacing,
            symbolSortKey,
            key,
            filter,
            minZoom,
            maxZoom,
            visible,
            beforeLayerId,
            layerGroup,
            beforeLayerGroup,
            afterLayerGroup
        );
}
