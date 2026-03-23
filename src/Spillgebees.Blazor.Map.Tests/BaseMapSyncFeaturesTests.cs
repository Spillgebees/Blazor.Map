using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Tests;

public class BaseMapSyncFeaturesTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string SyncFeaturesIdentifier = "Spillgebees.Map.mapFunctions.syncFeatures";
    private const string GetProtocolVersionIdentifier = "Spillgebees.Map.getProtocolVersion";

    /// <summary>
    /// Timeout in milliseconds for tests to prevent hanging.
    /// </summary>
    private const int TestTimeoutMs = 5000;

    public BaseMapSyncFeaturesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.Setup<int>(GetProtocolVersionIdentifier).SetResult(8);
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(SyncFeaturesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_invoke_syncFeatures_when_new_markers_are_added(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>(p => p.Add(c => c.Markers, []));
        await cut.Instance.OnMapInitializedAsync();

        // act
        var newMarkers = new List<Marker> { new("marker-1", new Coordinate(49.6, 6.1), "Test Marker") };
        cut.Render(p => p.Add(c => c.Markers, newMarkers));

        // assert
        JSInterop.VerifyInvoke(SyncFeaturesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_invoke_syncFeatures_when_markers_are_removed(CancellationToken cancellationToken)
    {
        // arrange
        var initialMarkers = new List<Marker> { new("marker-1", new Coordinate(49.6, 6.1), "Test Marker") };
        var cut = Render<SgbMap>(p => p.Add(c => c.Markers, initialMarkers));
        await cut.Instance.OnMapInitializedAsync();

        // act
        cut.Render(p => p.Add(c => c.Markers, []));

        // assert
        JSInterop.VerifyInvoke(SyncFeaturesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_invoke_syncFeatures_when_markers_change(CancellationToken cancellationToken)
    {
        // arrange
        var initialMarkers = new List<Marker> { new("marker-1", new Coordinate(49.6, 6.1), "Original Title") };
        var cut = Render<SgbMap>(p => p.Add(c => c.Markers, initialMarkers));
        await cut.Instance.OnMapInitializedAsync();

        // act
        var updatedMarkers = new List<Marker> { new("marker-1", new Coordinate(50.0, 7.0), "Updated Title") };
        cut.Render(p => p.Add(c => c.Markers, updatedMarkers));

        // assert
        JSInterop.VerifyInvoke(SyncFeaturesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_invoke_syncFeatures_when_markers_unchanged(CancellationToken cancellationToken)
    {
        // arrange
        var initialMarkers = new List<Marker> { new("marker-1", new Coordinate(49.6, 6.1), "Test Marker") };
        var cut = Render<SgbMap>(p => p.Add(c => c.Markers, initialMarkers));
        await cut.Instance.OnMapInitializedAsync();

        // act
        var sameMarkers = new List<Marker> { new("marker-1", new Coordinate(49.6, 6.1), "Test Marker") };
        cut.Render(p => p.Add(c => c.Markers, sameMarkers));

        // assert
        JSInterop.VerifyNotInvoke(SyncFeaturesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_invoke_syncFeatures_when_markers_are_swapped(CancellationToken cancellationToken)
    {
        // arrange
        var initialMarkers = new List<Marker> { new("marker-1", new Coordinate(49.6, 6.1), "First Marker") };
        var cut = Render<SgbMap>(p => p.Add(c => c.Markers, initialMarkers));
        await cut.Instance.OnMapInitializedAsync();

        // act
        var replacementMarkers = new List<Marker> { new("marker-2", new Coordinate(48.8, 2.3), "Second Marker") };
        cut.Render(p => p.Add(c => c.Markers, replacementMarkers));

        // assert
        JSInterop.VerifyInvoke(SyncFeaturesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_invoke_syncFeatures_before_initialization(CancellationToken cancellationToken)
    {
        // arrange
        var initialMarkers = new List<Marker> { new("marker-1", new Coordinate(49.6, 6.1), "Test Marker") };
        var cut = Render<SgbMap>(p => p.Add(c => c.Markers, initialMarkers));

        // act — re-render with different markers WITHOUT calling OnMapInitializedAsync
        var newMarkers = new List<Marker> { new("marker-2", new Coordinate(48.8, 2.3), "New Marker") };
        cut.Render(p => p.Add(c => c.Markers, newMarkers));

        // assert
        JSInterop.VerifyNotInvoke(SyncFeaturesIdentifier);
    }
}
