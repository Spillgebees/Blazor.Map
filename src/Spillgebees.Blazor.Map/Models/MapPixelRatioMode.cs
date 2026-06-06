namespace Spillgebees.Blazor.Map;

/// <summary>
/// Controls how the MapLibre canvas pixel ratio is resolved for a map instance.
/// </summary>
public enum MapPixelRatioMode
{
    /// <summary>
    /// Preserve MapLibre's browser default behavior.
    /// </summary>
    BrowserDefault,

    /// <summary>
    /// Use the next whole-number device pixel ratio, with a minimum of 1.
    /// </summary>
    RoundedUpDevicePixelRatio,
}
