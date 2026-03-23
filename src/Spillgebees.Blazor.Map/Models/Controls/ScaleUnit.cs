using System.Text.Json.Serialization;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Models.Controls;

/// <summary>
/// The unit system for the scale control.
/// </summary>
[JsonConverter(typeof(LowerCaseJsonStringEnumConverter))]
public enum ScaleUnit
{
    /// <summary>
    /// Metric units (meters/kilometers).
    /// </summary>
    Metric,

    /// <summary>
    /// Imperial units (feet/miles).
    /// </summary>
    Imperial,

    /// <summary>
    /// Nautical units (nautical miles).
    /// </summary>
    Nautical,
}
