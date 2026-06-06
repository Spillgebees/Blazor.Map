using System.Reflection;
using AwesomeAssertions;
using Spillgebees.Blazor.Map;
using Spillgebees.Blazor.Map.Docs.Samples;

namespace Spillgebees.Blazor.Map.Tests.Samples;

public class GeoJsonClusteringExampleTests : BunitContext
{
    public GeoJsonClusteringExampleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Test]
    public void Should_render_clustered_incident_source_with_custom_cluster_and_point_layers()
    {
        // arrange
        var cut = Render<GeoJsonClusteringExample>();

        // act
        var source = cut.FindComponent<GeoJsonSource>().Instance;

        // assert
        source.Id.Should().Be("incidents");
        source.ClusterOptions.Should().NotBeNull();
        source.ClusterOptions!.Enabled.Should().BeTrue();
        source.ClusterOptions.Properties.Should().ContainKey("maxSeverity");
        source.ClusterOptions.LayerSet.Layers.Should().HaveCount(3);
        source.Layers.Should().HaveCount(2);
        CountFeatures(source.Data).Should().Be(10);
        cut.FindComponents<NavigationMapControl>().Should().HaveCount(1);
    }

    [Test]
    public void Should_not_use_legacy_geojson_cluster_parameters_for_docs_example()
    {
        // arrange
        var cut = Render<GeoJsonClusteringExample>();

        // act
        var source = cut.FindComponent<GeoJsonSource>().Instance;

        // assert
        source.Cluster.Should().BeFalse();
        source.ClusterProperties.Should().BeNull();
        source.ClusterOptions.Should().NotBeNull();
    }

    [Test]
    public void Should_convert_numeric_severity_to_string_for_unclustered_labels()
    {
        // arrange
        var cut = Render<GeoJsonClusteringExample>();

        // act
        var source = cut.FindComponent<GeoJsonSource>().Instance;
        var labelLayer = source
            .Layers!.OfType<SymbolLayerDefinition>()
            .Single(layer => layer.IdSuffix == "unclustered-labels");
        var textFieldExpression = GetTextFieldExpression(labelLayer);

        // assert
        textFieldExpression.Should().BeEquivalentTo(new object[] { "to-string", new object[] { "get", "severity" } });
    }

    private static int CountFeatures(object? data)
    {
        // arrange
        data.Should().NotBeNull();

        // act
        var features =
            data!.GetType().GetProperty("features", BindingFlags.Instance | BindingFlags.Public)!.GetValue(data)
            as object[];

        // assert
        features.Should().NotBeNull();
        return features!.Length;
    }

    private static object[] GetTextFieldExpression(SymbolLayerDefinition symbolLayer)
    {
        // arrange
        symbolLayer.TextField.Should().NotBeNull();

        // act
        var expression =
            symbolLayer
                .TextField!.Value.GetType()
                .GetProperty("Expression", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(symbolLayer.TextField.Value) as object[];

        // assert
        expression.Should().NotBeNull();
        return expression!;
    }
}
