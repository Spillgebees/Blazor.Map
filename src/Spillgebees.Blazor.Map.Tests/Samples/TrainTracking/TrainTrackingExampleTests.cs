using System.Reflection;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Models.Expressions;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Tests.Samples.TrainTracking;

public class TrainTrackingExampleTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string GetClusterExpansionZoomIdentifier = "Spillgebees.Map.mapFunctions.getClusterExpansionZoom";
    private const string FlyToIdentifier = "Spillgebees.Map.mapFunctions.flyTo";
    private const string HasStyleLayerIdentifier = "Spillgebees.Map.mapFunctions.hasStyleLayer";
    private const string GetZoomIdentifier = "Spillgebees.Map.mapFunctions.getZoom";
    private const string ShowPopupIdentifier = "Spillgebees.Map.mapFunctions.showPopup";
    private const string ClosePopupIdentifier = "Spillgebees.Map.mapFunctions.closePopup";
    private const string SetStyleLayerVisibilityIdentifier = "Spillgebees.Map.mapFunctions.setStyleLayerVisibility";
    private const string SetControlContentIdentifier = "Spillgebees.Map.mapFunctions.setControlContent";

    public TrainTrackingExampleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IConfiguration>(CreateConfiguration());

        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.Setup<double>(GetClusterExpansionZoomIdentifier).SetResult(11.2);
        JSInterop.Setup<bool>(HasStyleLayerIdentifier).SetResult(true);
        JSInterop.Setup<double?>(GetZoomIdentifier).SetResult(9);
        JSInterop.SetupVoid(FlyToIdentifier);
        JSInterop.SetupVoid(ClosePopupIdentifier);
        JSInterop.SetupVoid(SetControlContentIdentifier);
        JSInterop.SetupVoid(SetStyleLayerVisibilityIdentifier);
    }

    private static IConfiguration CreateConfiguration(
        string? overlayStyleUrl = null,
        string? composedGlyphsUrl = null
    ) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [TrainTrackingPresentation.OverlayStyleUrlConfigurationKey] = overlayStyleUrl,
                    [TrainTrackingPresentation.ComposedGlyphsUrlConfigurationKey] = composedGlyphsUrl,
                }
            )
            .Build();

    [Test]
    public void Should_render_trains_via_tracked_data_source_for_feature_state_driven_interactions()
    {
        // arrange & act
        var cut = Render<TrainTrackingExample>();
        var trackedLayer = ResolveTrackedLayer(cut);

        // assert
        cut.FindComponents<TrackedEntityLayer<TrainSampleState>>().Should().HaveCount(1);
        cut.FindComponents<GeoJsonSource>()
            .Select(source => source.Instance.Id)
            .Should()
            .Contain(["train-source", "train-source-decorations"]);
        cut.FindComponents<SymbolLayer>()
            .Select(layer => layer.Instance.Id)
            .Should()
            .Contain(["train-source-symbols", "train-source-route-left", "train-source-operator-right"]);
        cut.FindComponents<CircleLayer>()
            .Select(layer => layer.Instance.Id)
            .Should()
            .Contain(["train-source-hit-area"]);

        trackedLayer.Visual.Cluster.Enabled.Should().BeTrue();
        trackedLayer.Visual.Cluster.ClickBehavior.Should().Be(TrackedEntityClusterClickBehavior.ZoomToDissolve);
        trackedLayer.Visual.Cluster.Properties.Should().NotBeNull();
        trackedLayer.Visual.Cluster.Properties!.Should().ContainKey("internationalPresence");
        trackedLayer.Items.Should().NotBeEmpty();
        trackedLayer.Items.Any(train => train.Id == "cfl-re11").Should().BeTrue();
        cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "train-source-cluster-count")
            .Instance.OnClick.HasDelegate.Should()
            .BeTrue();

        cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "train-source-cluster-hit-area")
            .Instance.OnClick.HasDelegate.Should()
            .BeTrue();
        cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "train-source-hit-area")
            .Instance.OnClick.HasDelegate.Should()
            .BeTrue();
    }

    [Test]
    public void Should_avoid_feature_state_expressions_in_train_layout_properties()
    {
        // arrange & act
        var cut = Render<TrainTrackingExample>();

        // assert
        var iconLayerSpec = GetLayerSpec(
            cut.FindComponents<SymbolLayer>().Single(layer => layer.Instance.Id == "train-source-symbols").Instance
        );
        var serviceLayerSpec = GetLayerSpec(
            cut.FindComponents<SymbolLayer>().Single(layer => layer.Instance.Id == "train-source-service-left").Instance
        );

        LayoutPropertyShouldNotContainFeatureState(iconLayerSpec, "icon-size");
        LayoutPropertyShouldNotContainFeatureState(iconLayerSpec, "symbol-sort-key");
        TryGetLayoutValue(serviceLayerSpec, "text-rotate", out var textRotate).Should().BeFalse();
        textRotate.Should().BeNull();
        GetLayoutValue(serviceLayerSpec, "text-pitch-alignment").Should().Be("viewport");
        GetLayoutValue(serviceLayerSpec, "text-rotation-alignment").Should().Be("viewport");
    }

    [Test]
    public void Should_keep_train_labels_screen_aligned_and_clusters_single_color()
    {
        // arrange & act
        var cut = Render<TrainTrackingExample>();

        // assert
        var serviceLayerSpec = GetLayerSpec(
            cut.FindComponents<SymbolLayer>().Single(layer => layer.Instance.Id == "train-source-service-left").Instance
        );
        var routeLayerSpec = GetLayerSpec(
            cut.FindComponents<SymbolLayer>().Single(layer => layer.Instance.Id == "train-source-route-left").Instance
        );
        var operatorLayerSpec = GetLayerSpec(
            cut.FindComponents<SymbolLayer>()
                .Single(layer => layer.Instance.Id == "train-source-operator-right")
                .Instance
        );
        var clusterLayerSpec = GetLayerSpec(
            cut.FindComponents<CircleLayer>().Single(layer => layer.Instance.Id == "train-source-clusters").Instance
        );
        var clusterHitAreaSpec = GetLayerSpec(
            cut.FindComponents<CircleLayer>()
                .Single(layer => layer.Instance.Id == "train-source-cluster-hit-area")
                .Instance
        );
        var clusterCountLayerSpec = GetLayerSpec(
            cut.FindComponents<SymbolLayer>()
                .Single(layer => layer.Instance.Id == "train-source-cluster-count")
                .Instance
        );

        GetLayoutValue(serviceLayerSpec, "text-pitch-alignment").Should().Be("viewport");
        GetLayoutValue(serviceLayerSpec, "text-rotation-alignment").Should().Be("viewport");
        GetLayoutValue(routeLayerSpec, "text-pitch-alignment").Should().Be("viewport");
        GetLayoutValue(routeLayerSpec, "text-rotation-alignment").Should().Be("viewport");
        GetLayoutValue(operatorLayerSpec, "text-pitch-alignment").Should().Be("viewport");
        GetLayoutValue(operatorLayerSpec, "text-rotation-alignment").Should().Be("viewport");
        GetLayoutValue(serviceLayerSpec, "text-offset")
            .Should()
            .BeEquivalentTo(
                new[] { 1.33, -0.33 },
                options =>
                    options
                        .Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 1e-10))
                        .WhenTypeIs<double>()
            );
        GetLayoutValue(routeLayerSpec, "text-offset")
            .Should()
            .BeEquivalentTo(
                new[] { 2.0, 0.75 },
                options =>
                    options
                        .Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 1e-10))
                        .WhenTypeIs<double>()
            );
        GetLayoutValue(operatorLayerSpec, "text-offset")
            .Should()
            .BeEquivalentTo(
                new[] { -1.6, -0.4 },
                options =>
                    options
                        .Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 1e-10))
                        .WhenTypeIs<double>()
            );
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
        GetPaintValue(clusterLayerSpec, "circle-color").Should().Be("#2563eb");
        GetPaintValue(clusterLayerSpec, "circle-stroke-color").Should().Be("#dbeafe");
        GetPaintValue(clusterLayerSpec, "circle-pitch-alignment").Should().Be("viewport");
        GetPaintValue(clusterHitAreaSpec, "circle-pitch-alignment").Should().Be("viewport");
        GetLayoutValue(clusterCountLayerSpec, "text-pitch-alignment").Should().Be("viewport");
        GetLayoutValue(clusterCountLayerSpec, "text-rotation-alignment").Should().Be("viewport");
    }

    [Test]
    public void Should_compose_base_and_overlay_styles_when_overlay_url_is_configured()
    {
        // arrange
        Services.AddSingleton<IConfiguration>(CreateConfiguration("https://example.com/luxembourg/style.json"));

        // act
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;

        // assert
        map.MapOptions.Style.Should().BeNull();
        map.MapOptions.Styles.Should().NotBeNull();
        map.MapOptions.Styles!.Should().HaveCount(2);
        map.MapOptions.Styles![0].Should().Be(MapStyle.OpenFreeMap.Positron);
        map.MapOptions.Styles![1].Url.Should().Be("https://example.com/luxembourg/style.json");
    }

    [Test]
    public void Should_fall_back_to_hosted_overlay_style_when_overlay_url_is_not_configured()
    {
        // arrange
        Services.AddSingleton<IConfiguration>(CreateConfiguration(null));

        // act
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;

        // assert
        map.MapOptions.Style.Should().BeNull();
        map.MapOptions.Styles.Should().NotBeNull();
        map.MapOptions.Styles!.Should().HaveCount(2);
        map.MapOptions.Styles![0].Should().Be(MapStyle.OpenFreeMap.Positron);
        map.MapOptions.Styles![1].Url.Should().Be(TrainTrackingPresentation.DefaultOverlayStyleUrl);
    }

    [Test]
    public void Should_set_composed_glyphs_url_when_configured()
    {
        // arrange
        Services.AddSingleton<IConfiguration>(
            CreateConfiguration(
                overlayStyleUrl: "https://example.com/luxembourg/style.json",
                composedGlyphsUrl: "https://example.com/fonts/{fontstack}/{range}.pbf"
            )
        );

        // act
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;

        // assert
        map.MapOptions.ComposedGlyphsUrl.Should().Be("https://example.com/fonts/{fontstack}/{range}.pbf");
    }

    [Test]
    public void Should_leave_composed_glyphs_url_null_when_not_configured()
    {
        // arrange
        Services.AddSingleton<IConfiguration>(CreateConfiguration(null));

        // act
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;

        // assert
        map.MapOptions.ComposedGlyphsUrl.Should().BeNull();
    }

    [Test]
    public void Should_render_map_with_expected_component_layers()
    {
        // arrange & act
        var cut = Render<TrainTrackingExample>();

        // assert
        cut.FindComponents<FillExtrusionLayer>()
            .Select(layer => layer.Instance.Id)
            .Should()
            .Contain(["sgb-buildings-3d"])
            .And.NotContain(["sgb-platforms-3d"]);
        cut.FindComponents<CircleLayer>()
            .Select(layer => layer.Instance.Id)
            .Should()
            .Contain(["train-source-clusters"])
            .And.NotContain(["sgb-station-dots", "sgb-crossings", "sgb-switches"]);
        cut.FindComponents<SymbolLayer>()
            .Select(layer => layer.Instance.Id)
            .Should()
            .NotContain(["sgb-station-labels"]);
        cut.FindComponents<LineLayer>()
            .Select(layer => layer.Instance.Id)
            .Should()
            .NotContain(["sgb-rail-main", "sgb-rail-service"]);
        cut.FindComponents<GeoJsonSource>()
            .Select(source => source.Instance.Id)
            .Should()
            .Contain(["train-source", "train-source-decorations"])
            .And.NotContain(["stations", "rail-tracks", "rail-infra"]);
        cut.Markup.Should().Contain("Map layers");
        cut.Markup.Should().Contain("Platforms");
        cut.Markup.Should().Contain("Tracks &amp; tunnels");
        cut.Markup.Should().Contain("Infrastructure");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_render_overlay_group_toggles_with_expected_default_states(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;

        // act
        await map.OnMapInitializedAsync();

        var tracksToggle = cut.Find("input[data-testid='map-legend-toggle-tracks']");
        var tramToggle = cut.Find("input[data-testid='map-legend-toggle-tram']");
        var stationsToggle = cut.Find("input[data-testid='map-legend-toggle-stations']");
        var platformsToggle = cut.Find("input[data-testid='map-legend-toggle-platforms']");
        var routesToggle = cut.Find("input[data-testid='map-legend-toggle-routes']");
        var lifecycleToggle = cut.Find("input[data-testid='map-legend-toggle-lifecycle']");
        var infrastructureToggle = cut.Find("input[data-testid='map-legend-toggle-infrastructure']");
        var buildingsToggle = cut.Find("input[data-testid='map-legend-toggle-3d-buildings']");
        var trainsToggle = cut.Find("input[data-testid='map-legend-toggle-trains']");

        // assert
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke(SetControlContentIdentifier));
        cut.Markup.Should().Contain("Tracks &amp; tunnels");
        cut.Markup.Should().Contain("Tram &amp; metro");
        cut.Markup.Should().Contain("Stations &amp; borders");
        cut.Markup.Should().Contain("Platforms");
        cut.Markup.Should().Contain("Routes");
        cut.Markup.Should().Contain("Lifecycle");
        cut.Markup.Should().Contain("Infrastructure");
        cut.Markup.Should().Contain("3D Buildings");
        cut.Markup.Should().Contain("Trains");
        tracksToggle.HasAttribute("checked").Should().BeTrue();
        tramToggle.HasAttribute("checked").Should().BeFalse();
        stationsToggle.HasAttribute("checked").Should().BeTrue();
        platformsToggle.HasAttribute("checked").Should().BeTrue();
        routesToggle.HasAttribute("checked").Should().BeTrue();
        lifecycleToggle.HasAttribute("checked").Should().BeTrue();
        infrastructureToggle.HasAttribute("checked").Should().BeFalse();
        buildingsToggle.HasAttribute("checked").Should().BeTrue();
        trainsToggle.HasAttribute("checked").Should().BeTrue();
    }

    [Test]
    public void Should_render_luxembourg_specific_legend_item_template_previews()
    {
        // arrange & act
        var cut = Render<TrainTrackingExample>();

        // assert
        cut.Markup.Should().Contain("train-overlay-swatch train-overlay-swatch-tracks");
        cut.Markup.Should().Contain("tk-rail-top");
        cut.Markup.Should().Contain("tk-tie-top");
        cut.Markup.Should().Contain("train-overlay-swatch train-overlay-swatch-tram");
        cut.Markup.Should().Contain("train-overlay-swatch train-overlay-swatch-stations");
        cut.Markup.Should().Contain("train-overlay-swatch train-overlay-swatch-routes");
        cut.Markup.Should().Contain("rt-station");
        cut.Markup.Should().Contain("rt-line");
        cut.Markup.Should().Contain("train-overlay-swatch train-overlay-swatch-lifecycle");
        cut.Markup.Should().Contain("train-overlay-swatch train-overlay-swatch-infrastructure");
        cut.Markup.Should().Contain("sig-post");
        cut.Markup.Should().Contain("sig-red");
        cut.Markup.Should().Contain("train-overlay-swatch train-overlay-swatch-platforms");
        cut.Markup.Should().Contain("p-roof");
        cut.Markup.Should().Contain("train-overlay-swatch train-overlay-swatch-3d-buildings");
        cut.Markup.Should().Contain("b-roof");
        cut.Markup.Should().Contain("train-overlay-swatch train-overlay-swatch-trains");
        cut.Markup.Should().Contain("fill=\"#2563eb\"");
        cut.Markup.Should().Contain("train-overlay-swatch-art");
        cut.Markup.Should().Contain("train-overlay-swatch-badge");
        cut.Markup.Should().NotContain("train-overlay-legend-switch-track");
    }

    [Test]
    public void Should_place_the_luxembourg_legend_in_the_top_left_corner()
    {
        // arrange & act
        var cut = Render<TrainTrackingExample>();

        // assert
        var legendControl = cut.FindComponents<MapLegendControl>()
            .Should()
            .ContainSingle(component => component.Instance.Id == "overlay-legend")
            .Subject;
        legendControl.Instance.Position.Should().Be(ControlPosition.TopLeft);
    }

    [Test]
    public void Should_define_overlay_groups_for_the_live_luxembourg_overlay_contract()
    {
        // arrange & act
        var allItems = TrainTrackingPresentation.OverlayLegendDefinition.GetItems();

        var targetedGroups = allItems
            .Where(item => item.Targets is { Count: > 0 })
            .Select(group => new
            {
                group.Id,
                group.Label,
                group.IsVisibleByDefault,
                StyleId = group.Targets!.Single().StyleId,
                LayerIds = group.Targets!.Single().LayerIds.ToArray(),
            })
            .ToArray();

        var nonTargetedItems = allItems
            .Where(item => item.Targets is null or { Count: 0 })
            .Select(item => new
            {
                item.Id,
                item.Label,
                item.IsToggleable,
                item.Targets,
            })
            .ToArray();

        // assert
        targetedGroups
            .Should()
            .BeEquivalentTo(
                [
                    new
                    {
                        Id = "tracks",
                        Label = "Tracks & tunnels",
                        IsVisibleByDefault = true,
                        StyleId = TrainTrackingPresentation.OverlayStyleId,
                        LayerIds = new[]
                        {
                            "railway-line-rail",
                            "railway-line-light-rail",
                            "railway-line-subway",
                            "railway-line-narrow-gauge",
                            "railway-line-funicular",
                            "railway-line-monorail",
                            "railway-line-miniature",
                            "railway-line-service",
                            "railway-line-tunnel",
                            "railway-tunnel-label",
                            "railway-areas-fill",
                            "railway-areas-outline",
                        },
                    },
                    new
                    {
                        Id = "tram",
                        Label = "Tram & metro",
                        IsVisibleByDefault = false,
                        StyleId = TrainTrackingPresentation.OverlayStyleId,
                        LayerIds = new[]
                        {
                            "tram-line-fill",
                            "tram-line-tunnel",
                            "tram-stations-icon",
                            "subway-entrance-icon",
                            "tram-lifecycle-fill",
                            "railway-tram-crossings-circle",
                        },
                    },
                    new
                    {
                        Id = "stations",
                        Label = "Stations & borders",
                        IsVisibleByDefault = true,
                        StyleId = TrainTrackingPresentation.OverlayStyleId,
                        LayerIds = new[]
                        {
                            "railway-stations-circle",
                            "railway-stations-label",
                            "railway-border-circle",
                            "railway-border-label",
                        },
                    },
                    new
                    {
                        Id = "platforms",
                        Label = "Platforms",
                        IsVisibleByDefault = true,
                        StyleId = TrainTrackingPresentation.OverlayStyleId,
                        LayerIds = new[]
                        {
                            "railway-platforms-fill",
                            "railway-platforms-3d",
                            "railway-platforms-label",
                            "railway-platform-refs-label",
                            "railway-platform-names-label",
                        },
                    },
                    new
                    {
                        Id = "routes",
                        Label = "Routes",
                        IsVisibleByDefault = true,
                        StyleId = TrainTrackingPresentation.OverlayStyleId,
                        LayerIds = new[] { "railway-routes-casing", "railway-routes", "railway-routes-label" },
                    },
                    new
                    {
                        Id = "lifecycle",
                        Label = "Lifecycle",
                        IsVisibleByDefault = true,
                        StyleId = TrainTrackingPresentation.OverlayStyleId,
                        LayerIds = new[]
                        {
                            "railway-lifecycle-construction",
                            "railway-lifecycle-proposed",
                            "railway-lifecycle-disused",
                            "railway-lifecycle-abandoned",
                            "railway-lifecycle-preserved",
                            "railway-lifecycle-razed",
                        },
                    },
                    new
                    {
                        Id = "infrastructure",
                        Label = "Infrastructure",
                        IsVisibleByDefault = false,
                        StyleId = TrainTrackingPresentation.OverlayStyleId,
                        LayerIds = new[]
                        {
                            "railway-switches",
                            "railway-signals",
                            "railway-buffer-stops",
                            "railway-milestones",
                            "railway-turntables",
                            "railway-derails",
                            "railway-crossings-track",
                            "railway-owner-change",
                            "railway-crossings-circle",
                        },
                    },
                ],
                options => options.WithStrictOrderingFor(group => group.LayerIds)
            );

        nonTargetedItems
            .Should()
            .BeEquivalentTo([
                new
                {
                    Id = "3d-buildings",
                    Label = "3D Buildings",
                    IsToggleable = true,
                    Targets = (IReadOnlyList<MapLegendTarget>?)null,
                },
                new
                {
                    Id = "trains",
                    Label = "Trains",
                    IsToggleable = true,
                    Targets = (IReadOnlyList<MapLegendTarget>?)null,
                },
            ]);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_toggle_all_overlay_group_state_together(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var targetedItems = TrainTrackingPresentation
            .OverlayLegendDefinition.GetItems()
            .Where(item => item.Targets is { Count: > 0 });

        // act
        foreach (var group in targetedItems)
        {
            await InvokePrivateAsync(
                cut.Instance,
                "HandleLegendItemVisibilityChangedAsync",
                new MapLegendVisibilityChangedEventArgs(group, !group.IsVisibleByDefault)
            );
        }

        // assert
        var visibility = GetPrivateField<TrainTrackingVisibilityState>(cut.Instance, "_visibility");
        visibility!
            .GetOverlayGroupVisibility()
            .Should()
            .BeEquivalentTo(
                targetedItems.ToDictionary(
                    group => group.Id,
                    group => !group.IsVisibleByDefault,
                    StringComparer.Ordinal
                )
            );
        visibility!.ShowBuildings.Should().BeTrue();
        visibility.ShowTrains.Should().BeTrue();
        JSInterop.Invocations[SetStyleLayerVisibilityIdentifier].Count.Should().Be(0);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_toggle_buildings_visibility_via_legend(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();

        var buildingsToggle = cut.Find("input[data-testid='map-legend-toggle-3d-buildings']");
        buildingsToggle.HasAttribute("checked").Should().BeTrue();

        // act
        await buildingsToggle.ChangeAsync(new ChangeEventArgs { Value = false });

        // assert
        var visibility = GetPrivateField<TrainTrackingVisibilityState>(cut.Instance, "_visibility");
        visibility!.ShowBuildings.Should().BeFalse();

        var fillExtrusionLayer = cut.FindComponents<FillExtrusionLayer>()
            .Single(layer => layer.Instance.Id == "sgb-buildings-3d")
            .Instance;
        fillExtrusionLayer.Visible.Should().BeFalse();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_toggle_trains_visibility_via_legend(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();

        var trainsToggle = cut.Find("input[data-testid='map-legend-toggle-trains']");
        trainsToggle.HasAttribute("checked").Should().BeTrue();

        // act
        await trainsToggle.ChangeAsync(new ChangeEventArgs { Value = false });

        // assert
        var visibility = GetPrivateField<TrainTrackingVisibilityState>(cut.Instance, "_visibility");
        visibility!.ShowTrains.Should().BeFalse();

        var trainIconLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "train-source-symbols")
            .Instance;
        trainIconLayer.Visible.Should().BeFalse();

        var clusterLayer = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "train-source-clusters")
            .Instance;
        clusterLayer.Visible.Should().BeFalse();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_zoom_to_dissolve_when_cluster_count_layer_is_clicked(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var clusterEvent = new LayerFeatureEventArgs(
            "train-source-cluster-count",
            new Coordinate(49.7, 6.12),
            JsonSerializer.Deserialize<JsonElement>("{" + '"' + "cluster_id" + '"' + ":42}")
        );

        // act
        var clusterLayer = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "train-source-cluster-count")
            .Instance;

        await cut.InvokeAsync(() => clusterLayer.OnClick.InvokeAsync(clusterEvent));

        // assert
        JSInterop.VerifyInvoke(GetClusterExpansionZoomIdentifier);
        JSInterop.VerifyInvoke(FlyToIdentifier);
    }

    [Test]
    public void Should_express_train_showcase_ordering_declaratively()
    {
        // arrange & act
        var cut = Render<TrainTrackingExample>();

        var clusterHitArea = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "train-source-cluster-hit-area")
            .Instance;
        var clusters = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "train-source-clusters")
            .Instance;
        var clusterCount = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "train-source-cluster-count")
            .Instance;
        var trainIcons = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "train-source-symbols")
            .Instance;
        var trainService = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "train-source-service-left")
            .Instance;

        // assert
        clusterHitArea.LayerGroup.Should().Be("train-source-cluster-hit-area");
        clusterHitArea.AfterLayerGroup.Should().BeNull();
        clusters.AfterLayerGroup.Should().Be("train-source-cluster-hit-area");
        clusterCount.AfterLayerGroup.Should().Be("train-source-clusters");
        trainIcons.AfterLayerGroup.Should().Be("train-source-cluster-count");
        trainService.AfterLayerGroup.Should().Be("train-source-hit-area");
    }

    [Test]
    public void Should_use_a_more_reliable_train_hit_area_radius()
    {
        // arrange & act
        var cut = Render<TrainTrackingExample>();

        var trainHitArea = cut.FindComponents<CircleLayer>()
            .Single(layer => layer.Instance.Id == "train-source-hit-area")
            .Instance;

        // assert
        GetPaintValue(GetLayerSpec(trainHitArea), "circle-radius").Should().Be(24.0);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_update_feature_state_and_selection_without_showing_stale_popup(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var firstEntity = ResolveTrackedEntity(cut, "cfl-re11");
        var interaction = CreateTrainInteraction(firstEntity);

        // act
        await InvokePrivateAsync(cut.Instance, "HandleTrainHover", interaction);
        await InvokePrivateAsync(cut.Instance, "HandleTrainClick", interaction);
        await InvokePrivateAsync(cut.Instance, "HandleTrainLeave");

        // assert
        JSInterop.VerifyInvoke(FlyToIdentifier);
        JSInterop.VerifyInvoke(ClosePopupIdentifier);
        JSInterop.VerifyNotInvoke(ShowPopupIdentifier);
        GetPrivateField<string?>(cut.Instance, "_selectedTrainId").Should().Be(interaction.EntityId);
    }

    [Test]
    public void Should_preserve_current_zoom_when_selecting_train_above_label_threshold()
    {
        // arrange
        var cut = Render<TrainTrackingExample>();

        // act
        var result = InvokePrivateStatic<int?>(cut.Instance.GetType(), "GetSelectionFocusZoom", 14.2);

        // assert
        result.Should().BeNull();
    }

    [Test]
    public void Should_raise_selection_zoom_to_label_threshold_when_current_zoom_is_too_low()
    {
        // arrange
        var cut = Render<TrainTrackingExample>();

        // act
        var result = InvokePrivateStatic<int?>(cut.Instance.GetType(), "GetSelectionFocusZoom", 8.4);

        // assert
        result.Should().Be(13);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_keep_hover_and_selection_identity_after_data_refresh(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var trackedEntity = ResolveTrackedEntity(cut, "cfl-re11");
        var interaction = CreateTrainInteraction(trackedEntity);

        await InvokePrivateAsync(cut.Instance, "HandleTrainHover", interaction);
        await InvokePrivateAsync(cut.Instance, "HandleTrainClick", interaction);

        // act
        cut.Render();

        // assert
        GetPrivateField<string?>(cut.Instance, "_hoveredTrainId").Should().Be(interaction.EntityId);
        GetPrivateField<string?>(cut.Instance, "_selectedTrainId").Should().Be(interaction.EntityId);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_keep_selected_train_identity_stable_after_rebuild(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var selectedTrainBefore = ResolveTrackedEntity(cut, "cfl-re11");
        var interaction = CreateTrainInteraction(selectedTrainBefore);

        await InvokePrivateAsync(cut.Instance, "HandleTrainClick", interaction);

        // act
        cut.Render();

        // assert
        GetPrivateField<string?>(cut.Instance, "_selectedTrainId").Should().Be(selectedTrainBefore.Id);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replay_selected_train_follow_after_refreshing_tracked_entities(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var firstEntity = ResolveTrackedEntity(cut, "cfl-re11");
        var interaction = CreateTrainInteraction(firstEntity);

        await InvokePrivateAsync(cut.Instance, "HandleTrainClick", interaction);
        var flyToInvocationCountBeforeRefresh = JSInterop.Invocations[FlyToIdentifier].Count;

        // act
        await InvokePrivateAsync(cut.Instance, "RefreshMapFocusForSelectionAsync");

        // assert
        JSInterop.Invocations[FlyToIdentifier].Count.Should().Be(flyToInvocationCountBeforeRefresh + 1);
    }

    [Test]
    public void Should_keep_manual_selection_follow_outside_the_tracked_data_interaction_surface()
    {
        // arrange & act
        var cut = Render<TrainTrackingExample>();
        var trackedLayer = ResolveTrackedLayer(cut);

        // assert
        trackedLayer.Behavior.Interaction.IsHovered.Should().NotBeNull();
        trackedLayer.Behavior.Interaction.IsSelected.Should().NotBeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_close_popup_when_clearing_selected_train(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<TrainTrackingExample>();
        var lastEntity = ResolveTrackedEntity(cut, "cfl-re11");
        var interaction = CreateTrainInteraction(lastEntity);
        await InvokePrivateAsync(cut.Instance, "HandleTrainClick", interaction);

        // act
        await InvokePrivateAsync(cut.Instance, "ClearSelectionAsync");

        // assert
        JSInterop.Invocations[ClosePopupIdentifier].Count.Should().BeGreaterThanOrEqualTo(2);
        GetPrivateField<string?>(cut.Instance, "_selectedTrainId").Should().BeNull();
    }

    private static TrackedEntityInteractionEventArgs<TrainSampleState> CreateTrainInteraction(
        TrackedEntity<TrainSampleState> entity,
        string? decorationId = null
    ) => new(entity, new LayerFeatureEventArgs("train-source-symbols", entity.Position, null), decorationId);

    private static TrackedEntityLayerDefinition<TrainSampleState> ResolveTrackedLayer(
        IRenderedComponent<TrainTrackingExample> cut
    )
    {
        var trackedLayer = cut.FindComponent<TrackedEntityLayer<TrainSampleState>>().Instance.Layer!;
        return trackedLayer;
    }

    private static TrackedEntity<TrainSampleState> ResolveTrackedEntity(
        IRenderedComponent<TrainTrackingExample> cut,
        string entityId
    )
    {
        var trackedLayer = ResolveTrackedLayer(cut);
        var entities = TrackedEntityMaterializer.Materialize(
            trackedLayer.Items,
            trackedLayer.IdOptions,
            trackedLayer.Visual.Symbol,
            trackedLayer.Visual.Decorations
        );
        var entity = entities.Single(candidate => candidate.Id == entityId);
        return entity;
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[]? arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        var result = method!.Invoke(instance, arguments ?? []);

        if (result is Task task)
        {
            await task;
        }
    }

    private static async Task<T?> InvokePrivateAsync<T>(
        object instance,
        string methodName,
        Type[] parameterTypes,
        params object[]? arguments
    )
    {
        var method = instance
            .GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic, parameterTypes);

        method.Should().NotBeNull();
        var result = method!.Invoke(instance, arguments ?? []);

        if (result is Task<T> task)
        {
            return await task;
        }

        return default;
    }

    private static void InvokePrivate(object instance, string methodName, params object[]? arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        _ = method!.Invoke(instance, arguments ?? []);
    }

    private static T? GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        field.Should().NotBeNull();
        var value = field!.GetValue(instance);

        return (T?)value;
    }

    private static T? InvokePrivateStatic<T>(Type type, string methodName, params object[]? arguments)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        var result = method!.Invoke(null, arguments ?? []);

        return (T?)result;
    }

    private static IReadOnlyDictionary<string, object?> GetLayerSpec(LayerBase layer)
    {
        var method = typeof(LayerBase).GetMethod("BuildLayerSpec", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        var spec = method!.Invoke(layer, []);

        spec.Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>();
        return (IReadOnlyDictionary<string, object?>)spec!;
    }

    private static object? GetPaintValue(IReadOnlyDictionary<string, object?> layerSpec, string propertyName)
    {
        layerSpec.TryGetValue("paint", out var paint);
        var paintDictionary = paint.Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        paintDictionary.Should().ContainKey(propertyName);
        return paintDictionary[propertyName] is StyleValue<string> styleValue
            ? GetStyleValueLiteral(styleValue)
            : paintDictionary[propertyName];
    }

    private static string? GetStyleValueLiteral(StyleValue<string> styleValue)
    {
        var property = typeof(StyleValue<string>).GetProperty(
            "Literal",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        property.Should().NotBeNull();
        var value = property!.GetValue(styleValue);
        return value as string;
    }

    private static void LayoutPropertyShouldNotContainFeatureState(
        IReadOnlyDictionary<string, object?> layerSpec,
        string propertyName
    )
    {
        var layoutProperty = GetLayoutValue(layerSpec, propertyName);
        var containsFeatureState = ContainsExpressionOperator(layoutProperty, "feature-state");
        containsFeatureState.Should().BeFalse();
    }

    private static object? GetLayoutValue(IReadOnlyDictionary<string, object?> layerSpec, string propertyName)
    {
        layerSpec.TryGetValue("layout", out var layout);
        var layoutDictionary = layout.Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        layoutDictionary.Should().ContainKey(propertyName);
        return layoutDictionary[propertyName];
    }

    private static bool TryGetLayoutValue(
        IReadOnlyDictionary<string, object?> layerSpec,
        string propertyName,
        out object? value
    )
    {
        layerSpec.TryGetValue("layout", out var layout);
        var layoutDictionary = layout.Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        var found = layoutDictionary.TryGetValue(propertyName, out value);
        return found;
    }

    private static bool TryGetValue(
        IReadOnlyDictionary<string, object?> dictionary,
        string propertyName,
        out object? value
    )
    {
        var found = dictionary.TryGetValue(propertyName, out value);
        return found;
    }

    private static bool ContainsExpressionOperator(object? expression, string operatorName)
    {
        if (expression is not object[] expressionArray)
        {
            return false;
        }
        var nestedExpressions = expressionArray.OfType<object[]>();
        return expressionArray.FirstOrDefault() as string == operatorName
            || nestedExpressions.Any(nested => ContainsExpressionOperator(nested, operatorName));
    }
}
