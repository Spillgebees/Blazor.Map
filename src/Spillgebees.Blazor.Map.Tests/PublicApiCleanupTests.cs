using AwesomeAssertions;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Tests;

public class PublicApiCleanupTests
{
    [Test]
    public void Should_expose_tracked_entity_definition_public_types()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var expectedTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.TrackedEntityLayerDefinition`1",
            "Spillgebees.Blazor.Map.ITrackedEntityLayerDefinition",
            "Spillgebees.Blazor.Map.TrackedEntityIdOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntityVisualOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntityVisualDefaults",
            "Spillgebees.Blazor.Map.TrackedEntitySymbolOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntityDecorationOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntitySourceOptions",
            "Spillgebees.Blazor.Map.TrackedEntityBehaviorOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntityInteractionOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntityCallbacks`1",
            "Spillgebees.Blazor.Map.TrackedEntityInteractionEventArgs`1",
        };

        // act
        var resolvedTypes = expectedTypeNames.Select(assembly.GetType);

        // assert
        resolvedTypes.Should().AllSatisfy(type => type.Should().NotBeNull());
    }

    [Test]
    public void Should_not_expose_accidental_helper_runtime_types()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var accidentalTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.TrackedEntityMaterializer",
            "Spillgebees.Blazor.Map.TrackedEntityGeoJsonBuilder",
            "Spillgebees.Blazor.Map.TrackedEntity`1",
            "Spillgebees.Blazor.Map.TrackedEntitySymbol",
            "Spillgebees.Blazor.Map.TrackedEntityDecoration",
            "Spillgebees.Blazor.Map.Utilities.FeatureDiffer",
            "Spillgebees.Blazor.Map.Utilities.FeatureDiffResult`1",
            "Spillgebees.Blazor.Map.IMapSource",
            "Spillgebees.Blazor.Map.MapLayerOrderOptions",
            "Spillgebees.Blazor.Map.Utilities.LowerCaseJsonStringEnumConverter",
            "Spillgebees.Blazor.Map.Utilities.LowercaseNamingPolicy",
            "Spillgebees.Blazor.Map.StyleValueConverterFactory",
            "Spillgebees.Blazor.Map.MapButtonGroupContext",
            "Spillgebees.Blazor.Map.MapControlComponentRegistration",
            "Spillgebees.Blazor.Map.MapControlRegistryContext",
            "Spillgebees.Blazor.Map.StyledContentMapControlRegistration",
            "Spillgebees.Blazor.Map.MapOverlayComponentBase",
            "Spillgebees.Blazor.Map.MapSectionBase",
            "Spillgebees.Blazor.Map.LegendMapControlHost",
            "Spillgebees.Blazor.Map.TrackedEntityClusterClickBehavior",
            "Spillgebees.Blazor.Map.TrackedEntityClusterOptions",
        };

        // act
        var exportedTypeNames = assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .ToHashSet(StringComparer.Ordinal);

        // assert
        foreach (var accidentalTypeName in accidentalTypeNames)
        {
            exportedTypeNames.Should().NotContain(accidentalTypeName);
        }
    }

    [Test]
    public void Should_expose_public_component_and_model_api_allow_list()
    {
        // arrange
        var expectedTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.BaseMap",
            "Spillgebees.Blazor.Map.CircleLayer",
            "Spillgebees.Blazor.Map.FillExtrusionLayer",
            "Spillgebees.Blazor.Map.FillLayer",
            "Spillgebees.Blazor.Map.GeoJsonSource",
            "Spillgebees.Blazor.Map.LayerBase",
            "Spillgebees.Blazor.Map.LineLayer",
            "Spillgebees.Blazor.Map.SymbolLayer",
            "Spillgebees.Blazor.Map.TrackedEntityLayer`1",
            "Spillgebees.Blazor.Map.VectorTileSource",
            "Spillgebees.Blazor.Map.ButtonMapControl",
            "Spillgebees.Blazor.Map.CenterMapControl",
            "Spillgebees.Blazor.Map.MapCircle",
            "Spillgebees.Blazor.Map.MapCircles`1",
            "Spillgebees.Blazor.Map.MapButton",
            "Spillgebees.Blazor.Map.ButtonGroupMapControl",
            "Spillgebees.Blazor.Map.MapToggleButton",
            "Spillgebees.Blazor.Map.MapControls",
            "Spillgebees.Blazor.Map.CustomMapControl",
            "Spillgebees.Blazor.Map.FullscreenMapControl",
            "Spillgebees.Blazor.Map.GeolocateMapControl",
            "Spillgebees.Blazor.Map.LayerMapControl",
            "Spillgebees.Blazor.Map.LegendMapControl",
            "Spillgebees.Blazor.Map.MapFeatures",
            "Spillgebees.Blazor.Map.MapMarker",
            "Spillgebees.Blazor.Map.MapMarkers`1",
            "Spillgebees.Blazor.Map.NavigationMapControl",
            "Spillgebees.Blazor.Map.OverlayMapControl",
            "Spillgebees.Blazor.Map.PanelMapControl",
            "Spillgebees.Blazor.Map.MapOverlay",
            "Spillgebees.Blazor.Map.MapOverlays",
            "Spillgebees.Blazor.Map.MapOverlayPart",
            "Spillgebees.Blazor.Map.MapPolyline",
            "Spillgebees.Blazor.Map.MapPolylines`1",
            "Spillgebees.Blazor.Map.MapPopup",
            "Spillgebees.Blazor.Map.ScaleMapControl",
            "Spillgebees.Blazor.Map.StyleOverlay",
            "Spillgebees.Blazor.Map.MapSources",
            "Spillgebees.Blazor.Map.TerrainMapControl",
            "Spillgebees.Blazor.Map.ToggleButtonMapControl",
            "Spillgebees.Blazor.Map.SgbMap",
            "Spillgebees.Blazor.Map.AnimationEasing",
            "Spillgebees.Blazor.Map.AnimationOptions",
            "Spillgebees.Blazor.Map.ClusterCircleLayerDefinition",
            "Spillgebees.Blazor.Map.ClusterClickBehavior",
            "Spillgebees.Blazor.Map.ClusterLayerDefinition",
            "Spillgebees.Blazor.Map.ClusterLayerSet",
            "Spillgebees.Blazor.Map.ClusterOptions",
            "Spillgebees.Blazor.Map.ClusterSymbolLayerDefinition",
            "Spillgebees.Blazor.Map.CircleLayerDefinition",
            "Spillgebees.Blazor.Map.CenterControlDefinition",
            "Spillgebees.Blazor.Map.ContentControlDefinition",
            "Spillgebees.Blazor.Map.ControlPosition",
            "Spillgebees.Blazor.Map.FullscreenControlDefinition",
            "Spillgebees.Blazor.Map.GeolocateControlDefinition",
            "Spillgebees.Blazor.Map.LegendChromeOptions",
            "Spillgebees.Blazor.Map.LegendContentOptions",
            "Spillgebees.Blazor.Map.LegendControlDefinition",
            "Spillgebees.Blazor.Map.MapControlDefinition",
            "Spillgebees.Blazor.Map.MapButtonSize",
            "Spillgebees.Blazor.Map.MapButtonVariant",
            "Spillgebees.Blazor.Map.MapControlPlacement",
            "Spillgebees.Blazor.Map.NavigationControlDefinition",
            "Spillgebees.Blazor.Map.PanelChromeOptions",
            "Spillgebees.Blazor.Map.PanelControlDefinition",
            "Spillgebees.Blazor.Map.ScaleControlDefinition",
            "Spillgebees.Blazor.Map.ScaleUnit",
            "Spillgebees.Blazor.Map.TerrainControlDefinition",
            "Spillgebees.Blazor.Map.Coordinate",
            "Spillgebees.Blazor.Map.LayerFeatureEventArgs",
            "Spillgebees.Blazor.Map.MapClickEventArgs",
            "Spillgebees.Blazor.Map.MapViewEventArgs",
            "Spillgebees.Blazor.Map.MarkerClickEventArgs",
            "Spillgebees.Blazor.Map.MarkerDragEventArgs",
            "Spillgebees.Blazor.Map.Expr",
            "Spillgebees.Blazor.Map.FeatureState",
            "Spillgebees.Blazor.Map.FeatureStateKey`1",
            "Spillgebees.Blazor.Map.StyleValue`1",
            "Spillgebees.Blazor.Map.FitBoundsOptions",
            "Spillgebees.Blazor.Map.Circle",
            "Spillgebees.Blazor.Map.Marker",
            "Spillgebees.Blazor.Map.MarkerIcon",
            "Spillgebees.Blazor.Map.Polyline",
            "Spillgebees.Blazor.Map.MapLegend",
            "Spillgebees.Blazor.Map.MapLegendItem",
            "Spillgebees.Blazor.Map.MapLegendItemTemplateContext",
            "Spillgebees.Blazor.Map.MapLegendSection",
            "Spillgebees.Blazor.Map.MapLegendSymbol",
            "Spillgebees.Blazor.Map.MapLegendSymbol+CircleSymbol",
            "Spillgebees.Blazor.Map.MapLegendSymbol+ColorSwatchSymbol",
            "Spillgebees.Blazor.Map.MapLegendSymbol+IconSymbol",
            "Spillgebees.Blazor.Map.MapLegendSymbol+LineSymbol",
            "Spillgebees.Blazor.Map.MapLegendSymbol+NoneSymbol",
            "Spillgebees.Blazor.Map.MapBounds",
            "Spillgebees.Blazor.Map.MapImage",
            "Spillgebees.Blazor.Map.MapOptions",
            "Spillgebees.Blazor.Map.MapOverlayChangedEventArgs",
            "Spillgebees.Blazor.Map.MapOverlayControlItemContext",
            "Spillgebees.Blazor.Map.MapOverlayItem",
            "Spillgebees.Blazor.Map.MapOverlayPartControlItemContext",
            "Spillgebees.Blazor.Map.MapOverlayPartItem",
            "Spillgebees.Blazor.Map.MapPixelRatioMode",
            "Spillgebees.Blazor.Map.MapProjection",
            "Spillgebees.Blazor.Map.MapStyle",
            "Spillgebees.Blazor.Map.MapStyle+OpenFreeMap",
            "Spillgebees.Blazor.Map.MapStyle+OpenStreetMap",
            "Spillgebees.Blazor.Map.MapTheme",
            "Spillgebees.Blazor.Map.CirclePitchAlignment",
            "Spillgebees.Blazor.Map.EnumJsonName",
            "Spillgebees.Blazor.Map.IconTextFit",
            "Spillgebees.Blazor.Map.LayerOptionEnumExtensions",
            "Spillgebees.Blazor.Map.LineCap",
            "Spillgebees.Blazor.Map.LineJoin",
            "Spillgebees.Blazor.Map.MapAlignment",
            "Spillgebees.Blazor.Map.SymbolAnchor",
            "Spillgebees.Blazor.Map.SymbolPlacement",
            "Spillgebees.Blazor.Map.TextTransform",
            "Spillgebees.Blazor.Map.PixelPoint",
            "Spillgebees.Blazor.Map.PopupAnchor",
            "Spillgebees.Blazor.Map.PopupContentMode",
            "Spillgebees.Blazor.Map.PopupOptions",
            "Spillgebees.Blazor.Map.PopupTrigger",
            "Spillgebees.Blazor.Map.RasterTileSource",
            "Spillgebees.Blazor.Map.ReferrerPolicy",
            "Spillgebees.Blazor.Map.TileOverlay",
            "Spillgebees.Blazor.Map.ITrackedEntityLayerDefinition",
            "Spillgebees.Blazor.Map.TrackedEntityBehaviorOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntityCallbacks`1",
            "Spillgebees.Blazor.Map.TrackedEntityDecorationDisplayMode",
            "Spillgebees.Blazor.Map.TrackedEntityDecorationOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntityFeatureKind",
            "Spillgebees.Blazor.Map.TrackedEntityFeatureProperties",
            "Spillgebees.Blazor.Map.TrackedEntityFeatureStates",
            "Spillgebees.Blazor.Map.TrackedEntityHoverIntent",
            "Spillgebees.Blazor.Map.TrackedEntityIdOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntityInteractionEventArgs`1",
            "Spillgebees.Blazor.Map.TrackedEntityInteractionOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntityLayerDefinition`1",
            "Spillgebees.Blazor.Map.TrackedEntitySymbolOptions`1",
            "Spillgebees.Blazor.Map.TrackedEntitySourceOptions",
            "Spillgebees.Blazor.Map.TrackedEntityVisualDefaults",
            "Spillgebees.Blazor.Map.TrackedEntityVisualOptions`1",
            "Spillgebees.Blazor.Map.MapLayerVisibilityChangeKind",
            "Spillgebees.Blazor.Map.MapLayerVisibilityChangedEventArgs",
            "Spillgebees.Blazor.Map.MapLayerControlItemContext",
            "Spillgebees.Blazor.Map.MapLayerVisibilityGroup",
            "Spillgebees.Blazor.Map.MapLayerVisibilityState",
            "Spillgebees.Blazor.Map.MapLayerVisibilityTarget",
            "Spillgebees.Blazor.Map.MapLayerVisibilityTargetKind",
            "Spillgebees.Blazor.Map.MapLayer",
            "Spillgebees.Blazor.Map.MapLayerDefinition",
            "Spillgebees.Blazor.Map.SymbolLayerDefinition",
            "Spillgebees.Blazor.Map.WmsTileSource",
        };

        // act
        var exportedTypeNames = typeof(SgbMap)
            .Assembly.GetExportedTypes()
            .Where(type => type.FullName is not "Spillgebees.Blazor.Map._Imports")
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal);

        // assert
        exportedTypeNames.Should().BeEquivalentTo(expectedTypeNames);
    }

    [Test]
    public void Should_expose_tracked_entity_layer_definition_public_type()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;

        // act
        var trackedEntityLayerDefinitionType = assembly.GetType(
            "Spillgebees.Blazor.Map.TrackedEntityLayerDefinition`1"
        );

        // assert
        trackedEntityLayerDefinitionType.Should().NotBeNull();
    }

    [Test]
    public void Should_not_expose_legacy_tracked_data_public_types()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var legacyTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.TrackedDataLayer`1",
            "Spillgebees.Blazor.Map.ITrackedDataLayer",
            "Spillgebees.Blazor.Map.TrackedDataIdOptions`1",
            "Spillgebees.Blazor.Map.TrackedDataVisualOptions`1",
            "Spillgebees.Blazor.Map.TrackedDataVisualDefaults",
            "Spillgebees.Blazor.Map.TrackedDataSymbolOptions`1",
            "Spillgebees.Blazor.Map.TrackedDataDecorationOptions`1",
            "Spillgebees.Blazor.Map.TrackedDataClusterOptions",
            "Spillgebees.Blazor.Map.TrackedDataBehaviorOptions`1",
            "Spillgebees.Blazor.Map.TrackedDataInteractionOptions`1",
            "Spillgebees.Blazor.Map.TrackedDataCallbacks`1",
            "Spillgebees.Blazor.Map.TrackedDataEntityMaterializer",
        };

        // act
        var resolvedTypes = legacyTypeNames.Select(assembly.GetType);

        // assert
        resolvedTypes.Should().AllSatisfy(type => type.Should().BeNull());
    }

    [Test]
    public void Should_expose_clean_legend_and_image_model_names()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var expectedTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.MapLegend",
            "Spillgebees.Blazor.Map.MapLegendSection",
            "Spillgebees.Blazor.Map.MapLegendItem",
            "Spillgebees.Blazor.Map.MapImage",
            "Spillgebees.Blazor.Map.MapLayerVisibilityState",
        };

        // act
        var resolvedTypes = expectedTypeNames.Select(assembly.GetType);

        // assert
        resolvedTypes.Should().AllSatisfy(type => type.Should().NotBeNull());
    }

    [Test]
    public void Should_not_expose_legacy_legend_and_image_model_names()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var legacyTypeNames = new[]
        {
            "Spillgebees.Blazor.Map.MapLegendDefinition",
            "Spillgebees.Blazor.Map.MapLegendSectionDefinition",
            "Spillgebees.Blazor.Map.MapLegendItemDefinition",
            "Spillgebees.Blazor.Map.MapLegendTargetDefinition",
            "Spillgebees.Blazor.Map.MapImageDefinition",
        };

        // act
        var resolvedTypes = legacyTypeNames.Select(assembly.GetType);

        // assert
        resolvedTypes.Should().AllSatisfy(type => type.Should().BeNull());
    }

    [Test]
    public void Should_expose_map_image_id_and_sdf_property_names()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;
        var mapImageType = assembly.GetType("Spillgebees.Blazor.Map.MapImage");

        // act
        var publicPropertyNames = mapImageType
            ?.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name);

        // assert
        publicPropertyNames.Should().BeEquivalentTo(["Id", "Url", "Width", "Height", "PixelRatio", "IsSdf"]);
    }

    [Test]
    public void Should_expose_map_legend_model_from_root_namespace()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;

        // act
        var mapLegendType = assembly.GetType("Spillgebees.Blazor.Map.MapLegend");

        // assert
        mapLegendType.Should().Be(typeof(MapLegend));
    }

    [Test]
    public void Should_not_expose_legacy_tracked_data_source_component()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;

        // act
        var trackedDataSourceType = assembly.GetType("Spillgebees.Blazor.Map.TrackedDataSource`1");

        // assert
        trackedDataSourceType.Should().BeNull();
    }

    [Test]
    public void Should_not_expose_legacy_tracked_data_layers_map_parameter()
    {
        // arrange
        var mapType = typeof(SgbMap);

        // act
        var trackedDataLayersProperty = mapType.GetProperty("TrackedDataLayers");

        // assert
        trackedDataLayersProperty.Should().BeNull();
    }

    [Test]
    public void Should_expose_pixel_point_without_legacy_point_type()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;

        // act
        var pixelPointType = assembly.GetType("Spillgebees.Blazor.Map.PixelPoint");
        var pointType = assembly.GetType("Spillgebees.Blazor.Map.Point");

        // assert
        pixelPointType.Should().Be(typeof(PixelPoint));
        pointType.Should().BeNull();
    }

    [Test]
    public void Should_expose_fractional_zoom_option_types()
    {
        // arrange
        var mapOptionsType = typeof(MapOptions);

        // act
        var zoomType = mapOptionsType.GetProperty(nameof(MapOptions.Zoom))?.PropertyType;
        var minZoomType = mapOptionsType.GetProperty(nameof(MapOptions.MinZoom))?.PropertyType;
        var maxZoomType = mapOptionsType.GetProperty(nameof(MapOptions.MaxZoom))?.PropertyType;

        // assert
        zoomType.Should().Be(typeof(double));
        minZoomType.Should().Be(typeof(double?));
        maxZoomType.Should().Be(typeof(double?));
    }

    [Test]
    public void Should_expose_pixel_ratio_option_types()
    {
        // arrange
        var mapOptionsType = typeof(MapOptions);

        // act
        var pixelRatioModeType = mapOptionsType.GetProperty(nameof(MapOptions.PixelRatioMode))?.PropertyType;
        var pixelRatioType = mapOptionsType.GetProperty(nameof(MapOptions.PixelRatio))?.PropertyType;

        // assert
        pixelRatioModeType.Should().Be(typeof(MapPixelRatioMode));
        pixelRatioType.Should().Be(typeof(double?));
    }

    [Test]
    public void Should_expose_fit_bounds_feature_ids_as_read_only_list()
    {
        // arrange
        var fitBoundsOptionsType = typeof(FitBoundsOptions);

        // act
        var featureIdsType = fitBoundsOptionsType.GetProperty(nameof(FitBoundsOptions.FeatureIds))?.PropertyType;

        // assert
        featureIdsType.Should().Be(typeof(IReadOnlyList<string>));
    }

    [Test]
    public void Should_expose_tracked_entity_layer_component()
    {
        // arrange
        var trackedEntityLayerType = GetTrackedEntityLayerType();

        // act
        var publicPropertyNames = trackedEntityLayerType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name);

        // assert
        publicPropertyNames.Should().BeEquivalentTo(["Layer"]);
    }

    [Test]
    public void Should_not_expose_legacy_tracked_entity_layer_parameter_surface()
    {
        // arrange
        var trackedEntityLayerType = GetTrackedEntityLayerType();
        var legacyPropertyNames = new[]
        {
            "SourceId",
            "Items",
            "Id",
            "Symbol",
            "Decorations",
            "Cluster",
            "Interaction",
            "Animation",
            "Visible",
            "PrimaryIconOpacity",
            "OnItemClick",
            "OnItemMouseEnter",
            "OnItemMouseLeave",
            "MaxZoom",
            "Attribution",
            "Stack",
            "BeforeStack",
            "AfterStack",
        };

        // act
        var publicPropertyNames = trackedEntityLayerType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        // assert
        foreach (var legacyPropertyName in legacyPropertyNames)
        {
            publicPropertyNames.Should().NotContain(legacyPropertyName);
        }
    }

    private static Type GetTrackedEntityLayerType()
    {
        var assembly = typeof(SgbMap).Assembly;
        var trackedEntityLayerType = assembly.GetType("Spillgebees.Blazor.Map.TrackedEntityLayer`1");

        trackedEntityLayerType.Should().NotBeNull();
        return trackedEntityLayerType!;
    }
}
