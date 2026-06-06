namespace Spillgebees.Blazor.Map;

/// <summary>
/// Parameter-based definition for a MapLibre circle layer.
/// </summary>
public sealed record CircleLayerDefinition : MapLayerDefinition
{
    public CircleLayerDefinition(
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
    )
        : base(
            idSuffix,
            key,
            filter,
            minZoom,
            maxZoom,
            visible,
            beforeLayerId,
            layerGroup,
            beforeLayerGroup,
            afterLayerGroup
        )
    {
        Color = color;
        Radius = radius;
        Opacity = opacity;
        StrokeWidth = strokeWidth;
        StrokeColor = strokeColor;
        StrokeOpacity = strokeOpacity;
        PitchAlignment = pitchAlignment;
    }

    /// <inheritdoc />
    public override string Type => "circle";

    /// <summary>The circle fill color (CSS color string or expression).</summary>
    public StyleValue<string>? Color { get; init; }

    /// <summary>The circle radius in pixels (literal or expression).</summary>
    public StyleValue<double>? Radius { get; init; }

    /// <summary>The circle fill opacity (0.0-1.0, literal or expression).</summary>
    public StyleValue<double>? Opacity { get; init; }

    /// <summary>The stroke width in pixels (literal or expression).</summary>
    public StyleValue<double>? StrokeWidth { get; init; }

    /// <summary>The stroke color (CSS color string or expression).</summary>
    public StyleValue<string>? StrokeColor { get; init; }

    /// <summary>The stroke opacity (0.0-1.0, literal or expression).</summary>
    public StyleValue<double>? StrokeOpacity { get; init; }

    /// <summary>The alignment of the circle when the map is pitched.</summary>
    public CirclePitchAlignment? PitchAlignment { get; init; }
}
