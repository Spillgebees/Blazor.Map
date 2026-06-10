namespace Spillgebees.Blazor.Map;

/// <summary>
/// Describes the visual symbol rendered for a legend item.
/// </summary>
public abstract record MapLegendSymbol
{
    private MapLegendSymbol() { }

    /// <summary>
    /// Renders no symbol before the legend item copy.
    /// </summary>
    public sealed record NoneSymbol : MapLegendSymbol;

    /// <summary>
    /// Renders a square color swatch.
    /// </summary>
    /// <param name="Color">CSS color of the swatch.</param>
    public sealed record ColorSwatchSymbol(string Color) : MapLegendSymbol;

    /// <summary>
    /// Renders a horizontal line swatch.
    /// </summary>
    /// <param name="Color">CSS color of the line.</param>
    /// <param name="Width">Line thickness in CSS pixels. Default is 3.</param>
    /// <param name="Dashed">Whether the line is rendered dashed. Default is false.</param>
    public sealed record LineSymbol(string Color, double Width = 3, bool Dashed = false) : MapLegendSymbol;

    /// <summary>
    /// Renders a filled circle swatch.
    /// </summary>
    /// <param name="Color">CSS fill color of the circle.</param>
    /// <param name="StrokeColor">Optional CSS color of the circle outline.</param>
    public sealed record CircleSymbol(string Color, string? StrokeColor = null) : MapLegendSymbol;

    /// <summary>
    /// Renders an icon element styled by a CSS class.
    /// </summary>
    /// <param name="CssClass">CSS class applied to the icon element.</param>
    public sealed record IconSymbol(string CssClass) : MapLegendSymbol;

    /// <summary>
    /// A symbol that renders nothing.
    /// </summary>
    public static MapLegendSymbol None { get; } = new NoneSymbol();

    /// <summary>
    /// Creates a square color swatch symbol.
    /// </summary>
    public static MapLegendSymbol ColorSwatch(string color) => new ColorSwatchSymbol(color);

    /// <summary>
    /// Creates a solid line symbol.
    /// </summary>
    public static MapLegendSymbol Line(string color, double width = 3, bool dashed = false) =>
        new LineSymbol(color, width, dashed);

    /// <summary>
    /// Creates a dashed line symbol.
    /// </summary>
    public static MapLegendSymbol DashedLine(string color, double width = 3) => new LineSymbol(color, width, true);

    /// <summary>
    /// Creates a filled circle symbol with an optional outline color.
    /// </summary>
    public static MapLegendSymbol Circle(string color, string? strokeColor = null) =>
        new CircleSymbol(color, strokeColor);

    /// <summary>
    /// Creates an icon symbol styled by a CSS class.
    /// </summary>
    public static MapLegendSymbol Icon(string cssClass) => new IconSymbol(cssClass);
}
