using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class GeoJsonSourceClusterOptionsTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";

    public GeoJsonSourceClusterOptionsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
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
        sourceSpec["clusterProperties"].Should().BeSameAs(properties);
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

        var sourceSpec = JSInterop
            .Invocations[ApplySceneMutationsIdentifier]
            .Select(invocation => invocation.Arguments[1])
            .OfType<MapSceneMutationBatch>()
            .SelectMany(batch => batch.Mutations)
            .Single(mutation => mutation.Kind == "addSource" && mutation.SourceId == sourceId)
            .SourceSpec;

        sourceSpec.Should().NotBeNull();

        return sourceSpec!;
    }

    public sealed class GeoJsonClusterSourceHarness : ComponentBase
    {
        public SgbMap Map { get; private set; } = null!;

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
                        mapBuilder.AddAttribute(4, nameof(GeoJsonSource.Cluster), Cluster);
                        mapBuilder.AddAttribute(5, nameof(GeoJsonSource.ClusterRadius), ClusterRadius);
                        mapBuilder.AddAttribute(6, nameof(GeoJsonSource.ClusterMaxZoom), ClusterMaxZoom);
                        mapBuilder.AddAttribute(7, nameof(GeoJsonSource.ClusterMinPoints), ClusterMinPoints);
                        mapBuilder.AddAttribute(8, nameof(GeoJsonSource.ClusterOptions), ClusterOptions);
                        mapBuilder.CloseComponent();
                    }
                )
            );
            builder.AddComponentReferenceCapture(2, value => Map = (SgbMap)value);
            builder.CloseComponent();
        }
    }
}
