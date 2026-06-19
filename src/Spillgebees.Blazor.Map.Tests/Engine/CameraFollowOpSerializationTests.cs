using System.Text.Json;
using AwesomeAssertions;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map.Tests.Engine;

public class CameraFollowOpSerializationTests
{
    private static string Serialize(params EngineOp[] ops) =>
        JsonSerializer.Serialize((IReadOnlyList<EngineOp>)ops, MapEngineJsonContext.Default.IReadOnlyListEngineOp);

    [Test]
    public void Should_serialize_minimal_follow_op()
    {
        // arrange
        var op = new CameraFollowOp("vehicles", "bus-1");

        // act
        var json = Serialize(op);

        // assert
        json.Should().Be("""[{"op":"camera.follow","layerId":"vehicles","entityId":"bus-1"}]""");
    }

    [Test]
    public void Should_serialize_follow_op_with_camera_animation_and_interaction()
    {
        // arrange
        var op = new CameraFollowOp(
            "vehicles",
            "bus-1",
            new EngineFollowCamera(
                ZoomMode: "anchored",
                Zoom: 15,
                OrientationMode: "locked",
                Pitch: 45,
                BearingSource: "matchheading"
            ),
            new EngineAnimation(500, "easeInOut"),
            new EngineFollowInteraction(ClearWhenFeatureMissing: true)
        );

        // act
        var json = Serialize(op);

        // assert: camelCase, nulls omitted, mode strings and bools always written
        json.Should()
            .Be(
                """[{"op":"camera.follow","layerId":"vehicles","entityId":"bus-1","camera":{"zoomMode":"anchored","zoom":15,"orientationMode":"locked","pitch":45,"bearingSource":"matchheading"},"animation":{"durationMs":500,"easing":"easeInOut"},"interaction":{"clearOnUserPan":true,"clearWhenFeatureMissing":true}}]"""
            );
    }

    [Test]
    public void Should_serialize_clear_follow_op()
    {
        // arrange & act
        var json = Serialize(new CameraClearFollowOp());

        // assert
        json.Should().Be("""[{"op":"camera.clearFollow"}]""");
    }
}
