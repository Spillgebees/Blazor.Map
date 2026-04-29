using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Options;

namespace Spillgebees.Blazor.Map.Tests.Interop;

public class MapJsInteropPayloadTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string FlyToIdentifier = "Spillgebees.Map.mapFunctions.flyTo";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string SyncFeaturesIdentifier = "Spillgebees.Map.mapFunctions.syncFeatures";
    private const int TestTimeoutMs = 5000;

    public MapJsInteropPayloadTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(FlyToIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(SyncFeaturesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_send_fractional_zoom_when_flying_to_map_location(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.FlyToAsync(new Coordinate(49.61, 6.13), zoom: 12.5);

        // assert
        var invocation = JSInterop.Invocations[FlyToIdentifier].Single();
        invocation.Arguments[2].Should().Be(12.5);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_send_fit_bounds_feature_ids_as_arrays_when_initializing_map(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.Add(
                p => p.MapOptions,
                new MapOptions(
                    new Coordinate(49.61, 6.13),
                    FitBoundsOptions: new FitBoundsOptions(["route-1", "route-2"], Padding: new PixelPoint(20, 30))
                )
            )
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var mapOptionsPayload = invocation.Arguments[3];
        var fitBoundsPayload = GetRequiredPropertyValue(mapOptionsPayload!, "FitBoundsOptions");
        var featureIds = GetRequiredPropertyValue(fitBoundsPayload, "FeatureIds");

        featureIds.Should().BeOfType<string[]>();
        ((string[])featureIds).Should().BeEquivalentTo(["route-1", "route-2"]);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_send_simplified_center_control_payload_when_initializing_map(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.Add<IReadOnlyList<MapControl>>(p => p.Controls, [new CenterMapControl()])
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var controlsPayload = invocation.Arguments[4].Should().BeOfType<object[]>().Subject;
        var centerPayload = controlsPayload.Single();
        var kindValue = GetRequiredPropertyValue(centerPayload, "Kind");
        var enabledValue = GetRequiredPropertyValue(centerPayload, "Enabled");
        var positionValue = GetRequiredPropertyValue(centerPayload, "Position");

        kindValue.Should().Be("center");
        enabledValue.Should().Be(true);
        positionValue.Should().Be(ControlPosition.TopLeft);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_send_content_control_kind_as_content_when_initializing_map(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.Add<IReadOnlyList<MapControl>>(p => p.Controls, [new ContentMapControl("content-main")])
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var controlsPayload = invocation.Arguments[4].Should().BeOfType<object[]>().Subject;
        var contentPayload = controlsPayload.Single();
        var kindValue = GetRequiredPropertyValue(contentPayload, "Kind");

        kindValue.Should().Be("content");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_send_control_order_payload_when_initializing_map(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.Add(
                p => p.Controls,
                new List<MapControl> { new NavigationMapControl(Order: 250), new ScaleMapControl(Order: 25) }
            )
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var controlsPayload = invocation.Arguments[4].Should().BeOfType<object[]>().Subject;
        var navigationPayload = controlsPayload.Single(payload =>
            (string)GetRequiredPropertyValue(payload, "Kind") == "navigation"
        );
        var scalePayload = controlsPayload.Single(payload =>
            (string)GetRequiredPropertyValue(payload, "Kind") == "scale"
        );

        GetRequiredPropertyValue(navigationPayload, "Order").Should().Be(250);
        GetRequiredPropertyValue(scalePayload, "Order").Should().Be(25);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_send_default_terrain_source_id_when_initializing_map(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.Add<IReadOnlyList<MapControl>>(p => p.Controls, [new TerrainMapControl()])
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var controlsPayload = invocation.Arguments[4].Should().BeOfType<object[]>().Subject;
        var terrainPayload = controlsPayload.Single();

        GetRequiredPropertyValue(terrainPayload, "SourceId").Should().Be("terrain");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_send_custom_terrain_source_id_when_initializing_map(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.Add<IReadOnlyList<MapControl>>(p => p.Controls, [new TerrainMapControl(SourceId: "dem-source")])
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var controlsPayload = invocation.Arguments[4].Should().BeOfType<object[]>().Subject;
        var terrainPayload = controlsPayload.Single();

        GetRequiredPropertyValue(terrainPayload, "SourceId").Should().Be("dem-source");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_send_map_option_lists_as_arrays_when_initializing_map(CancellationToken cancellationToken)
    {
        // arrange
        IReadOnlyList<MapStyle> styles =
        [
            MapStyle.OpenFreeMap.Positron,
            MapStyle.FromUrl("https://example.com/overlay-style.json").WithId("overlay-style"),
        ];
        IReadOnlyList<string> webFonts = ["24px 'DM Sans'", "16px 'Inter'"];

        // act
        Render<SgbMap>(parameters =>
            parameters.Add(
                p => p.MapOptions,
                new MapOptions(new Coordinate(49.61, 6.13), Styles: styles, WebFonts: webFonts)
            )
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var mapOptionsPayload = invocation.Arguments[3];
        var stylesPayload = GetRequiredPropertyValue(mapOptionsPayload!, "Styles");
        var webFontsPayload = GetRequiredPropertyValue(mapOptionsPayload!, "WebFonts");

        stylesPayload.Should().BeOfType<object[]>();
        ((object[])stylesPayload).Should().HaveCount(styles.Count);
        GetRequiredPropertyValue(((object[])stylesPayload)[0], "Url").Should().Be(MapStyle.OpenFreeMap.Positron.Url);
        GetRequiredPropertyValue(((object[])stylesPayload)[1], "Url")
            .Should()
            .Be("https://example.com/overlay-style.json");

        webFontsPayload.Should().BeOfType<string[]>();
        ((string[])webFontsPayload).Should().Equal(webFonts);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_send_referrer_policies_when_initializing_map(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters
                .Add(
                    p => p.MapOptions,
                    new MapOptions(
                        new Coordinate(49.61, 6.13),
                        Style: MapStyle
                            .FromUrl("https://example.com/style.json")
                            .WithReferrerPolicy(ReferrerPolicy.NoReferrer)
                    )
                )
                .Add(
                    p => p.Overlays,
                    [
                        new TileOverlay(
                            "overlay-1",
                            "https://tiles.example.com/{z}/{x}/{y}.png",
                            ReferrerPolicy: ReferrerPolicy.SameOrigin
                        ),
                    ]
                )
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var mapOptionsPayload = invocation.Arguments[3];
        var stylePayload = GetRequiredPropertyValue(mapOptionsPayload!, "Style");
        var overlaysPayload = invocation.Arguments[9].Should().BeAssignableTo<Array>().Subject;
        var overlayPayload = overlaysPayload.GetValue(0);

        GetRequiredPropertyValue(stylePayload, "ReferrerPolicy").Should().Be(ReferrerPolicy.NoReferrer);
        GetRequiredPropertyValue(overlayPayload!, "ReferrerPolicy").Should().Be(ReferrerPolicy.SameOrigin);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_send_polyline_coordinates_as_arrays_when_initializing_map(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.Add(
                p => p.Polylines,
                new List<Polyline>
                {
                    new("route-1", [new Coordinate(49.60, 6.10), new Coordinate(49.61, 6.13)], Color: "#3B82F6"),
                }
            )
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var polylinePayloads = invocation.Arguments[8].Should().BeAssignableTo<Array>().Subject;
        var polylinePayload = polylinePayloads.GetValue(0);
        polylinePayload.Should().NotBeNull();
        var coordinates = GetRequiredPropertyValue(polylinePayload!, "Coordinates");

        coordinates.Should().BeOfType<Coordinate[]>();
        ((Coordinate[])coordinates).Should().HaveCount(2);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_send_sync_feature_payload_using_arrays_for_updated_polylines(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var initialPolylines = new List<Polyline>
        {
            new("route-1", [new Coordinate(49.60, 6.10), new Coordinate(49.61, 6.13)], Width: 4),
        };
        var cut = Render<SgbMap>(parameters => parameters.Add(p => p.Polylines, initialPolylines));
        await cut.Instance.OnMapInitializedAsync();

        // act
        cut.Render(parameters =>
            parameters.Add(
                p => p.Polylines,
                new List<Polyline>
                {
                    new("route-1", [new Coordinate(49.60, 6.10), new Coordinate(49.62, 6.14)], Width: 5),
                }
            )
        );

        // assert
        var invocation = JSInterop.Invocations[SyncFeaturesIdentifier].Single();
        var payload = invocation.Arguments[1];
        var polylinePayload = GetRequiredPropertyValue(payload!, "polylines");
        var updated = GetRequiredPropertyValue(polylinePayload, "updated").Should().BeAssignableTo<Array>().Subject;
        var removedIds = GetRequiredPropertyValue(polylinePayload, "removedIds");

        updated.Length.Should().Be(1);
        removedIds.Should().BeOfType<string[]>();
        ((string[])removedIds).Should().BeEmpty();

        var updatedPolylinePayload = updated.GetValue(0);
        updatedPolylinePayload.Should().NotBeNull();
        var coordinates = GetRequiredPropertyValue(updatedPolylinePayload!, "Coordinates");
        coordinates.Should().BeOfType<Coordinate[]>();
        ((Coordinate[])coordinates).Should().HaveCount(2);
    }

    [Test, Timeout(TestTimeoutMs)]
    [Arguments(MapAlignment.Map, "map")]
    [Arguments(MapAlignment.Viewport, "viewport")]
    [Arguments(MapAlignment.Auto, "auto")]
    public void Should_wire_map_marker_alignment_values_as_maplibre_strings(
        MapAlignment alignment,
        string expectedValue,
        CancellationToken cancellationToken
    )
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.Add(
                p => p.ChildContent,
                mapBuilder =>
                {
                    mapBuilder.OpenComponent<MapOverlays>(0);
                    mapBuilder.AddAttribute(
                        1,
                        nameof(MapOverlays.ChildContent),
                        (RenderFragment)(
                            overlayBuilder =>
                            {
                                overlayBuilder.OpenComponent<MapMarker>(0);
                                overlayBuilder.AddAttribute(1, nameof(MapMarker.Id), "marker-1");
                                overlayBuilder.AddAttribute(2, nameof(MapMarker.Position), new Coordinate(49.61, 6.13));
                                overlayBuilder.AddAttribute(3, nameof(MapMarker.RotationAlignment), alignment);
                                overlayBuilder.AddAttribute(4, nameof(MapMarker.PitchAlignment), alignment);
                                overlayBuilder.CloseComponent();
                            }
                        )
                    );
                    mapBuilder.CloseComponent();
                }
            )
        );

        // assert
        var invocation = JSInterop.Invocations[CreateMapIdentifier].Single();
        var markerPayloads = invocation.Arguments[6].Should().BeAssignableTo<IReadOnlyList<Marker>>().Subject;
        markerPayloads.Should().HaveCount(1);
        var json = JsonSerializer.Serialize(markerPayloads[0]);

        json.Should().Contain($"\"RotationAlignment\":\"{expectedValue}\"");
        json.Should().Contain($"\"PitchAlignment\":\"{expectedValue}\"");
    }

    private static object GetRequiredPropertyValue(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        property.Should().NotBeNull($"property {propertyName} should exist on {source.GetType().Name}");

        var value = property!.GetValue(source);
        value.Should().NotBeNull($"property {propertyName} should have a value");
        return value!;
    }
}
