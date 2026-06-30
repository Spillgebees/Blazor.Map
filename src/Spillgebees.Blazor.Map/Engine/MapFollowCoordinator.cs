namespace Spillgebees.Blazor.Map.Engine;

/// <summary>Queues camera-follow ops and maps the public follow models onto the engine op records.</summary>
internal sealed class MapFollowCoordinator(MapEngineChannel channel)
{
    public Task FollowAsync(MapFollowOptions options) =>
        channel.QueueAndFlushAsync(
            new CameraFollowOp(
                options.LayerId,
                options.EntityId,
                ToEngine(options.Camera),
                ToEngine(options.Animation),
                ToEngine(options.Interaction)
            )
        );

    public Task ClearAsync() => channel.QueueAndFlushAsync(new CameraClearFollowOp());

    private static EngineFollowCamera? ToEngine(MapFollowCameraOptions? camera) =>
        camera is null
            ? null
            : new EngineFollowCamera(
                ToWire(camera.ZoomMode),
                camera.Zoom,
                ToWire(camera.OrientationMode),
                camera.Pitch,
                ToWire(camera.BearingSource),
                camera.Bearing,
                camera.Offset,
                ToEngine(camera.TrackingAnimation)
            );

    private static string ToWire(MapFollowGestureMode mode) =>
        mode switch
        {
            MapFollowGestureMode.Anchored => "anchored",
            MapFollowGestureMode.Locked => "locked",
            MapFollowGestureMode.Clear => "clear",
            _ => "free",
        };

    private static string ToWire(MapFollowBearingSource source) =>
        source switch
        {
            MapFollowBearingSource.Fixed => "fixed",
            MapFollowBearingSource.MatchHeading => "matchheading",
            _ => "keepcurrent",
        };

    private static EngineAnimation? ToEngine(AnimationOptions? animation) =>
        animation is null
            ? null
            : new EngineAnimation(
                animation.Duration,
                animation.Easing == AnimationEasing.EaseInOut ? "easeInOut" : "linear"
            );

    private static EngineFollowInteraction? ToEngine(MapFollowInteractionOptions? interaction) =>
        interaction is null
            ? null
            : new EngineFollowInteraction(interaction.ClearOnUserPan, interaction.ClearWhenFeatureMissing);
}
