using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models.Events;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class LayerBaseEventRewireTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";
    private const string AddMapSourceIdentifier = "Spillgebees.Map.mapFunctions.addMapSource";
    private const string AddMapLayerIdentifier = "Spillgebees.Map.mapFunctions.addMapLayer";
    private const string WireLayerEventsIdentifier = "Spillgebees.Map.mapFunctions.wireLayerEvents";
    private const string UnregisterLayerEventsIdentifier = "Spillgebees.Map.mapFunctions.unregisterLayerEvents";

    public LayerBaseEventRewireTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
        JSInterop.SetupVoid(AddMapSourceIdentifier);
        JSInterop.SetupVoid(AddMapLayerIdentifier);
        JSInterop.SetupVoid(WireLayerEventsIdentifier);
        JSInterop.SetupVoid(UnregisterLayerEventsIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_wire_events_when_handler_is_added_after_first_render(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<LayerEventHarness>();
        await cut.Instance.Map.OnMapInitializedAsync();

        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        // act
        cut.Instance.OnClick = EventCallback.Factory.Create<LayerFeatureEventArgs>(
            this,
            static _ => Task.CompletedTask
        );
        cut.Render();

        // assert
        JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_unwire_events_when_last_handler_is_removed_after_first_render(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<LayerEventHarness>();
        await cut.Instance.Map.OnMapInitializedAsync();

        cut.Instance.OnClick = EventCallback.Factory.Create<LayerFeatureEventArgs>(
            this,
            static _ => Task.CompletedTask
        );
        cut.Render();
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        // act
        cut.Instance.OnClick = default;
        cut.Render();

        // assert
        JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_rewire_events_when_handler_delegate_is_replaced_after_first_render(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<LayerEventHarness>();
        await cut.Instance.Map.OnMapInitializedAsync();

        cut.Instance.OnClick = EventCallback.Factory.Create<LayerFeatureEventArgs>(
            this,
            static _ => Task.CompletedTask
        );
        cut.Render();
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        // act
        cut.Instance.OnClick = EventCallback.Factory.Create<LayerFeatureEventArgs>(this, async _ => await Task.Yield());
        cut.Render();

        // assert
        JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_rewire_geojson_source_layer_events_after_the_layer_is_added(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonLayerEventHarnessWithInitialClick>();
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        await cut.WaitForAssertionAsync(() =>
        {
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount);
        });
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_rewire_vector_tile_source_layer_events_after_the_layer_is_added(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<VectorTileLayerEventHarnessWithInitialClick>();
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        await cut.WaitForAssertionAsync(() =>
        {
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount);
        });
    }

    public sealed class LayerEventHarness : ComponentBase
    {
        public SgbMap Map { get; private set; } = null!;

        public EventCallback<LayerFeatureEventArgs> OnClick { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(
                1,
                "ChildContent",
                (RenderFragment)(
                    mapBuilder =>
                    {
                        mapBuilder.OpenComponent<GeoJsonSource>(0);
                        mapBuilder.AddAttribute(1, "Id", "source-1");
                        mapBuilder.AddAttribute(2, nameof(GeoJsonSource.AllowOutsideMapSources), true);
                        mapBuilder.AddAttribute(
                            3,
                            "Data",
                            new Dictionary<string, object?>
                            {
                                ["type"] = "FeatureCollection",
                                ["features"] = Array.Empty<object>(),
                            }
                        );
                        mapBuilder.AddAttribute(
                            4,
                            "ChildContent",
                            (RenderFragment)(
                                sourceBuilder =>
                                {
                                    sourceBuilder.OpenComponent<SymbolLayer>(0);
                                    sourceBuilder.AddAttribute(1, "Id", "layer-1");
                                    sourceBuilder.AddAttribute(
                                        2,
                                        "TextField",
                                        (Spillgebees.Blazor.Map.Models.Expressions.StyleValue<string>)"label"
                                    );
                                    sourceBuilder.AddAttribute(3, "OnClick", OnClick);
                                    sourceBuilder.CloseComponent();
                                }
                            )
                        );
                        mapBuilder.CloseComponent();
                    }
                )
            );
            builder.AddComponentReferenceCapture(2, value => Map = (SgbMap)value);
            builder.CloseComponent();
        }
    }

    public sealed class GeoJsonLayerEventHarnessWithInitialClick : ComponentBase
    {
        public SgbMap Map { get; private set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(
                1,
                "ChildContent",
                (RenderFragment)(
                    mapBuilder =>
                    {
                        mapBuilder.OpenComponent<GeoJsonSource>(0);
                        mapBuilder.AddAttribute(1, "Id", "source-1");
                        mapBuilder.AddAttribute(2, nameof(GeoJsonSource.AllowOutsideMapSources), true);
                        mapBuilder.AddAttribute(
                            3,
                            "Data",
                            new Dictionary<string, object?>
                            {
                                ["type"] = "FeatureCollection",
                                ["features"] = Array.Empty<object>(),
                            }
                        );
                        mapBuilder.AddAttribute(
                            4,
                            "ChildContent",
                            (RenderFragment)(
                                sourceBuilder =>
                                {
                                    sourceBuilder.OpenComponent<SymbolLayer>(0);
                                    sourceBuilder.AddAttribute(1, "Id", "layer-1");
                                    sourceBuilder.AddAttribute(
                                        2,
                                        "TextField",
                                        (Spillgebees.Blazor.Map.Models.Expressions.StyleValue<string>)"label"
                                    );
                                    sourceBuilder.AddAttribute(
                                        3,
                                        "OnClick",
                                        EventCallback.Factory.Create<LayerFeatureEventArgs>(
                                            this,
                                            static _ => Task.CompletedTask
                                        )
                                    );
                                    sourceBuilder.CloseComponent();
                                }
                            )
                        );
                        mapBuilder.CloseComponent();
                    }
                )
            );
            builder.AddComponentReferenceCapture(2, value => Map = (SgbMap)value);
            builder.CloseComponent();
        }
    }

    public sealed class VectorTileLayerEventHarnessWithInitialClick : ComponentBase
    {
        public SgbMap Map { get; private set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(
                1,
                "ChildContent",
                (RenderFragment)(
                    mapBuilder =>
                    {
                        mapBuilder.OpenComponent<VectorTileSource>(0);
                        mapBuilder.AddAttribute(1, "Id", "source-1");
                        mapBuilder.AddAttribute(2, "Url", "mapbox://example.tileset");
                        mapBuilder.AddAttribute(3, nameof(VectorTileSource.AllowOutsideMapSources), true);
                        mapBuilder.AddAttribute(
                            4,
                            "ChildContent",
                            (RenderFragment)(
                                sourceBuilder =>
                                {
                                    sourceBuilder.OpenComponent<SymbolLayer>(0);
                                    sourceBuilder.AddAttribute(1, "Id", "layer-1");
                                    sourceBuilder.AddAttribute(2, "SourceLayerId", "transportation");
                                    sourceBuilder.AddAttribute(
                                        3,
                                        "TextField",
                                        (Spillgebees.Blazor.Map.Models.Expressions.StyleValue<string>)"label"
                                    );
                                    sourceBuilder.AddAttribute(
                                        4,
                                        "OnClick",
                                        EventCallback.Factory.Create<LayerFeatureEventArgs>(
                                            this,
                                            static _ => Task.CompletedTask
                                        )
                                    );
                                    sourceBuilder.CloseComponent();
                                }
                            )
                        );
                        mapBuilder.CloseComponent();
                    }
                )
            );
            builder.AddComponentReferenceCapture(2, value => Map = (SgbMap)value);
            builder.CloseComponent();
        }
    }
}
