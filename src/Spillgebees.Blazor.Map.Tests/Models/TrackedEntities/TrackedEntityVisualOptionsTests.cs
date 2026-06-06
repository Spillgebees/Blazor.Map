using AwesomeAssertions;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Tests.Models.TrackedEntities;

public class TrackedEntityVisualOptionsTests
{
    [Test]
    public void Should_expose_shared_cluster_options_from_source_options()
    {
        // arrange
        var cluster = ClusterOptions.Create(
            radius: 64,
            maxZoom: 12,
            minPoints: 3,
            clickBehavior: ClusterClickBehavior.None
        );
        var source = new TrackedEntitySourceOptions(cluster);

        // act
        var options = CreateVisualOptions(source);

        // assert
        options.Source.Should().Be(source);
        options.Cluster.Should().BeSameAs(cluster);
        options.Source.Cluster.ClickBehavior.Should().Be(ClusterClickBehavior.None);
    }

    [Test]
    public void Should_create_visual_options_with_cluster_click_behavior_as_single_source_of_truth()
    {
        // arrange
        var properties = new Dictionary<string, object>
        {
            ["sum"] = new object[] { "+", new object[] { "get", "value" } },
        };
        var cluster = ClusterOptions.Create(
            radius: 72,
            maxZoom: 10,
            minPoints: 4,
            properties: properties,
            clickBehavior: ClusterClickBehavior.None
        );

        // act
        var options = new TrackedEntityVisualOptions<TestVehicle>(
            CreateSymbolOptions(),
            [],
            cluster,
            Animation: null,
            Visible: true,
            PrimaryIconOpacity: null
        );

        // assert
        options.Cluster.Enabled.Should().BeTrue();
        options.Cluster.Radius.Should().Be(72);
        options.Cluster.MaxZoom.Should().Be(10);
        options.Cluster.MinPoints.Should().Be(4);
        options.Cluster.Properties.Should().NotBeSameAs(properties);
        options.Cluster.Properties.Should().Equal(properties);
        options.Cluster.ClickBehavior.Should().Be(ClusterClickBehavior.None);
    }

    [Test]
    public void Should_reject_null_source_options()
    {
        // arrange
        TrackedEntitySourceOptions source = null!;

        // act
        var act = () => CreateVisualOptions(source);

        // assert
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("Source");
    }

    private static TrackedEntityVisualOptions<TestVehicle> CreateVisualOptions(TrackedEntitySourceOptions source) =>
        new(CreateSymbolOptions(), [], source, Animation: null, Visible: true, PrimaryIconOpacity: null);

    private static TrackedEntitySymbolOptions<TestVehicle> CreateSymbolOptions() =>
        new(vehicle => vehicle.Position, vehicle => vehicle.IconImage);

    private sealed record TestVehicle(Coordinate Position, string IconImage);
}
