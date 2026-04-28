using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Options;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Tests.Models.TrackedEntities;

public class TrackedEntityMaterializerTests
{
    [Test]
    public void Should_materialize_raw_items_into_tracked_entities_with_item_and_generated_decorations()
    {
        // arrange
        var item = new TestVehicle(
            "vehicle-1",
            new Coordinate(49.6, 6.1),
            "vehicle-icon",
            "Vehicle 1",
            "#2563eb",
            32,
            90,
            true,
            7
        );

        // act
        var entities = TrackedEntityMaterializer.Materialize(
            [item],
            new TrackedEntityIdOptions<TestVehicle>(vehicle => vehicle.Id),
            new TrackedEntitySymbolOptions<TestVehicle>(
                vehicle => vehicle.Position,
                vehicle => vehicle.IconImage,
                SizeSelector: vehicle => vehicle.Size,
                RotationSelector: vehicle => vehicle.Rotation,
                ColorSelector: vehicle => vehicle.Color,
                RenderOrderSelector: vehicle => vehicle.RenderOrder,
                HoverSelector: vehicle => vehicle.IsEmphasized ? new TrackedEntityHoverIntent(1.2, true) : null,
                PropertiesSelector: vehicle => new Dictionary<string, object?> { ["group"] = "freight" }
            ),
            [
                new TrackedEntityDecorationOptions<TestVehicle>(
                    "label",
                    TextSelector: vehicle => vehicle.Label,
                    Offset: new PixelPoint(0, -18),
                    Anchor: SymbolAnchor.Top,
                    DisplayMode: TrackedEntityDecorationDisplayMode.Hover,
                    TextSizeSelector: _ => 14
                ),
            ]
        );

        // assert
        entities.Should().HaveCount(1);
        entities[0].Id.Should().Be("vehicle-1");
        entities[0].Item.Should().BeSameAs(item);
        entities[0].Position.Should().Be(item.Position);
        entities[0].Color.Should().Be("#2563eb");
        entities[0].RenderOrder.Should().Be(7);
        entities[0].Hover.Should().BeEquivalentTo(new TrackedEntityHoverIntent(1.2, true));
        entities[0].Symbol.IconImage.Should().Be("vehicle-icon");
        entities[0].Symbol.Size.Should().Be(32);
        entities[0].Symbol.Rotation.Should().Be(90);
        entities[0].Decorations.Should().HaveCount(1);
        entities[0].Decorations[0].Id.Should().Be("label");
        entities[0].Decorations[0].Text.Should().Be("Vehicle 1");
        entities[0].Decorations[0].DisplayMode.Should().Be(TrackedEntityDecorationDisplayMode.Hover);
        entities[0].Properties.Should().ContainKey("group");
        entities[0].Properties!["group"].Should().Be("freight");
    }

    [Test]
    public void Should_skip_optional_decorations_when_selectors_return_no_content()
    {
        // arrange
        var item = new TestVehicle(
            "vehicle-1",
            new Coordinate(49.6, 6.1),
            "vehicle-icon",
            null,
            "#2563eb",
            28,
            0,
            false,
            1
        );

        // act
        var entities = TrackedEntityMaterializer.Materialize(
            [item],
            new TrackedEntityIdOptions<TestVehicle>(vehicle => vehicle.Id),
            new TrackedEntitySymbolOptions<TestVehicle>(vehicle => vehicle.Position, vehicle => vehicle.IconImage),
            [new TrackedEntityDecorationOptions<TestVehicle>("label", TextSelector: vehicle => vehicle.Label)]
        );

        // assert
        entities.Should().HaveCount(1);
        entities[0].Decorations.Should().BeEmpty();
    }

    private sealed record TestVehicle(
        string Id,
        Coordinate Position,
        string IconImage,
        string? Label,
        string Color,
        double Size,
        double Rotation,
        bool IsEmphasized,
        double RenderOrder
    );
}
