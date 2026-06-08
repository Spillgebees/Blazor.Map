using AwesomeAssertions;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Tests.Models.Layers;

public class MapLayerDefinitionTests
{
    [Test]
    public void Should_create_circle_layer_definition_with_expected_defaults()
    {
        // arrange
        var idSuffix = "clusters";

        // act
        var layer = new CircleLayerDefinition(idSuffix);

        // assert
        layer.IdSuffix.Should().Be(idSuffix);
        layer.Key.Should().Be(idSuffix);
        layer.Type.Should().Be("circle");
        layer.ResolveId("earthquakes").Should().Be("earthquakes-clusters");
        layer.Filter.Should().BeNull();
        layer.MinZoom.Should().BeNull();
        layer.MaxZoom.Should().BeNull();
    }

    [Test]
    public void Should_create_symbol_layer_definition_with_expected_defaults()
    {
        // arrange
        var idSuffix = "cluster-count";

        // act
        var layer = new SymbolLayerDefinition(idSuffix);

        // assert
        layer.IdSuffix.Should().Be(idSuffix);
        layer.Key.Should().Be(idSuffix);
        layer.Type.Should().Be("symbol");
        layer.ResolveId("earthquakes").Should().Be("earthquakes-cluster-count");
    }

    [Test]
    public void Should_reject_blank_layer_id_suffix()
    {
        // arrange
        var idSuffix = " ";

        // act
        var act = () => new CircleLayerDefinition(idSuffix);

        // assert
        act.Should().Throw<ArgumentException>().WithParameterName("idSuffix");
    }

    [Test]
    public void Should_reject_invalid_zoom_range()
    {
        // arrange
        var idSuffix = "clusters";

        // act
        var act = () => new SymbolLayerDefinition(idSuffix, minZoom: 12, maxZoom: 10);

        // assert
        act.Should().Throw<ArgumentException>().WithParameterName("minZoom");
    }

    [Test]
    public void Should_create_valid_zoom_range_with_constructor_parameters()
    {
        // arrange
        var original = new CircleLayerDefinition("clusters", maxZoom: 4);

        // act
        var replacement = new CircleLayerDefinition(
            original.IdSuffix,
            color: original.Color,
            radius: original.Radius,
            opacity: original.Opacity,
            strokeWidth: original.StrokeWidth,
            strokeColor: original.StrokeColor,
            strokeOpacity: original.StrokeOpacity,
            pitchAlignment: original.PitchAlignment,
            key: original.Key,
            filter: original.Filter,
            minZoom: 5,
            maxZoom: 10,
            beforeLayerId: original.BeforeLayerId,
            layerGroup: original.LayerGroup,
            beforeLayerGroup: original.BeforeLayerGroup,
            afterLayerGroup: original.AfterLayerGroup
        );

        // assert
        replacement.MinZoom.Should().Be(5);
        replacement.MaxZoom.Should().Be(10);
    }

    [Test]
    public void Should_keep_zoom_bounds_immutable_after_construction()
    {
        // arrange
        var layer = new SymbolLayerDefinition("cluster-count", minZoom: 2, maxZoom: 4);
        var minZoomProperty = typeof(MapLayerDefinition).GetProperty(nameof(MapLayerDefinition.MinZoom));
        var maxZoomProperty = typeof(MapLayerDefinition).GetProperty(nameof(MapLayerDefinition.MaxZoom));

        // act
        var replacement = layer with
        {
            TextColor = "#ffffff",
        };

        // assert
        minZoomProperty?.SetMethod.Should().BeNull();
        maxZoomProperty?.SetMethod.Should().BeNull();
        replacement.MinZoom.Should().Be(2);
        replacement.MaxZoom.Should().Be(4);
    }

    [Test]
    public void Should_reject_out_of_range_zoom_from_constructor()
    {
        // arrange
        var idSuffix = "clusters";

        // act
        var act = () => new CircleLayerDefinition(idSuffix, maxZoom: 25);

        // assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxZoom");
    }

    [Test]
    public void Should_keep_identity_properties_immutable_after_construction()
    {
        // arrange
        var layer = new CircleLayerDefinition("clusters");
        var idSuffixProperty = typeof(MapLayerDefinition).GetProperty(nameof(MapLayerDefinition.IdSuffix));
        var keyProperty = typeof(MapLayerDefinition).GetProperty(nameof(MapLayerDefinition.Key));

        // act
        var mutatedLayer = layer with
        {
            Color = "#51bbd6",
        };

        // assert
        idSuffixProperty?.SetMethod.Should().BeNull();
        keyProperty?.SetMethod.Should().BeNull();
        mutatedLayer.IdSuffix.Should().Be("clusters");
        mutatedLayer.Key.Should().Be("clusters");
    }

    [Test]
    public void Should_create_circle_layer_definition_with_style_and_ordering_options()
    {
        // arrange
        object[] filter = ["has", "point_count"];

        // act
        var layer = MapLayer.Circle(
            "clusters",
            color: "#51bbd6",
            radius: 24,
            opacity: 0.75,
            strokeColor: "#ffffff",
            strokeWidth: 2,
            filter: filter,
            minZoom: 4,
            maxZoom: 14,
            layerGroup: "clustered-points"
        );

        // assert
        layer.Should().BeOfType<CircleLayerDefinition>();
        layer.Color.Should().NotBeNull();
        layer.Radius.Should().NotBeNull();
        layer.Opacity.Should().NotBeNull();
        layer.StrokeColor.Should().NotBeNull();
        layer.StrokeWidth.Should().NotBeNull();
        layer.Filter.Should().BeSameAs(filter);
        layer.MinZoom.Should().Be(4);
        layer.MaxZoom.Should().Be(14);
        layer.LayerGroup.Should().Be("clustered-points");
    }

    [Test]
    public void Should_snapshot_symbol_sequence_values_from_factory()
    {
        // arrange
        var textFont = new[] { "Open Sans Semibold", "Arial Unicode MS Bold" };
        var iconOffset = new[] { 1d, 2d };

        // act
        var layer = MapLayer.Symbol("labels", textFont: textFont, iconOffset: iconOffset);
        textFont[0] = "Mutated";
        iconOffset[0] = 9;

        // assert
        layer.TextFont.Should().BeEquivalentTo(["Open Sans Semibold", "Arial Unicode MS Bold"]);
        layer.IconOffset.Should().BeEquivalentTo([1d, 2d]);
    }
}
