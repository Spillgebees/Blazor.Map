using System.Text.Json.Serialization;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Models.Popups;

/// <summary>
/// The anchor position of a popup relative to its feature.
/// </summary>
[JsonConverter(typeof(LowerCaseJsonStringEnumConverter))]
public enum PopupAnchor
{
    /// <summary>
    /// MapLibre chooses the position based on available space.
    /// </summary>
    Auto,

    /// <summary>
    /// Popup appears above the feature.
    /// </summary>
    Top,

    /// <summary>
    /// Popup appears below the feature.
    /// </summary>
    Bottom,

    /// <summary>
    /// Popup appears to the left of the feature.
    /// </summary>
    Left,

    /// <summary>
    /// Popup appears to the right of the feature.
    /// </summary>
    Right,
}
