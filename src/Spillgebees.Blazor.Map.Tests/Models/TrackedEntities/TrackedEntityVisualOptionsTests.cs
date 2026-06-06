using AwesomeAssertions;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Tests.Models.TrackedEntities;

public class TrackedEntityVisualOptionsTests
{
    [Test]
    public void Should_expose_shared_cluster_options_from_source_options()
    {
        // arrange
        var cluster = ClusterOptions.Create(radius: 64, maxZoom: 12, minPoints: 3);
        var source = new TrackedEntitySourceOptions(cluster, ClusterClickBehavior.None);

        // act
        var options = CreateVisualOptions(source);

        // assert
        options.Source.Should().Be(source);
        options.Cluster.Should().BeSameAs(cluster);
        options.Source.ClusterClickBehavior.Should().Be(ClusterClickBehavior.None);
    }

    [Test]
    public void Should_intentionally_adapt_legacy_cluster_options_to_source_options()
    {
        // arrange
        var properties = new Dictionary<string, object>
        {
            ["sum"] = new object[] { "+", new object[] { "get", "value" } },
        };
        var legacy = new TrackedEntityClusterOptions(
            Enabled: true,
            Radius: 72,
            MaxZoom: 10,
            MinPoints: 4,
            ClickBehavior: TrackedEntityClusterClickBehavior.None,
            Properties: properties
        );

        // act
        var options = new TrackedEntityVisualOptions<TestVehicle>(
            CreateSymbolOptions(),
            [],
            legacy,
            Animation: null,
            Visible: true,
            PrimaryIconOpacity: null
        );

        // assert
        options.Cluster.Enabled.Should().BeTrue();
        options.Cluster.Radius.Should().Be(72);
        options.Cluster.MaxZoom.Should().Be(10);
        options.Cluster.MinPoints.Should().Be(4);
        options.Cluster.Properties.Should().BeSameAs(properties);
        options.Source.ClusterClickBehavior.Should().Be(ClusterClickBehavior.None);
    }

    private static TrackedEntityVisualOptions<TestVehicle> CreateVisualOptions(TrackedEntitySourceOptions source) =>
        new(CreateSymbolOptions(), [], source, Animation: null, Visible: true, PrimaryIconOpacity: null);

    private static TrackedEntitySymbolOptions<TestVehicle> CreateSymbolOptions() =>
        new(vehicle => vehicle.Position, vehicle => vehicle.IconImage);

    private sealed record TestVehicle(Coordinate Position, string IconImage);
}
