using System.Collections;
using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class MapSingleOverlayTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string SyncFeaturesIdentifier = "Spillgebees.Map.mapFunctions.syncFeatures";

    public MapSingleOverlayTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(SyncFeaturesIdentifier);
    }

    [Test]
    public void Should_throw_when_marker_is_outside_map()
    {
        // arrange
        var action = () =>
            Render<MapMarker>(parameters =>
                parameters
                    .Add(marker => marker.Id, "marker-1")
                    .Add(marker => marker.Position, new Coordinate(49.61, 6.13))
            );

        // act & assert
        action.Should().Throw<InvalidOperationException>().WithMessage("MapMarker must be placed inside SgbMap.");
    }

    [Test]
    public void Should_throw_when_circle_is_outside_map()
    {
        // arrange
        var action = () =>
            Render<MapCircle>(parameters =>
                parameters
                    .Add(circle => circle.Id, "circle-1")
                    .Add(circle => circle.Position, new Coordinate(49.61, 6.13))
            );

        // act & assert
        action.Should().Throw<InvalidOperationException>().WithMessage("MapCircle must be placed inside SgbMap.");
    }

    [Test]
    public void Should_throw_when_polyline_is_outside_map()
    {
        // arrange
        var action = () =>
            Render<MapPolyline>(parameters =>
                parameters
                    .Add(polyline => polyline.Id, "polyline-1")
                    .Add(polyline => polyline.Coordinates, [new Coordinate(49.61, 6.13), new Coordinate(49.62, 6.14)])
            );

        // act & assert
        action.Should().Throw<InvalidOperationException>().WithMessage("MapPolyline must be placed inside SgbMap.");
    }

    [Test]
    public void Should_throw_when_marker_is_outside_map_overlays_section()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapMarker>(marker =>
                    marker.Add(m => m.Id, "marker-1").Add(m => m.Position, new Coordinate(49.61, 6.13))
                )
            );

        // act & assert
        action.Should().Throw<InvalidOperationException>().WithMessage("MapMarker must be placed inside MapOverlays.");
    }

    [Test]
    public void Should_skip_marker_overlay_sync_when_rerender_is_unchanged()
    {
        // arrange
        var cut = RenderMarker("marker-1", new Coordinate(49.61, 6.13));
        var initialMarkers = GetSingleOverlayValue(cut.Instance, "_registeredOverlayMarkers");

        // act
        RenderMarker(cut, "marker-1", new Coordinate(49.61, 6.13));

        // assert
        GetSingleOverlayValue(cut.Instance, "_registeredOverlayMarkers").Should().BeSameAs(initialMarkers);
    }

    [Test]
    public void Should_update_marker_overlay_sync_when_parameters_change()
    {
        // arrange
        var cut = RenderMarker("marker-1", new Coordinate(49.61, 6.13));
        var initialMarkers = GetSingleOverlayValue(cut.Instance, "_registeredOverlayMarkers");

        // act
        RenderMarker(cut, "marker-1", new Coordinate(49.62, 6.14));

        // assert
        GetSingleOverlayValue(cut.Instance, "_registeredOverlayMarkers").Should().NotBeSameAs(initialMarkers);
    }

    [Test]
    public void Should_skip_circle_overlay_sync_when_rerender_is_unchanged()
    {
        // arrange
        var cut = RenderCircle("circle-1", new Coordinate(49.61, 6.13));
        var initialCircles = GetSingleOverlayValue(cut.Instance, "_registeredOverlayCircles");

        // act
        RenderCircle(cut, "circle-1", new Coordinate(49.61, 6.13));

        // assert
        GetSingleOverlayValue(cut.Instance, "_registeredOverlayCircles").Should().BeSameAs(initialCircles);
    }

    [Test]
    public void Should_skip_polyline_overlay_sync_when_rerender_is_unchanged()
    {
        // arrange
        var coordinates = new[] { new Coordinate(49.61, 6.13), new Coordinate(49.62, 6.14) };
        var cut = RenderPolyline("polyline-1", coordinates);
        var initialPolylines = GetSingleOverlayValue(cut.Instance, "_registeredOverlayPolylines");

        // act
        RenderPolyline(cut, "polyline-1", coordinates);

        // assert
        GetSingleOverlayValue(cut.Instance, "_registeredOverlayPolylines").Should().BeSameAs(initialPolylines);
    }

    private IRenderedComponent<SgbMap> RenderMarker(string id, Coordinate position) =>
        Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapMarker>(marker => marker.Add(m => m.Id, id).Add(m => m.Position, position))
            )
        );

    private static void RenderMarker(IRenderedComponent<SgbMap> cut, string id, Coordinate position) =>
        cut.Render(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapMarker>(marker => marker.Add(m => m.Id, id).Add(m => m.Position, position))
            )
        );

    private IRenderedComponent<SgbMap> RenderCircle(string id, Coordinate position) =>
        Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapCircle>(circle => circle.Add(c => c.Id, id).Add(c => c.Position, position))
            )
        );

    private static void RenderCircle(IRenderedComponent<SgbMap> cut, string id, Coordinate position) =>
        cut.Render(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapCircle>(circle => circle.Add(c => c.Id, id).Add(c => c.Position, position))
            )
        );

    private IRenderedComponent<SgbMap> RenderPolyline(string id, IReadOnlyList<Coordinate> coordinates) =>
        Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPolyline>(polyline =>
                    polyline.Add(p => p.Id, id).Add(p => p.Coordinates, coordinates)
                )
            )
        );

    private static void RenderPolyline(
        IRenderedComponent<SgbMap> cut,
        string id,
        IReadOnlyList<Coordinate> coordinates
    ) =>
        cut.Render(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPolyline>(polyline =>
                    polyline.Add(p => p.Id, id).Add(p => p.Coordinates, coordinates)
                )
            )
        );

    private static object GetSingleOverlayValue(SgbMap map, string fieldName)
    {
        var field = typeof(BaseMap).GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var dictionary = (IDictionary)field!.GetValue(map)!;

        dictionary.Count.Should().Be(1);
        return dictionary.Values.Cast<object>().Single();
    }
}
