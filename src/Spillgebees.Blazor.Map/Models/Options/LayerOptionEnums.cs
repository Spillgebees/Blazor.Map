using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// The part of a symbol that is placed closest to its anchor coordinate.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SymbolAnchor>))]
public enum SymbolAnchor
{
    /// <summary>
    /// Anchors the symbol's center to its coordinate.
    /// </summary>
    [EnumMember(Value = "center")]
    [JsonStringEnumMemberName("center")]
    Center,

    /// <summary>
    /// Anchors the symbol's left side to its coordinate.
    /// </summary>
    [EnumMember(Value = "left")]
    [JsonStringEnumMemberName("left")]
    Left,

    /// <summary>
    /// Anchors the symbol's right side to its coordinate.
    /// </summary>
    [EnumMember(Value = "right")]
    [JsonStringEnumMemberName("right")]
    Right,

    /// <summary>
    /// Anchors the symbol's top to its coordinate.
    /// </summary>
    [EnumMember(Value = "top")]
    [JsonStringEnumMemberName("top")]
    Top,

    /// <summary>
    /// Anchors the symbol's bottom to its coordinate.
    /// </summary>
    [EnumMember(Value = "bottom")]
    [JsonStringEnumMemberName("bottom")]
    Bottom,

    /// <summary>
    /// Anchors the symbol's top-left corner to its coordinate.
    /// </summary>
    [EnumMember(Value = "top-left")]
    [JsonStringEnumMemberName("top-left")]
    TopLeft,

    /// <summary>
    /// Anchors the symbol's top-right corner to its coordinate.
    /// </summary>
    [EnumMember(Value = "top-right")]
    [JsonStringEnumMemberName("top-right")]
    TopRight,

    /// <summary>
    /// Anchors the symbol's bottom-left corner to its coordinate.
    /// </summary>
    [EnumMember(Value = "bottom-left")]
    [JsonStringEnumMemberName("bottom-left")]
    BottomLeft,

    /// <summary>
    /// Anchors the symbol's bottom-right corner to its coordinate.
    /// </summary>
    [EnumMember(Value = "bottom-right")]
    [JsonStringEnumMemberName("bottom-right")]
    BottomRight,
}

/// <summary>
/// Controls whether an element is aligned to the map plane or the viewport.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MapAlignment>))]
public enum MapAlignment
{
    /// <summary>
    /// Aligns to the map plane, rotating and pitching with the map.
    /// </summary>
    [EnumMember(Value = "map")]
    [JsonStringEnumMemberName("map")]
    Map,

    /// <summary>
    /// Aligns to the viewport, staying fixed relative to the screen.
    /// </summary>
    [EnumMember(Value = "viewport")]
    [JsonStringEnumMemberName("viewport")]
    Viewport,

    /// <summary>
    /// MapLibre chooses the alignment automatically based on the symbol placement.
    /// </summary>
    [EnumMember(Value = "auto")]
    [JsonStringEnumMemberName("auto")]
    Auto,
}

/// <summary>
/// Controls the orientation of circles when the map is pitched.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CirclePitchAlignment>))]
public enum CirclePitchAlignment
{
    /// <summary>
    /// Circles are aligned to the map plane and lean with the pitch.
    /// </summary>
    [EnumMember(Value = "map")]
    [JsonStringEnumMemberName("map")]
    Map,

    /// <summary>
    /// Circles are aligned to the viewport and always face the screen.
    /// </summary>
    [EnumMember(Value = "viewport")]
    [JsonStringEnumMemberName("viewport")]
    Viewport,
}

/// <summary>
/// Case transformation applied to label text.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TextTransform>))]
public enum TextTransform
{
    /// <summary>
    /// Renders the text as-is.
    /// </summary>
    [EnumMember(Value = "none")]
    [JsonStringEnumMemberName("none")]
    None,

    /// <summary>
    /// Forces all letters to uppercase.
    /// </summary>
    [EnumMember(Value = "uppercase")]
    [JsonStringEnumMemberName("uppercase")]
    Uppercase,

    /// <summary>
    /// Forces all letters to lowercase.
    /// </summary>
    [EnumMember(Value = "lowercase")]
    [JsonStringEnumMemberName("lowercase")]
    Lowercase,
}

/// <summary>
/// Controls whether an icon is scaled to fit the dimensions of its accompanying text.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<IconTextFit>))]
public enum IconTextFit
{
    /// <summary>
    /// The icon is displayed at its intrinsic aspect ratio.
    /// </summary>
    [EnumMember(Value = "none")]
    [JsonStringEnumMemberName("none")]
    None,

