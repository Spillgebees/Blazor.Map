using System.Text.Json.Serialization;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Models.Tooltips;

/// <summary>
/// Direction of the tooltip relative to the layer.
/// </summary>
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
