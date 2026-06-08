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
    public void Should_expose_custom_layers_as_read_only_snapshot()
    {
        // arrange
        var layers = new[] { ClusterLayerDefinition.Circle("cluster-bubble") };

        // act
        var layerSet = ClusterLayerSet.Custom(layers);
        layers[0] = ClusterLayerDefinition.Circle("replacement-bubble");

        // assert
        layerSet.Layers.Should().ContainSingle(layer => layer.IdSuffix == "cluster-bubble");
        layerSet.Layers.Should().NotBeAssignableTo<ClusterLayerDefinition[]>();
        layerSet
            .Layers.Invoking(readOnlyLayers => ((IList<ClusterLayerDefinition>)readOnlyLayers).Clear())
            .Should()
            .Throw<NotSupportedException>();
    }

    [Test]
    public void Should_reject_empty_custom_layer_set()
    {
        // arrange
        var act = () => ClusterLayerSet.Custom([]);

        // act
        var assertion = act.Should().Throw<ArgumentException>();

        // assert
        assertion.WithMessage("*at least one layer*").Which.ParamName.Should().Be("layers");
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
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*must not contain null layers*")
            .Which.ParamName.Should()
            .Be("layers");
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
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*duplicate*id suffix*")
            .Which.ParamName.Should()
            .Be("layers");
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

    [Test]
    public void Should_reject_invalid_cluster_layer_zoom_ranges_from_factory()
    {
        // arrange
        var invalidFactories = new Action[]
        {
            () => ClusterLayerDefinition.Circle("cluster", minZoom: -1),
            () => ClusterLayerDefinition.Circle("cluster", maxZoom: 25),
            () => ClusterLayerDefinition.Symbol("cluster", minZoom: 12, maxZoom: 10),
        };

        // act
        var assertions = invalidFactories.Select(factory => factory.Should().Throw<ArgumentException>());

        // assert
        assertions
            .Should()
            .AllSatisfy(assertion => assertion.WithMessage("*zoom*").Which.ParamName.Should().NotBeNull());
    }

    [Test]
    public void Should_create_cluster_layer_with_factory_zoom_bounds()
    {
        // arrange
        var original = ClusterLayerDefinition.Circle("cluster", color: "#2563eb", maxZoom: 4);

        // act
        var replacement = ClusterLayerDefinition.Circle(
            original.IdSuffix,
            color: original.Color,
            radius: original.Radius,
            opacity: original.Opacity,
            strokeColor: original.StrokeColor,
            strokeWidth: original.StrokeWidth,
            minZoom: 5,
            maxZoom: 10,
            beforeLayerId: original.BeforeLayerId,
            layerGroup: original.LayerGroup,
            beforeLayerGroup: original.BeforeLayerGroup,
            afterLayerGroup: original.AfterLayerGroup,
            interactive: original.Interactive
        );

        // assert
        replacement.MinZoom.Should().Be(5);
        replacement.MaxZoom.Should().Be(10);
    }

    [Test]
    public void Should_keep_cluster_layer_zoom_bounds_immutable_after_construction()
    {
        // arrange
        var layer = new ClusterSymbolLayerDefinition("cluster", minZoom: 2, maxZoom: 4);
        var minZoomProperty = typeof(ClusterLayerDefinition).GetProperty(nameof(ClusterLayerDefinition.MinZoom));
        var maxZoomProperty = typeof(ClusterLayerDefinition).GetProperty(nameof(ClusterLayerDefinition.MaxZoom));

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
    public void Should_create_cluster_circle_with_constructor_parameters()
    {
        // arrange
        var idSuffix = "cluster";

        // act
        var layer = new ClusterCircleLayerDefinition(idSuffix, color: "#2563eb", radius: 24, minZoom: 4, maxZoom: 10);

        // assert
        layer.IdSuffix.Should().Be("cluster");
        layer.Color.Should().NotBeNull();
        layer.Radius.Should().NotBeNull();
        layer.MinZoom.Should().Be(4);
        layer.MaxZoom.Should().Be(10);
        layer.Interactive.Should().BeTrue();
    }

    [Test]
    public void Should_create_cluster_symbol_with_constructor_parameters()
    {
        // arrange
        var idSuffix = "cluster-count";

        // act
        var layer = new ClusterSymbolLayerDefinition(
            idSuffix,
            textField: Expr.Get("point_count_abbreviated"),
            textSize: 14,
            textColor: "#ffffff",
            minZoom: 4,
            maxZoom: 10
        );

        // assert
        layer.IdSuffix.Should().Be("cluster-count");
        layer.TextField.Should().NotBeNull();
        layer.TextSize.Should().NotBeNull();
        layer.TextColor.Should().NotBeNull();
        layer.MinZoom.Should().Be(4);
        layer.MaxZoom.Should().Be(10);
        layer.Interactive.Should().BeTrue();
    }
}
