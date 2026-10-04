using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class LayerSlotTests : BunitContext
{
    private const string ApplyOpsIdentifier = "Spillgebees.Engine.applyOps";

    public LayerSlotTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(ApplyOpsIdentifier);
    }

    [Test]
    public async Task Should_add_layers_without_a_slot_by_default()
    {
        // arrange
        var cut = Render<LayerSlotHarness>();

        // act
        await cut.FindComponent<SgbMap>().Instance.Router.OnMapEvent("load", default);

        // assert
        await cut.WaitForAssertionAsync(() =>
        {
            var layerAdd = FindLastOp("layer.add", "slot-layer");
            layerAdd.TryGetProperty("slot", out _).Should().BeFalse();
        });
    }

    [Test]
    public async Task Should_add_below_labels_layers_to_the_below_labels_slot()
    {
        // arrange
        var cut = Render<LayerSlotHarness>(parameters =>
            parameters.Add(component => component.Slot, LayerSlot.BelowLabels)
        );

        // act
        await cut.FindComponent<SgbMap>().Instance.Router.OnMapEvent("load", default);

        // assert
        await cut.WaitForAssertionAsync(() =>
        {
            var layerAdd = FindLastOp("layer.add", "slot-layer");
            layerAdd.GetProperty("slot").GetString().Should().Be("below-labels");
            layerAdd.TryGetProperty("before", out _).Should().BeFalse();
        });
    }

    [Test]
    public async Task Should_move_the_layer_when_the_slot_changes()
    {
        // arrange
        var cut = Render<LayerSlotHarness>(parameters =>
            parameters.Add(component => component.Slot, LayerSlot.BelowLabels)
        );
        await cut.FindComponent<SgbMap>().Instance.Router.OnMapEvent("load", default);

        // act
        await cut.InvokeAsync(() => cut.Instance.SetLayerParameters(LayerSlot.AboveLabels, null));

        // assert
        await cut.WaitForAssertionAsync(() =>
        {
            var layerMove = FindLastOp("layer.move", "slot-layer");
            layerMove.TryGetProperty("slot", out _).Should().BeFalse();
            layerMove.TryGetProperty("before", out _).Should().BeFalse();
        });
    }

    [Test]
    public async Task Should_move_the_layer_when_before_changes()
    {
        // arrange
        var cut = Render<LayerSlotHarness>(parameters =>
            parameters.Add(component => component.Slot, LayerSlot.BelowLabels)
        );
        await cut.FindComponent<SgbMap>().Instance.Router.OnMapEvent("load", default);

        // act
        await cut.InvokeAsync(() => cut.Instance.SetLayerParameters(LayerSlot.BelowLabels, "labels"));

        // assert
        await cut.WaitForAssertionAsync(() =>
        {
            var layerMove = FindLastOp("layer.move", "slot-layer");
            layerMove.GetProperty("slot").GetString().Should().Be("below-labels");
            layerMove.GetProperty("before").GetString().Should().Be("labels");
        });
    }

    [Test]
    public async Task Should_add_tile_overlays_below_every_vector_layer_by_default()
    {
        // arrange
        var cut = Render<SgbMap>(parameters =>
            parameters.Add(map => map.Overlays, [new TileOverlay("wms", "https://tiles.example.com/{z}/{x}/{y}.png")])
        );

        // act
        await cut.Instance.Router.OnMapEvent("load", default);

        // assert
        await cut.WaitForAssertionAsync(() =>
        {
            var layerAdd = FindLastOp("layer.add", "sgb-overlay-wms");
            layerAdd.GetProperty("slot").GetString().Should().Be("sgb:tile-overlays");
        });
    }

    [Test]
    public async Task Should_add_above_labels_tile_overlays_without_a_slot()
    {
        // arrange
        var overlay = new TileOverlay("wms", "https://tiles.example.com/{z}/{x}/{y}.png")
        {
            Slot = LayerSlot.AboveLabels,
        };
        var cut = Render<SgbMap>(parameters => parameters.Add(map => map.Overlays, [overlay]));

        // act
        await cut.Instance.Router.OnMapEvent("load", default);

        // assert
        await cut.WaitForAssertionAsync(() =>
        {
            var layerAdd = FindLastOp("layer.add", "sgb-overlay-wms");
            layerAdd.TryGetProperty("slot", out _).Should().BeFalse();
        });
    }

    private JsonElement FindLastOp(string op, string id)
    {
        var payloads = JSInterop
            .Invocations[ApplyOpsIdentifier]
            .Select(invocation => invocation.Arguments[1] as string ?? "[]")
            .ToArray();

        for (var payloadIndex = payloads.Length - 1; payloadIndex >= 0; payloadIndex--)
        {
            using var document = JsonDocument.Parse(payloads[payloadIndex]);
            var ops = document.RootElement.EnumerateArray().ToArray();
            for (var opIndex = ops.Length - 1; opIndex >= 0; opIndex--)
            {
                var candidate = ops[opIndex];
                if (candidate.GetProperty("op").GetString() == op && candidate.GetProperty("id").GetString() == id)
                {
                    return candidate.Clone();
                }
            }
        }

        throw new InvalidOperationException($"Op '{op}' for layer '{id}' was not queued.");
    }

    public sealed class LayerSlotHarness : ComponentBase
    {
        [Parameter]
        public LayerSlot Slot { get; set; }

        [Parameter]
        public string? Before { get; set; }

        public void SetLayerParameters(LayerSlot slot, string? before)
        {
            Slot = slot;
            Before = before;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SgbMap>(0);
            builder.AddComponentParameter(
                1,
                nameof(SgbMap.ChildContent),
                (RenderFragment)(
                    mapChildBuilder =>
                    {
                        mapChildBuilder.OpenComponent<GeoJsonSource>(0);
                        mapChildBuilder.AddComponentParameter(1, nameof(GeoJsonSource.Id), "slot-source");
                        mapChildBuilder.AddComponentParameter(
                            2,
                            nameof(GeoJsonSource.ChildContent),
                            (RenderFragment)(
                                sourceChildBuilder =>
                                {
                                    sourceChildBuilder.OpenComponent<CircleLayer>(0);
                                    sourceChildBuilder.AddComponentParameter(1, nameof(CircleLayer.Id), "slot-layer");
                                    sourceChildBuilder.AddComponentParameter(2, nameof(CircleLayer.Slot), Slot);
                                    sourceChildBuilder.AddComponentParameter(3, nameof(CircleLayer.Before), Before);
                                    sourceChildBuilder.CloseComponent();
                                }
                            )
                        );
                        mapChildBuilder.CloseComponent();
                    }
                )
            );
            builder.CloseComponent();
        }
    }
}
