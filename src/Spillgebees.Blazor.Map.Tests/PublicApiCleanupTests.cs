using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;

namespace Spillgebees.Blazor.Map.Tests;

public class PublicApiCleanupTests
{
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
    public void Should_not_expose_legacy_tracked_data_source_parameter_surface()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var trackedDataSourceType = assembly.GetType("Spillgebees.Blazor.Map.Components.Layers.TrackedDataSource`1");
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
        trackedDataSourceType.Should().NotBeNull();

        var publicPropertyNames = trackedDataSourceType!
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        // assert
        foreach (var legacyPropertyName in legacyPropertyNames)
        {
            publicPropertyNames.Should().NotContain(legacyPropertyName);
        }
    }
}
