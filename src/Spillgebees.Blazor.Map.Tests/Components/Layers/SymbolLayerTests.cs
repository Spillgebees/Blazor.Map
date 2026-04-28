using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models.Expressions;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class SymbolLayerTests
{
    [Test]
    public void Should_include_icon_opacity_in_paint_properties_when_set_to_literal()
    {
        // arrange
        var layer = new SymbolLayer { IconOpacity = 0.5 };

        // act
        var paint = layer.GetPaintProperties();

        // assert
        paint.Should().ContainKey("icon-opacity");
        paint["icon-opacity"].Should().Be(0.5);
    }

    [Test]
    public void Should_include_icon_opacity_in_paint_properties_when_set_to_expression()
    {
        // arrange
        var hoverKey = FeatureState.Bool("hover");
        var expression = hoverKey.When(trueValue: 1.0, falseValue: 0.5);
        var layer = new SymbolLayer { IconOpacity = expression };

        // act
        var paint = layer.GetPaintProperties();

        // assert
        paint.Should().ContainKey("icon-opacity");
        var value = paint["icon-opacity"] as object[];
        value.Should().NotBeNull();
        value![0].Should().Be("case");
    }

    [Test]
    public void Should_have_null_icon_opacity_in_paint_properties_when_not_set()
    {
        // arrange
        var layer = new SymbolLayer();

        // act
        var paint = layer.GetPaintProperties();

        // assert
        paint.Should().ContainKey("icon-opacity");
        paint["icon-opacity"].Should().BeNull();
    }

    [Test]
    public void Should_not_include_icon_opacity_in_layout_properties()
    {
        // arrange
        var layer = new SymbolLayer { IconOpacity = 0.8 };

        // act
        var layout = layer.GetLayoutProperties();

        // assert
        layout.Should().NotContainKey("icon-opacity");
    }

    [Test]
    public void Should_include_symbol_sort_key_in_layout_properties_when_set()
    {
        // arrange
        var layer = new SymbolLayer { SymbolSortKey = 42 };

        // act
        var layout = layer.GetLayoutProperties();

        // assert
        layout.Should().ContainKey("symbol-sort-key");
        layout["symbol-sort-key"].Should().Be(42);
    }

    [Test]
    public void Should_include_expression_icon_offset_in_layout_properties_when_set()
    {
        // arrange
        var layer = new SymbolLayer { IconOffset = new object[] { "get", "offset" } };

        // act
        var layout = layer.GetLayoutProperties();

        // assert
        layout.Should().ContainKey("icon-offset");
        var offset = layout["icon-offset"] as object[];
        offset.Should().NotBeNull();
        offset![0].Should().Be("get");
        offset[1].Should().Be("offset");
    }

    [Test]
    public void Should_include_expression_text_offset_in_layout_properties_when_set()
    {
        // arrange
        var layer = new SymbolLayer { TextOffset = new object[] { "get", "offset" } };

        // act
        var layout = layer.GetLayoutProperties();

        // assert
        layout.Should().ContainKey("text-offset");
        var offset = layout["text-offset"] as object[];
        offset.Should().NotBeNull();
        offset![0].Should().Be("get");
        offset[1].Should().Be("offset");
    }

    [Test]
    public void Should_include_text_alignment_properties_in_layout_properties_when_set()
    {
        // arrange
        var layer = new SymbolLayer
        {
            TextPitchAlignment = MapAlignment.Viewport,
            TextRotationAlignment = MapAlignment.Viewport,
        };

        // act
        var layout = layer.GetLayoutProperties();

        // assert
        layout.Should().ContainKey("text-pitch-alignment");
        layout["text-pitch-alignment"].Should().Be("viewport");
        layout.Should().ContainKey("text-rotation-alignment");
        layout["text-rotation-alignment"].Should().Be("viewport");
    }

    [Test]
    public void Should_serialize_symbol_enum_layout_values_as_maplibre_strings()
    {
        // arrange
        var layer = new SymbolLayer
        {
            TextAnchor = SymbolAnchor.TopLeft,
            IconAnchor = SymbolAnchor.BottomRight,
            TextTransform = TextTransform.Uppercase,
            IconTextFit = IconTextFit.Both,
            RotationAlignment = MapAlignment.Auto,
            Placement = SymbolPlacement.LineCenter,
        };

        // act
        var layout = layer.GetLayoutProperties();

        // assert
        layout["text-anchor"].Should().Be("top-left");
        layout["icon-anchor"].Should().Be("bottom-right");
        layout["text-transform"].Should().Be("uppercase");
        layout["icon-text-fit"].Should().Be("both");
        layout["icon-rotation-alignment"].Should().Be("auto");
        layout["symbol-placement"].Should().Be("line-center");
    }
}
