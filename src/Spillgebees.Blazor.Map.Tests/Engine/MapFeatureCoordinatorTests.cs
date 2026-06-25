using AwesomeAssertions;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Engine;
using System.Text.Json;

namespace Spillgebees.Blazor.Map.Tests.Engine;

/// <summary>
/// The shapes flush path runs on every render of the host component, so value-identical
/// payloads (lists rebuilt per render, e.g. after every moveend) must not cross the
/// wire: each redundant setSourceData costs a JS-side JSON.parse plus a full MapLibre
/// source re-tile, which degrades pan smoothness.
/// </summary>
public class MapFeatureCoordinatorTests
{
    private const string ApplyOpsIdentifier = "Spillgebees.Engine.applyOps";
    private const string SetSourceDataIdentifier = "Spillgebees.Engine.setSourceData";

    [Test]
    public async Task Should_not_repush_circles_when_a_rebuilt_list_is_value_identical()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        coordinator.SetCircles("owner", [BuildCircle("c1"), BuildCircle("c2")]);
        var callsAfterFirstFlush = CountSourceDataCalls(js);

        // act: a fresh list with freshly constructed, value-equal records
        coordinator.SetCircles("owner", [BuildCircle("c1"), BuildCircle("c2")]);

        // assert
        callsAfterFirstFlush.Should().Be(1);
        CountSourceDataCalls(js).Should().Be(1);
    }

    [Test]
    public async Task Should_push_circles_when_a_rebuilt_list_actually_changed()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        coordinator.SetCircles("owner", [BuildCircle("c1")]);

        // act
        coordinator.SetCircles("owner", [BuildCircle("c1", latitude: 49.7)]);

        // assert
        CountSourceDataCalls(js).Should().Be(2);
    }

    [Test]
    public async Task Should_not_repush_polylines_when_a_rebuilt_list_is_value_identical()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        coordinator.SetPolylines("owner", [BuildPolyline("l1")]);
        var callsAfterFirstFlush = CountSourceDataCalls(js);

        // act
        coordinator.SetPolylines("owner", [BuildPolyline("l1")]);

        // assert
        callsAfterFirstFlush.Should().Be(1);
        CountSourceDataCalls(js).Should().Be(1);
    }

    [Test]
    public async Task Should_push_polylines_when_coordinates_changed()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        coordinator.SetPolylines("owner", [BuildPolyline("l1")]);

        // act
        coordinator.SetPolylines("owner", [BuildPolyline("l1", latitudeOffset: 0.01)]);

        // assert
        CountSourceDataCalls(js).Should().Be(2);
    }

    [Test]
    public async Task Should_sync_recreated_parameter_lists_without_repushing()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        coordinator.SyncParameters(markers: null, circles: [BuildCircle("c1")], polylines: [BuildPolyline("l1")]);
        var callsAfterFirstSync = CountSourceDataCalls(js);

        // act: every render hands over freshly allocated, value-identical lists
        coordinator.SyncParameters(markers: null, circles: [BuildCircle("c1")], polylines: [BuildPolyline("l1")]);

        // assert
        callsAfterFirstSync.Should().Be(2);
        CountSourceDataCalls(js).Should().Be(2);
    }

    [Test]
    public async Task Should_sync_top_level_features_in_polyline_circle_marker_order()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();

        // act
        coordinator.SyncParameters(
            markers: [BuildMarker("m1")],
            circles: [BuildCircle("c1")],
            polylines: [BuildPolyline("p1")]
        );
        await Task.Yield();

        // assert
        DescribeAppliedOps(js)
            .Should()
            .Equal(
                "source.add:sgb-polylines-source",
                "layer.add:sgb-polylines-layer",
                "source.add:sgb-circles-source",
                "layer.add:sgb-circles-layer",
                "marker.set:m1"
            );
        SetSourceDataSources(js).Should().Equal("sgb-polylines-source", "sgb-circles-source");
    }

    [Test]
    public async Task Should_keep_polylines_below_circles_when_polylines_are_added_after_circles()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();

        // act
        coordinator.SetCircles("owner", [BuildCircle("c1")]);
        coordinator.SetPolylines("owner", [BuildPolyline("p1")]);

        // assert
        DescribeAppliedOps(js)
            .Should()
            .ContainInOrder("layer.add:sgb-polylines-layer", "layer.add:sgb-circles-layer");
    }

    [Test]
    public async Task Should_push_the_emptied_collection_when_an_owner_is_removed()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        coordinator.SetCircles("owner", [BuildCircle("c1")]);

        // act
        coordinator.RemoveOwner("owner");

        // assert: circles flush again (now empty); polylines never had content to flush
        CountSourceDataCalls(js).Should().Be(2);
    }

    private static async Task<(MapFeatureCoordinator Coordinator, RecordingJsRuntime Js)> CreateReadyCoordinatorAsync()
    {
        var js = new RecordingJsRuntime();
        var channel = new MapEngineChannel(js);
        channel.Attach(default);
        await channel.MarkReadyAsync();
        return (new MapFeatureCoordinator(channel), js);
    }

    private static Circle BuildCircle(string id, double latitude = 49.6117, double longitude = 6.1319) =>
        new(id, new Coordinate(latitude, longitude), Radius: 4, Color: "#2563eb");

    private static Marker BuildMarker(string id, double latitude = 49.6117, double longitude = 6.1319) =>
        new(id, new Coordinate(latitude, longitude), Title: $"marker-{id}");

    private static Polyline BuildPolyline(string id, double latitudeOffset = 0) =>
        new(
            id,
            [new Coordinate(49.61 + latitudeOffset, 6.13), new Coordinate(49.62 + latitudeOffset, 6.14)],
            Color: "#0ea5e9",
            Width: 2
        );

    private static int CountSourceDataCalls(RecordingJsRuntime js) =>
        js.Identifiers.Count(identifier => identifier == SetSourceDataIdentifier);

    private static IReadOnlyList<string> DescribeAppliedOps(RecordingJsRuntime js)
    {
        var described = new List<string>();
        foreach (var invocation in js.Invocations.Where(call => call.Identifier == ApplyOpsIdentifier))
        {
            using var document = JsonDocument.Parse((string)invocation.Args[1]!);
            described.AddRange(
                document.RootElement.EnumerateArray().Select(op =>
                {
                    var opName = op.GetProperty("op").GetString();
                    return opName switch
                    {
                        "marker.set" => $"marker.set:{op.GetProperty("marker").GetProperty("id").GetString()}",
                        _ => $"{opName}:{op.GetProperty("id").GetString()}",
                    };
                })
            );
        }

        return described;
    }

    private static IReadOnlyList<string> SetSourceDataSources(RecordingJsRuntime js) =>
        js.Invocations
            .Where(call => call.Identifier == SetSourceDataIdentifier)
            .Select(call => (string)call.Args[1]!)
            .ToArray();

    private sealed record Invocation(string Identifier, object?[] Args);

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public List<string> Identifiers { get; } = [];
        public List<Invocation> Invocations { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Identifiers.Add(identifier);
            Invocations.Add(new Invocation(identifier, args ?? []));
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args
        ) => InvokeAsync<TValue>(identifier, args);
    }
}
