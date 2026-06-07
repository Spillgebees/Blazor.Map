using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class GeoJsonSourceClusterOptionsTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";
    private const string GetClusterExpansionZoomIdentifier = "Spillgebees.Map.mapFunctions.getClusterExpansionZoom";
    private const string FlyToIdentifier = "Spillgebees.Map.mapFunctions.flyTo";
    private readonly JSRuntimeInvocationHandler _applySceneMutations;

    public GeoJsonSourceClusterOptionsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        _applySceneMutations = JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
        _applySceneMutations.SetVoidResult();
        JSInterop.Setup<double>(GetClusterExpansionZoomIdentifier).SetResult(11.2);
        JSInterop.SetupVoid(FlyToIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_emit_default_cluster_options_source_spec(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );
        var sourceSpec = GetSourceSpec("geojson-source");
        sourceSpec["cluster"].Should().Be(true);
        sourceSpec["clusterRadius"].Should().Be(ClusterOptions.DefaultRadius);
        sourceSpec.Should().NotContainKey("clusterMaxZoom");
        sourceSpec.Should().NotContainKey("clusterMinPoints");
        sourceSpec.Should().NotContainKey("clusterProperties");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_default_cluster_visual_layers_for_default_cluster_options(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));
        var circleSpec = GetLayerSpec("geojson-source-clusters");
        circleSpec["type"].Should().Be("circle");
        circleSpec["source"].Should().Be("geojson-source");
        circleSpec["filter"].Should().BeEquivalentTo(new object[] { "has", "point_count" });
        circleSpec["paint"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>();

        var symbolSpec = GetLayerSpec("geojson-source-cluster-count");
        symbolSpec["type"].Should().Be("symbol");
        symbolSpec["source"].Should().Be("geojson-source");
        symbolSpec["filter"].Should().BeEquivalentTo(new object[] { "has", "point_count" });
        symbolSpec["layout"]
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["text-field"] = new object[] { "get", "point_count_abbreviated" },
                    ["text-size"] = 14d,
                }
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_wire_click_events_for_default_interactive_cluster_layers(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetWireLayerEventMutations().Should().HaveCount(2));
        GetWireLayerEventMutations()
            .Select(mutation => mutation.LayerId)
            .Should()
            .Equal("geojson-source-clusters", "geojson-source-cluster-count");
        GetWireLayerEventMutations().Should().AllSatisfy(mutation => mutation.OnClick.Should().BeTrue());
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_zoom_to_dissolve_when_generated_geojson_cluster_layer_is_clicked(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetWireLayerEventMutations().Should().HaveCount(2));
        var dotNetRef = GetWireLayerEventMutations()[0]
            .DotNetRef.Should()
            .BeAssignableTo<DotNetObjectReference<GeoJsonSource>>()
            .Subject;
        var properties = JsonDocument.Parse("{\"cluster_id\":42}").RootElement;

        // act
        await dotNetRef.Value.OnLayerClickAsync(45.5, -63.5, properties);

        // assert
        JSInterop.VerifyInvoke(GetClusterExpansionZoomIdentifier);
        JSInterop.VerifyInvoke(FlyToIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_wire_click_events_when_cluster_click_behavior_is_none(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var options = ClusterOptions.Create(clickBehavior: ClusterClickBehavior.None);
        var cut = Render<GeoJsonClusterSourceHarness>(parameters => parameters.Add(p => p.ClusterOptions, options));

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));
        GetWireLayerEventMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_wire_click_events_for_non_interactive_custom_cluster_layers(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var options = ClusterOptions.Create(
            layerSet: ClusterLayerSet.Custom(
                ClusterLayerDefinition.Circle("decorative", interactive: false),
                ClusterLayerDefinition.Symbol("count")
            )
        );
        var cut = Render<GeoJsonClusterSourceHarness>(parameters => parameters.Add(p => p.ClusterOptions, options));

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));
        GetWireLayerEventMutations().Select(mutation => mutation.LayerId).Should().Equal("geojson-source-count");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_rewire_cluster_click_events_when_source_is_replaced(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetWireLayerEventMutations().Should().HaveCount(2));

        // act
        cut.SetParametersAndRender(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Create(radius: 64))
        );

        // assert
        cut.WaitForAssertion(() => GetWireLayerEventMutations().Should().HaveCount(4));
        GetRemoveSourceMutations().Select(mutation => mutation.SourceId).Should().Contain("geojson-source");
        GetUnregisterLayerEventMutations()
            .Select(mutation => mutation.LayerId)
            .Should()
            .Contain(["geojson-source-clusters", "geojson-source-cluster-count"]);
        GetWireLayerEventMutations()
            .ToArray()[^2..]
            .Select(mutation => mutation.LayerId)
            .Should()
            .Equal("geojson-source-clusters", "geojson-source-cluster-count");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_wire_cluster_click_events_when_click_behavior_changes_from_none_to_zoom_to_dissolve(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Create(clickBehavior: ClusterClickBehavior.None))
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));
        GetWireLayerEventMutations().Should().BeEmpty();

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.ClusterOptions, ClusterOptions.Default));

        // assert
        cut.WaitForAssertion(() => GetWireLayerEventMutations().Should().HaveCount(2));
        GetWireLayerEventMutations()
            .Select(mutation => mutation.LayerId)
            .Should()
            .Equal("geojson-source-clusters", "geojson-source-cluster-count");
        GetRemoveSourceMutations().Should().BeEmpty();
        GetRemoveLayerMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_unwire_cluster_click_events_when_click_behavior_changes_from_zoom_to_dissolve_to_none(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetWireLayerEventMutations().Should().HaveCount(2));

        // act
        cut.SetParametersAndRender(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Create(clickBehavior: ClusterClickBehavior.None))
        );

        // assert
        cut.WaitForAssertion(() => GetUnregisterLayerEventMutations().Should().HaveCount(2));
        GetUnregisterLayerEventMutations()
            .Select(mutation => mutation.LayerId)
            .Should()
            .Equal("geojson-source-clusters", "geojson-source-cluster-count");
        GetWireLayerEventMutations().Should().HaveCount(2);
        GetRemoveSourceMutations().Should().BeEmpty();
        GetRemoveLayerMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_preserve_cluster_event_state_when_unwire_disconnects(CancellationToken cancellationToken)
    {
        // arrange
        var jsRuntime = new DisconnectingMapJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => jsRuntime.GetMutations("wireLayerEvents").Should().HaveCount(2));
        GetRegisteredLayerEventIds(cut.Instance.Map)
            .Should()
            .Equal("geojson-source-clusters", "geojson-source-cluster-count");
        jsRuntime.DisconnectOnApplySceneMutations = true;

        // act
        cut.SetParametersAndRender(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Create(clickBehavior: ClusterClickBehavior.None))
        );

        // assert
        GetRegisteredLayerEventIds(cut.Instance.Map)
            .Should()
            .BeEquivalentTo(["geojson-source-clusters", "geojson-source-cluster-count"]);

        // arrange
        jsRuntime.DisconnectOnApplySceneMutations = false;

        // act
        cut.SetParametersAndRender(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Create(clickBehavior: ClusterClickBehavior.None))
        );

        // assert
        cut.WaitForAssertion(() => jsRuntime.GetMutations("unregisterLayerEvents").Should().HaveCount(2));
        jsRuntime
            .GetMutations("unregisterLayerEvents")
            .Select(mutation => mutation.LayerId)
            .Should()
            .Equal("geojson-source-clusters", "geojson-source-cluster-count");
        GetRegisteredLayerEventIds(cut.Instance.Map).Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_preserve_unrelated_layer_event_changes_when_unwire_disconnects(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var jsRuntime = new DisconnectingMapJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => jsRuntime.GetMutations("wireLayerEvents").Should().HaveCount(2));
        jsRuntime.DisconnectOnApplySceneMutations = true;
        jsRuntime.BeforeDisconnect = () =>
            cut.Instance.Map.SceneRegistry.SetLayerEvents(
                new LayerEventDescriptor("unrelated-layer", new object(), true, false, false)
            );

        // act
        cut.SetParametersAndRender(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Create(clickBehavior: ClusterClickBehavior.None))
        );

        // assert
        GetRegisteredLayerEventIds(cut.Instance.Map)
            .Should()
            .BeEquivalentTo(["geojson-source-clusters", "geojson-source-cluster-count", "unrelated-layer"]);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_count_failed_apply_scene_mutations_as_applied(CancellationToken cancellationToken)
    {
        // arrange
        var jsRuntime = new DisconnectingMapJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => jsRuntime.GetMutations("wireLayerEvents").Should().HaveCount(2));
        jsRuntime.DisconnectOnApplySceneMutations = true;

        // act
        cut.SetParametersAndRender(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Create(clickBehavior: ClusterClickBehavior.None))
        );

        // assert
        jsRuntime.GetMutations("unregisterLayerEvents").Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_wire_cluster_click_events_when_generated_layer_changes_from_non_interactive_to_interactive(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var initialOptions = ClusterOptions.Create(
            layerSet: ClusterLayerSet.Custom(ClusterLayerDefinition.Circle("clusters", interactive: false))
        );
        var updatedOptions = ClusterOptions.Create(
            layerSet: ClusterLayerSet.Custom(ClusterLayerDefinition.Circle("clusters"))
        );
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, initialOptions)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(1));
        GetWireLayerEventMutations().Should().BeEmpty();

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.ClusterOptions, updatedOptions));

        // assert
        cut.WaitForAssertion(() => GetWireLayerEventMutations().Should().HaveCount(1));
        GetWireLayerEventMutations().Single().LayerId.Should().Be("geojson-source-clusters");
        GetRemoveSourceMutations().Should().BeEmpty();
        GetRemoveLayerMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_unwire_cluster_click_events_when_generated_layer_changes_from_interactive_to_non_interactive(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var initialOptions = ClusterOptions.Create(
            layerSet: ClusterLayerSet.Custom(ClusterLayerDefinition.Circle("clusters"))
        );
        var updatedOptions = ClusterOptions.Create(
            layerSet: ClusterLayerSet.Custom(ClusterLayerDefinition.Circle("clusters", interactive: false))
        );
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, initialOptions)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetWireLayerEventMutations().Should().HaveCount(1));

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.ClusterOptions, updatedOptions));

        // assert
        cut.WaitForAssertion(() => GetUnregisterLayerEventMutations().Should().HaveCount(1));
        GetUnregisterLayerEventMutations().Single().LayerId.Should().Be("geojson-source-clusters");
        GetWireLayerEventMutations().Should().HaveCount(1);
        GetRemoveSourceMutations().Should().BeEmpty();
        GetRemoveLayerMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_emit_none_cluster_options_source_spec(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.None)
        );

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );
        var sourceSpec = GetSourceSpec("geojson-source");
        sourceSpec.Should().NotContainKey("cluster");
        sourceSpec.Should().NotContainKey("clusterRadius");
        sourceSpec.Should().NotContainKey("clusterMaxZoom");
        sourceSpec.Should().NotContainKey("clusterMinPoints");
        sourceSpec.Should().NotContainKey("clusterProperties");
        GetAddLayerMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_register_cluster_visual_layers_for_legacy_cluster_parameters(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters => parameters.Add(p => p.Cluster, true));

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetSourceSpec("geojson-source")["cluster"].Should().Be(true));
        GetAddLayerMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_wire_cluster_click_events_for_legacy_cluster_parameters_after_source_replacement(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters => parameters.Add(p => p.Cluster, true));
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetSourceSpec("geojson-source")["cluster"].Should().Be(true));

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Cluster, true).Add(p => p.ClusterRadius, 64));

        // assert
        cut.WaitForAssertion(() =>
            GetRemoveSourceMutations().Select(mutation => mutation.SourceId).Should().Contain("geojson-source")
        );
        GetAddLayerMutations().Should().BeEmpty();
        GetWireLayerEventMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_register_cluster_visual_layers_for_none_cluster_layer_set(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Create(layerSet: ClusterLayerSet.None))
        );

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetSourceSpec("geojson-source")["cluster"].Should().Be(true));
        GetAddLayerMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_custom_cluster_visual_layers_with_options(CancellationToken cancellationToken)
    {
        // arrange
        var options = ClusterOptions.Create(
            layerSet: ClusterLayerSet.Custom(
                ClusterLayerDefinition.Circle(
                    "outer",
                    color: "#0f172a",
                    radius: 28,
                    opacity: 0.4,
                    strokeColor: "#f8fafc",
                    strokeWidth: 3,
                    minZoom: 2,
                    maxZoom: 12,
                    interactive: false
                ),
                ClusterLayerDefinition.Circle("inner", color: "#38bdf8", radius: 18),
                ClusterLayerDefinition.Symbol("count", textSize: 16, textColor: "#ffffff")
            )
        );
        var cut = Render<GeoJsonClusterSourceHarness>(parameters => parameters.Add(p => p.ClusterOptions, options));

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(3));
        GetAddLayerMutations()
            .Select(mutation => mutation.LayerId)
            .Should()
            .Equal("geojson-source-outer", "geojson-source-inner", "geojson-source-count");
        var outerSpec = GetLayerSpec("geojson-source-outer");
        outerSpec["filter"].Should().BeEquivalentTo(new object[] { "has", "point_count" });
        outerSpec["minzoom"].Should().Be(2d);
        outerSpec["maxzoom"].Should().Be(12d);
        outerSpec.Should().NotContainKey("layout");
        outerSpec["paint"]
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["circle-color"] = "#0f172a",
                    ["circle-radius"] = 28d,
                    ["circle-opacity"] = 0.4d,
                    ["circle-stroke-color"] = "#f8fafc",
                    ["circle-stroke-width"] = 3d,
                }
            );

        var countSpec = GetLayerSpec("geojson-source-count");
        countSpec["layout"]
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["text-field"] = new object[] { "get", "point_count_abbreviated" },
                    ["text-size"] = 16d,
                }
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_cluster_visual_layers_before_parameter_and_child_layers(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters
                .Add(p => p.ClusterOptions, ClusterOptions.Default)
                .Add(
                    p => p.Layers,
                    [
                        MapLayer.Circle(
                            "unclustered",
                            filter: new object[] { "!", new object[] { "has", "point_count" } }
                        ),
                    ]
                )
                .Add(p => p.IncludeChildLayer, true)
        );

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(4));
        GetAddLayerMutations()
            .Select(mutation => mutation.LayerId)
            .Should()
            .Equal(
                "geojson-source-clusters",
                "geojson-source-cluster-count",
                "geojson-source-unclustered",
                "child-layer"
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_fail_clearly_for_duplicate_generated_and_user_layer_ids(
        CancellationToken cancellationToken
    )
    {
        // arrange

        // act
        var act = () =>
            Render<GeoJsonClusterSourceHarness>(parameters =>
                parameters
                    .Add(p => p.ClusterOptions, ClusterOptions.Default)
                    .Add(p => p.Layers, [MapLayer.Circle("clusters", radius: 10)])
            );

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*duplicate*geojson-source-clusters*");
        await Task.CompletedTask;
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replace_cluster_visual_layers_when_cluster_layer_set_changes(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));

        // act
        cut.SetParametersAndRender(parameters =>
            parameters.Add(
                p => p.ClusterOptions,
                ClusterOptions.Create(layerSet: ClusterLayerSet.Custom(ClusterLayerDefinition.Symbol("custom-count")))
            )
        );

        // assert
        cut.WaitForAssertion(() =>
        {
            GetRemoveLayerMutations().Select(mutation => mutation.LayerId).Should().Contain("geojson-source-clusters");
            GetRemoveLayerMutations()
                .Select(mutation => mutation.LayerId)
                .Should()
                .Contain("geojson-source-cluster-count");
            GetAddLayerMutations().Select(mutation => mutation.LayerId).Should().Contain("geojson-source-custom-count");
        });
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replace_source_when_cluster_options_change_from_none_to_default(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.None)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetSourceSpecs("geojson-source").Should().HaveCount(1));

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.ClusterOptions, ClusterOptions.Default));

        // assert
        cut.WaitForAssertion(() =>
        {
            GetRemoveSourceMutations().Select(mutation => mutation.SourceId).Should().Contain("geojson-source");
            var sourceSpecs = GetSourceSpecs("geojson-source");
            sourceSpecs.Should().HaveCount(2);
            sourceSpecs[^1]["cluster"].Should().Be(true);
            GetAddLayerMutations()
                .Select(mutation => mutation.LayerId)
                .Should()
                .Contain(["geojson-source-clusters", "geojson-source-cluster-count"]);
        });
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_reregister_cluster_parameter_and_child_layers_when_replacing_source(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters
                .Add(p => p.ClusterOptions, ClusterOptions.None)
                .Add(
                    p => p.Layers,
                    [
                        MapLayer.Circle(
                            "unclustered",
                            filter: new object[] { "!", new object[] { "has", "point_count" } }
                        ),
                    ]
                )
                .Add(p => p.IncludeChildLayer, true)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.ClusterOptions, ClusterOptions.Default));

        // assert
        cut.WaitForAssertion(() =>
        {
            GetRemoveSourceMutations().Select(mutation => mutation.SourceId).Should().Contain("geojson-source");
            var layerIds = GetAddLayerMutations().Select(mutation => mutation.LayerId).ToArray();
            layerIds[^4..]
                .Should()
                .Equal(
                    "geojson-source-clusters",
                    "geojson-source-cluster-count",
                    "geojson-source-unclustered",
                    "child-layer"
                );
        });
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replace_source_when_cluster_options_change_from_default_to_none(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.ClusterOptions, ClusterOptions.None));

        // assert
        cut.WaitForAssertion(() =>
        {
            GetRemoveSourceMutations().Select(mutation => mutation.SourceId).Should().Contain("geojson-source");
            var sourceSpecs = GetSourceSpecs("geojson-source");
            sourceSpecs.Should().HaveCount(2);
            sourceSpecs[^1].Should().NotContainKey("cluster");
        });
        GetAddLayerMutations().Should().HaveCount(2);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replace_visual_layers_when_enabled_cluster_layer_set_changes(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, ClusterOptions.Default)
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));

        // act
        cut.SetParametersAndRender(parameters =>
            parameters.Add(
                p => p.ClusterOptions,
                ClusterOptions.Create(layerSet: ClusterLayerSet.Custom(ClusterLayerDefinition.Symbol("custom-count")))
            )
        );

        // assert
        cut.WaitForAssertion(() =>
        {
            GetRemoveLayerMutations().Select(mutation => mutation.LayerId).Should().Contain("geojson-source-clusters");
            GetRemoveLayerMutations()
                .Select(mutation => mutation.LayerId)
                .Should()
                .Contain("geojson-source-cluster-count");
            GetAddLayerMutations().Select(mutation => mutation.LayerId).Should().Contain("geojson-source-custom-count");
        });
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replace_source_when_enabled_cluster_source_options_change(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var initialProperties = new Dictionary<string, object>
        {
            ["total"] = new object[] { "+", new object[] { "get", "count" } },
        };
        var updatedProperties = new Dictionary<string, object>
        {
            ["maximum"] = new object[] { "max", new object[] { "get", "count" } },
        };
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(
                p => p.ClusterOptions,
                ClusterOptions.Create(radius: 48, maxZoom: 10, minPoints: 2, properties: initialProperties)
            )
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetSourceSpecs("geojson-source").Should().HaveCount(1));

        // act
        cut.SetParametersAndRender(parameters =>
            parameters.Add(
                p => p.ClusterOptions,
                ClusterOptions.Create(radius: 64, maxZoom: 12, minPoints: 3, properties: updatedProperties)
            )
        );

        // assert
        cut.WaitForAssertion(() =>
        {
            GetRemoveSourceMutations().Select(mutation => mutation.SourceId).Should().Contain("geojson-source");
            var sourceSpecs = GetSourceSpecs("geojson-source");
            var sourceSpec = sourceSpecs[^1];
            sourceSpec["clusterRadius"].Should().Be(64);
            sourceSpec["clusterMaxZoom"].Should().Be(12);
            sourceSpec["clusterMinPoints"].Should().Be(3);
            sourceSpec["clusterProperties"].Should().BeEquivalentTo(updatedProperties);
        });
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_replace_source_for_equivalent_recreated_list_cluster_properties(
        CancellationToken cancellationToken
    )
    {
        // arrange
        static ClusterOptions CreateOptions() =>
            ClusterOptions.Create(
                properties: new Dictionary<string, object>
                {
                    ["total"] = new List<object> { "+", new object[] { "get", "count" } },
                    ["maximum"] = new Dictionary<string, object>
                    {
                        ["accumulator"] = new object[]
                        {
                            "max",
                            new List<object> { "get", "count" },
                        },
                    },
                }
            );
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters.Add(p => p.ClusterOptions, CreateOptions())
        );
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetSourceSpecs("geojson-source").Should().HaveCount(1));

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.ClusterOptions, CreateOptions()));

        // assert
        cut.WaitForAssertion(() => GetSourceSpecs("geojson-source").Should().HaveCount(1));
        GetRemoveSourceMutations().Should().BeEmpty();
        GetSourceSpecs("geojson-source").Should().HaveCount(1);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_emit_custom_cluster_options_source_spec(CancellationToken cancellationToken)
    {
        // arrange
        var properties = new Dictionary<string, object>
        {
            ["total"] = new object[] { "+", new object[] { "get", "count" } },
        };
        var options = ClusterOptions.Create(radius: 64, maxZoom: 12, minPoints: 3, properties: properties);
        var cut = Render<GeoJsonClusterSourceHarness>(parameters => parameters.Add(p => p.ClusterOptions, options));

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );
        var sourceSpec = GetSourceSpec("geojson-source");
        sourceSpec["cluster"].Should().Be(true);
        sourceSpec["clusterRadius"].Should().Be(64);
        sourceSpec["clusterMaxZoom"].Should().Be(12);
        sourceSpec["clusterMinPoints"].Should().Be(3);
        sourceSpec["clusterProperties"].Should().BeEquivalentTo(properties);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_prefer_cluster_options_over_legacy_cluster_parameters(CancellationToken cancellationToken)
    {
        // arrange
        var options = ClusterOptions.Create(radius: 64, maxZoom: 12, minPoints: 3);
        var cut = Render<GeoJsonClusterSourceHarness>(parameters =>
            parameters
                .Add(p => p.Cluster, true)
                .Add(p => p.ClusterRadius, 99)
                .Add(p => p.ClusterMaxZoom, 16)
                .Add(p => p.ClusterMinPoints, 8)
                .Add(p => p.ClusterOptions, options)
        );

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );
        var sourceSpec = GetSourceSpec("geojson-source");
        sourceSpec["clusterRadius"].Should().Be(64);
        sourceSpec["clusterMaxZoom"].Should().Be(12);
        sourceSpec["clusterMinPoints"].Should().Be(3);
    }

    private IReadOnlyDictionary<string, object?> GetSourceSpec(string sourceId)
    {
        JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0);

        var sourceSpec = GetSourceSpecs(sourceId).Single();

        sourceSpec.Should().NotBeNull();

        return sourceSpec!;
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> GetSourceSpecs(string sourceId) =>
        JSInterop
            .Invocations[ApplySceneMutationsIdentifier]
            .Select(invocation => invocation.Arguments[1])
            .OfType<MapSceneMutationBatch>()
            .SelectMany(batch => batch.Mutations)
            .Where(mutation => mutation.Kind == "addSource" && mutation.SourceId == sourceId)
            .Select(mutation => mutation.SourceSpec!)
            .ToArray();

    private IReadOnlyDictionary<string, object?> GetLayerSpec(string layerId)
    {
        var layerSpec = GetAddLayerMutations().Single(mutation => mutation.LayerId == layerId).LayerSpec;
        layerSpec.Should().NotBeNull();

        return layerSpec!;
    }

    private IReadOnlyList<MapSceneMutation> GetAddLayerMutations() => GetMutations("addLayer");

    private IReadOnlyList<MapSceneMutation> GetRemoveLayerMutations() => GetMutations("removeLayer");

    private IReadOnlyList<MapSceneMutation> GetRemoveSourceMutations() => GetMutations("removeSource");

    private IReadOnlyList<MapSceneMutation> GetWireLayerEventMutations() => GetMutations("wireLayerEvents");

    private IReadOnlyList<MapSceneMutation> GetUnregisterLayerEventMutations() => GetMutations("unregisterLayerEvents");

    private static IReadOnlyList<string> GetRegisteredLayerEventIds(SgbMap map)
    {
        var field = typeof(MapSceneRegistry).GetField(
            "_layerEvents",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        field.Should().NotBeNull();
        var layerEvents = field!
            .GetValue(map.SceneRegistry)
            .Should()
            .BeAssignableTo<IDictionary<string, LayerEventDescriptor>>()
            .Subject;

        return layerEvents.Keys.ToArray();
    }

    private IReadOnlyList<MapSceneMutation> GetMutations(string kind)
    {
        JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0);

        return JSInterop
            .Invocations[ApplySceneMutationsIdentifier]
            .Select(invocation => invocation.Arguments[1])
            .OfType<MapSceneMutationBatch>()
            .SelectMany(batch => batch.Mutations)
            .Where(mutation => mutation.Kind == kind)
            .ToArray();
    }

    public sealed class GeoJsonClusterSourceHarness : ComponentBase
    {
        public SgbMap Map { get; private set; } = null!;

        public object Data { get; } =
            new Dictionary<string, object?> { ["type"] = "FeatureCollection", ["features"] = Array.Empty<object>() };

        [Parameter]
        public bool Cluster { get; set; }

        [Parameter]
        public int ClusterRadius { get; set; } = 50;

        [Parameter]
        public int? ClusterMaxZoom { get; set; }

        [Parameter]
        public int? ClusterMinPoints { get; set; }

        [Parameter]
        public ClusterOptions? ClusterOptions { get; set; }

        [Parameter]
        public IReadOnlyList<MapLayerDefinition>? Layers { get; set; }

        [Parameter]
        public bool IncludeChildLayer { get; set; }

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
                        mapBuilder.AddAttribute(1, nameof(GeoJsonSource.Id), "geojson-source");
                        mapBuilder.AddAttribute(2, nameof(GeoJsonSource.AllowOutsideMapSources), true);
                        mapBuilder.AddAttribute(3, nameof(GeoJsonSource.Data), Data);
                        mapBuilder.AddAttribute(4, nameof(GeoJsonSource.Cluster), Cluster);
                        mapBuilder.AddAttribute(5, nameof(GeoJsonSource.ClusterRadius), ClusterRadius);
                        mapBuilder.AddAttribute(6, nameof(GeoJsonSource.ClusterMaxZoom), ClusterMaxZoom);
                        mapBuilder.AddAttribute(7, nameof(GeoJsonSource.ClusterMinPoints), ClusterMinPoints);
                        mapBuilder.AddAttribute(8, nameof(GeoJsonSource.ClusterOptions), ClusterOptions);
                        mapBuilder.AddAttribute(9, nameof(GeoJsonSource.Layers), Layers);
                        if (IncludeChildLayer)
                        {
                            mapBuilder.AddAttribute(
                                10,
                                nameof(GeoJsonSource.ChildContent),
                                (RenderFragment)(
                                    sourceBuilder =>
                                    {
                                        sourceBuilder.OpenComponent<CircleLayer>(0);
                                        sourceBuilder.AddAttribute(1, nameof(CircleLayer.Id), "child-layer");
                                        sourceBuilder.AddAttribute(
                                            2,
                                            nameof(CircleLayer.Radius),
                                            (StyleValue<double>)12
                                        );
                                        sourceBuilder.CloseComponent();
                                    }
                                )
                            );
                        }

                        mapBuilder.CloseComponent();
                    }
                )
            );
            builder.AddComponentReferenceCapture(2, value => Map = (SgbMap)value);
            builder.CloseComponent();
        }
    }

    private sealed class DisconnectingMapJsRuntime : IJSRuntime
    {
        private readonly List<MapSceneMutationBatch> _sceneMutationBatches = [];

        public bool DisconnectOnApplySceneMutations { get; set; }

        public Action? BeforeDisconnect { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == ApplySceneMutationsIdentifier)
            {
                if (DisconnectOnApplySceneMutations)
                {
                    BeforeDisconnect?.Invoke();
                    throw new JSDisconnectedException("test disconnect");
                }

                if (args is not null && args.Length > 1 && args[1] is MapSceneMutationBatch batch)
                {
                    _sceneMutationBatches.Add(batch);
                }
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args
        ) => InvokeAsync<TValue>(identifier, args);

        public IReadOnlyList<MapSceneMutation> GetMutations(string kind) =>
            _sceneMutationBatches
                .SelectMany(batch => batch.Mutations)
                .Where(mutation => mutation.Kind == kind)
                .ToArray();
    }
}
