using AwesomeAssertions;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map.Tests.Engine;

// Covers the public follow models -> engine op mapping (enum -> wire string, defaults, nulls),
// exercised through the real channel so the full C# path to the applyOps payload is asserted.
public class MapFollowCoordinatorTests
{
    private static async Task<string> CaptureFollowOps(Func<MapFollowCoordinator, Task> act)
    {
        var jsRuntime = new CapturingJsRuntime();
        var channel = new MapEngineChannel(jsRuntime);
        await channel.MarkReadyAsync();
        var coordinator = new MapFollowCoordinator(channel);

        await act(coordinator);

        return jsRuntime.LastApplyOpsJson ?? throw new InvalidOperationException("applyOps was never invoked.");
    }

    [Test]
    public async Task Should_map_minimal_follow_options_to_minimal_op()
    {
        // arrange & act
        var json = await CaptureFollowOps(c => c.FollowAsync(new MapFollowOptions("vehicles", "bus-1")));

        // assert
        json.Should().Be("""[{"op":"camera.follow","layerId":"vehicles","entityId":"bus-1"}]""");
    }

    [Test]
    public async Task Should_map_gesture_modes_to_wire_strings()
    {
        // arrange & act
        var json = await CaptureFollowOps(c =>
            c.FollowAsync(
                new MapFollowOptions(
                    "vehicles",
                    "bus-1",
                    Camera: new MapFollowCameraOptions(
                        ZoomMode: MapFollowGestureMode.Anchored,
                        OrientationMode: MapFollowGestureMode.Locked
                    )
                )
            )
        );

        // assert
        json.Should().Contain("\"zoomMode\":\"anchored\"").And.Contain("\"orientationMode\":\"locked\"");
    }

    [Test]
    public async Task Should_map_fixed_bearing_source_to_wire_string()
    {
        // arrange & act
        var json = await CaptureFollowOps(c =>
            c.FollowAsync(
                new MapFollowOptions(
                    "vehicles",
                    "bus-1",
                    Camera: new MapFollowCameraOptions(
                        OrientationMode: MapFollowGestureMode.Anchored,
                        BearingSource: MapFollowBearingSource.Fixed,
                        Bearing: 90
                    )
                )
            )
        );

        // assert
        json.Should().Contain("\"bearingSource\":\"fixed\"").And.Contain("\"bearing\":90");
    }

    [Test]
    public async Task Should_map_match_heading_bearing_source_to_wire_string()
    {
        // arrange & act
        var json = await CaptureFollowOps(c =>
            c.FollowAsync(
                new MapFollowOptions(
                    "vehicles",
                    "bus-1",
                    Camera: new MapFollowCameraOptions(
                        OrientationMode: MapFollowGestureMode.Anchored,
                        BearingSource: MapFollowBearingSource.MatchHeading
                    )
                )
            )
        );

        // assert
        json.Should().Contain("\"bearingSource\":\"matchheading\"");
    }

    [Test]
    public async Task Should_map_full_options_including_offset_easing_and_interaction()
    {
        // arrange & act
        var json = await CaptureFollowOps(c =>
            c.FollowAsync(
                new MapFollowOptions(
                    "vehicles",
                    "bus-1",
                    Camera: new MapFollowCameraOptions(
                        ZoomMode: MapFollowGestureMode.Anchored,
                        Zoom: 15,
                        OrientationMode: MapFollowGestureMode.Locked,
                        Pitch: 45,
                        BearingSource: MapFollowBearingSource.Fixed,
                        Bearing: 90,
                        Offset: new PixelPoint(10, 20)
                    ),
                    Animation: new AnimationOptions(Duration: 500, Easing: AnimationEasing.EaseInOut),
                    Interaction: new MapFollowInteractionOptions(ClearWhenFeatureMissing: true)
                )
            )
        );

        // assert: enums -> wire strings, PixelPoint -> {x,y}, easing -> "easeInOut", bools always written
        json.Should()
            .Be(
                """[{"op":"camera.follow","layerId":"vehicles","entityId":"bus-1","camera":{"zoomMode":"anchored","zoom":15,"orientationMode":"locked","pitch":45,"bearingSource":"fixed","bearing":90,"offset":{"x":10,"y":20}},"animation":{"durationMs":500,"easing":"easeInOut"},"interaction":{"clearOnUserPan":true,"clearWhenFeatureMissing":true}}]"""
            );
    }

    [Test]
    public async Task Should_default_animation_easing_to_linear()
    {
        // arrange & act
        var json = await CaptureFollowOps(c =>
            c.FollowAsync(new MapFollowOptions("vehicles", "bus-1", Animation: new AnimationOptions(Duration: 200)))
        );

        // assert
        json.Should().Contain("\"animation\":{\"durationMs\":200,\"easing\":\"linear\"}");
    }

    [Test]
    public async Task Should_map_clear_to_clear_follow_op()
    {
        // arrange & act
        var json = await CaptureFollowOps(c => c.ClearAsync());

        // assert
        json.Should().Be("""[{"op":"camera.clearFollow"}]""");
    }

    // Minimal IJSRuntime that records the last Spillgebees.Engine.applyOps payload.
    private sealed class CapturingJsRuntime : IJSRuntime
    {
        public string? LastApplyOpsJson { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args
        )
        {
            if (identifier == "Spillgebees.Engine.applyOps" && args is [_, string opsJson])
            {
                LastApplyOpsJson = opsJson;
            }

            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