    /// <summary>
    /// The icon is scaled in the x-dimension to fit the width of the text.
    /// </summary>
    [EnumMember(Value = "width")]
    [JsonStringEnumMemberName("width")]
    Width,

    /// <summary>
    /// The icon is scaled in the y-dimension to fit the height of the text.
    /// </summary>
    [EnumMember(Value = "height")]
    [JsonStringEnumMemberName("height")]
    Height,

    /// <summary>
    /// The icon is scaled in both dimensions to fit the text.
    /// </summary>
    [EnumMember(Value = "both")]
    [JsonStringEnumMemberName("both")]
    Both,
}

/// <summary>
/// Controls where symbols are placed relative to their geometry.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SymbolPlacement>))]
public enum SymbolPlacement
{
    /// <summary>
    /// The symbol is placed at the point where the geometry is located.
    /// </summary>
    [EnumMember(Value = "point")]
    [JsonStringEnumMemberName("point")]
    Point,

    /// <summary>
    /// The symbol is placed repeatedly along the line of the geometry.
    /// </summary>
    [EnumMember(Value = "line")]
    [JsonStringEnumMemberName("line")]
    Line,

    /// <summary>
    /// The symbol is placed once at the center of the line of the geometry.
    /// </summary>
    [EnumMember(Value = "line-center")]
    [JsonStringEnumMemberName("line-center")]
    LineCenter,
}

/// <summary>
/// The display of line endings.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<LineCap>))]
public enum LineCap
{
    /// <summary>
    /// The line ends squared off exactly at the endpoint.
    /// </summary>
    [EnumMember(Value = "butt")]
    [JsonStringEnumMemberName("butt")]
    Butt,

    /// <summary>
    /// The line ends with a semicircle extending half the line width beyond the endpoint.
    /// </summary>
    [EnumMember(Value = "round")]
    [JsonStringEnumMemberName("round")]
    Round,

    /// <summary>
    /// The line ends squared off, extending half the line width beyond the endpoint.
    /// </summary>
    [EnumMember(Value = "square")]
    [JsonStringEnumMemberName("square")]
    Square,
}

/// <summary>
/// The display of joints between line segments.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<LineJoin>))]
public enum LineJoin
{
    /// <summary>
    /// Joins are flattened, extending half the line width beyond the joint.
    /// </summary>
    [EnumMember(Value = "bevel")]
    [JsonStringEnumMemberName("bevel")]
    Bevel,

    /// <summary>
    /// Joins are rounded with a semicircle centered on the joint.
    /// </summary>
    [EnumMember(Value = "round")]
    [JsonStringEnumMemberName("round")]
    Round,

    /// <summary>
    /// Joins are sharp corners whose outside edges extend to meet at a point.
    /// </summary>
    [EnumMember(Value = "miter")]
    [JsonStringEnumMemberName("miter")]
    Miter,
}

/// <summary>
/// Extension methods that resolve the MapLibre wire value of layer option enums.
/// </summary>
public static class LayerOptionEnumExtensions
{
    /// <summary>
    /// Returns the MapLibre wire value of a <see cref="SymbolAnchor"/> value.
    /// </summary>
    public static string ToJsonName(this SymbolAnchor value) => EnumJsonName.Get(value);

    /// <summary>
    /// Returns the MapLibre wire value of a <see cref="MapAlignment"/> value.
    /// </summary>
    public static string ToJsonName(this MapAlignment value) => EnumJsonName.Get(value);

    /// <summary>
    /// Returns the MapLibre wire value of a <see cref="CirclePitchAlignment"/> value.
    /// </summary>
    public static string ToJsonName(this CirclePitchAlignment value) => EnumJsonName.Get(value);

    /// <summary>
    /// Returns the MapLibre wire value of a <see cref="TextTransform"/> value.
    /// </summary>
    public static string ToJsonName(this TextTransform value) => EnumJsonName.Get(value);

    /// <summary>
    /// Returns the MapLibre wire value of an <see cref="IconTextFit"/> value.
    /// </summary>
    public static string ToJsonName(this IconTextFit value) => EnumJsonName.Get(value);

    /// <summary>
    /// Returns the MapLibre wire value of a <see cref="SymbolPlacement"/> value.
    /// </summary>
    public static string ToJsonName(this SymbolPlacement value) => EnumJsonName.Get(value);

    /// <summary>
    /// Returns the MapLibre wire value of a <see cref="LineCap"/> value.
    /// </summary>
    public static string ToJsonName(this LineCap value) => EnumJsonName.Get(value);

    /// <summary>
    /// Returns the MapLibre wire value of a <see cref="LineJoin"/> value.
    /// </summary>
    public static string ToJsonName(this LineJoin value) => EnumJsonName.Get(value);
}
