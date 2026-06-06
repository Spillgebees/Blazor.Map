using AwesomeAssertions;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Tests.Runtime.Scene;

public class MapSceneRegistryTests
{
    [Test]
    public async Task Should_preserve_unrelated_changes_when_batch_rolls_back()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        var failedBatch = registry.CreateBatchBuilder();
        failedBatch.AddSource(Source("failed"));
        var successfulBatch = registry.CreateBatchBuilder();
        successfulBatch.AddSource(Source("unrelated"));

        // act
        failedBatch.RestoreRegistrySnapshot();

        // assert
        var state = registry.CaptureState();
        state.Sources.Should().NotContainKey("failed");
        state.Sources.Should().ContainKey("unrelated");
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_not_overwrite_newer_same_key_changes_when_batch_rolls_back()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        registry.SetSource(Source("same", "initial"));
        var failedBatch = registry.CreateBatchBuilder();
        failedBatch.SetSourceData("same", "failed", null);
        var successfulBatch = registry.CreateBatchBuilder();
        successfulBatch.SetSourceData("same", "newer", null);

        // act
        failedBatch.RestoreRegistrySnapshot();

        // assert
        registry.CaptureState().Sources["same"].SourceSpec["data"].Should().Be("newer");
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_deep_clone_nested_source_specs()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        var coordinates = new object?[] { 1d, 2d };
        var features = new List<object?> { new Dictionary<string, object?> { ["coordinates"] = coordinates } };
        var sourceSpec = new Dictionary<string, object?>
        {
            ["type"] = "geojson",
            ["data"] = new Dictionary<string, object?> { ["features"] = features },
        };

        // act
        registry.SetSource(new MapSourceDescriptor("source", sourceSpec));
        coordinates[0] = 99d;
        features.Add("mutated");
        var storedSourceSpec = registry.CaptureState().Sources["source"].SourceSpec;

        // assert
        var storedData = storedSourceSpec["data"]
            .Should()
            .BeAssignableTo<IReadOnlyDictionary<string, object?>>()
            .Subject;
        var storedFeatures = storedData["features"].Should().BeAssignableTo<IReadOnlyList<object?>>().Subject;
        storedFeatures.Should().HaveCount(1);
        var storedFeature = storedFeatures[0].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        storedFeature["coordinates"].Should().BeEquivalentTo(new object?[] { 1d, 2d });
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_deep_clone_nested_layer_specs()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        var colorStops = new object?[] { new object?[] { 0d, "red" } };
        var paint = new Dictionary<string, object?> { ["circle-color"] = colorStops };
        var layerSpec = new Dictionary<string, object?>
        {
            ["id"] = "layer",
            ["type"] = "circle",
            ["paint"] = paint,
        };

        // act
        registry.SetLayer(
            new MapLayerDescriptor("layer", layerSpec, null, new LayerOrderRegistration(0, null, null, null))
        );
        ((object?[])colorStops[0]!)[1] = "blue";
        paint["circle-radius"] = 12d;
        var storedLayerSpec = registry.CaptureState().Layers["layer"].LayerSpec;

        // assert
        var storedPaint = storedLayerSpec["paint"]
            .Should()
            .BeAssignableTo<IReadOnlyDictionary<string, object?>>()
            .Subject;
        storedPaint.Should().NotContainKey("circle-radius");
        storedPaint["circle-color"].Should().BeEquivalentTo(new object?[] { new object?[] { 0d, "red" } });
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_deep_clone_typed_jagged_arrays_in_source_specs()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        var coordinates = new double[][] { [1d, 2d], [3d, 4d] };
        var labels = new string[][] { ["alpha", "beta"], ["gamma", "delta"] };
        var sourceSpec = new Dictionary<string, object?>
        {
            ["type"] = "geojson",
            ["coordinates"] = coordinates,
            ["labels"] = labels,
        };

        // act
        registry.SetSource(new MapSourceDescriptor("source", sourceSpec));
        coordinates[0][0] = 99d;
        labels[1][0] = "mutated";
        var storedSourceSpec = registry.CaptureState().Sources["source"].SourceSpec;

        // assert
        var storedCoordinates = storedSourceSpec["coordinates"].Should().BeAssignableTo<double[][]>().Subject;
        var storedLabels = storedSourceSpec["labels"].Should().BeAssignableTo<string[][]>().Subject;
        storedCoordinates.Should().BeEquivalentTo(new double[][] { [1d, 2d], [3d, 4d] });
        storedLabels.Should().BeEquivalentTo(new string[][] { ["alpha", "beta"], ["gamma", "delta"] });
        storedCoordinates[0].Should().NotBeSameAs(coordinates[0]);
        storedLabels[1].Should().NotBeSameAs(labels[1]);
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_deep_clone_mutable_data_when_setting_source_data()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        registry.SetSource(Source("source"));
        var coordinates = new double[][] { [1d, 2d], [3d, 4d] };
        var features = new List<object?>
        {
            new Dictionary<string, object?>
            {
                ["coordinates"] = coordinates,
                ["properties"] = new object?[] { "alpha" },
            },
        };
        var data = new Dictionary<string, object?> { ["type"] = "FeatureCollection", ["features"] = features };

