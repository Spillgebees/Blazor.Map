using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;

namespace Spillgebees.Blazor.Map.Tests;

public class PublicApiCleanupTests
{
    [Test]
    public void Should_expose_tracked_entity_definition_public_types()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var expectedTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityLayerDefinition`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.ITrackedEntityLayerDefinition",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityIdOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityVisualOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityVisualDefaults",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntitySymbolOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityDecorationOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityClusterOptions",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityBehaviorOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityInteractionOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityCallbacks`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityMaterializer",
        };

        // act
        var resolvedTypes = expectedTypeNames.Select(assembly.GetType);

        // assert
        resolvedTypes.Should().AllSatisfy(type => type.Should().NotBeNull());
    }

    [Test]
    public void Should_expose_tracked_entity_layer_definition_public_type()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;

        // act
        var trackedEntityLayerDefinitionType = assembly.GetType(
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityLayerDefinition`1"
        );

        // assert
        trackedEntityLayerDefinitionType.Should().NotBeNull();
    }

    [Test]
    public void Should_not_expose_legacy_tracked_data_public_types()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var legacyTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataLayer`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.ITrackedDataLayer",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataIdOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataVisualOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataVisualDefaults",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataSymbolOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataDecorationOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataClusterOptions",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataBehaviorOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataInteractionOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataCallbacks`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataEntityMaterializer",
        };

        // act
        var resolvedTypes = legacyTypeNames.Select(assembly.GetType);

        // assert
        resolvedTypes.Should().AllSatisfy(type => type.Should().BeNull());
    }

    [Test]
    public void Should_expose_clean_legend_and_image_model_names()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var expectedTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.Models.Legends.MapLegend",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendSection",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendItem",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendTarget",
            "Spillgebees.Blazor.Map.Models.MapImage",
        };

        // act
        var resolvedTypes = expectedTypeNames.Select(assembly.GetType);

        // assert
        resolvedTypes.Should().AllSatisfy(type => type.Should().NotBeNull());
    }

    [Test]
    public void Should_not_expose_legacy_legend_and_image_model_names()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var legacyTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendDefinition",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendSectionDefinition",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendItemDefinition",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendTargetDefinition",
            "Spillgebees.Blazor.Map.Models.MapImageDefinition",
        };

        // act
        var resolvedTypes = legacyTypeNames.Select(assembly.GetType);

        // assert
        resolvedTypes.Should().AllSatisfy(type => type.Should().BeNull());
    }

    [Test]
    public void Should_expose_map_image_id_and_sdf_property_names()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var mapImageType = assembly.GetType("Spillgebees.Blazor.Map.Models.MapImage");

        // act
        var publicPropertyNames = mapImageType
            ?.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name);

        // assert
        publicPropertyNames.Should().BeEquivalentTo(["Id", "Url", "Width", "Height", "PixelRatio", "IsSdf"]);
    }

    [Test]
    public void Should_not_expose_legacy_map_legend_component_type()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;

        // act
        var legacyMapLegendType = assembly.GetType("Spillgebees.Blazor.Map.Components.MapLegend");

        // assert
        legacyMapLegendType.Should().BeNull();
    }

    [Test]
    public void Should_not_expose_legacy_tracked_data_source_component()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;

        // act
        var trackedDataSourceType = assembly.GetType("Spillgebees.Blazor.Map.Components.Layers.TrackedDataSource`1");

        // assert
        trackedDataSourceType.Should().BeNull();
    }

    [Test]
    public void Should_not_expose_legacy_tracked_data_layers_map_parameter()
    {
        // arrange
        var mapType = typeof(SgbMap);

        // act
        var trackedDataLayersProperty = mapType.GetProperty("TrackedDataLayers");

        // assert
        trackedDataLayersProperty.Should().BeNull();
    }

    [Test]
    public void Should_expose_tracked_entity_layer_component()
    {
        // arrange
        var trackedEntityLayerType = GetTrackedEntityLayerType();

        // act
        var publicPropertyNames = trackedEntityLayerType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name);

        // assert
        publicPropertyNames.Should().BeEquivalentTo(["Layer"]);
    }

    [Test]
    public void Should_not_expose_legacy_tracked_entity_layer_parameter_surface()
    {
        // arrange
        var trackedEntityLayerType = GetTrackedEntityLayerType();
        var legacyPropertyNames = new[]
        {
            "SourceId",
            "Items",
            "Id",
            "Symbol",
            "Decorations",
            "Cluster",
            "Interaction",
            "Animation",
            "Visible",
            "PrimaryIconOpacity",
            "OnItemClick",
            "OnItemMouseEnter",
            "OnItemMouseLeave",
            "MaxZoom",
            "Attribution",
            "Stack",
            "BeforeStack",
            "AfterStack",
        };

        // act
        var publicPropertyNames = trackedEntityLayerType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        // assert
        foreach (var legacyPropertyName in legacyPropertyNames)
        {
            publicPropertyNames.Should().NotContain(legacyPropertyName);
        }
    }

    private static Type GetTrackedEntityLayerType()
    {
        var assembly = typeof(SgbMap).Assembly;
        var trackedEntityLayerType = assembly.GetType("Spillgebees.Blazor.Map.Components.Layers.TrackedEntityLayer`1");

        trackedEntityLayerType.Should().NotBeNull();
        return trackedEntityLayerType!;
    }
}
