using System.Text.Json.Serialization;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Models.Popups;

/// <summary>
/// Determines how a popup is triggered.
/// </summary>
[JsonConverter(typeof(LowerCaseJsonStringEnumConverter))]
public enum PopupTrigger
{
    /// <summary>
    /// Show on click, dismiss via close button or clicking elsewhere.
    /// </summary>
    Click,

    /// <summary>
    /// Show on mouse enter, hide on mouse leave.
    /// </summary>
    Hover,

    /// <summary>
    /// Always visible — ideal for labels.
    /// </summary>
    Permanent,
}
