using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models.Options;

[JsonConverter(typeof(JsonStringEnumConverter<SymbolAnchor>))]
public enum SymbolAnchor
{
    [EnumMember(Value = "center")]
    [JsonStringEnumMemberName("center")]
    Center,

    [EnumMember(Value = "left")]
    [JsonStringEnumMemberName("left")]
    Left,

    [EnumMember(Value = "right")]
    [JsonStringEnumMemberName("right")]
    Right,

    [EnumMember(Value = "top")]
    [JsonStringEnumMemberName("top")]
    Top,

    [EnumMember(Value = "bottom")]
    [JsonStringEnumMemberName("bottom")]
    Bottom,

    [EnumMember(Value = "top-left")]
    [JsonStringEnumMemberName("top-left")]
    TopLeft,

    [EnumMember(Value = "top-right")]
    [JsonStringEnumMemberName("top-right")]
    TopRight,

    [EnumMember(Value = "bottom-left")]
    [JsonStringEnumMemberName("bottom-left")]
    BottomLeft,

    [EnumMember(Value = "bottom-right")]
    [JsonStringEnumMemberName("bottom-right")]
    BottomRight,
}

[JsonConverter(typeof(JsonStringEnumConverter<MapAlignment>))]
public enum MapAlignment
{
    [EnumMember(Value = "map")]
    [JsonStringEnumMemberName("map")]
    Map,

    [EnumMember(Value = "viewport")]
    [JsonStringEnumMemberName("viewport")]
    Viewport,

    [EnumMember(Value = "auto")]
    [JsonStringEnumMemberName("auto")]
    Auto,
}

[JsonConverter(typeof(JsonStringEnumConverter<CirclePitchAlignment>))]
public enum CirclePitchAlignment
{
    [EnumMember(Value = "map")]
    [JsonStringEnumMemberName("map")]
    Map,

    [EnumMember(Value = "viewport")]
    [JsonStringEnumMemberName("viewport")]
    Viewport,
}

[JsonConverter(typeof(JsonStringEnumConverter<TextTransform>))]
public enum TextTransform
{
    [EnumMember(Value = "none")]
    [JsonStringEnumMemberName("none")]
    None,

    [EnumMember(Value = "uppercase")]
    [JsonStringEnumMemberName("uppercase")]
    Uppercase,

    [EnumMember(Value = "lowercase")]
    [JsonStringEnumMemberName("lowercase")]
    Lowercase,
}

[JsonConverter(typeof(JsonStringEnumConverter<IconTextFit>))]
public enum IconTextFit
{
    [EnumMember(Value = "none")]
    [JsonStringEnumMemberName("none")]
    None,

    [EnumMember(Value = "width")]
    [JsonStringEnumMemberName("width")]
    Width,

    [EnumMember(Value = "height")]
    [JsonStringEnumMemberName("height")]
    Height,

    [EnumMember(Value = "both")]
    [JsonStringEnumMemberName("both")]
    Both,
}

[JsonConverter(typeof(JsonStringEnumConverter<SymbolPlacement>))]
public enum SymbolPlacement
{
    [EnumMember(Value = "point")]
    [JsonStringEnumMemberName("point")]
    Point,

    [EnumMember(Value = "line")]
    [JsonStringEnumMemberName("line")]
    Line,

    [EnumMember(Value = "line-center")]
    [JsonStringEnumMemberName("line-center")]
    LineCenter,
}

[JsonConverter(typeof(JsonStringEnumConverter<LineCap>))]
public enum LineCap
{
    [EnumMember(Value = "butt")]
    [JsonStringEnumMemberName("butt")]
    Butt,

    [EnumMember(Value = "round")]
    [JsonStringEnumMemberName("round")]
    Round,

    [EnumMember(Value = "square")]
    [JsonStringEnumMemberName("square")]
    Square,
}

[JsonConverter(typeof(JsonStringEnumConverter<LineJoin>))]
public enum LineJoin
{
    [EnumMember(Value = "bevel")]
    [JsonStringEnumMemberName("bevel")]
    Bevel,

    [EnumMember(Value = "round")]
    [JsonStringEnumMemberName("round")]
    Round,

    [EnumMember(Value = "miter")]
    [JsonStringEnumMemberName("miter")]
    Miter,
}

public static class LayerOptionEnumExtensions
{
    public static string ToJsonName(this SymbolAnchor value) => EnumJsonName.Get(value);

    public static string ToJsonName(this MapAlignment value) => EnumJsonName.Get(value);

    public static string ToJsonName(this CirclePitchAlignment value) => EnumJsonName.Get(value);

    public static string ToJsonName(this TextTransform value) => EnumJsonName.Get(value);

    public static string ToJsonName(this IconTextFit value) => EnumJsonName.Get(value);

    public static string ToJsonName(this SymbolPlacement value) => EnumJsonName.Get(value);

    public static string ToJsonName(this LineCap value) => EnumJsonName.Get(value);

    public static string ToJsonName(this LineJoin value) => EnumJsonName.Get(value);
}
