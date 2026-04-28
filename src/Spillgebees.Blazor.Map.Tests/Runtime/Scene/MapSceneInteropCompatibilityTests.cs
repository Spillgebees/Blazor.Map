using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Tests.Runtime.Scene;

public class MapSceneInteropCompatibilityTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";
    private const string AddMapSourceIdentifier = "Spillgebees.Map.mapFunctions.addMapSource";
    private const string AddMapLayerIdentifier = "Spillgebees.Map.mapFunctions.addMapLayer";
    private const string WireLayerEventsIdentifier = "Spillgebees.Map.mapFunctions.wireLayerEvents";
    private const string SetSourceDataIdentifier = "Spillgebees.Map.mapFunctions.setSourceData";
    private const string SetLayoutPropertyIdentifier = "Spillgebees.Map.mapFunctions.setLayoutProperty";
    private const string MoveMapLayerIdentifier = "Spillgebees.Map.mapFunctions.moveMapLayer";

    public MapSceneInteropCompatibilityTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
        JSInterop.SetupVoid(AddMapSourceIdentifier);
        JSInterop.SetupVoid(AddMapLayerIdentifier);
        JSInterop.SetupVoid(WireLayerEventsIdentifier);
        JSInterop.SetupVoid(SetSourceDataIdentifier);
        JSInterop.SetupVoid(SetLayoutPropertyIdentifier);
        JSInterop.SetupVoid(MoveMapLayerIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_custom_scene_via_batched_scene_mutations(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SceneRegistrationHarness>();

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );

        // assert
        JSInterop.VerifyNotInvoke(AddMapSourceIdentifier);
        JSInterop.VerifyNotInvoke(AddMapLayerIdentifier);
        JSInterop.VerifyNotInvoke(WireLayerEventsIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_apply_custom_scene_updates_via_batched_scene_mutations(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SceneUpdateHarness>();
        await cut.Instance.Map.OnMapInitializedAsync();

        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        // act
        await cut.InvokeAsync(() => cut.Instance.UpdateScene());

        // assert
        JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount);
        JSInterop.VerifyNotInvoke(SetSourceDataIdentifier);
        JSInterop.VerifyNotInvoke(SetLayoutPropertyIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_reconcile_ordering_once_per_layer_registration_batch(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SceneOrderingHarness>();

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );

        var mutationBatches = JSInterop
            .Invocations[ApplySceneMutationsIdentifier]
            .Select(invocation => invocation.Arguments[1])
            .OfType<MapSceneMutationBatch>()
            .ToArray();

        var orderingBatchCount = mutationBatches.Count(batch =>
            batch.Mutations.Count(mutation => mutation.Kind == "reconcileOrdering") == 1
        );
        orderingBatchCount.Should().Be(1);

        JSInterop.VerifyNotInvoke(MoveMapLayerIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_refresh_inherited_ordering_and_reconcile_once_when_source_stack_changes(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<SceneOrderingUpdateHarness>();
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        // act
        await cut.InvokeAsync(cut.Instance.UpdateSourceOrdering);

        // assert
        JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().Be(initialBatchCount + 2);

        var updatedBatch = JSInterop
            .Invocations[ApplySceneMutationsIdentifier][initialBatchCount + 1]
            .Arguments[1]
            .Should()
            .BeOfType<MapSceneMutationBatch>()
            .Subject;

        updatedBatch.Mutations.Count(mutation => mutation.Kind == "addLayer").Should().Be(2);
        updatedBatch.Mutations.Count(mutation => mutation.Kind == "reconcileOrdering").Should().Be(1);

        updatedBatch
            .Mutations.Where(mutation => mutation.Kind == "addLayer")
            .Select(mutation => mutation.Ordering)
            .Should()
            .AllSatisfy(ordering => ordering!.Stack.Should().Be("updated-stack"));

        JSInterop.VerifyNotInvoke(MoveMapLayerIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_include_referrer_policy_in_vector_tile_source_registration(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<ReferrerPolicyVectorTileHarness>();

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );

        var batches = JSInterop
            .Invocations[ApplySceneMutationsIdentifier]
            .Select(invocation => invocation.Arguments[1])
            .OfType<MapSceneMutationBatch>()
            .ToArray();

        var addSourceMutation = batches
            .SelectMany(batch => batch.Mutations)
            .Single(mutation => mutation.Kind == "addSource" && mutation.SourceId == "vector-source");

        addSourceMutation.SourceSpec.Should().NotBeNull();
        addSourceMutation.SourceSpec!.Should().ContainKey("referrerPolicy");
        addSourceMutation.SourceSpec["referrerPolicy"].Should().Be(ReferrerPolicy.StrictOriginWhenCrossOrigin);
    }

    public sealed class SceneRegistrationHarness : ComponentBase
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

    public sealed class SceneUpdateHarness : ComponentBase
    {
        public SgbMap Map { get; private set; } = null!;

        private object _data = new Dictionary<string, object?>
        {
            ["type"] = "FeatureCollection",
            ["features"] = Array.Empty<object>(),
        };

        private bool _visible = true;

        public void UpdateScene()
        {
            _data = new Dictionary<string, object?>
            {
                ["type"] = "FeatureCollection",
                ["features"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["type"] = "Feature",
                        ["geometry"] = new Dictionary<string, object?>
                        {
                            ["type"] = "Point",
                            ["coordinates"] = new[] { 6.13, 49.61 },
                        },
                        ["properties"] = new Dictionary<string, object?> { ["label"] = "updated" },
                    },
                },
            };
            _visible = false;
            StateHasChanged();
        }

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
                        mapBuilder.AddAttribute(3, "Data", _data);
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
                                    sourceBuilder.AddAttribute(3, "Visible", _visible);
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

    public sealed class SceneOrderingHarness : ComponentBase
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
                                    sourceBuilder.OpenComponent<LineLayer>(0);
                                    sourceBuilder.AddAttribute(1, "Id", "layer-a");
                                    sourceBuilder.AddAttribute(2, "Stack", "a");
                                    sourceBuilder.CloseComponent();

                                    sourceBuilder.OpenComponent<LineLayer>(3);
                                    sourceBuilder.AddAttribute(4, "Id", "layer-b");
                                    sourceBuilder.AddAttribute(5, "Stack", "b");
                                    sourceBuilder.AddAttribute(6, "AfterStack", "a");
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

    public sealed class SceneOrderingUpdateHarness : ComponentBase
    {
        private string _stack = "initial-stack";

        public SgbMap Map { get; private set; } = null!;

        public void UpdateSourceOrdering()
        {
            _stack = "updated-stack";
            StateHasChanged();
        }

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
                        mapBuilder.AddAttribute(2, "Stack", _stack);
                        mapBuilder.AddAttribute(3, nameof(GeoJsonSource.AllowOutsideMapSources), true);
                        mapBuilder.AddAttribute(
                            4,
                            "Data",
                            new Dictionary<string, object?>
                            {
                                ["type"] = "FeatureCollection",
                                ["features"] = Array.Empty<object>(),
                            }
                        );
                        mapBuilder.AddAttribute(
                            5,
                            "ChildContent",
                            (RenderFragment)(
                                sourceBuilder =>
                                {
                                    sourceBuilder.OpenComponent<LineLayer>(0);
                                    sourceBuilder.AddAttribute(1, "Id", "layer-a");
                                    sourceBuilder.CloseComponent();

                                    sourceBuilder.OpenComponent<LineLayer>(2);
                                    sourceBuilder.AddAttribute(3, "Id", "layer-b");
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

    public sealed class ReferrerPolicyVectorTileHarness : ComponentBase
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
                        mapBuilder.AddAttribute(1, "Id", "vector-source");
                        mapBuilder.AddAttribute(2, "Url", "https://example.com/tiles.json");
                        mapBuilder.AddAttribute(3, "ReferrerPolicy", ReferrerPolicy.StrictOriginWhenCrossOrigin);
                        mapBuilder.AddAttribute(4, nameof(VectorTileSource.AllowOutsideMapSources), true);
                        mapBuilder.CloseComponent();
                    }
                )
            );
            builder.AddComponentReferenceCapture(2, value => Map = (SgbMap)value);
            builder.CloseComponent();
        }
    }
}
