using System.Text.Json.Serialization;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// The map projection to use for rendering.
/// </summary>
[JsonConverter(typeof(LowerCaseJsonStringEnumConverter))]
public enum MapProjection
{
    /// <summary>
    /// Standard Web Mercator projection (flat map).
    /// </summary>
    Mercator,

    /// <summary>
    /// Globe projection (3D sphere) — available at low zoom levels.
    /// </summary>
    Globe,
}
