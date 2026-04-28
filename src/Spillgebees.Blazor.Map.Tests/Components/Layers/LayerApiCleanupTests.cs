using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components.Layers;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class LayerApiCleanupTests
{
    [Test]
    public void Should_expose_renamed_layer_order_and_source_layer_properties()
    {
        // arrange
        var layerType = typeof(SymbolLayer);

        // act
        var publicPropertyNames = layerType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        // assert
        publicPropertyNames
            .Should()
            .Contain(["BeforeLayerId", "LayerGroup", "BeforeLayerGroup", "AfterLayerGroup", "SourceLayerId"]);
    }

    [Test]
    public void Should_not_expose_legacy_layer_order_and_source_layer_properties()
    {
        // arrange
        var layerType = typeof(SymbolLayer);

        // act
        var publicPropertyNames = layerType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        // assert
        publicPropertyNames.Should().NotContain(["BeforeId", "Stack", "BeforeStack", "AfterStack", "SourceLayer"]);
    }

    [Test]
    public void Should_keep_maplibre_source_layer_wire_key_with_renamed_property()
    {
        // arrange
        var layer = new SymbolLayer
        {
            Id = "labels",
            SourceId = "openmaptiles",
            SourceLayerId = "place",
        };

        // act
        var spec = layer.BuildLayerSpec();

        // assert
        spec.Should().ContainKey("source-layer");
        spec["source-layer"].Should().Be("place");
        spec.Should().NotContainKey("sourceLayer");
        spec.Should().NotContainKey("sourceLayerId");
    }

    [Test]
    public void Should_expose_renamed_layer_group_order_options()
    {
        // arrange
        var options = new MapLayerOrderOptions("labels", "roads", "water");

        // act
        var propertyNames = options
            .GetType()
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        // assert
        propertyNames.Should().Contain(["LayerGroup", "BeforeLayerGroup", "AfterLayerGroup"]);
        propertyNames.Should().NotContain(["Stack", "BeforeStack", "AfterStack"]);
    }
}
