namespace Spillgebees.Blazor.Map.Models.Legends;

/// <summary>
/// Describes the visual symbol rendered for a legend item.
/// </summary>
public abstract record MapLegendSymbol
{
    private MapLegendSymbol() { }

    public sealed record NoneSymbol : MapLegendSymbol;

    public sealed record ColorSwatchSymbol(string Color) : MapLegendSymbol;

    public sealed record LineSymbol(string Color, double Width = 3, bool Dashed = false) : MapLegendSymbol;

    public sealed record CircleSymbol(string Color, string? StrokeColor = null) : MapLegendSymbol;

    public sealed record IconSymbol(string CssClass) : MapLegendSymbol;

    public static MapLegendSymbol None { get; } = new NoneSymbol();

    public static MapLegendSymbol ColorSwatch(string color) => new ColorSwatchSymbol(color);

    public static MapLegendSymbol Line(string color, double width = 3, bool dashed = false) =>
        new LineSymbol(color, width, dashed);

    public static MapLegendSymbol DashedLine(string color, double width = 3) => new LineSymbol(color, width, true);

    public static MapLegendSymbol Circle(string color, string? strokeColor = null) => new CircleSymbol(color, strokeColor);

    public static MapLegendSymbol Icon(string cssClass) => new IconSymbol(cssClass);
}
