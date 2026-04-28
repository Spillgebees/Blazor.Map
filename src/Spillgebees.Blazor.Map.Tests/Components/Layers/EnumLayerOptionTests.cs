using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components.Layers;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class EnumLayerOptionTests
{
    [Test]
    public void Should_serialize_line_enum_layout_values_as_maplibre_strings()
    {
        // arrange
        var layer = new LineLayer { Cap = LineCap.Square, Join = LineJoin.Miter };

        // act
        var layout = layer.GetLayoutProperties();

        // assert
        layout["line-cap"].Should().Be("square");
        layout["line-join"].Should().Be("miter");
    }

    [Test]
    public void Should_serialize_circle_pitch_alignment_as_maplibre_string()
    {
        // arrange
        var layer = new CircleLayer { PitchAlignment = CirclePitchAlignment.Viewport };

        // act
        var paint = layer.GetPaintProperties();

        // assert
        paint["circle-pitch-alignment"].Should().Be("viewport");
    }
}
