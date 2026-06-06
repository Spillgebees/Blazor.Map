using AwesomeAssertions;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Tests.Models.Clustering;

public class ClusterLayerSetTests
{
    [Test]
    public void Should_provide_disabled_none_layer_set()
    {
        // arrange
        var layerSet = ClusterLayerSet.None;

        // act
        var enabled = layerSet.Enabled;

        // assert
        enabled.Should().BeFalse();
        layerSet.Layers.Should().BeEmpty();
    }

    [Test]
    public void Should_provide_default_circle_and_count_layers()
    {
        // arrange
        var layerSet = ClusterLayerSet.Default;

        // act
        var layers = layerSet.Layers;

        // assert
        layerSet.Enabled.Should().BeTrue();
        layers.Should().HaveCount(2);
        layers[0].Should().BeOfType<ClusterCircleLayerDefinition>();
        layers[1].Should().BeOfType<ClusterSymbolLayerDefinition>();
    }

    [Test]
    public void Should_create_custom_layer_set()
    {
        // arrange
        var layers = new ClusterLayerDefinition[]
        {
            ClusterLayerDefinition.Circle("cluster-bubble", color: "#2563eb", radius: 24),
            ClusterLayerDefinition.Symbol("cluster-label", textField: Expr.Get("point_count_abbreviated")),
        };
        var originalCircle = layers[0];
        var symbol = layers[1];

        // act
        var layerSet = ClusterLayerSet.Custom(layers);
        layers[0] = ClusterLayerDefinition.Circle("replacement-bubble");

        // assert
        layerSet.Enabled.Should().BeTrue();
        layerSet.Layers.Should().Equal(originalCircle, symbol);
    }

    [Test]
    public void Should_reject_empty_custom_layer_set()
    {
        // arrange
        var act = () => ClusterLayerSet.Custom([]);

        // act
        var assertion = act.Should().Throw<ArgumentException>();

        // assert
        assertion.WithMessage("*at least one layer*");
    }

    [Test]
    public void Should_reject_null_custom_layer_set_array()
    {
        // arrange
        ClusterLayerDefinition[] layers = null!;

        // act
        var act = () => ClusterLayerSet.Custom(layers);

        // assert
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("layers");
    }

    [Test]
    public void Should_reject_null_custom_layer_entry()
    {
        // arrange
        var circle = ClusterLayerDefinition.Circle("cluster-bubble", color: "#2563eb", radius: 24);
        ClusterLayerDefinition nullLayer = null!;

        // act
        var act = () => ClusterLayerSet.Custom(circle, nullLayer);

        // assert
        act.Should().Throw<ArgumentException>().WithMessage("*must not contain null layers*");
    }

    [Test]
    public void Should_reject_duplicate_custom_layer_id_suffixes()
    {
        // arrange
        var circle = ClusterLayerDefinition.Circle("cluster", color: "#2563eb", radius: 24);
        var symbol = ClusterLayerDefinition.Symbol("cluster", textField: Expr.Get("point_count_abbreviated"));

        // act
        var act = () => ClusterLayerSet.Custom(circle, symbol);

        // assert
        act.Should().Throw<ArgumentException>().WithMessage("*duplicate*id suffix*");
    }

    [Test]
    public void Should_reject_invalid_layer_id_suffix()
    {
        // arrange
        var invalidSuffixes = new[] { null!, "", "   " };

        // act
        var assertions = invalidSuffixes.Select(idSuffix =>
            new Action(() => ClusterLayerDefinition.Circle(idSuffix)).Should().Throw<ArgumentException>()
        );

        // assert
        assertions.Should().AllSatisfy(assertion => assertion.WithMessage("*id suffix*"));
    }
}
