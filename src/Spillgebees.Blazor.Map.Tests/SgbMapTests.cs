using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests;

public class SgbMapTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string GetProtocolVersionIdentifier = "Spillgebees.Map.getProtocolVersion";
    private const string HasStyleLayerIdentifier = "Spillgebees.Map.mapFunctions.hasStyleLayer";
    private const string SetStyleLayerVisibilityIdentifier = "Spillgebees.Map.mapFunctions.setStyleLayerVisibility";
    private const string GetCenterIdentifier = "Spillgebees.Map.mapFunctions.getCenter";
    private const string GetZoomIdentifier = "Spillgebees.Map.mapFunctions.getZoom";
    private const string GetBoundsIdentifier = "Spillgebees.Map.mapFunctions.getBounds";
    private const string QueryRenderedFeaturesIdentifier = "Spillgebees.Map.mapFunctions.queryRenderedFeatures";
    private const string MoveMapLayerIdentifier = "Spillgebees.Map.mapFunctions.moveMapLayer";
    private const string SetTrackedEntityFeatureStateIdentifier =
        "Spillgebees.Map.mapFunctions.setTrackedEntityFeatureState";
    private const string AddMapSourceIdentifier = "Spillgebees.Map.mapFunctions.addMapSource";
    private const string AddMapLayerIdentifier = "Spillgebees.Map.mapFunctions.addMapLayer";

    /// <summary>
    /// Timeout in milliseconds for tests to prevent hanging.
    /// </summary>
    private const int TestTimeoutMs = 5000;

    public SgbMapTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.Setup<int>(GetProtocolVersionIdentifier).SetResult(10);
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.Setup<bool>(HasStyleLayerIdentifier).SetResult(true);
        JSInterop.SetupVoid(SetStyleLayerVisibilityIdentifier);
        JSInterop.Setup<Coordinate?>(GetCenterIdentifier).SetResult(new Coordinate(49.61, 6.13));
        JSInterop.Setup<double?>(GetZoomIdentifier).SetResult(13.5);
        JSInterop
            .Setup<MapBounds?>(GetBoundsIdentifier)
            .SetResult(new MapBounds(new Coordinate(49.4, 5.9), new Coordinate(49.8, 6.3)));
        JSInterop.Setup<List<object>>(QueryRenderedFeaturesIdentifier).SetResult([]);
        JSInterop.SetupVoid(MoveMapLayerIdentifier);
        JSInterop.SetupVoid(SetTrackedEntityFeatureStateIdentifier);
        JSInterop.SetupVoid(AddMapSourceIdentifier);
        JSInterop.SetupVoid(AddMapLayerIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_render_with_custom_dimensions(CancellationToken cancellationToken)
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters => parameters.Add(p => p.Width, "800px").Add(p => p.Height, "600px"));

        // assert
        var mapContainer = cut.Find("div.sgb-map-container");
        var style = mapContainer.GetAttribute("style") ?? "";
        style.Should().Contain("width:800px");
        style.Should().Contain("height:600px");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_add_custom_css_to_map_container(CancellationToken cancellationToken)
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters => parameters.Add(p => p.ContainerClass, "my-custom-class"));

        // assert
        var mapContainer = cut.Find("div.sgb-map-container.my-custom-class");
        mapContainer.Should().NotBeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_trigger_map_initialization_after_render(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>();

        // assert
        JSInterop.VerifyInvoke(CreateMapIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_dispose_map_correctly_when_js_initialization_has_finished(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        // simulate map initialization completion
        await cut.Instance.OnMapInitializedAsync();
        await cut.Instance.DisposeAsync();

        // assert
        JSInterop.VerifyInvoke(DisposeMapIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_dispose_map_correctly_when_js_initialization_has_not_finished(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.DisposeAsync();

        // assert
        JSInterop.VerifyInvoke(DisposeMapIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_get_current_center_via_advanced_interop(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.GetCenterAsync();

        // assert
        JSInterop.VerifyInvoke(GetCenterIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_get_current_bounds_via_advanced_interop(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.GetBoundsAsync();

        // assert
        JSInterop.VerifyInvoke(GetBoundsIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_get_current_zoom_via_advanced_interop(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.GetZoomAsync();

        // assert
        JSInterop.VerifyInvoke(GetZoomIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_query_rendered_features_via_raw_escape_hatch(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.QueryRenderedFeaturesAsync(new Point(10, 20), ["tracked-primary"]);

        // assert
        JSInterop.VerifyInvoke(QueryRenderedFeaturesIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_move_layer_via_advanced_interop(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.MoveLayerAsync("tracked-primary", "clusters");

        // assert
        JSInterop.VerifyInvoke(MoveMapLayerIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_check_composed_style_layer_by_style_id(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.HasStyleLayerAsync("sgb-positron", "roads");

        // assert
        JSInterop.VerifyInvoke(HasStyleLayerIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_set_composed_style_layer_visibility_by_style_id(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.SetStyleLayerVisibilityAsync("sgb-train-tracking-overlay", "railway-stations-circle", false);

        // assert
        JSInterop.VerifyInvoke(SetStyleLayerVisibilityIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_require_non_empty_ids_for_custom_composed_styles(CancellationToken cancellationToken)
    {
        // arrange
        var customStyle = MapStyle.FromUrl("https://example.com/overlay.json");

        // act
        var act = () =>
            Render<SgbMap>(parameters =>
                parameters.Add(
                    p => p.MapOptions,
                    new MapOptions(new Coordinate(0, 0), Styles: [MapStyle.OpenFreeMap.Positron, customStyle])
                )
            );

        // assert
        act.Should().Throw<ArgumentException>().WithMessage("*non-empty style ID*");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_require_unique_ids_within_composed_styles(CancellationToken cancellationToken)
    {
        // arrange
        var styleA = MapStyle.FromUrl("https://example.com/a.json").WithId("duplicate");
        var styleB = MapStyle.FromUrl("https://example.com/b.json").WithId("duplicate");

        // act
        var act = () =>
            Render<SgbMap>(parameters =>
                parameters.Add(p => p.MapOptions, new MapOptions(new Coordinate(0, 0), Styles: [styleA, styleB]))
            );

        // assert
        act.Should().Throw<ArgumentException>().WithMessage("*unique IDs*duplicate*");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_assign_stable_built_in_style_ids(CancellationToken cancellationToken)
    {
        // arrange & act
        var positron = MapStyle.OpenFreeMap.Positron;
        var liberty = MapStyle.OpenFreeMap.Liberty;
        var bright = MapStyle.OpenFreeMap.Bright;
        var standard = MapStyle.OpenStreetMap.Standard;

        // assert
        positron.Id.Should().Be("sgb-positron");
        liberty.Id.Should().Be("sgb-liberty");
        bright.Id.Should().Be("sgb-bright");
        standard.Id.Should().Be("sgb-openstreetmap-standard");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_assign_origin_referrer_policy_to_openstreetmap_standard(CancellationToken cancellationToken)
    {
        // arrange & act
        var standard = MapStyle.OpenStreetMap.Standard;

        // assert
        standard.RasterSource.Should().NotBeNull();
        standard.RasterSource!.ReferrerPolicy.Should().Be(ReferrerPolicy.Origin);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_apply_with_referrer_policy_to_raster_tile_styles(CancellationToken cancellationToken)
    {
        // arrange
        var style = MapStyle.FromRasterUrl("https://tiles.example.com/{z}/{x}/{y}.png", "© Example");

        // act
        var updatedStyle = style.WithReferrerPolicy(ReferrerPolicy.StrictOriginWhenCrossOrigin);

        // assert
        updatedStyle.RasterSource.Should().NotBeNull();
        updatedStyle.RasterSource!.ReferrerPolicy.Should().Be(ReferrerPolicy.StrictOriginWhenCrossOrigin);
        updatedStyle.ReferrerPolicy.Should().BeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_apply_with_referrer_policy_to_wms_styles(CancellationToken cancellationToken)
    {
        // arrange
        var style = MapStyle.FromWmsUrl("https://example.com/wms", "roads", "© Example");

        // act
        var updatedStyle = style.WithReferrerPolicy(ReferrerPolicy.NoReferrer);

        // assert
        updatedStyle.WmsSource.Should().NotBeNull();
        updatedStyle.WmsSource!.ReferrerPolicy.Should().Be(ReferrerPolicy.NoReferrer);
        updatedStyle.ReferrerPolicy.Should().BeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_propagate_referrer_policy_from_wmts_url_to_raster_source(CancellationToken cancellationToken)
    {
        // arrange & act
        var style = MapStyle.FromWmtsUrl(
            "https://server/arcgis/rest/services/Name/MapServer/WMTS",
            "myLayer",
            "© Example",
            referrerPolicy: ReferrerPolicy.StrictOriginWhenCrossOrigin
        );

        // assert
        style.RasterSource.Should().NotBeNull();
        style.RasterSource!.ReferrerPolicy.Should().Be(ReferrerPolicy.StrictOriginWhenCrossOrigin);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_leave_referrer_policy_null_on_wmts_url_when_not_specified(CancellationToken cancellationToken)
    {
        // arrange & act
        var style = MapStyle.FromWmtsUrl(
            "https://server/arcgis/rest/services/Name/MapServer/WMTS",
            "myLayer",
            "© Example"
        );

        // assert
        style.RasterSource.Should().NotBeNull();
        style.RasterSource!.ReferrerPolicy.Should().BeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_propagate_referrer_policy_from_arcgis_map_server_to_raster_source(
        CancellationToken cancellationToken
    )
    {
        // arrange & act
        var style = MapStyle.FromArcGisMapServer(
            "https://server/arcgis/rest/services/Name/MapServer",
            "© Example",
            referrerPolicy: ReferrerPolicy.Origin
        );

        // assert
        style.RasterSource.Should().NotBeNull();
        style.RasterSource!.ReferrerPolicy.Should().Be(ReferrerPolicy.Origin);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_leave_referrer_policy_null_on_arcgis_map_server_when_not_specified(
        CancellationToken cancellationToken
    )
    {
        // arrange & act
        var style = MapStyle.FromArcGisMapServer("https://server/arcgis/rest/services/Name/MapServer", "© Example");

        // assert
        style.RasterSource.Should().NotBeNull();
        style.RasterSource!.ReferrerPolicy.Should().BeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_assign_stable_declaration_order_to_custom_layers(CancellationToken cancellationToken)
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters =>
            parameters.Add(
                p => p.ChildContent,
                (RenderFragment)(
                    builder =>
                    {
                        builder.OpenComponent<GeoJsonSource>(0);
                        builder.AddAttribute(1, nameof(GeoJsonSource.Id), "ordered-source");
                        builder.AddAttribute(
                            2,
                            nameof(GeoJsonSource.Data),
                            new Dictionary<string, object?>
                            {
                                ["type"] = "FeatureCollection",
                                ["features"] = Array.Empty<object>(),
                            }
                        );
                        builder.AddAttribute(
                            3,
                            nameof(GeoJsonSource.ChildContent),
                            (RenderFragment)(
                                sourceBuilder =>
                                {
                                    sourceBuilder.OpenComponent<SymbolLayer>(0);
                                    sourceBuilder.AddAttribute(1, nameof(SymbolLayer.Id), "layer-a");
                                    sourceBuilder.AddAttribute(
                                        2,
                                        nameof(SymbolLayer.TextField),
                                        (Spillgebees.Blazor.Map.Models.Expressions.StyleValue<string>)"a"
                                    );
                                    sourceBuilder.CloseComponent();

                                    sourceBuilder.OpenComponent<SymbolLayer>(3);
                                    sourceBuilder.AddAttribute(4, nameof(SymbolLayer.Id), "layer-b");
                                    sourceBuilder.AddAttribute(
                                        5,
                                        nameof(SymbolLayer.TextField),
                                        (Spillgebees.Blazor.Map.Models.Expressions.StyleValue<string>)"b"
                                    );
                                    sourceBuilder.CloseComponent();
                                }
                            )
                        );
                        builder.CloseComponent();
                    }
                )
            )
        );

        var layers = cut.FindComponents<SymbolLayer>().Select(layer => layer.Instance).ToArray();

        // assert
        layers.Should().HaveCount(2);
        layers[0]
            .GetLayerOrderRegistration()
            .DeclarationOrder.Should()
            .BeLessThan(layers[1].GetLayerOrderRegistration().DeclarationOrder);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_reuse_declaration_order_when_a_layer_is_toggled_off_and_on(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<LayerToggleHarness>();
        var initialPrimaryOrder = cut.FindComponents<SymbolLayer>()
            .Single(layer => layer.Instance.Id == "layer-a")
            .Instance.GetLayerOrderRegistration()
            .DeclarationOrder;

        // act
        await cut.InvokeAsync(() => cut.Instance.SetShowPrimary(false));
        await cut.InvokeAsync(() => cut.Instance.SetShowPrimary(true));

        var layers = cut.FindComponents<SymbolLayer>().Select(layer => layer.Instance).ToArray();
        var recreatedPrimaryOrder = layers
            .Single(layer => layer.Id == "layer-a")
            .GetLayerOrderRegistration()
            .DeclarationOrder;
        var secondaryOrder = layers.Single(layer => layer.Id == "layer-b").GetLayerOrderRegistration().DeclarationOrder;

        // assert
        recreatedPrimaryOrder.Should().Be(initialPrimaryOrder);
        recreatedPrimaryOrder.Should().BeLessThan(secondaryOrder);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_set_tracked_entity_feature_state_via_advanced_interop(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.SetTrackedEntityFeatureStateAsync(
            "tracked-primary",
            "tracked-primary-decorations",
            "train-1",
            new Dictionary<string, object> { ["hover"] = true, ["selected"] = false }
        );

        // assert
        JSInterop.VerifyInvoke(SetTrackedEntityFeatureStateIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_allow_child_components_to_exit_cleanly_when_map_is_disposed_before_ready(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<MapDisposalHarness>();

        // act
        var act = async () => await cut.Instance.Map.DisposeAsync();

        // assert
        await act.Should().NotThrowAsync();
        cut.Instance.SourceReady.Should().BeFalse();
        JSInterop.VerifyNotInvoke(AddMapSourceIdentifier);
        JSInterop.VerifyNotInvoke(AddMapLayerIdentifier);
    }

    public sealed class MapDisposalHarness : ComponentBase
    {
        public SgbMap Map { get; private set; } = null!;
        public bool SourceReady { get; private set; }
        public bool LayerReady { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(
                1,
                "ChildContent",
                (RenderFragment)(
                    mapBuilder =>
                    {
                        mapBuilder.OpenComponent<TestGeoJsonSource>(0);
                        mapBuilder.AddAttribute(
                            1,
                            nameof(TestGeoJsonSource.OnInitializedFlagChanged),
                            EventCallback.Factory.Create<bool>(this, value => SourceReady = value)
                        );
                        mapBuilder.AddAttribute(
                            2,
                            "ChildContent",
                            (RenderFragment)(
                                sourceBuilder =>
                                {
                                    sourceBuilder.OpenComponent<TestSymbolLayer>(0);
                                    sourceBuilder.AddAttribute(
                                        1,
                                        nameof(TestSymbolLayer.OnInitializedFlagChanged),
                                        EventCallback.Factory.Create<bool>(this, value => LayerReady = value)
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

    public sealed class TestGeoJsonSource : GeoJsonSource
    {
        [Parameter]
        public EventCallback<bool> OnInitializedFlagChanged { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                await OnInitializedFlagChanged.InvokeAsync(_isInitializedAccessor()());
            }
        }

        private Func<bool> _isInitializedAccessor() =>
            () =>
                (bool)
                    typeof(GeoJsonSource)
                        .GetField(
                            "_isInitialized",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                        )!
                        .GetValue(this)!;

        public TestGeoJsonSource()
        {
            Id = "test-source";
            Data = new Dictionary<string, object?>
            {
                ["type"] = "FeatureCollection",
                ["features"] = Array.Empty<object>(),
            };
        }
    }

    public sealed class TestSymbolLayer : SymbolLayer
    {
        [Parameter]
        public EventCallback<bool> OnInitializedFlagChanged { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                var isInitialized = (bool)
                    typeof(LayerBase)
                        .GetField(
                            "_isInitialized",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                        )!
                        .GetValue(this)!;
                await OnInitializedFlagChanged.InvokeAsync(isInitialized);
            }
        }

        public TestSymbolLayer()
        {
            Id = "test-layer";
            TextField = "label";
        }
    }

    public sealed class LayerToggleHarness : ComponentBase
    {
        public bool ShowPrimary { get; private set; } = true;

        public void SetShowPrimary(bool value)
        {
            ShowPrimary = value;
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
                        mapBuilder.AddAttribute(1, nameof(GeoJsonSource.Id), "toggle-source");
                        mapBuilder.AddAttribute(
                            2,
                            nameof(GeoJsonSource.Data),
                            new Dictionary<string, object?>
                            {
                                ["type"] = "FeatureCollection",
                                ["features"] = Array.Empty<object>(),
                            }
                        );
                        mapBuilder.AddAttribute(
                            3,
                            nameof(GeoJsonSource.ChildContent),
                            (RenderFragment)(
                                sourceBuilder =>
                                {
                                    if (ShowPrimary)
                                    {
                                        sourceBuilder.OpenComponent<SymbolLayer>(0);
                                        sourceBuilder.AddAttribute(1, nameof(SymbolLayer.Id), "layer-a");
                                        sourceBuilder.AddAttribute(
                                            2,
                                            nameof(SymbolLayer.TextField),
                                            (Spillgebees.Blazor.Map.Models.Expressions.StyleValue<string>)"a"
                                        );
                                        sourceBuilder.CloseComponent();
                                    }

                                    sourceBuilder.OpenComponent<SymbolLayer>(3);
                                    sourceBuilder.AddAttribute(4, nameof(SymbolLayer.Id), "layer-b");
                                    sourceBuilder.AddAttribute(
                                        5,
                                        nameof(SymbolLayer.TextField),
                                        (Spillgebees.Blazor.Map.Models.Expressions.StyleValue<string>)"b"
                                    );
                                    sourceBuilder.CloseComponent();
                                }
                            )
                        );
                        mapBuilder.CloseComponent();
                    }
                )
            );
            builder.CloseComponent();
        }
    }
}
