using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

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
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityLayerDefinition`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.ITrackedEntityLayerDefinition",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityIdOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityVisualOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityVisualDefaults",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntitySymbolOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityDecorationOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityClusterOptions",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityBehaviorOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityInteractionOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityCallbacks`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityInteractionEventArgs`1",
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
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityMaterializer",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityGeoJsonBuilder",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntity`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntitySymbol",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityDecoration",
            "Spillgebees.Blazor.Map.Utilities.FeatureDiffer",
            "Spillgebees.Blazor.Map.Utilities.FeatureDiffResult`1",
            "Spillgebees.Blazor.Map.Components.Layers.IMapSource",
            "Spillgebees.Blazor.Map.Components.Layers.MapLayerOrderOptions",
            "Spillgebees.Blazor.Map.Utilities.LowerCaseJsonStringEnumConverter",
            "Spillgebees.Blazor.Map.Utilities.LowercaseNamingPolicy",
            "Spillgebees.Blazor.Map.Models.Expressions.StyleValueConverterFactory",
            "Spillgebees.Blazor.Map.Components.MapControlComponentBase",
            "Spillgebees.Blazor.Map.Components.StyledContentMapControlBase",
            "Spillgebees.Blazor.Map.Components.MapOverlayComponentBase",
            "Spillgebees.Blazor.Map.Components.MapSectionBase",
            "Spillgebees.Blazor.Map.Components.MapLegendControlHost",
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
            "Spillgebees.Blazor.Map.Components.BaseMap",
            "Spillgebees.Blazor.Map.Components.Layers.CircleLayer",
            "Spillgebees.Blazor.Map.Components.Layers.FillExtrusionLayer",
            "Spillgebees.Blazor.Map.Components.Layers.FillLayer",
            "Spillgebees.Blazor.Map.Components.Layers.GeoJsonSource",
            "Spillgebees.Blazor.Map.Components.Layers.LayerBase",
            "Spillgebees.Blazor.Map.Components.Layers.LineLayer",
            "Spillgebees.Blazor.Map.Components.Layers.SymbolLayer",
            "Spillgebees.Blazor.Map.Components.Layers.TrackedEntityLayer`1",
            "Spillgebees.Blazor.Map.Components.Layers.VectorTileSource",
            "Spillgebees.Blazor.Map.Components.MapActionControl",
            "Spillgebees.Blazor.Map.Components.MapCenterControl",
            "Spillgebees.Blazor.Map.Components.MapCircle",
            "Spillgebees.Blazor.Map.Components.MapCircles`1",
            "Spillgebees.Blazor.Map.Components.MapControlButton",
            "Spillgebees.Blazor.Map.Components.MapControlButtonGroup",
            "Spillgebees.Blazor.Map.Components.MapControlToggleButton",
            "Spillgebees.Blazor.Map.Components.MapControls",
            "Spillgebees.Blazor.Map.Components.MapCustomControl",
            "Spillgebees.Blazor.Map.Components.MapCustomControls",
            "Spillgebees.Blazor.Map.Components.MapFullscreenControl",
            "Spillgebees.Blazor.Map.Components.MapGeolocateControl",
            "Spillgebees.Blazor.Map.Components.MapLegendControl",
            "Spillgebees.Blazor.Map.Components.MapMarker",
            "Spillgebees.Blazor.Map.Components.MapMarkers`1",
            "Spillgebees.Blazor.Map.Components.MapNavigationControl",
            "Spillgebees.Blazor.Map.Components.MapOverlays",
            "Spillgebees.Blazor.Map.Components.MapPolyline",
            "Spillgebees.Blazor.Map.Components.MapPolylines`1",
            "Spillgebees.Blazor.Map.Components.MapPopup",
            "Spillgebees.Blazor.Map.Components.MapScaleControl",
            "Spillgebees.Blazor.Map.Components.MapSources",
            "Spillgebees.Blazor.Map.Components.MapTerrainControl",
            "Spillgebees.Blazor.Map.Components.MapToggleControl",
            "Spillgebees.Blazor.Map.Components.SgbMap",
            "Spillgebees.Blazor.Map.Models.AnimationEasing",
            "Spillgebees.Blazor.Map.Models.AnimationOptions",
            "Spillgebees.Blazor.Map.Models.Controls.CenterMapControl",
            "Spillgebees.Blazor.Map.Models.Controls.ContentMapControl",
            "Spillgebees.Blazor.Map.Models.Controls.ControlPosition",
            "Spillgebees.Blazor.Map.Models.Controls.FullscreenMapControl",
            "Spillgebees.Blazor.Map.Models.Controls.GeolocateMapControl",
            "Spillgebees.Blazor.Map.Models.Controls.LegendChromeOptions",
            "Spillgebees.Blazor.Map.Models.Controls.LegendContentOptions",
            "Spillgebees.Blazor.Map.Models.Controls.LegendMapControl",
            "Spillgebees.Blazor.Map.Models.Controls.MapControl",
            "Spillgebees.Blazor.Map.Models.Controls.MapControlButtonSize",
            "Spillgebees.Blazor.Map.Models.Controls.MapControlButtonVariant",
            "Spillgebees.Blazor.Map.Models.Controls.MapControlPlacement",
            "Spillgebees.Blazor.Map.Models.Controls.NavigationMapControl",
            "Spillgebees.Blazor.Map.Models.Controls.ScaleMapControl",
            "Spillgebees.Blazor.Map.Models.Controls.ScaleUnit",
            "Spillgebees.Blazor.Map.Models.Controls.TerrainMapControl",
            "Spillgebees.Blazor.Map.Models.Coordinate",
            "Spillgebees.Blazor.Map.Models.Events.LayerFeatureEventArgs",
            "Spillgebees.Blazor.Map.Models.Events.MapClickEventArgs",
            "Spillgebees.Blazor.Map.Models.Events.MapViewEventArgs",
            "Spillgebees.Blazor.Map.Models.Events.MarkerClickEventArgs",
            "Spillgebees.Blazor.Map.Models.Events.MarkerDragEventArgs",
            "Spillgebees.Blazor.Map.Models.Expressions.Expr",
            "Spillgebees.Blazor.Map.Models.Expressions.FeatureState",
            "Spillgebees.Blazor.Map.Models.Expressions.FeatureStateKey`1",
            "Spillgebees.Blazor.Map.Models.Expressions.StyleValue`1",
            "Spillgebees.Blazor.Map.Models.FitBoundsOptions",
            "Spillgebees.Blazor.Map.Models.Layers.Circle",
            "Spillgebees.Blazor.Map.Models.Layers.Marker",
            "Spillgebees.Blazor.Map.Models.Layers.MarkerIcon",
            "Spillgebees.Blazor.Map.Models.Layers.Polyline",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegend",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendItem",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendItemTemplateContext",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendSection",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendTarget",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendVisibilityChangedEventArgs",
            "Spillgebees.Blazor.Map.Models.MapBounds",
            "Spillgebees.Blazor.Map.Models.MapImage",
            "Spillgebees.Blazor.Map.Models.MapOptions",
            "Spillgebees.Blazor.Map.Models.MapProjection",
            "Spillgebees.Blazor.Map.Models.MapStyle",
            "Spillgebees.Blazor.Map.Models.MapStyle+OpenFreeMap",
            "Spillgebees.Blazor.Map.Models.MapStyle+OpenStreetMap",
            "Spillgebees.Blazor.Map.Models.MapTheme",
            "Spillgebees.Blazor.Map.Models.Options.CirclePitchAlignment",
            "Spillgebees.Blazor.Map.Models.Options.EnumJsonName",
            "Spillgebees.Blazor.Map.Models.Options.IconTextFit",
            "Spillgebees.Blazor.Map.Models.Options.LayerOptionEnumExtensions",
            "Spillgebees.Blazor.Map.Models.Options.LineCap",
            "Spillgebees.Blazor.Map.Models.Options.LineJoin",
            "Spillgebees.Blazor.Map.Models.Options.MapAlignment",
            "Spillgebees.Blazor.Map.Models.Options.SymbolAnchor",
            "Spillgebees.Blazor.Map.Models.Options.SymbolPlacement",
            "Spillgebees.Blazor.Map.Models.Options.TextTransform",
            "Spillgebees.Blazor.Map.Models.PixelPoint",
            "Spillgebees.Blazor.Map.Models.Popups.PopupAnchor",
            "Spillgebees.Blazor.Map.Models.Popups.PopupContentMode",
            "Spillgebees.Blazor.Map.Models.Popups.PopupOptions",
            "Spillgebees.Blazor.Map.Models.Popups.PopupTrigger",
            "Spillgebees.Blazor.Map.Models.RasterTileSource",
            "Spillgebees.Blazor.Map.Models.ReferrerPolicy",
            "Spillgebees.Blazor.Map.Models.TileOverlay",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.ITrackedEntityLayerDefinition",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityBehaviorOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityCallbacks`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityClusterClickBehavior",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityClusterOptions",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityDecorationDisplayMode",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityDecorationOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityFeatureKind",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityFeatureProperties",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityFeatureStates",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityHoverIntent",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityIdOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityInteractionEventArgs`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityInteractionOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityLayerDefinition`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntitySymbolOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityVisualDefaults",
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityVisualOptions`1",
            "Spillgebees.Blazor.Map.Models.WmsTileSource",
            "Spillgebees.Blazor.Map._Imports",
        };

        // act
        var exportedTypeNames = typeof(SgbMap)
            .Assembly.GetExportedTypes()
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
            "Spillgebees.Blazor.Map.Models.TrackedEntities.TrackedEntityLayerDefinition`1"
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
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataLayer`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.ITrackedDataLayer",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataIdOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataVisualOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataVisualDefaults",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataSymbolOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataDecorationOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataClusterOptions",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataBehaviorOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataInteractionOptions`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataCallbacks`1",
            "Spillgebees.Blazor.Map.Models.TrackedData.TrackedDataEntityMaterializer",
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
            "Spillgebees.Blazor.Map.Models.Legends.MapLegend",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendSection",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendItem",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendTarget",
            "Spillgebees.Blazor.Map.Models.MapImage",
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
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendDefinition",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendSectionDefinition",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendItemDefinition",
            "Spillgebees.Blazor.Map.Models.Legends.MapLegendTargetDefinition",
            "Spillgebees.Blazor.Map.Models.MapImageDefinition",
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
        var mapImageType = assembly.GetType("Spillgebees.Blazor.Map.Models.MapImage");

        // act
        var publicPropertyNames = mapImageType
            ?.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(property => property.Name);

        // assert
        publicPropertyNames.Should().BeEquivalentTo(["Id", "Url", "Width", "Height", "PixelRatio", "IsSdf"]);
    }

    [Test]
    public void Should_not_expose_legacy_map_legend_component_type()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;

        // act
        var legacyMapLegendType = assembly.GetType("Spillgebees.Blazor.Map.Components.MapLegend");

        // assert
        legacyMapLegendType.Should().BeNull();
    }

    [Test]
    public void Should_not_expose_legacy_tracked_data_source_component()
    {
        // arrange
        var assembly = typeof(SgbMap).Assembly;

        // act
        var trackedDataSourceType = assembly.GetType("Spillgebees.Blazor.Map.Components.Layers.TrackedDataSource`1");

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
        var pixelPointType = assembly.GetType("Spillgebees.Blazor.Map.Models.PixelPoint");
        var pointType = assembly.GetType("Spillgebees.Blazor.Map.Models.Point");

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
        var trackedEntityLayerType = assembly.GetType("Spillgebees.Blazor.Map.Components.Layers.TrackedEntityLayer`1");

        trackedEntityLayerType.Should().NotBeNull();
        return trackedEntityLayerType!;
    }
}
