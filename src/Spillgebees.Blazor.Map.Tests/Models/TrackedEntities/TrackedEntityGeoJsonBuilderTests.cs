using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Tests.Models.TrackedEntities;

public class TrackedEntityGeoJsonBuilderTests
{
    [Test]
    public void Should_build_primary_and_decoration_feature_collections_with_stable_ids_and_properties()
    {
        // arrange
        var entities = new List<TrackedEntity<string>>
        {
            new(
                "train-42",
                new Coordinate(49.6116, 6.1319),
                new TrackedEntitySymbol("train-red", 1.2, 18),
                "#ff3344",
                new TrackedEntityHoverIntent(1.3, true),
                25,
                [new TrackedEntityDecoration("route", text: "Luxembourg - Trier", offset: new PixelPoint(0, 1.4))],
                "express",
                new Dictionary<string, object?> { ["service"] = "RE 1" }
            ),
        };

        // act
        var primary = TrackedEntityGeoJsonBuilder.BuildPrimaryFeatureCollection(entities);
        var decorations = TrackedEntityGeoJsonBuilder.BuildDecorationFeatureCollection(entities);

        // assert
        var primaryFeatures = GetFeatures(primary);
        primaryFeatures.Should().HaveCount(1);

        var primaryFeature = primaryFeatures[0];
        primaryFeature["id"].Should().Be("train-42");

        var primaryProperties = GetProperties(primaryFeature);
        primaryProperties[TrackedEntityFeatureProperties.Kind]
            .Should()
            .Be(TrackedEntityFeatureKind.Primary.ToMapLibreValue());
        primaryProperties[TrackedEntityFeatureProperties.EntityId].Should().Be("train-42");
        primaryProperties[TrackedEntityFeatureProperties.Color].Should().Be("#ff3344");
        primaryProperties[TrackedEntityFeatureProperties.IconImage].Should().Be("train-red");
        primaryProperties[TrackedEntityFeatureProperties.IconSize].Should().Be(1.2);
        primaryProperties[TrackedEntityFeatureProperties.IconRotation].Should().Be(18d);
        primaryProperties[TrackedEntityFeatureProperties.HoverScale].Should().Be(1.3);
        primaryProperties[TrackedEntityFeatureProperties.HoverRaise].Should().Be(true);
        primaryProperties[TrackedEntityFeatureProperties.RenderOrder].Should().Be(25d);
        primaryProperties[TrackedEntityFeatureProperties.Item].Should().Be("express");
        primaryProperties["service"].Should().Be("RE 1");

        var decorationFeatures = GetFeatures(decorations);
        decorationFeatures.Should().HaveCount(1);

        var decorationFeature = decorationFeatures[0];
        decorationFeature["id"].Should().Be("train-42::route");

        var decorationProperties = GetProperties(decorationFeature);
        decorationProperties[TrackedEntityFeatureProperties.Kind]
            .Should()
            .Be(TrackedEntityFeatureKind.Decoration.ToMapLibreValue());
        decorationProperties[TrackedEntityFeatureProperties.EntityId].Should().Be("train-42");
        decorationProperties[TrackedEntityFeatureProperties.DecorationId].Should().Be("route");
        decorationProperties[TrackedEntityFeatureProperties.Text].Should().Be("Luxembourg - Trier");
        decorationProperties[TrackedEntityFeatureProperties.DisplayMode]
            .Should()
            .Be(TrackedEntityDecorationDisplayMode.Always.ToMapLibreValue());
        decorationProperties[TrackedEntityFeatureProperties.Offset].Should().BeEquivalentTo(new[] { 0d, 1.4d });
    }

    [Test]
    public void Should_throw_when_custom_properties_collide_with_reserved_tracked_entity_properties()
    {
        // arrange
        var entities = new List<TrackedEntity<string>>
        {
            new(
                "entity-1",
                new Coordinate(49.6, 6.1),
                new TrackedEntitySymbol("marker"),
                properties: new Dictionary<string, object?> { [TrackedEntityFeatureProperties.EntityId] = "collision" }
            ),
        };

        // act
        var act = () => TrackedEntityGeoJsonBuilder.BuildPrimaryFeatureCollection(entities);

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{TrackedEntityFeatureProperties.EntityId}*");
    }

    [Test]
    public void Should_throw_when_decoration_has_neither_text_nor_icon()
    {
        // arrange
        var entities = new List<TrackedEntity<string>>
        {
            new(
                "entity-1",
                new Coordinate(49.6, 6.1),
                new TrackedEntitySymbol("marker"),
                decorations: [new TrackedEntityDecoration("empty")]
            ),
        };

        // act
        var act = () => TrackedEntityGeoJsonBuilder.BuildDecorationFeatureCollection(entities);

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*text*icon*");
    }

    [Test]
    public void Should_defensively_copy_decorations_from_input_collection()
    {
        // arrange
        var decorations = new List<TrackedEntityDecoration> { new("label", text: "Vehicle 1") };

        // act
        var entity = new TrackedEntity<string>(
            "entity-1",
            new Coordinate(49.6, 6.1),
            new TrackedEntitySymbol("marker"),
            decorations: decorations
        );
        decorations.Add(new TrackedEntityDecoration("status", text: "Delayed"));

        // assert
        entity.Decorations.Should().HaveCount(1);
        entity.Decorations[0].Id.Should().Be("label");
    }

    [Test]
    public void Should_include_decoration_rotation_property_when_defined()
    {
        // arrange
        var entities = new List<TrackedEntity<string>>
        {
            new(
                "train-42",
                new Coordinate(49.6116, 6.1319),
                new TrackedEntitySymbol("train-red", 1.2, 18),
                decorations: [new TrackedEntityDecoration("route", text: "Luxembourg - Trier", rotation: 34)]
            ),
        };

        // act
        var decorations = TrackedEntityGeoJsonBuilder.BuildDecorationFeatureCollection(entities);

        // assert
        var decorationFeature = GetFeatures(decorations).Single();
        var decorationProperties = GetProperties(decorationFeature);
        decorationProperties.Should().ContainKey(TrackedEntityFeatureProperties.IconRotation);
        decorationProperties[TrackedEntityFeatureProperties.IconRotation].Should().Be(34d);
    }

    private static List<Dictionary<string, object?>> GetFeatures(IReadOnlyDictionary<string, object?> featureCollection)
    {
        return ((IEnumerable<object>)featureCollection["features"]!).Cast<Dictionary<string, object?>>().ToList();
    }

    private static Dictionary<string, object?> GetProperties(Dictionary<string, object?> feature)
    {
        return (Dictionary<string, object?>)feature["properties"]!;
    }
}
