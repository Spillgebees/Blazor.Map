using System.Text.Json.Nodes;
using AwesomeAssertions;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map.Tests.Engine;

public class EngineStyleJsonTests
{
    [Test]
    public void Should_omit_slot_fields_when_no_slot_is_set()
    {
        // arrange
        var composed = MapStyle.FromUrl("https://example.com/rail.json");

        // act
        var node = BuildComposedStyleNode(composed);

        // assert
        var composedNode = node["styles"]![1]!.AsObject();
        composedNode["slot"].Should().BeNull();
        composedNode["layerSlots"].Should().BeNull();
    }

    [Test]
    public void Should_write_the_style_slot_in_kebab_case()
    {
        // arrange
        var composed = MapStyle.FromUrl("https://example.com/rail.json").WithSlot(LayerSlot.AboveLabels);

        // act
        var node = BuildComposedStyleNode(composed);

        // assert
        node["styles"]![1]!["slot"]!.GetValue<string>().Should().Be("above-labels");
    }

    [Test]
    public void Should_write_per_layer_slots()
    {
        // arrange
        var composed = MapStyle
            .FromUrl("https://example.com/rail.json")
            .WithLayerSlot("station-dots", LayerSlot.AboveLabels)
            .WithLayerSlot("platforms", LayerSlot.BelowLabels);

        // act
        var node = BuildComposedStyleNode(composed);

        // assert
        var layerSlots = node["styles"]![1]!["layerSlots"]!.AsObject();
        layerSlots["station-dots"]!.GetValue<string>().Should().Be("above-labels");
        layerSlots["platforms"]!.GetValue<string>().Should().Be("below-labels");
    }

    [Test]
    public void Should_serialize_layer_slots_in_key_order_regardless_of_insertion_order()
    {
        // arrange: style changes are detected by comparing serialized JSON, so the same
        // slots added in a different order must serialize identically
        var first = MapStyle
            .FromUrl("https://example.com/rail.json")
            .WithLayerSlot("station-dots", LayerSlot.AboveLabels)
            .WithLayerSlot("platforms", LayerSlot.BelowLabels);
        var second = MapStyle
            .FromUrl("https://example.com/rail.json")
            .WithLayerSlot("platforms", LayerSlot.BelowLabels)
            .WithLayerSlot("station-dots", LayerSlot.AboveLabels);

        // act
        var firstJson = BuildComposedStyleNode(first).ToJsonString();
        var secondJson = BuildComposedStyleNode(second).ToJsonString();

        // assert
        firstJson.Should().Be(secondJson);
    }

    [Test]
    public void WithLayerSlot_should_not_mutate_the_original_style()
    {
        // arrange
        var original = MapStyle.FromUrl("https://example.com/rail.json").WithLayerSlot("a", LayerSlot.AboveLabels);

        // act
        var updated = original.WithLayerSlot("b", LayerSlot.BelowLabels);

        // assert
        original.LayerSlots.Should().HaveCount(1);
        updated.LayerSlots.Should().HaveCount(2);
        updated.LayerSlots!["a"].Should().Be(LayerSlot.AboveLabels);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("\t")]
    public void WithLayerSlot_should_reject_empty_or_whitespace_layer_ids(string layerId)
    {
        // arrange
        var style = MapStyle.FromUrl("https://example.com/rail.json");

        // act
        var act = () => style.WithLayerSlot(layerId, LayerSlot.AboveLabels);

        // assert
        act.Should().Throw<ArgumentException>().WithParameterName(nameof(layerId));
    }

    private static JsonObject BuildComposedStyleNode(MapStyle composed) =>
        EngineStyleJson.BuildStylesNode(
            styles: [MapStyle.OpenFreeMap.Liberty, composed.WithId("rail")],
            style: null,
            styleSpec: null,
            composedGlyphsUrl: null
        );
}
