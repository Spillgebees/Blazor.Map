namespace Spillgebees.Blazor.Map;

/// <summary>
/// Describes the effect of a built-in MapLibre interaction.
/// </summary>
public enum MapInteractionEffect
{
    /// <summary>Moves the map viewport.</summary>
    Pan,

    /// <summary>Changes the zoom level continuously in either direction.</summary>
    Zoom,

    /// <summary>Increases the zoom level.</summary>
    ZoomIn,

    /// <summary>Decreases the zoom level.</summary>
    ZoomOut,

    /// <summary>Changes the map bearing.</summary>
    Rotate,

    /// <summary>Changes the map pitch.</summary>
    Pitch,

    /// <summary>Zooms the map to a selected rectangular area.</summary>
    BoxZoom,

    /// <summary>Cancels an active rectangular zoom selection.</summary>
    CancelBoxZoom,
}
