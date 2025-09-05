using System.Text.Json.Serialization;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Models.Tooltips;

[JsonConverter(typeof(LowerCaseJsonStringEnumConverter))]
public enum TooltipDirection
{
    Top,
    Bottom,
    Left,
    Right,
    Center,
    Auto
}