        // act
        registry.SetSourceData("source", data);
        coordinates[0][0] = 99d;
        features.Add("mutated");
        ((object?[])((Dictionary<string, object?>)features[0]!)["properties"]!)[0] = "mutated";
        var storedSourceSpec = registry.CaptureState().Sources["source"].SourceSpec;

        // assert
        var storedData = storedSourceSpec["data"]
            .Should()
            .BeAssignableTo<IReadOnlyDictionary<string, object?>>()
            .Subject;
        var storedFeatures = storedData["features"].Should().BeAssignableTo<IReadOnlyList<object?>>().Subject;
        storedFeatures.Should().HaveCount(1);
        var storedFeature = storedFeatures[0].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        storedFeature["coordinates"].Should().BeEquivalentTo(new double[][] { [1d, 2d], [3d, 4d] });
        storedFeature["properties"].Should().BeEquivalentTo(new object?[] { "alpha" });
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_not_restore_layers_or_events_from_failed_source_removal_when_source_has_newer_change()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        registry.SetSource(Source("source", "initial"));
        registry.SetLayer(Layer("layer", "source"));
        registry.SetLayerEvents(new LayerEventDescriptor("layer", new object(), true, true, true));
        var failedBatch = registry.CreateBatchBuilder();
        failedBatch.RemoveSource("source");
        var successfulBatch = registry.CreateBatchBuilder();
        successfulBatch.AddSource(Source("source", "newer"));

        // act
        failedBatch.RestoreRegistrySnapshot();

        // assert
        var state = registry.CaptureState();
        state.Sources["source"].SourceSpec["data"].Should().Be("newer");
        state.Layers.Should().NotContainKey("layer");
        state.LayerEvents.Should().NotContainKey("layer");
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_not_restore_layer_events_when_layer_rollback_is_skipped_for_newer_same_layer_change()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        registry.SetSource(Source("source"));
        registry.SetLayer(Layer("layer", "source"));
        registry.SetLayerEvents(new LayerEventDescriptor("layer", new object(), true, true, true));
        var failedBatch = registry.CreateBatchBuilder();
        failedBatch.RemoveLayer("layer");
        var successfulBatch = registry.CreateBatchBuilder();
        successfulBatch.AddLayer(Layer("layer", "source"));

        // act
        failedBatch.RestoreRegistrySnapshot();

        // assert
        var state = registry.CaptureState();
        state.Layers.Should().ContainKey("layer");
        state.LayerEvents.Should().NotContainKey("layer");
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_deep_clone_mutable_paint_property_values()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        registry.SetLayer(Layer("layer", "source"));
        var stops = new object?[] { new object?[] { 0d, "red" } };

        // act
        registry.SetLayerPaintProperty("layer", "circle-color", stops);
        ((object?[])stops[0]!)[1] = "blue";
        var storedLayerSpec = registry.CaptureState().Layers["layer"].LayerSpec;

        // assert
        var storedPaint = storedLayerSpec["paint"]
            .Should()
            .BeAssignableTo<IReadOnlyDictionary<string, object?>>()
            .Subject;
        storedPaint["circle-color"].Should().BeEquivalentTo(new object?[] { new object?[] { 0d, "red" } });
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_deep_clone_mutable_layout_property_values()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        registry.SetLayer(Layer("layer", "source"));
        var visibility = new List<object?> { "visible" };

        // act
        registry.SetLayerLayoutProperty("layer", "visibility", visibility);
        visibility[0] = "none";
        var storedLayerSpec = registry.CaptureState().Layers["layer"].LayerSpec;

        // assert
        var storedLayout = storedLayerSpec["layout"]
            .Should()
            .BeAssignableTo<IReadOnlyDictionary<string, object?>>()
            .Subject;
        storedLayout["visibility"].Should().BeEquivalentTo(new object?[] { "visible" });
        await Task.CompletedTask;
    }

    [Test]
    public async Task Should_deep_clone_mutable_filter_values()
    {
        // arrange
        var registry = new MapSceneRegistry(new SgbMap());
        registry.SetLayer(Layer("layer", "source"));
        var filter = new object?[] { "==", new object?[] { "get", "kind" }, "park" };

        // act
        registry.SetLayerFilter("layer", filter);
        ((object?[])filter[1]!)[1] = "type";
        filter[2] = "water";
        var storedLayerSpec = registry.CaptureState().Layers["layer"].LayerSpec;

        // assert
        storedLayerSpec["filter"]
            .Should()
            .BeEquivalentTo(new object?[] { "==", new object?[] { "get", "kind" }, "park" });
        await Task.CompletedTask;
    }

    private static MapSourceDescriptor Source(string sourceId, object? data = null) =>
        new(sourceId, new Dictionary<string, object?> { ["type"] = "geojson", ["data"] = data });

    private static MapLayerDescriptor Layer(string layerId, string sourceId) =>
        new(
            layerId,
            new Dictionary<string, object?>
            {
                ["id"] = layerId,
                ["type"] = "circle",
                ["source"] = sourceId,
            },
            null,
            new LayerOrderRegistration(0, null, null, null)
        );
}
