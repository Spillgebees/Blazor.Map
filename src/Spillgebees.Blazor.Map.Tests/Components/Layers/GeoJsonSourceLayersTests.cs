using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class GeoJsonSourceLayersTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";

    public GeoJsonSourceLayersTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_emit_circle_and_symbol_layer_specs_from_layers_parameter(
        CancellationToken cancellationToken
    )
    {
        // arrange
        object[] filter = ["has", "point_count"];
        IReadOnlyList<MapLayerDefinition> layers =
        [
            MapLayer.Circle(
                "clusters",
                color: "#51bbd6",
                radius: 24,
                opacity: 0.75,
                strokeColor: "#ffffff",
                strokeWidth: 2,
                filter: filter,
                minZoom: 4,
                maxZoom: 14,
                visible: false
            ),
            MapLayer.Symbol(
                "cluster-count",
                textField: Expr.Get("point_count_abbreviated"),
                textSize: 12,
                textColor: "#111827",
                textHaloColor: "#ffffff",
                textHaloWidth: 1,
                textAllowOverlap: true
            ),
        ];
        var cut = Render<GeoJsonLayersSourceHarness>(parameters => parameters.Add(p => p.Layers, layers));

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));
        var circleSpec = GetLayerSpec("geojson-source-clusters");
        circleSpec["type"].Should().Be("circle");
        circleSpec["source"].Should().Be("geojson-source");
        circleSpec["filter"].Should().BeSameAs(filter);
        circleSpec["minzoom"].Should().Be(4d);
        circleSpec["maxzoom"].Should().Be(14d);
        circleSpec["paint"]
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["circle-color"] = "#51bbd6",
                    ["circle-radius"] = 24d,
                    ["circle-opacity"] = 0.75d,
                    ["circle-stroke-color"] = "#ffffff",
                    ["circle-stroke-width"] = 2d,
                }
            );
        circleSpec["layout"].Should().BeEquivalentTo(new Dictionary<string, object?> { ["visibility"] = "none" });

        var symbolSpec = GetLayerSpec("geojson-source-cluster-count");
        symbolSpec["type"].Should().Be("symbol");
        symbolSpec["source"].Should().Be("geojson-source");
        symbolSpec["paint"]
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["text-color"] = "#111827",
                    ["text-halo-color"] = "#ffffff",
                    ["text-halo-width"] = 1d,
                }
            );
        symbolSpec["layout"]
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["text-field"] = new object[] { "get", "point_count_abbreviated" },
                    ["text-size"] = 12d,
                    ["text-allow-overlap"] = true,
                    ["visibility"] = "visible",
                }
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_parameter_layers_before_child_layers_for_deterministic_order(
        CancellationToken cancellationToken
    )
    {
        // arrange
        IReadOnlyList<MapLayerDefinition> layers = [MapLayer.Circle("parameter", radius: 10)];
        var cut = Render<GeoJsonLayersSourceHarness>(parameters =>
            parameters.Add(p => p.Layers, layers).Add(p => p.IncludeChildLayer, true)
        );

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));
        GetAddLayerMutations()
            .Select(mutation => mutation.LayerId)
            .Should()
            .Equal("geojson-source-parameter", "child-layer");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_emit_parameter_layers_when_layers_parameter_is_empty(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<GeoJsonLayersSourceHarness>(parameters =>
            parameters.Add(p => p.Layers, Array.Empty<MapLayerDefinition>())
        );

        // act
        await cut.Instance.Map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );
        GetAddLayerMutations().Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replace_parameter_layer_registrations_when_layers_change(
        CancellationToken cancellationToken
    )
    {
        // arrange
        IReadOnlyList<MapLayerDefinition> initialLayers = [MapLayer.Circle("clusters", radius: 10)];
        IReadOnlyList<MapLayerDefinition> updatedLayers = [MapLayer.Symbol("labels", textField: "Station")];
        var cut = Render<GeoJsonLayersSourceHarness>(parameters => parameters.Add(p => p.Layers, initialLayers));
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() =>
            GetAddLayerMutations().Should().Contain(mutation => mutation.LayerId == "geojson-source-clusters")
        );

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Layers, updatedLayers));

        // assert
        cut.WaitForAssertion(() =>
        {
            GetRemoveLayerMutations().Should().Contain(mutation => mutation.LayerId == "geojson-source-clusters");
            GetAddLayerMutations().Should().Contain(mutation => mutation.LayerId == "geojson-source-labels");
        });
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_reregister_equivalent_recreated_parameter_layers(CancellationToken cancellationToken)
    {
        // arrange
        static IReadOnlyList<MapLayerDefinition> CreateLayers() =>
            [
                MapLayer.Circle(
                    "clusters",
                    color: new object[] { "step", new object[] { "get", "point_count" }, "#51bbd6", 100, "#f1f075" },
                    radius: new object[]
                    {
                        "interpolate",
                        new object[] { "linear" },
                        new object[] { "zoom" },
                        4,
                        12,
                        12,
                        24,
                    },
                    filter: new List<object> { "has", "point_count" }
                ),
                MapLayer.Symbol(
                    "cluster-count",
                    textField: Expr.Get("point_count_abbreviated"),
                    textFont: new[] { "Open Sans Regular", "Arial Unicode MS Regular" },
                    textOffset: new[] { 0d, 1.25d }
                ),
            ];
        var cut = Render<GeoJsonLayersSourceHarness>(parameters => parameters.Add(p => p.Layers, CreateLayers()));
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(2));

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Layers, CreateLayers()));

        // assert
        await Task.Delay(50, cancellationToken);
        GetRemoveLayerMutations().Should().BeEmpty();
        GetAddLayerMutations().Should().HaveCount(2);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_detect_structural_changes_in_parameter_layer_expressions(
        CancellationToken cancellationToken
    )
    {
        // arrange
        IReadOnlyList<MapLayerDefinition> initialLayers =
        [
            MapLayer.Circle("clusters", radius: new object[] { "get", "small_radius" }),
        ];
        IReadOnlyList<MapLayerDefinition> updatedLayers =
        [
            MapLayer.Circle("clusters", radius: new object[] { "get", "large_radius" }),
        ];
        var cut = Render<GeoJsonLayersSourceHarness>(parameters => parameters.Add(p => p.Layers, initialLayers));
        await cut.Instance.Map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => GetAddLayerMutations().Should().HaveCount(1));

        // act
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Layers, updatedLayers));

        // assert
        cut.WaitForAssertion(() =>
        {
            GetRemoveLayerMutations().Should().Contain(mutation => mutation.LayerId == "geojson-source-clusters");
            GetAddLayerMutations().Should().HaveCount(2);
        });
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_fail_clearly_for_duplicate_user_layer_ids(CancellationToken cancellationToken)
    {
        // arrange
        IReadOnlyList<MapLayerDefinition> layers =
        [
            MapLayer.Circle("duplicate", radius: 10),
            MapLayer.Symbol("duplicate", textField: "Station"),
        ];

        // act
        var act = () => Render<GeoJsonLayersSourceHarness>(parameters => parameters.Add(p => p.Layers, layers));

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*duplicate*geojson-source-duplicate*");
        await Task.CompletedTask;
    }

    private IReadOnlyDictionary<string, object?> GetLayerSpec(string layerId)
    {
        var layerSpec = GetAddLayerMutations().Single(mutation => mutation.LayerId == layerId).LayerSpec;
        layerSpec.Should().NotBeNull();

        return layerSpec!;
    }

    private IReadOnlyList<MapSceneMutation> GetAddLayerMutations() => GetMutations("addLayer");

    private IReadOnlyList<MapSceneMutation> GetRemoveLayerMutations() => GetMutations("removeLayer");

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

    public sealed class GeoJsonLayersSourceHarness : ComponentBase
    {
        public SgbMap Map { get; private set; } = null!;

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
                        mapBuilder.AddAttribute(
                            3,
                            nameof(GeoJsonSource.Data),
                            new Dictionary<string, object?>
                            {
                                ["type"] = "FeatureCollection",
                                ["features"] = Array.Empty<object>(),
                            }
                        );
                        mapBuilder.AddAttribute(4, nameof(GeoJsonSource.Layers), Layers);
                        if (IncludeChildLayer)
                        {
                            mapBuilder.AddAttribute(
                                5,
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
}

internal static class RenderedComponentParameterExtensions
{
    public static void SetParametersAndRender<TComponent>(
        this IRenderedComponent<TComponent> renderedComponent,
        Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder
    )
        where TComponent : IComponent
    {
        renderedComponent.Render(parameterBuilder);
    }
}
