using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models.Expressions;
using Spillgebees.Blazor.Map.Models.Options;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class EnumLayerOptionTests
{
    private enum TestEnum
    {
        [System.Text.Json.Serialization.JsonStringEnumMemberName("known-value")]
        KnownValue,
    }

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

    [Test]
    public void Should_return_json_member_name_for_known_enum_values()
    {
        // arrange
        var value = SymbolAnchor.TopLeft;

        // act
        var jsonName = value.ToJsonName();

        // assert
        jsonName.Should().Be("top-left");
    }

    [Test]
    public void Should_fallback_to_enum_string_for_unknown_extension_values()
    {
        // arrange
        var value = (SymbolAnchor)999;

        // act
        var jsonName = value.ToJsonName();

        // assert
        jsonName.Should().Be("999");
    }

    [Test]
    public void Should_fallback_to_enum_string_for_unknown_style_values()
    {
        // arrange
        StyleValue<SymbolAnchor> value = (SymbolAnchor)999;

        // act
        var serializable = value.ToSerializable();

        // assert
        serializable.Should().Be("999");
    }

    [Test]
    public void Should_serialize_style_value_enum_with_json_member_name()
    {
        // arrange
        StyleValue<TestEnum> value = TestEnum.KnownValue;

        // act
        var serializable = value.ToSerializable();

        // assert
        serializable.Should().Be("known-value");
    }

    [Test]
    public void Should_expose_layer_option_enums_from_models_options_namespace()
    {
        // arrange
        var enumType = typeof(SymbolAnchor);

        // act
        var enumNamespace = enumType.Namespace;

        // assert
        enumNamespace.Should().Be("Spillgebees.Blazor.Map.Models.Options");
    }
}
