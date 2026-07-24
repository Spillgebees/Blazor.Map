using System.Collections.ObjectModel;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Provides read-only reference metadata for built-in map interactions.
/// </summary>
public static class MapInteractionMetadata
{
    private static readonly IReadOnlyDictionary<MapInteractionInput, MapInteractionEffect> _defaults = CreateDefaults(
        cooperativeGestures: false
    );

    private static readonly IReadOnlyDictionary<MapInteractionInput, MapInteractionEffect> _cooperativeDefaults =
        CreateDefaults(cooperativeGestures: true);

    /// <summary>
    /// Gets MapLibre's default input-to-effect metadata for an interactive map.
    /// </summary>
    /// <param name="cooperativeGestures">
    /// Whether cooperative gestures are enabled. Changes some of the scroll, touch-pan, and touch-pitch inputs.
    /// </param>
    /// <returns>
    /// Read-only reference metadata.
    /// </returns>
    public static IReadOnlyDictionary<MapInteractionInput, MapInteractionEffect> GetDefaults(
        bool cooperativeGestures = false
    ) => cooperativeGestures ? _cooperativeDefaults : _defaults;

    private static ReadOnlyDictionary<MapInteractionInput, MapInteractionEffect> CreateDefaults(
        bool cooperativeGestures
    )
    {
        var interactions = new Dictionary<MapInteractionInput, MapInteractionEffect>
        {
            [MapInteractionInput.KeyboardArrowKeys] = MapInteractionEffect.Pan,
            [MapInteractionInput.KeyboardShiftHorizontalArrowKeys] = MapInteractionEffect.Rotate,
            [MapInteractionInput.KeyboardShiftVerticalArrowKeys] = MapInteractionEffect.Pitch,
            [MapInteractionInput.KeyboardZoomIn] = MapInteractionEffect.ZoomIn,
            [MapInteractionInput.KeyboardZoomOut] = MapInteractionEffect.ZoomOut,
            [MapInteractionInput.KeyboardShiftZoomIn] = MapInteractionEffect.ZoomIn,
            [MapInteractionInput.KeyboardShiftZoomOut] = MapInteractionEffect.ZoomOut,
            [MapInteractionInput.KeyboardEscape] = MapInteractionEffect.CancelBoxZoom,
            [MapInteractionInput.MousePrimaryDrag] = MapInteractionEffect.Pan,
            [MapInteractionInput.MouseShiftPrimaryDrag] = MapInteractionEffect.BoxZoom,
            [MapInteractionInput.MouseSecondaryOrControlPrimaryHorizontalDrag] = MapInteractionEffect.Rotate,
            [MapInteractionInput.MouseSecondaryOrControlPrimaryVerticalDrag] = MapInteractionEffect.Pitch,
            [MapInteractionInput.MouseDoubleClick] = MapInteractionEffect.ZoomIn,
            [MapInteractionInput.MouseShiftDoubleClick] = MapInteractionEffect.ZoomOut,
            [MapInteractionInput.TrackpadPinch] = MapInteractionEffect.Zoom,
            [MapInteractionInput.TouchPinch] = MapInteractionEffect.Zoom,
            [MapInteractionInput.TouchTwist] = MapInteractionEffect.Rotate,
            [MapInteractionInput.TouchDoubleTap] = MapInteractionEffect.ZoomIn,
            [MapInteractionInput.TouchTwoFingerTap] = MapInteractionEffect.ZoomOut,
            [MapInteractionInput.TouchDoubleTapVerticalDrag] = MapInteractionEffect.Zoom,
        };

        if (cooperativeGestures)
        {
            interactions[MapInteractionInput.ControlOrMetaScroll] = MapInteractionEffect.Zoom;
            interactions[MapInteractionInput.ControlOrMetaShiftScroll] = MapInteractionEffect.Zoom;
            interactions[MapInteractionInput.TouchTwoFingerDrag] = MapInteractionEffect.Pan;
            interactions[MapInteractionInput.TouchThreeFingerVerticalDrag] = MapInteractionEffect.Pitch;
        }
        else
        {
            interactions[MapInteractionInput.Scroll] = MapInteractionEffect.Zoom;
            interactions[MapInteractionInput.ShiftScroll] = MapInteractionEffect.Zoom;
            interactions[MapInteractionInput.TouchOneFingerDrag] = MapInteractionEffect.Pan;
            interactions[MapInteractionInput.TouchTwoFingerVerticalDrag] = MapInteractionEffect.Pitch;
        }

        return new ReadOnlyDictionary<MapInteractionInput, MapInteractionEffect>(interactions);
    }
}
