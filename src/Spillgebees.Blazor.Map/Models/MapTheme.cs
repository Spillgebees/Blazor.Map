using System.Text.Json.Serialization;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Controls the visual theme of UI controls, popups, and attribution.
/// This does NOT affect the map tiles — use <see cref="MapOptions.Style"/> for that.
/// </summary>
[JsonConverter(typeof(LowerCaseJsonStringEnumConverter))]
public enum MapTheme
{
    /// <summary>
    /// Light theme for UI controls and popups.
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme for UI controls and popups.
    /// </summary>
    Dark,
}
