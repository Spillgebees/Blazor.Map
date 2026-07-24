namespace Spillgebees.Blazor.Map;

/// <summary>
/// Identifies a built-in MapLibre input for descriptive metadata.
/// </summary>
public enum MapInteractionInput
{
    /// <summary>Press an arrow key to pan the map by 100 pixels.</summary>
    KeyboardArrowKeys,

    /// <summary>Press Shift and the left or right arrow key to rotate the map by 15 degrees.</summary>
    KeyboardShiftHorizontalArrowKeys,

    /// <summary>Press Shift and the up or down arrow key to change the map pitch by 10 degrees.</summary>
    KeyboardShiftVerticalArrowKeys,

    /// <summary>Press the plus or equals key to zoom in by one level.</summary>
    KeyboardZoomIn,

    /// <summary>Press the minus key to zoom out by one level.</summary>
    KeyboardZoomOut,

    /// <summary>Press Shift and the plus or equals key to zoom in by two levels.</summary>
    KeyboardShiftZoomIn,

    /// <summary>Press Shift and the minus key to zoom out by two levels.</summary>
    KeyboardShiftZoomOut,

    /// <summary>Press Escape to cancel an active box zoom.</summary>
    KeyboardEscape,

    /// <summary>Drag with the primary mouse button to pan the map.</summary>
    MousePrimaryDrag,

    /// <summary>Drag with the primary mouse button while holding Shift to zoom to a box.</summary>
    MouseShiftPrimaryDrag,

    /// <summary>
    /// Drag horizontally with the secondary mouse button, or with the primary button while holding Control,
    /// to rotate the map.
    /// </summary>
    MouseSecondaryOrControlPrimaryHorizontalDrag,

    /// <summary>
    /// Drag vertically with the secondary mouse button, or with the primary button while holding Control,
    /// to change the map pitch.
    /// </summary>
    MouseSecondaryOrControlPrimaryVerticalDrag,

    /// <summary>Double-click the map to zoom in by one level around the pointer.</summary>
    MouseDoubleClick,

    /// <summary>Double-click the map while holding Shift to zoom out by one level around the pointer.</summary>
    MouseShiftDoubleClick,

    /// <summary>Scroll a mouse wheel or trackpad to zoom around the pointer.</summary>
    Scroll,

    /// <summary>Scroll while holding Shift for finer zoom control.</summary>
    ShiftScroll,

    /// <summary>
    /// Scroll while holding Control on Windows or Linux, or Command on macOS, when cooperative gestures are enabled.
    /// </summary>
    ControlOrMetaScroll,

    /// <summary>
    /// Scroll while holding Shift and Control on Windows or Linux, or Shift and Command on macOS, for finer zoom
    /// control when cooperative gestures are enabled.
    /// </summary>
    ControlOrMetaShiftScroll,

    /// <summary>Pinch on a trackpad to zoom the map.</summary>
    TrackpadPinch,

    /// <summary>Drag with one finger to pan the map.</summary>
    TouchOneFingerDrag,

    /// <summary>Drag with two fingers to pan the map when cooperative gestures are enabled.</summary>
    TouchTwoFingerDrag,

    /// <summary>Pinch with two fingers to zoom the map.</summary>
    TouchPinch,

    /// <summary>Twist with two fingers to rotate the map.</summary>
    TouchTwist,

    /// <summary>Drag two fingers together vertically to change the map pitch.</summary>
    TouchTwoFingerVerticalDrag,

    /// <summary>Drag three fingers together vertically to change the map pitch when cooperative gestures are enabled.</summary>
    TouchThreeFingerVerticalDrag,

    /// <summary>Double-tap with one finger to zoom in by one level.</summary>
    TouchDoubleTap,

    /// <summary>Tap once with two fingers to zoom out by one level.</summary>
    TouchTwoFingerTap,

    /// <summary>
    /// Double-tap, hold the second tap, and drag vertically for continuous zoom. Drag down to zoom in or up to zoom out.
    /// </summary>
    TouchDoubleTapVerticalDrag,
}
