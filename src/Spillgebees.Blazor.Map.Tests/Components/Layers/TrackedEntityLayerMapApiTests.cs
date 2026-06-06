using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map;

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
            parameters.Add(p => p.PopupSelector, vehicle => PopupOptions.FromText(vehicle.Id, PopupTrigger.Hover))
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
    public async Task Should_preserve_text_content_mode_when_opening_tracked_entity_popup(
        CancellationToken cancellationToken
    )
    {
        // arrange
        cancellationToken.ThrowIfCancellationRequested();
        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters.Add(p => p.PopupSelector, vehicle => PopupOptions.FromText(vehicle.Id, PopupTrigger.Hover))
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        var hitArea = GetPrimaryHitArea(cut);

        // act
        await cut.InvokeAsync(() => hitArea.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-1")));

        // assert
        JSInterop.Invocations[ShowPopupIdentifier].Should().HaveCount(1);
        var options = JSInterop
            .Invocations[ShowPopupIdentifier][0]
            .Arguments[2]
            .Should()
            .BeOfType<PopupOptions>()
            .Subject;
        options.Content.Should().Be("vehicle-1");
        options.ContentMode.Should().Be(PopupContentMode.Text);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_preserve_raw_html_content_mode_when_opening_tracked_entity_popup(
        CancellationToken cancellationToken
    )
    {
        // arrange
        cancellationToken.ThrowIfCancellationRequested();
        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters.Add(
                p => p.PopupSelector,
                vehicle => PopupOptions.FromRawHtml($"<strong>{vehicle.Id}</strong>", PopupTrigger.Hover)
            )
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        var hitArea = GetPrimaryHitArea(cut);

        // act
        await cut.InvokeAsync(() => hitArea.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-1")));

        // assert
        JSInterop.Invocations[ShowPopupIdentifier].Should().HaveCount(1);
        var options = JSInterop
            .Invocations[ShowPopupIdentifier][0]
            .Arguments[2]
            .Should()
            .BeOfType<PopupOptions>()
            .Subject;
        options.Content.Should().Be("<strong>vehicle-1</strong>");
        options.ContentMode.Should().Be(PopupContentMode.RawHtml);
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
                .Add(p => p.PopupSelector, vehicle => PopupOptions.FromText(vehicle.Id, PopupTrigger.Hover))
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
                .Add(p => p.PopupSelector, vehicle => PopupOptions.FromText(vehicle.Id, PopupTrigger.Hover))
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
        var options = JSInterop
            .Invocations[ShowPopupIdentifier][0]
            .Arguments[2]
            .Should()
            .BeOfType<PopupOptions>()
            .Subject;
        options.Content.Should().Be("vehicle-2");
        options.ContentMode.Should().Be(PopupContentMode.Text);
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
    public async Task Should_not_zoom_to_dissolve_when_cluster_click_behavior_is_none(
        CancellationToken cancellationToken
    )
    {
        // arrange
        cancellationToken.ThrowIfCancellationRequested();
        var clusterOptions = ClusterOptions.Create(minPoints: 1, clickBehavior: ClusterClickBehavior.None);
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.ClusterOptions, clusterOptions));
        await cut.Instance.Map.OnMapInitializedAsync();
        var clusterCount = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-cluster-count")
            .Instance;

        // act
        await cut.InvokeAsync(() => clusterCount.OnClick.InvokeAsync(CreateClusterFeatureEvent()));

        // assert
        JSInterop.Invocations[GetClusterExpansionZoomIdentifier].Should().BeEmpty();
        JSInterop.Invocations[FlyToIdentifier].Should().BeEmpty();
    }

    [Test]
    public void Should_not_render_tracked_cluster_hit_area_when_cluster_click_behavior_is_none()
    {
        // arrange
        var clusterOptions = ClusterOptions.Create(clickBehavior: ClusterClickBehavior.None);

        // act
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.ClusterOptions, clusterOptions));

        // assert
        cut.FindComponents<CircleLayer>()
            .Should()
            .NotContain(layer => layer.Instance.Id == "tracked-data-cluster-hit-area");
    }

    [Test]
    public void Should_render_tracked_cluster_hit_area_when_default_cluster_click_behavior_is_interactive()
    {
        // arrange
        var clusterOptions = ClusterOptions.Create(clickBehavior: ClusterClickBehavior.ZoomToDissolve);

        // act
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.ClusterOptions, clusterOptions));

        // assert
        cut.FindComponents<CircleLayer>()
            .Should()
            .Contain(layer => layer.Instance.Id == "tracked-data-cluster-hit-area");
    }

    [Test]
    public void Should_render_default_tracked_cluster_layers_with_existing_ids_and_order()
    {
        // arrange
        var cut = Render<MapTrackedEntityHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );

        // act
        var hitArea = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-cluster-hit-area")
            .Instance;
        var cluster = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-clusters")
            .Instance;
        var count = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-cluster-count")
            .Instance;

        // assert
        cluster.AfterLayerGroup.Should().Be(hitArea.Id);
        count.AfterLayerGroup.Should().Be(cluster.Id);
        GetPaintValue(GetLayerSpec(cluster), "circle-color").Should().Be("#2563eb");
        GetLayoutValue(GetLayerSpec(count), "text-field")
            .Should()
            .BeEquivalentTo(new object[] { "get", "point_count_abbreviated" });
    }

    [Test]
    public void Should_render_custom_tracked_cluster_circle_and_count_layers()
    {
        // arrange
        var clusterOptions = ClusterOptions.Create(
            layerSet: ClusterLayerSet.Custom(
                ClusterLayerDefinition.Circle("outer", color: "#111827", radius: 36),
                ClusterLayerDefinition.Circle("inner", color: "#60a5fa", radius: 18),
                ClusterLayerDefinition.Symbol("count", textColor: "#f8fafc", textSize: 12)
            )
        );

        // act
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.ClusterOptions, clusterOptions));

        // assert
        cut.FindComponents<CircleLayer>().Should().Contain(layer => layer.Instance.Id == "tracked-data-outer");
        cut.FindComponents<CircleLayer>().Should().Contain(layer => layer.Instance.Id == "tracked-data-inner");
        cut.FindComponents<SymbolLayer>().Should().Contain(layer => layer.Instance.Id == "tracked-data-count");
        cut.FindComponents<CircleLayer>()
            .Should()
            .NotContain(layer => layer.Instance.Id == "tracked-data-cluster-hit-area");
    }

    [Test]
    public void Should_enable_tracked_source_clustering_without_visual_layers()
    {
        // arrange
        var clusterOptions = ClusterOptions.Create(
            radius: 64,
            maxZoom: 12,
            minPoints: 3,
            layerSet: ClusterLayerSet.None
        );

        // act
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.ClusterOptions, clusterOptions));
        var source = cut.FindComponents<GeoJsonSource>().First(source => source.Instance.Id == "tracked-data").Instance;

        // assert
        source.ClusterOptions!.Enabled.Should().BeTrue();
        source.ClusterOptions.LayerSet.Should().Be(ClusterLayerSet.None);
        source.ClusterOptions.Radius.Should().Be(clusterOptions.Radius);
        cut.FindComponents<CircleLayer>()
            .Should()
            .NotContain(layer => layer.Instance.Id.Contains("cluster", StringComparison.Ordinal));
        cut.FindComponents<SymbolLayer>()
            .Should()
            .NotContain(layer => layer.Instance.Id.Contains("cluster", StringComparison.Ordinal));
    }

    [Test]
    public void Should_pass_shared_cluster_source_options_to_tracked_source()
    {
        // arrange
        var properties = new Dictionary<string, object>
        {
            ["sum"] = new object[] { "+", new object[] { "get", "value" } },
        };
        var clusterOptions = ClusterOptions.Create(radius: 72, maxZoom: 10, minPoints: 4, properties: properties);

        // act
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.ClusterOptions, clusterOptions));
        var source = cut.FindComponents<GeoJsonSource>().First(source => source.Instance.Id == "tracked-data").Instance;

        // assert
        source.ClusterOptions.Should().NotBeSameAs(clusterOptions);
        source.ClusterOptions!.Radius.Should().Be(72);
        source.ClusterOptions.MaxZoom.Should().Be(10);
        source.ClusterOptions.MinPoints.Should().Be(4);
        source.ClusterOptions.Properties.Should().BeEquivalentTo(properties);
        source.ClusterOptions.LayerSet.Should().Be(ClusterLayerSet.None);
    }

    [Test]
    public void Should_not_pass_primary_cluster_properties_to_decoration_source()
    {
        // arrange
        var properties = new Dictionary<string, object>
        {
            ["sum"] = new object[] { "+", new object[] { "get", "value" } },
        };
        var clusterOptions = ClusterOptions.Create(radius: 72, minPoints: 4, properties: properties);

        // act
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.ClusterOptions, clusterOptions));
        var decorationSource = cut.FindComponents<GeoJsonSource>()
            .First(source => source.Instance.Id == "tracked-data-decorations")
            .Instance;

        // assert
        decorationSource.ClusterOptions!.Enabled.Should().BeTrue();
        decorationSource.ClusterOptions.Properties.Should().BeNull();
    }

    [Test]
    public void Should_preserve_fractional_custom_cluster_layer_zoom_ranges()
    {
        // arrange
        var clusterOptions = ClusterOptions.Create(
            layerSet: ClusterLayerSet.Custom(
                ClusterLayerDefinition.Circle("outer", minZoom: 4.25, maxZoom: 10.75),
                ClusterLayerDefinition.Symbol("count", minZoom: 5.5, maxZoom: 12.25)
            )
        );

        // act
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.ClusterOptions, clusterOptions));
        var circle = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-outer")
            .Instance;
        var symbol = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-count")
            .Instance;

        // assert
        circle.MinZoom.Should().Be(4.25);
        circle.MaxZoom.Should().Be(10.75);
        GetLayerSpec(circle)["minzoom"].Should().Be(4.25);
        GetLayerSpec(circle)["maxzoom"].Should().Be(10.75);
        symbol.MinZoom.Should().Be(5.5);
        symbol.MaxZoom.Should().Be(12.25);
        GetLayerSpec(symbol)["minzoom"].Should().Be(5.5);
        GetLayerSpec(symbol)["maxzoom"].Should().Be(12.25);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_zoom_to_dissolve_when_custom_interactive_cluster_layer_is_clicked(
        CancellationToken cancellationToken
    )
    {
        // arrange
        cancellationToken.ThrowIfCancellationRequested();
        var clusterOptions = ClusterOptions.Create(
            layerSet: ClusterLayerSet.Custom(ClusterLayerDefinition.Circle("custom-cluster", color: "#000000"))
        );
        var cut = Render<MapTrackedEntityHarness>(parameters => parameters.Add(p => p.ClusterOptions, clusterOptions));
        await cut.Instance.Map.OnMapInitializedAsync();
        var customCluster = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-custom-cluster")
            .Instance;

        // act
        await cut.InvokeAsync(() => customCluster.OnClick.InvokeAsync(CreateClusterFeatureEvent()));

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
        public ClusterOptions? ClusterOptions { get; set; }

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
                    Source: new TrackedEntitySourceOptions(
                        ClusterOptions
                            ?? (
                                EnableCluster
                                    ? Spillgebees.Blazor.Map.ClusterOptions.Create(minPoints: 1)
                                    : Spillgebees.Blazor.Map.ClusterOptions.None
                            )
                    ),
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
                        mapBuilder.OpenComponent<MapFeatures>(0);
                        mapBuilder.AddAttribute(
                            1,
                            nameof(MapFeatures.ChildContent),
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
