using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Models.Options;
using Spillgebees.Blazor.Map.Models.Popups;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class TrackedEntityLayerMapApiTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string SetTrackedEntityFeatureStateIdentifier =
        "Spillgebees.Map.mapFunctions.setTrackedEntityFeatureState";
    private const string FlyToIdentifier = "Spillgebees.Map.mapFunctions.flyTo";
    private const string GetClusterExpansionZoomIdentifier = "Spillgebees.Map.mapFunctions.getClusterExpansionZoom";
    private const string ShowPopupIdentifier = "Spillgebees.Map.mapFunctions.showPopup";
    private const string ClosePopupIdentifier = "Spillgebees.Map.mapFunctions.closePopup";
    private static readonly TimeSpan HoverLeaveWait =
        TrackedEntityLayer<object>.HoverLeaveDebounce + TimeSpan.FromMilliseconds(50);

    public TrackedEntityLayerMapApiTests()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        // act
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(SetTrackedEntityFeatureStateIdentifier);
        JSInterop.SetupVoid(FlyToIdentifier);
        JSInterop.Setup<double>(GetClusterExpansionZoomIdentifier).SetResult(11.2);
        JSInterop.SetupVoid(ShowPopupIdentifier);
        JSInterop.SetupVoid(ClosePopupIdentifier);

        // assert
    }

    [Test]
    public void Should_render_tracked_entity_layer_from_map_overlays_section()
    {
        // arrange
        var cut = Render<MapTrackedEntityHarness>();

        // act
        var renderedSources = cut.FindComponents<TrackedEntityLayer<TestVehicle>>();

        // assert
        renderedSources.Should().HaveCount(1);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_open_hover_popup_and_close_after_hover_leave_debounce(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters.Add(p => p.PopupSelector, vehicle => new PopupOptions(vehicle.Id, PopupTrigger.Hover))
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        var hitArea = GetPrimaryHitArea(cut);

        // act
        await cut.InvokeAsync(() => hitArea.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-1")));
        await cut.InvokeAsync(() => hitArea.OnMouseLeave.InvokeAsync());
        await Task.Delay(HoverLeaveWait, cancellationToken);

        // assert
        JSInterop.Invocations[ShowPopupIdentifier].Should().HaveCount(1);
        JSInterop.Invocations[ClosePopupIdentifier].Should().HaveCount(1);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_show_stale_popup_when_hover_leave_happens_before_popup_open_completes(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var popupOpenStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var popupOpenGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters
                .Add(p => p.PopupSelector, vehicle => new PopupOptions(vehicle.Id, PopupTrigger.Hover))
                .Add(
                    p => p.BeforeShowPopupAsync,
                    () =>
                    {
                        popupOpenStarted.TrySetResult();
                        return popupOpenGate.Task;
                    }
                )
        );

        await cut.Instance.Map.OnMapInitializedAsync();
        var hitArea = GetPrimaryHitArea(cut);
        var initialShowPopupCount = JSInterop.Invocations[ShowPopupIdentifier].Count;
        var initialClosePopupCount = JSInterop.Invocations[ClosePopupIdentifier].Count;

        // act
        var openTask = cut.InvokeAsync(() => hitArea.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-1")));
        await popupOpenStarted.Task.WaitAsync(cancellationToken);
        await cut.InvokeAsync(() => hitArea.OnMouseLeave.InvokeAsync());
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ClosePopupIdentifier].Count.Should().Be(initialClosePopupCount + 1)
        );
        popupOpenGate.SetResult();
        await openTask;

        // assert
        JSInterop.Invocations[ShowPopupIdentifier].Count.Should().Be(initialShowPopupCount);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_keep_newest_popup_when_multiple_hover_popup_opens_overlap(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var firstPopupGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var openAttemptCount = 0;

        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters
                .Add(
                    p => p.Items,
                    [
                        new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon"),
                        new TestVehicle("vehicle-2", new Coordinate(49.7, 6.2), "vehicle-icon"),
                    ]
                )
                .Add(p => p.PopupSelector, vehicle => new PopupOptions(vehicle.Id, PopupTrigger.Hover))
                .Add(
                    p => p.BeforeShowPopupAsync,
                    () =>
                    {
                        var currentAttempt = Interlocked.Increment(ref openAttemptCount);
                        return currentAttempt == 1 ? firstPopupGate.Task : Task.CompletedTask;
                    }
                )
        );

        await cut.Instance.Map.OnMapInitializedAsync();
        var hitArea = GetPrimaryHitArea(cut);

        // act
        var firstOpen = cut.InvokeAsync(() => hitArea.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-1")));
        await cut.InvokeAsync(() => hitArea.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-2")));
        firstPopupGate.SetResult();
        await firstOpen;
        await Task.Delay(HoverLeaveWait, cancellationToken);

        // assert
        JSInterop.Invocations[ShowPopupIdentifier].Should().HaveCount(1);
        JSInterop.Invocations[ShowPopupIdentifier][0].Arguments[2].Should().Be("vehicle-2");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_zoom_to_dissolve_when_cluster_layer_is_clicked(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.EnableCluster, true));
        await cut.Instance.Map.OnMapInitializedAsync();
        var clusterCount = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-cluster-count")
            .Instance;

        // act
        await cut.InvokeAsync(() => clusterCount.OnClick.InvokeAsync(CreateClusterFeatureEvent()));

        // assert
        JSInterop.VerifyInvoke(GetClusterExpansionZoomIdentifier);
        JSInterop.VerifyInvoke(FlyToIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_apply_and_diff_hover_and_selected_feature_state(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters.Add(
                p => p.Items,
                [
                    new TestVehicle(
                        "vehicle-1",
                        new Coordinate(49.6, 6.1),
                        "vehicle-icon",
                        IsHovered: true,
                        IsSelected: true
                    ),
                    new TestVehicle("vehicle-2", new Coordinate(49.7, 6.2), "vehicle-icon"),
                ]
            )
        );

        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count.Should().Be(2));
        var initialCount = JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count;

        // act
        await cut.InvokeAsync(() =>
            cut.Instance.UpdateItems([
                new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon"),
                new TestVehicle(
                    "vehicle-2",
                    new Coordinate(49.7, 6.2),
                    "vehicle-icon",
                    IsHovered: true,
                    IsSelected: true
                ),
            ])
        );

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count.Should().Be(initialCount + 4)
        );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replay_active_hover_and_selected_state_when_entities_refresh(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters.Add(
                p => p.Items,
                [
                    new TestVehicle(
                        "vehicle-1",
                        new Coordinate(49.6, 6.1),
                        "vehicle-icon",
                        IsHovered: true,
                        IsSelected: true
                    ),
                ]
            )
        );

        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count.Should().Be(2));
        var initialCount = JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count;

        // act
        await cut.InvokeAsync(() =>
            cut.Instance.UpdateItems([
                new TestVehicle(
                    "vehicle-1",
                    new Coordinate(49.61, 6.11),
                    "vehicle-icon",
                    IsHovered: true,
                    IsSelected: true
                ),
            ])
        );

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count.Should().Be(initialCount + 2)
        );
    }

    [Test]
    public void Should_generate_decoration_selected_display_mode_expression_with_selected_feature_state()
    {
        // arrange
        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters.Add(
                p => p.Decorations,
                [
                    new TrackedEntityDecorationOptions<TestVehicle>(
                        "selected-label",
                        TextSelector: _ => "x",
                        DisplayMode: TrackedEntityDecorationDisplayMode.Selected
                    ),
                ]
            )
        );

        var selectedLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-selected-label")
            .Instance;

        // act
        var selectedLayerSpec = GetLayerSpec(selectedLayer);

        // assert
        GetPaintValue(selectedLayerSpec, "text-opacity")
            .Should()
            .BeEquivalentTo(
                new object[]
                {
                    "case",
                    new object[] { "==", new object[] { "get", TrackedEntityFeatureProperties.DisplayMode }, "always" },
                    1.0,
                    new object[] { "==", new object[] { "get", TrackedEntityFeatureProperties.DisplayMode }, "hover" },
                    new object[]
                    {
                        "case",
                        new object[] { "boolean", new object[] { "feature-state", "hover" }, false },
                        1.0,
                        0.0,
                    },
                    new object[]
                    {
                        "==",
                        new object[] { "get", TrackedEntityFeatureProperties.DisplayMode },
                        "selected",
                    },
                    new object[]
                    {
                        "case",
                        new object[] { "boolean", new object[] { "feature-state", "selected" }, false },
                        1.0,
                        0.0,
                    },
                    new object[]
                    {
                        "==",
                        new object[] { "get", TrackedEntityFeatureProperties.DisplayMode },
                        "hover-or-selected",
                    },
                    new object[]
                    {
                        "case",
                        new object[]
                        {
                            "any",
                            new object[] { "boolean", new object[] { "feature-state", "hover" }, false },
                            new object[] { "boolean", new object[] { "feature-state", "selected" }, false },
                        },
                        1.0,
                        0.0,
                    },
                    1.0,
                }
            );
    }

    [Test]
    public void Should_serialize_tracked_entity_decoration_symbol_enums_in_layer_spec()
    {
        // arrange
        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters.Add(
                p => p.Decorations,
                [
                    new TrackedEntityDecorationOptions<TestVehicle>(
                        "badge",
                        TextSelector: _ => "badge",
                        IconImageSelector: _ => "badge-icon",
                        Anchor: SymbolAnchor.TopRight,
                        IconTextFit: IconTextFit.Width,
                        IconTextFitPadding: [2, 4, 2, 4]
                    ),
                ]
            )
        );

        var decorationLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-badge-top-right")
            .Instance;

        // act
        var decorationLayerSpec = GetLayerSpec(decorationLayer);

        // assert
        GetLayoutValue(decorationLayerSpec, "text-anchor").Should().Be("top-right");
        GetLayoutValue(decorationLayerSpec, "icon-anchor").Should().Be("top-right");
        GetLayoutValue(decorationLayerSpec, "icon-text-fit").Should().Be("width");
        GetLayoutValue(decorationLayerSpec, "icon-text-fit-padding")
            .Should()
            .BeEquivalentTo(new[] { 2.0, 4.0, 2.0, 4.0 });
    }

    private static CircleLayer GetPrimaryHitArea(IRenderedComponent<MapTrackedEntityHarness> cut) =>
        cut.FindComponents<CircleLayer>().Single(layer => layer.Instance.Id == "tracked-data-hit-area").Instance;

    private static LayerFeatureEventArgs CreateItemFeatureEvent(string entityId, string? decorationId = null)
    {
        var json = decorationId is null
            ? $"{{\"{TrackedEntityFeatureProperties.EntityId}\":\"{entityId}\"}}"
            : $"{{\"{TrackedEntityFeatureProperties.EntityId}\":\"{entityId}\",\"{TrackedEntityFeatureProperties.DecorationId}\":\"{decorationId}\"}}";

        var properties = JsonSerializer.Deserialize<JsonElement>(json);

        return new LayerFeatureEventArgs("tracked-data-symbols", new Coordinate(49.6, 6.1), properties);
    }

    private static LayerFeatureEventArgs CreateClusterFeatureEvent()
    {
        var properties = JsonSerializer.Deserialize<JsonElement>("{\"cluster_id\":42}");
        return new LayerFeatureEventArgs("tracked-data-cluster-count", new Coordinate(49.6, 6.1), properties);
    }

    private static IReadOnlyDictionary<string, object?> GetLayerSpec(LayerBase layer)
    {
        var spec = layer.BuildLayerSpec();
        return spec;
    }

    private static object? GetPaintValue(IReadOnlyDictionary<string, object?> layerSpec, string propertyName)
    {
        var paint = layerSpec["paint"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        var found = paint.TryGetValue(propertyName, out var value);
        found.Should().BeTrue();
        return value;
    }

    private static object? GetLayoutValue(IReadOnlyDictionary<string, object?> layerSpec, string propertyName)
    {
        var layout = layerSpec["layout"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        var found = layout.TryGetValue(propertyName, out var value);
        found.Should().BeTrue();
        return value;
    }

    public sealed class MapTrackedEntityHarness : ComponentBase
    {
        [Parameter]
        public IReadOnlyList<TestVehicle> Items { get; set; } =
        [new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon")];

        [Parameter]
        public bool EnableCluster { get; set; }

        [Parameter]
        public Func<TestVehicle, PopupOptions?>? PopupSelector { get; set; }

        [Parameter]
        public Func<Task>? BeforeShowPopupAsync { get; set; }

        [Parameter]
        public IReadOnlyList<TrackedEntityDecorationOptions<TestVehicle>> Decorations { get; set; } =
        [new TrackedEntityDecorationOptions<TestVehicle>("label", TextSelector: _ => "label")];

        public SgbMap Map { get; private set; } = null!;

        public void UpdateItems(IReadOnlyList<TestVehicle> items)
        {
            Items = items;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var layer = new TrackedEntityLayerDefinition<TestVehicle>(
                Id: "tracked-data",
                Items: Items,
                IdOptions: new TrackedEntityIdOptions<TestVehicle>(vehicle => vehicle.Id),
                Visual: new TrackedEntityVisualOptions<TestVehicle>(
                    Symbol: new TrackedEntitySymbolOptions<TestVehicle>(
                        vehicle => vehicle.Position,
                        vehicle => vehicle.IconImage,
                        PopupSelector: PopupSelector
                    ),
                    Decorations: Decorations,
                    Cluster: EnableCluster
                        ? new TrackedEntityClusterOptions(Enabled: true, MinPoints: 1)
                        : new TrackedEntityClusterOptions(),
                    Animation: null,
                    Visible: true,
                    PrimaryIconOpacity: null
                ),
                Behavior: new TrackedEntityBehaviorOptions<TestVehicle>(
                    new TrackedEntityInteractionOptions<TestVehicle>(
                        IsHovered: vehicle => vehicle.IsHovered,
                        IsSelected: vehicle => vehicle.IsSelected
                    )
                ),
                Callbacks: new TrackedEntityCallbacks<TestVehicle>(
                    OnItemClick: null,
                    OnItemMouseEnter: null,
                    OnItemMouseLeave: null,
                    OnBeforeShowPopup: BeforeShowPopupAsync
                )
            );

            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(
                2,
                nameof(SgbMap.ChildContent),
                (RenderFragment)(
                    mapBuilder =>
                    {
                        mapBuilder.OpenComponent<MapOverlays>(0);
                        mapBuilder.AddAttribute(
                            1,
                            nameof(MapOverlays.ChildContent),
                            (RenderFragment)(
                                overlayBuilder =>
                                {
                                    overlayBuilder.OpenComponent<TrackedEntityLayer<TestVehicle>>(0);
                                    overlayBuilder.AddAttribute(
                                        1,
                                        nameof(TrackedEntityLayer<TestVehicle>.Layer),
                                        layer
                                    );
                                    overlayBuilder.CloseComponent();
                                }
                            )
                        );
                        mapBuilder.CloseComponent();
                    }
                )
            );
            builder.AddComponentReferenceCapture(3, value => Map = (SgbMap)value);
            builder.CloseComponent();
        }
    }

    public sealed record TestVehicle(
        string Id,
        Coordinate Position,
        string IconImage,
        bool IsHovered = false,
        bool IsSelected = false
    );
}
