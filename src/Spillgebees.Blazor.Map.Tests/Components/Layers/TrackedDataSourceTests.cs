using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Models.TrackedData;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class TrackedDataSourceTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string GetProtocolVersionIdentifier = "Spillgebees.Map.getProtocolVersion";
    private const string SetTrackedEntityFeatureStateIdentifier =
        "Spillgebees.Map.mapFunctions.setTrackedEntityFeatureState";
    private const string FlyToIdentifier = "Spillgebees.Map.mapFunctions.flyTo";
    private const string GetClusterExpansionZoomIdentifier = "Spillgebees.Map.mapFunctions.getClusterExpansionZoom";

    public TrackedDataSourceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.Setup<int>(GetProtocolVersionIdentifier).SetResult(9);
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(SetTrackedEntityFeatureStateIdentifier);
        JSInterop.SetupVoid(FlyToIdentifier);
        JSInterop.Setup<double>(GetClusterExpansionZoomIdentifier).SetResult(11.2);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_wire_generated_item_callbacks_cluster_click_behavior_and_visibility(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<TrackedDataSourceHarness>(parameters =>
            parameters
                .Add(
                    p => p.Items,
                    new[] { new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1") }
                )
                .Add(p => p.Visible, false)
                .Add(p => p.EnableCluster, true)
        );

        await cut.Instance.Map.OnMapInitializedAsync();

        var iconLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-symbols")
            .Instance;
        var primaryHitAreaLayer = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-hit-area")
            .Instance;
        var labelLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-label")
            .Instance;
        var clusterCountLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-cluster-count")
            .Instance;

        // act
        var iconLayerHasHoverEnter = iconLayer.OnMouseEnter.HasDelegate;
        var iconLayerHasHoverLeave = iconLayer.OnMouseLeave.HasDelegate;
        var primaryHitAreaHasClick = primaryHitAreaLayer.OnClick.HasDelegate;
        var primaryHitAreaHasHoverEnter = primaryHitAreaLayer.OnMouseEnter.HasDelegate;
        var primaryHitAreaHasHoverLeave = primaryHitAreaLayer.OnMouseLeave.HasDelegate;
        var labelLayerHasHoverEnter = labelLayer.OnMouseEnter.HasDelegate;
        var labelLayerHasHoverLeave = labelLayer.OnMouseLeave.HasDelegate;

        // act
        await cut.InvokeAsync(() => primaryHitAreaLayer.OnClick.InvokeAsync(CreateItemFeatureEvent("vehicle-1")));
        await cut.InvokeAsync(() => primaryHitAreaLayer.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-1")));
        await cut.InvokeAsync(() => labelLayer.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-1", "label")));
        await cut.InvokeAsync(() => primaryHitAreaLayer.OnMouseLeave.InvokeAsync());
        await cut.InvokeAsync(() => clusterCountLayer.OnClick.InvokeAsync(CreateClusterFeatureEvent()));

        // assert
        iconLayerHasHoverEnter.Should().BeTrue();
        iconLayerHasHoverLeave.Should().BeTrue();
        primaryHitAreaHasClick.Should().BeTrue();
        primaryHitAreaHasHoverEnter.Should().BeTrue();
        primaryHitAreaHasHoverLeave.Should().BeTrue();
        labelLayerHasHoverEnter.Should().BeTrue();
        labelLayerHasHoverLeave.Should().BeTrue();
        cut.Instance.LastItemClick.Should().NotBeNull();
        cut.Instance.LastItemClick!.EntityId.Should().Be("vehicle-1");
        cut.Instance.LastItemClick.DecorationId.Should().BeNull();
        cut.Instance.LastItemHover.Should().NotBeNull();
        cut.Instance.LastItemHover!.EntityId.Should().Be("vehicle-1");
        cut.Instance.LastItemHover.DecorationId.Should().Be("label");
        cut.Instance.ItemMouseLeaveCount.Should().Be(1);
        primaryHitAreaLayer.Visible.Should().BeFalse();
        iconLayer.Visible.Should().BeFalse();
        labelLayer.Visible.Should().BeFalse();
        clusterCountLayer.Visible.Should().BeFalse();
        JSInterop.VerifyInvoke(GetClusterExpansionZoomIdentifier);
        JSInterop.VerifyInvoke(FlyToIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_cancel_pending_hover_leave_when_the_pointer_re_enters_the_same_item(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<TrackedDataSourceHarness>(parameters =>
            parameters.Add(
                p => p.Items,
                [new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1")]
            )
        );

        await cut.Instance.Map.OnMapInitializedAsync();

        var iconLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-symbols")
            .Instance;
        var primaryHitAreaLayer = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-hit-area")
            .Instance;

        // act
        var leaveTask = cut.InvokeAsync(() => primaryHitAreaLayer.OnMouseLeave.InvokeAsync());
        await Task.Delay(100, cancellationToken);
        await cut.InvokeAsync(() => iconLayer.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-1")));
        await leaveTask;
        await Task.Delay(350, cancellationToken);

        // assert
        cut.Instance.LastItemHover.Should().NotBeNull();
        cut.Instance.LastItemHover!.EntityId.Should().Be("vehicle-1");
        cut.Instance.ItemMouseLeaveCount.Should().Be(0);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_clear_hover_after_pointer_leaves_a_decoration_without_re_entry(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<TrackedDataSourceHarness>(parameters =>
            parameters.Add(
                p => p.Items,
                [new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1")]
            )
        );

        await cut.Instance.Map.OnMapInitializedAsync();

        var labelLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "tracked-data-label")
            .Instance;

        // act
        await cut.InvokeAsync(() => labelLayer.OnMouseEnter.InvokeAsync(CreateItemFeatureEvent("vehicle-1", "label")));
        var leaveTask = cut.InvokeAsync(() => labelLayer.OnMouseLeave.InvokeAsync());
        await leaveTask;
        await Task.Delay(350, cancellationToken);

        // assert
        cut.Instance.LastItemHover.Should().NotBeNull();
        cut.Instance.LastItemHover!.DecorationId.Should().Be("label");
        cut.Instance.ItemMouseLeaveCount.Should().Be(1);
    }

    [Test]
    public void Should_honor_symbol_opacity_and_generated_decoration_visibility_expression()
    {
        // arrange
        var iconOpacity = new object[]
        {
            "case",
            new object[]
            {
                "boolean",
                new object[] { "feature-state", TrackedEntityFeatureStates.Selected.Name },
                false,
            },
            1.0,
            0.96,
        };

        var cut = Render<TrackedDataSource<TestVehicle>>(parameters =>
            parameters
                .Add(p => p.Id, "vehicles")
                .Add(
                    p => p.Items,
                    new[] { new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1") }
                )
                .Add(p => p.Identity, new TrackedDataIdentityOptions<TestVehicle>(vehicle => vehicle.Id))
                .Add(
                    p => p.Symbol,
                    new TrackedDataSymbolOptions<TestVehicle>(vehicle => vehicle.Position, vehicle => vehicle.IconImage)
                )
                .Add(p => p.PrimaryIconOpacity, iconOpacity)
                .Add(
                    p => p.Decorations,
                    [
                        new TrackedDataDecorationOptions<TestVehicle>(
                            "service",
                            TextSelector: vehicle => vehicle.Label,
                            Anchor: "left",
                            ColorSelector: _ => "#1e293b",
                            TextSizeSelector: _ => 11
                        ),
                    ]
                )
        );

        var primaryLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "vehicles-symbols")
            .Instance;
        var serviceLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "vehicles-service-left")
            .Instance;

        // act
        var primaryLayerSpec = GetLayerSpec(primaryLayer);
        var serviceLayerSpec = GetLayerSpec(serviceLayer);

        // assert
        GetPaintValue(primaryLayerSpec, "icon-opacity").Should().BeEquivalentTo(iconOpacity);
        serviceLayerSpec.ContainsKey("minzoom").Should().BeFalse();
        GetPaintValue(serviceLayerSpec, "text-opacity")
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
                    new object[] { "==", new object[] { "get", TrackedEntityFeatureProperties.DisplayMode }, "click" },
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
    public void Should_not_gate_decorations_by_cluster_zoom_when_clustering_is_enabled()
    {
        // arrange
        var cut = Render<TrackedDataSource<TestVehicle>>(parameters =>
            parameters
                .Add(p => p.Id, "vehicles")
                .Add(
                    p => p.Items,
                    new[] { new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1") }
                )
                .Add(p => p.Identity, new TrackedDataIdentityOptions<TestVehicle>(vehicle => vehicle.Id))
                .Add(
                    p => p.Symbol,
                    new TrackedDataSymbolOptions<TestVehicle>(vehicle => vehicle.Position, vehicle => vehicle.IconImage)
                )
                .Add(
                    p => p.Decorations,
                    [
                        new TrackedDataDecorationOptions<TestVehicle>(
                            "label",
                            TextSelector: vehicle => vehicle.Label,
                            DisplayMode: TrackedEntityDecorationDisplayMode.Hover
                        ),
                    ]
                )
                .Add(p => p.Cluster, new TrackedDataClusterOptions(Enabled: true, MaxZoom: 12, MinPoints: 1))
        );

        var labelLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "vehicles-label")
            .Instance;

        // act
        var labelLayerSpec = GetLayerSpec(labelLayer);

        // assert
        GetPaintValue(labelLayerSpec, "text-opacity")
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
                    new object[] { "==", new object[] { "get", TrackedEntityFeatureProperties.DisplayMode }, "click" },
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
        TryGetPaintValue(labelLayerSpec, "icon-opacity", out _).Should().BeFalse();
        labelLayerSpec.ContainsKey("minzoom").Should().BeFalse();
    }

    [Test]
    public void Should_use_a_more_reliable_primary_hit_area_radius()
    {
        // arrange
        var cut = Render<TrackedDataSource<TestVehicle>>(parameters =>
            parameters
                .Add(p => p.Id, "vehicles")
                .Add(
                    p => p.Items,
                    new[] { new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1") }
                )
                .Add(p => p.Identity, new TrackedDataIdentityOptions<TestVehicle>(vehicle => vehicle.Id))
                .Add(
                    p => p.Symbol,
                    new TrackedDataSymbolOptions<TestVehicle>(vehicle => vehicle.Position, vehicle => vehicle.IconImage)
                )
        );

        var primaryHitAreaLayer = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "vehicles-hit-area")
            .Instance;

        // act
        var primaryHitAreaSpec = GetLayerSpec(primaryHitAreaLayer);

        // assert
        GetPaintValue(primaryHitAreaSpec, "circle-radius").Should().Be(24.0);
    }

    [Test]
    public void Should_render_internal_tracked_entity_source_and_generated_layers_from_raw_items()
    {
        // arrange
        var items = new[] { new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1") };

        // act
        var cut = Render<TrackedDataSource<TestVehicle>>(parameters =>
            parameters
                .Add(p => p.Id, "vehicles")
                .Add(p => p.Items, items)
                .Add(p => p.Identity, new TrackedDataIdentityOptions<TestVehicle>(vehicle => vehicle.Id))
                .Add(
                    p => p.Symbol,
                    new TrackedDataSymbolOptions<TestVehicle>(
                        vehicle => vehicle.Position,
                        vehicle => vehicle.IconImage,
                        ColorSelector: _ => "#2563eb"
                    )
                )
                .Add(
                    p => p.Decorations,
                    [
                        new TrackedDataDecorationOptions<TestVehicle>(
                            "label",
                            TextSelector: vehicle => vehicle.Label,
                            Offset: new Point(0, -18),
                            Anchor: "top",
                            DisplayMode: TrackedEntityDecorationDisplayMode.Hover
                        ),
                    ]
                )
                .Add(
                    p => p.Cluster,
                    new TrackedDataClusterOptions(Enabled: true, Radius: 64, MaxZoom: 12, MinPoints: 2)
                )
        );

        // assert
        cut.FindComponents<GeoJsonSource>()
            .Select(source => source.Instance.Id)
            .Should()
            .Contain(new[] { "vehicles", "vehicles-decorations" });
        cut.FindComponents<CircleLayer>()
            .Select(layer => layer.Instance.Id)
            .Should()
            .Contain(new[] { "vehicles-cluster-hit-area", "vehicles-hit-area", "vehicles-clusters" });
        cut.FindComponents<SymbolLayer>()
            .Select(layer => layer.Instance.Id)
            .Should()
            .Contain(new[] { "vehicles-cluster-count", "vehicles-symbols", "vehicles-label-top" });
    }

    [Test]
    public void Should_generate_decoration_visibility_from_display_mode_feature_state_contract()
    {
        // arrange
        var cut = Render<TrackedDataSource<TestVehicle>>(parameters =>
            parameters
                .Add(p => p.Id, "vehicles")
                .Add(
                    p => p.Items,
                    new[] { new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1") }
                )
                .Add(p => p.Identity, new TrackedDataIdentityOptions<TestVehicle>(vehicle => vehicle.Id))
                .Add(
                    p => p.Symbol,
                    new TrackedDataSymbolOptions<TestVehicle>(vehicle => vehicle.Position, vehicle => vehicle.IconImage)
                )
                .Add(
                    p => p.Decorations,
                    [
                        new TrackedDataDecorationOptions<TestVehicle>(
                            "hover-label",
                            TextSelector: vehicle => vehicle.Label,
                            DisplayMode: TrackedEntityDecorationDisplayMode.Hover
                        ),
                        new TrackedDataDecorationOptions<TestVehicle>(
                            "selected-label",
                            TextSelector: vehicle => vehicle.Label,
                            DisplayMode: TrackedEntityDecorationDisplayMode.Click
                        ),
                    ]
                )
        );

        var hoverLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "vehicles-hover-label")
            .Instance;
        var selectedLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "vehicles-selected-label")
            .Instance;
        // act
        var hoverLayerSpec = GetLayerSpec(hoverLayer);
        var selectedLayerSpec = GetLayerSpec(selectedLayer);

        // assert
        TryGetPaintValue(hoverLayerSpec, "icon-opacity", out _).Should().BeFalse();
        TryGetPaintValue(selectedLayerSpec, "icon-opacity", out _).Should().BeFalse();
        GetPaintValue(hoverLayerSpec, "text-opacity")
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
                    new object[] { "==", new object[] { "get", TrackedEntityFeatureProperties.DisplayMode }, "click" },
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
    public void Should_honor_generated_primary_and_decoration_anchor_and_text_offset_options()
    {
        // arrange
        var cut = Render<TrackedDataSource<TestVehicle>>(parameters =>
            parameters
                .Add(p => p.Id, "vehicles")
                .Add(
                    p => p.Items,
                    new[] { new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1") }
                )
                .Add(p => p.Identity, new TrackedDataIdentityOptions<TestVehicle>(vehicle => vehicle.Id))
                .Add(
                    p => p.Symbol,
                    new TrackedDataSymbolOptions<TestVehicle>(
                        vehicle => vehicle.Position,
                        vehicle => vehicle.IconImage,
                        AnchorSelector: _ => "bottom"
                    )
                )
                .Add(
                    p => p.Decorations,
                    [
                        new TrackedDataDecorationOptions<TestVehicle>(
                            "label",
                            TextSelector: vehicle => vehicle.Label,
                            Offset: new Point(10, -20),
                            Anchor: "top"
                        ),
                    ]
                )
        );

        var primaryLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "vehicles-symbols")
            .Instance;
        var decorationLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "vehicles-label-top")
            .Instance;

        // act
        var primaryLayerSpec = GetLayerSpec(primaryLayer);
        var decorationLayerSpec = GetLayerSpec(decorationLayer);

        // assert
        // primary layer now uses data-driven anchor expression
        GetLayoutValue(primaryLayerSpec, "icon-anchor")
            .Should()
            .BeEquivalentTo(
                new object[] { "coalesce", new object[] { "get", TrackedEntityFeatureProperties.Anchor }, "center" }
            );
        TryGetLayoutValue(decorationLayerSpec, "icon-anchor", out _).Should().BeFalse();
        GetLayoutValue(decorationLayerSpec, "text-anchor").Should().Be("top");
        GetLayoutValue(decorationLayerSpec, "text-offset").Should().BeEquivalentTo(new[] { 1.0, -2.0 });
        GetLayoutValue(decorationLayerSpec, "text-max-width").Should().Be(999.0);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_apply_declarative_hover_and_selection_state_diffs(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<TrackedDataSourceHarness>(parameters =>
            parameters.Add(
                p => p.Items,
                new[]
                {
                    new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1", true, true),
                    new TestVehicle("vehicle-2", new Coordinate(49.7, 6.2), "vehicle-icon", "Vehicle 2"),
                }
            )
        );

        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count.Should().Be(2));
        var initialInvocationCount = JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count;

        // act
        // act
        await cut.InvokeAsync(() =>
            cut.Instance.UpdateItems([
                new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1"),
                new TestVehicle("vehicle-2", new Coordinate(49.7, 6.2), "vehicle-icon", "Vehicle 2", true, true),
            ])
        );

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count.Should().Be(initialInvocationCount + 4)
        );

        var invocations = JSInterop
            .Invocations[SetTrackedEntityFeatureStateIdentifier]
            .Skip(initialInvocationCount)
            .ToArray();
        invocations.Should().HaveCount(4);
        invocations
            .Select(invocation => invocation.Arguments[3])
            .Should()
            .Equal("vehicle-1", "vehicle-2", "vehicle-1", "vehicle-2");
        invocations
            .Select(invocation => ((IReadOnlyDictionary<string, object>)invocation.Arguments[4]!).Single())
            .Should()
            .Equal(
                new KeyValuePair<string, object>(TrackedEntityFeatureStates.Hover.Name, false),
                new KeyValuePair<string, object>(TrackedEntityFeatureStates.Hover.Name, true),
                new KeyValuePair<string, object>(TrackedEntityFeatureStates.Selected.Name, false),
                new KeyValuePair<string, object>(TrackedEntityFeatureStates.Selected.Name, true)
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replay_active_hover_and_selection_state_after_refreshing_tracked_items(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<TrackedDataSourceHarness>(parameters =>
            parameters.Add(
                p => p.Items,
                [new TestVehicle("vehicle-1", new Coordinate(49.6, 6.1), "vehicle-icon", "Vehicle 1", true, true)]
            )
        );

        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count.Should().Be(2));
        var initialInvocationCount = JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count;

        // act
        await cut.InvokeAsync(() =>
            cut.Instance.UpdateItems([
                new TestVehicle("vehicle-1", new Coordinate(49.61, 6.11), "vehicle-icon", "Vehicle 1", true, true),
            ])
        );

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[SetTrackedEntityFeatureStateIdentifier].Count.Should().Be(initialInvocationCount + 2)
        );

        var invocations = JSInterop
            .Invocations[SetTrackedEntityFeatureStateIdentifier]
            .Skip(initialInvocationCount)
            .ToArray();

        invocations.Should().HaveCount(2);
        invocations.Select(invocation => invocation.Arguments[3]).Should().Equal("vehicle-1", "vehicle-1");
        invocations
            .Select(invocation => ((IReadOnlyDictionary<string, object>)invocation.Arguments[4]!).Single())
            .Should()
            .Equal(
                new KeyValuePair<string, object>(TrackedEntityFeatureStates.Hover.Name, true),
                new KeyValuePair<string, object>(TrackedEntityFeatureStates.Selected.Name, true)
            );
    }

    public sealed class TrackedDataSourceHarness : ComponentBase
    {
        [Parameter]
        public IReadOnlyList<TestVehicle> Items { get; set; } = [];

        [Parameter]
        public bool Visible { get; set; } = true;

        [Parameter]
        public bool EnableCluster { get; set; }

        public SgbMap Map { get; private set; } = null!;

        public TrackedEntityInteractionEventArgs<TestVehicle>? LastItemClick { get; private set; }

        public TrackedEntityInteractionEventArgs<TestVehicle>? LastItemHover { get; private set; }

        public int ItemMouseLeaveCount { get; private set; }

        public void UpdateItems(IReadOnlyList<TestVehicle> items)
        {
            // arrange
            Items = items;

            // act
            StateHasChanged();

            // assert
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
                        mapBuilder.OpenComponent<TrackedDataSource<TestVehicle>>(0);
                        mapBuilder.AddAttribute(1, "Id", "tracked-data");
                        mapBuilder.AddAttribute(2, "Items", Items);
                        mapBuilder.AddAttribute(
                            3,
                            "Identity",
                            new TrackedDataIdentityOptions<TestVehicle>(vehicle => vehicle.Id)
                        );
                        mapBuilder.AddAttribute(
                            4,
                            "Symbol",
                            new TrackedDataSymbolOptions<TestVehicle>(
                                vehicle => vehicle.Position,
                                vehicle => vehicle.IconImage,
                                ColorSelector: _ => "#2563eb"
                            )
                        );
                        mapBuilder.AddAttribute(
                            5,
                            "Decorations",
                            new[]
                            {
                                new TrackedDataDecorationOptions<TestVehicle>(
                                    "label",
                                    TextSelector: vehicle => vehicle.Label,
                                    DisplayMode: TrackedEntityDecorationDisplayMode.Hover
                                ),
                            }
                        );
                        mapBuilder.AddAttribute(
                            6,
                            "Interaction",
                            new TrackedDataInteractionOptions<TestVehicle>(
                                IsHovered: vehicle => vehicle.IsHovered,
                                IsSelected: vehicle => vehicle.IsSelected
                            )
                        );
                        mapBuilder.AddAttribute(7, "Visible", Visible);
                        mapBuilder.AddAttribute(
                            8,
                            "Cluster",
                            EnableCluster
                                ? new TrackedDataClusterOptions(Enabled: true, MinPoints: 1)
                                : new TrackedDataClusterOptions()
                        );
                        mapBuilder.AddAttribute(
                            9,
                            "OnItemClick",
                            EventCallback.Factory.Create<TrackedEntityInteractionEventArgs<TestVehicle>>(
                                this,
                                (TrackedEntityInteractionEventArgs<TestVehicle> args) => LastItemClick = args
                            )
                        );
                        mapBuilder.AddAttribute(
                            10,
                            "OnItemMouseEnter",
                            EventCallback.Factory.Create<TrackedEntityInteractionEventArgs<TestVehicle>>(
                                this,
                                (TrackedEntityInteractionEventArgs<TestVehicle> args) => LastItemHover = args
                            )
                        );
                        mapBuilder.AddAttribute(
                            11,
                            "OnItemMouseLeave",
                            EventCallback.Factory.Create(this, () => ItemMouseLeaveCount++)
                        );
                        mapBuilder.CloseComponent();
                    }
                )
            );
            builder.AddComponentReferenceCapture(2, value => Map = (SgbMap)value);
            builder.CloseComponent();
        }
    }

    public sealed record TestVehicle(
        string Id,
        Coordinate Position,
        string IconImage,
        string? Label,
        bool IsHovered = false,
        bool IsSelected = false
    );

    private static IReadOnlyDictionary<string, object?> GetLayerSpec(LayerBase layer)
    {
        // arrange

        // act
        var spec = layer.BuildLayerSpec();

        // assert
        return spec;
    }

    private static object? GetPaintValue(IReadOnlyDictionary<string, object?> layerSpec, string propertyName)
    {
        // arrange
        var paint = layerSpec["paint"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;

        // act
        var found = paint.TryGetValue(propertyName, out var value);

        // assert
        found.Should().BeTrue();
        return value;
    }

    private static object? GetLayoutValue(IReadOnlyDictionary<string, object?> layerSpec, string propertyName)
    {
        // arrange
        var layout = layerSpec["layout"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;

        // act
        var found = layout.TryGetValue(propertyName, out var value);

        // assert
        found.Should().BeTrue();
        return value;
    }

    private static bool TryGetPaintValue(
        IReadOnlyDictionary<string, object?> layerSpec,
        string propertyName,
        out object? value
    )
    {
        // arrange
        var paint = layerSpec["paint"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;

        // act
        return paint.TryGetValue(propertyName, out value);
    }

    private static bool TryGetLayoutValue(
        IReadOnlyDictionary<string, object?> layerSpec,
        string propertyName,
        out object? value
    )
    {
        // arrange
        var layout = layerSpec["layout"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;

        // act
        return layout.TryGetValue(propertyName, out value);
    }

    private static LayerFeatureEventArgs CreateItemFeatureEvent(string entityId, string? decorationId = null)
    {
        // arrange
        var json = decorationId is null
            ? $"{{\"{TrackedEntityFeatureProperties.EntityId}\":\"{entityId}\"}}"
            : $"{{\"{TrackedEntityFeatureProperties.EntityId}\":\"{entityId}\",\"{TrackedEntityFeatureProperties.DecorationId}\":\"{decorationId}\"}}";

        // act
        var properties = JsonSerializer.Deserialize<JsonElement>(json);

        // assert
        return new LayerFeatureEventArgs("tracked-data-symbols", new Coordinate(49.6, 6.1), properties);
    }

    private static LayerFeatureEventArgs CreateClusterFeatureEvent()
    {
        // arrange
        var properties = JsonSerializer.Deserialize<JsonElement>("{\"cluster_id\":42}");

        // act

        // assert
        return new LayerFeatureEventArgs("tracked-data-cluster-count", new Coordinate(49.6, 6.1), properties);
    }
}
