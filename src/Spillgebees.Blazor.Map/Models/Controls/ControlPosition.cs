using System.Text.Json.Serialization;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Models.Controls;

[JsonConverter(typeof(LowerCaseJsonStringEnumConverter))]
public enum ControlPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}
