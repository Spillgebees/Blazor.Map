namespace Spillgebees.Blazor.Map;

/// <summary>
/// Camera behaviour while following an entity, set per gesture. Zoom has its own gesture; pitch and
/// bearing share one (MapLibre's drag-to-rotate tilts and turns together), so they share
/// <paramref name="OrientationMode"/>. Each mode decides whether the follow holds a target and whether
/// the user may still move it: see <see cref="MapFollowGestureMode"/>. Only
/// <see cref="MapFollowGestureMode.Anchored"/> and <see cref="MapFollowGestureMode.Locked"/> hold a
/// target; under <see cref="MapFollowGestureMode.Free"/> or <see cref="MapFollowGestureMode.Clear"/>
/// that gesture's paired value below is ignored.
/// </summary>
/// <param name="ZoomMode">How the zoom gesture behaves. Default <see cref="MapFollowGestureMode.Free"/> leaves zoom to the user.</param>
/// <param name="Zoom">Zoom to hold while <paramref name="ZoomMode"/> holds a target; ignored otherwise. Null holds the zoom from when the follow engaged.</param>
/// <param name="OrientationMode">How the combined rotate-and-tilt gesture behaves. Default <see cref="MapFollowGestureMode.Free"/> leaves it to the user.</param>
/// <param name="Pitch">Pitch to hold while <paramref name="OrientationMode"/> holds a target; ignored otherwise. Null holds the pitch from when the follow engaged.</param>
/// <param name="BearingSource">Where the held bearing comes from while <paramref name="OrientationMode"/> holds a target; ignored otherwise.</param>
/// <param name="Bearing">Bearing to hold when <paramref name="BearingSource"/> is <see cref="MapFollowBearingSource.Fixed"/>; ignored otherwise.</param>
/// <param name="Offset">Pixel offset of the followed entity from the screen centre. Null centres exactly.</param>
public sealed record MapFollowCameraOptions(
    MapFollowGestureMode ZoomMode = MapFollowGestureMode.Free,
    double? Zoom = null,
    MapFollowGestureMode OrientationMode = MapFollowGestureMode.Free,
    double? Pitch = null,
    MapFollowBearingSource BearingSource = MapFollowBearingSource.KeepCurrent,
    double? Bearing = null,
    PixelPoint? Offset = null
);
