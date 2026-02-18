using System.Text.Json.Serialization;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Models.Controls;

/// <summary>
/// Position of the control on the map.
/// </summary>
[JsonConverter(typeof(LowerCaseJsonStringEnumConverter))]
public enum ControlPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}
