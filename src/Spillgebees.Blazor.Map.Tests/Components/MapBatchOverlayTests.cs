using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class MapBatchOverlayTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string SyncFeaturesIdentifier = "Spillgebees.Map.mapFunctions.syncFeatures";

    public MapBatchOverlayTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(SyncFeaturesIdentifier);
    }

    [Test]
    public async Task Should_sync_markers_from_batch_overlay_component()
    {
        // arrange
        var items = new[]
        {
            new Station("lux", "Luxembourg", new Coordinate(49.599, 6.134)),
            new Station("bet", "Bettembourg", new Coordinate(49.518, 6.102)),
        };
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapMarkers<Station>>(markers =>
                    markers
                        .Add(m => m.Items, items)
                        .Add(m => m.IdSelector, station => station.Id)
                        .Add(m => m.PositionSelector, station => station.Position)
                        .Add(m => m.TitleSelector, station => station.Name)
                )
            )
        );
        await cut.Instance.OnMapInitializedAsync();
        var initialSyncCount = JSInterop.Invocations[SyncFeaturesIdentifier].Count;

        // act
        cut.Render(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapMarkers<Station>>(markers =>
                    markers
                        .Add(m => m.Items, items.Take(1).ToArray())
                        .Add(m => m.IdSelector, station => station.Id)
                        .Add(m => m.PositionSelector, station => station.Position)
                        .Add(m => m.TitleSelector, station => station.Name)
                )
            )
        );

        // assert
        JSInterop.Invocations[SyncFeaturesIdentifier].Count.Should().BeGreaterThan(initialSyncCount);
    }

    [Test]
    public async Task Should_sync_circles_with_empty_items_when_items_is_null()
    {
        // arrange
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapCircles<Station>>(circles =>
                    circles
                        .Add(c => c.Items, null!)
                        .Add(c => c.IdSelector, station => station.Id)
                        .Add(c => c.PositionSelector, station => station.Position)
                )
            )
        );

        // act
        await cut.Instance.OnMapInitializedAsync();

        // assert
        JSInterop.VerifyInvoke(CreateMapIdentifier);
    }

    [Test]
    public void Should_throw_when_batch_overlay_component_is_outside_map_overlays_section()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapMarkers<Station>>(markers =>
                    markers
                        .Add(m => m.Items, [])
                        .Add(m => m.IdSelector, station => station.Id)
                        .Add(m => m.PositionSelector, station => station.Position)
                )
            );

        // act & assert
        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("MapMarkers must be placed inside MapOverlays.");
    }

    public sealed record Station(string Id, string Name, Coordinate Position);
}
