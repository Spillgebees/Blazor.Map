using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map.Tests.Engine;

public class EngineOpsSerializationTests
{
    private static string Serialize(params EngineOp[] ops) =>
        JsonSerializer.Serialize((IReadOnlyList<EngineOp>)ops, MapEngineJsonContext.Default.IReadOnlyListEngineOp);

    [Test]
    public void Should_write_the_op_discriminator_and_camel_case_properties()
    {
        // arrange
        var op = new LayerAddOp("tracks", JsonNode.Parse("""{"id":"tracks","type":"line"}""")!, Slot: "overlay");

        // act
        var json = Serialize(op);

        // assert
        json.Should()
            .Be("""[{"op":"layer.add","id":"tracks","spec":{"id":"tracks","type":"line"},"slot":"overlay"}]""");
    }

    [Test]
    public void Should_omit_null_optional_fields()
    {
        // arrange
        var op = new EntitiesUpsertOp(
            "vehicles",
            Epoch: 3,
            Upserts: [new EngineEntityUpsert(Idx: 5, Id: "bus-1", Lng: 6.13, Lat: 49.61, Icon: "bus")],
            Removes: [7]
        );

        // act
        var json = Serialize(op);

        // assert
        json.Should()
            .Be(
                """[{"op":"entities.upsert","id":"vehicles","epoch":3,"upserts":[{"idx":5,"id":"bus-1","lng":6.13,"lat":49.61,"icon":"bus"}],"removes":[7]}]"""
            );
    }

    [Test]
    public void Should_serialize_entity_layer_config_with_cluster_passthrough()
    {
        // arrange
        var op = new EntitiesCreateOp(
            "vehicles",
            new EngineEntityLayerConfig(
                Cluster: JsonNode.Parse("""{"cluster":true,"clusterRadius":40}"""),
                Animation: new EngineAnimation(350, "easeInOut"),
                HoverLayerIds: ["vehicles-symbols"]
            )
        );

        // act
        var json = Serialize(op);

        // assert
        json.Should()
            .Be(
                """[{"op":"entities.create","id":"vehicles","config":{"cluster":{"cluster":true,"clusterRadius":40},"animation":{"durationMs":350,"easing":"easeInOut"},"hoverLayerIds":["vehicles-symbols"]}}]"""
            );
    }

    [Test]
    public void Should_serialize_event_handler_registrations()
    {
        // arrange
        var op = new EventsSetOp("vehicles-symbols", new EngineEventHandlers(Click: 7));

        // act
        var json = Serialize(op);

        // assert
        json.Should().Be("""[{"op":"events.set","layerId":"vehicles-symbols","handlers":{"click":7}}]""");
    }

    [Test]
    public void Should_serialize_control_ops_with_the_v1_control_wire_shape()
    {
        // arrange
        var setOp = new ControlSetOp(
            EngineControl.From(new NavigationControlDefinition(ShowCompass: false, Order: 150))
        );
        var panelContentOp = new ControlContentOp("panel-1", new EngineControlEvents(OpenChanged: 7));
        var popupOp = new PopupSetOp(
            new EnginePopup(
                "popup-1",
                new Coordinate(49.61, 6.13),
                PopupOptions.FromText("Hello"),
                new EnginePopupEvents(Closed: 9)
            )
        );

        // act
        var json = Serialize(setOp, panelContentOp, popupOp, new ControlRemoveOp("navigation"));

        // assert
        json.Should()
            .Be(
                """[{"op":"control.set","control":{"kind":"navigation","controlId":"navigation","visible":true,"position":"top-right","order":150,"showCompass":false,"showZoom":true}},{"op":"control.content","id":"panel-1","events":{"openChanged":7}},{"op":"popup.set","popup":{"id":"popup-1","position":{"latitude":49.61,"longitude":6.13},"options":{"content":"Hello","contentMode":"text","trigger":"click","anchor":"auto","closeButton":true},"events":{"closed":9}}},{"op":"control.remove","id":"navigation"}]"""
            );
    }

    [Test]
    public void Should_serialize_map_config_and_camera_ops()
    {
        // arrange
        var configureOp = new MapConfigureOp(
            new EngineMapConfig(
                Pitch: 30,
                Bearing: 90,
                Projection: MapProjection.Globe,
                MaxZoom: 18,
                MaxBounds: new MapBounds(new Coordinate(49, 5), new Coordinate(50, 7)),
                PixelRatioMode: MapPixelRatioMode.RoundedUpDevicePixelRatio
            )
        );
        var fitOp = new CameraFitFeaturesOp(["m1", "c1"], Padding: new PixelPoint(20, 10));
        var policyOp = new MapRequestPolicyOp("https://tiles.example", "no-referrer");

        // act
        var json = Serialize(configureOp, fitOp, policyOp, new CameraFlyToOp(new Coordinate(49.61, 6.13), Zoom: 14));

        // assert
        json.Should()
            .Be(
                """[{"op":"map.configure","config":{"pitch":30,"bearing":90,"projection":"globe","maxZoom":18,"maxBounds":{"southwest":{"latitude":49,"longitude":5},"northeast":{"latitude":50,"longitude":7}},"pixelRatioMode":"roundedUpDevicePixelRatio"}},{"op":"camera.fitFeatures","featureIds":["m1","c1"],"padding":{"x":20,"y":10}},{"op":"map.requestPolicy","origin":"https://tiles.example","policy":"no-referrer"},{"op":"camera.flyTo","center":{"latitude":49.61,"longitude":6.13},"zoom":14}]"""
            );
    }

    [Test]
    public void Should_serialize_marker_ops_with_the_v1_marker_wire_shape()
    {
        // arrange — the public Marker record rides the op verbatim; its attribute
        // converters must produce the camelCase/lowercase shape engine/markers.ts reads
        var setOp = new MarkerSetOp(
            new Marker(
                "hq",
                new Coordinate(49.61, 6.13),
                Title: "Headquarters",
                Popup: PopupOptions.FromText("Hello", PopupTrigger.Hover),
                Color: "#dc2626",
                RotationAlignment: MapAlignment.Map,
                Draggable: true,
                ClassName: "hq-marker"
            )
        );
        var removeOp = new MarkerRemoveOp("hq");

        // act
        var json = Serialize(setOp, removeOp);

        // assert
        json.Should()
            .Be(
                """[{"op":"marker.set","marker":{"id":"hq","position":{"latitude":49.61,"longitude":6.13},"title":"Headquarters","popup":{"content":"Hello","contentMode":"text","trigger":"hover","anchor":"auto","closeButton":true},"color":"#dc2626","rotationAlignment":"map","draggable":true,"className":"hq-marker"}},{"op":"marker.remove","id":"hq"}]"""
            );
    }
}
