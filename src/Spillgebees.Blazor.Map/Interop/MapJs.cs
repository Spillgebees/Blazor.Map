using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Runtime.Scene;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Interop;

/// <summary>
/// Static helper for invoking map-related JavaScript interop functions.
/// </summary>
internal static class MapJs
{
    private const string JsNamespace = "Spillgebees.Map.mapFunctions";

    /// <summary>
    /// Creates a new map instance with the given options, controls, and initial features.
    /// </summary>
    internal static ValueTask CreateMapAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        DotNetObjectReference<BaseMap>? dotNetObjectReference,
        string onAfterCreateMapCallback,
        ElementReference mapReference,
        MapOptions mapOptions,
        IReadOnlyList<MapControl> controls,
        MapTheme theme,
        List<Marker> markers,
        List<Circle> circles,
        List<Polyline> polylines,
        List<TileOverlay> overlays
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.createMap",
            dotNetObjectReference,
            onAfterCreateMapCallback,
            mapReference,
            ToJsModel(mapOptions),
            controls.Select(ToJsModel).ToArray(),
            theme,
            markers,
            circles,
            polylines.Select(ToJsModel).ToArray(),
            overlays.Select(ToJsModel).ToArray()
        );

    /// <summary>
    /// Synchronizes features (markers, circles, polylines) by sending a consolidated diff to JavaScript.
    /// </summary>
    internal static ValueTask SyncFeaturesAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        FeatureDiffResult<Marker> markerDiff,
        FeatureDiffResult<Circle> circleDiff,
        FeatureDiffResult<Polyline> polylineDiff
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.syncFeatures",
            mapReference,
            new
            {
                markers = new
                {
                    added = markerDiff.Added.ToArray(),
                    updated = markerDiff.Updated.ToArray(),
                    removedIds = markerDiff.Removed.ToArray(),
                },
                circles = new
                {
                    added = circleDiff.Added.ToArray(),
                    updated = circleDiff.Updated.ToArray(),
                    removedIds = circleDiff.Removed.ToArray(),
                },
                polylines = new
                {
                    added = polylineDiff.Added.Select(ToJsModel).ToArray(),
                    updated = polylineDiff.Updated.Select(ToJsModel).ToArray(),
                    removedIds = polylineDiff.Removed.ToArray(),
                },
            }
        );

    internal static ValueTask ApplySceneMutationsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        MapSceneMutationBatch mutationBatch
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.applySceneMutations", mapReference, mutationBatch);

    /// <summary>
    /// Sets the raster tile overlays on the map.
    /// </summary>
    internal static ValueTask SetOverlaysAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        List<TileOverlay> overlays
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.setOverlays",
            mapReference,
            overlays.Select(ToJsModel).ToArray()
        );

    /// <summary>
    /// Sets declarative map images on the map instance.
    /// </summary>
    internal static ValueTask SetImagesAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        List<MapImageDefinition> images
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.setImages",
            mapReference,
            images.Select(ToJsModel).ToArray()
        );

    /// <summary>
    /// Sets the map controls.
    /// </summary>
    internal static ValueTask SetControlsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        IReadOnlyList<MapControl> controls
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.setControls",
            mapReference,
            controls.Select(ToJsModel).ToArray()
        );

    internal static ValueTask SetControlContentAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        string controlId,
        string kind,
        ElementReference placeholderReference,
        ElementReference contentReference
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.setControlContent",
            mapReference,
            controlId,
            kind,
            placeholderReference,
            contentReference
        );

    internal static ValueTask RemoveControlContentAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        string controlId
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.removeControlContent", mapReference, controlId);

    /// <summary>
    /// Updates map options (style, pitch, bearing, terrain, projection).
    /// </summary>
    internal static ValueTask SetMapOptionsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        MapOptions mapOptions
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.setMapOptions", mapReference, ToJsModel(mapOptions));

    /// <summary>
    /// Applies the UI theme (light/dark) to the map.
    /// </summary>
    internal static ValueTask SetThemeAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        MapTheme theme
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.setTheme", mapReference, theme);

    /// <summary>
    /// Fits the map view to the bounds of the specified features.
    /// </summary>
    internal static ValueTask FitBoundsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        FitBoundsOptions fitBoundsOptions
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.fitBounds", mapReference, ToJsModel(fitBoundsOptions));

    /// <summary>
    /// Performs an animated camera flight to the specified position.
    /// </summary>
    internal static ValueTask FlyToAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        Coordinate center,
        int? zoom,
        double? bearing,
        double? pitch
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.flyTo", mapReference, center, zoom, bearing, pitch);

    /// <summary>
    /// Triggers a resize recalculation on the map.
    /// </summary>
    internal static ValueTask ResizeAsync(IJSRuntime jsRuntime, ILogger logger, ElementReference mapReference) =>
        jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.resize", mapReference);

    internal static ValueTask<Coordinate?> GetCenterAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference
    ) => jsRuntime.SafeInvokeAsync<Coordinate?>(logger, $"{JsNamespace}.getCenter", mapReference);

    internal static ValueTask<double?> GetZoomAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference
    ) => jsRuntime.SafeInvokeAsync<double?>(logger, $"{JsNamespace}.getZoom", mapReference);

    internal static ValueTask<bool> HasLayerAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        string layerId
    ) => jsRuntime.SafeInvokeAsync<bool>(logger, $"{JsNamespace}.hasLayer", mapReference, layerId);

    internal static ValueTask<bool> HasStyleLayerAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        string styleId,
        string layerId
    ) => jsRuntime.SafeInvokeAsync<bool>(logger, $"{JsNamespace}.hasStyleLayer", mapReference, styleId, layerId);

    internal static ValueTask<MapBounds?> GetBoundsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference
    ) => jsRuntime.SafeInvokeAsync<MapBounds?>(logger, $"{JsNamespace}.getBounds", mapReference);

    internal static ValueTask<List<object>> QueryRenderedFeaturesAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        Point point,
        IReadOnlyList<string>? layerIds = null
    ) =>
        jsRuntime.SafeInvokeAsync<List<object>>(
            logger,
            $"{JsNamespace}.queryRenderedFeatures",
            mapReference,
            point,
            layerIds
        );

    internal static ValueTask MoveLayerAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        string layerId,
        string? beforeId = null
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.moveMapLayer", mapReference, layerId, beforeId);

    internal static ValueTask SetStyleLayerVisibilityAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        string styleId,
        string layerId,
        bool visible
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.setStyleLayerVisibility",
            mapReference,
            styleId,
            layerId,
            visible
        );

    internal static ValueTask SetTrackedEntityFeatureStateAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        string primarySourceId,
        string? decorationSourceId,
        string entityId,
        IReadOnlyDictionary<string, object> state
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.setTrackedEntityFeatureState",
            mapReference,
            primarySourceId,
            decorationSourceId,
            entityId,
            state
        );

    /// <summary>
    /// Disposes the map instance and cleans up all associated resources.
    /// </summary>
    internal static ValueTask DisposeMapAsync(IJSRuntime jsRuntime, ILogger logger, ElementReference mapReference) =>
        jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.disposeMap", mapReference);

    private static ValueTask SafeInvokeVoidAsync(
        this IJSRuntime jsRuntime,
        ILogger logger,
        string identifier,
        params object?[] args
    )
    {
        try
        {
            return jsRuntime.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException exception)
        {
            logger.LogTrace(exception, "JS interop skipped for {Identifier}, circuit disconnected.", identifier);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogTrace(exception, "JS interop cancelled for {Identifier}.", identifier);
        }

        return ValueTask.CompletedTask;
    }

    private static object ToJsModel(MapOptions mapOptions) =>
        new
        {
            mapOptions.Center,
            mapOptions.Zoom,
            Style = ToJsModel(mapOptions.Style),
            Styles = mapOptions.Styles?.Select(ToJsModel).ToArray(),
            mapOptions.ComposedGlyphsUrl,
            mapOptions.Pitch,
            mapOptions.Bearing,
            mapOptions.Projection,
            mapOptions.Terrain,
            mapOptions.TerrainExaggeration,
            FitBoundsOptions = ToJsModel(mapOptions.FitBoundsOptions),
            mapOptions.MinZoom,
            mapOptions.MaxZoom,
            mapOptions.MaxBounds,
            mapOptions.Interactive,
            mapOptions.CooperativeGestures,
            WebFonts = mapOptions.WebFonts?.ToArray(),
        };

    private static object ToJsModel(MapControl control) =>
        control switch
        {
            NavigationMapControl navigation => new
            {
                Kind = "navigation",
                navigation.ControlId,
                navigation.Enabled,
                navigation.Position,
                navigation.Order,
                navigation.ShowCompass,
                navigation.ShowZoom,
            },
            ScaleMapControl scale => new
            {
                Kind = "scale",
                scale.ControlId,
                scale.Enabled,
                scale.Position,
                scale.Order,
                scale.Unit,
            },
            FullscreenMapControl fullscreen => new
            {
                Kind = "fullscreen",
                fullscreen.ControlId,
                fullscreen.Enabled,
                fullscreen.Position,
                fullscreen.Order,
            },
            GeolocateMapControl geolocate => new
            {
                Kind = "geolocate",
                geolocate.ControlId,
                geolocate.Enabled,
                geolocate.Position,
                geolocate.Order,
                geolocate.TrackUser,
            },
            TerrainMapControl terrain => new
            {
                Kind = "terrain",
                terrain.ControlId,
                terrain.Enabled,
                terrain.Position,
                terrain.Order,
            },
            CenterMapControl center => new
            {
                Kind = "center",
                center.ControlId,
                center.Enabled,
                center.Position,
                center.Order,
            },
            LegendMapControl legend => new
            {
                Kind = "legend",
                legend.ControlId,
                legend.Enabled,
                legend.Position,
                legend.Order,
                Title = legend.Chrome.Title,
                Collapsible = legend.Chrome.Collapsible,
                InitiallyOpen = legend.Chrome.InitiallyOpen,
                ClassName = legend.Chrome.ClassName,
            },
            ContentMapControl content => new
            {
                Kind = "content",
                content.ControlId,
                content.Enabled,
                content.Position,
                content.Order,
                content.ClassName,
            },
            _ => throw new InvalidOperationException($"Unsupported map control type '{control.GetType().Name}'."),
        };

    private static object? ToJsModel(MapStyle? mapStyle) =>
        mapStyle is null
            ? null
            : new
            {
                mapStyle.Id,
                mapStyle.Url,
                mapStyle.ReferrerPolicy,
                RasterSource = ToJsModel(mapStyle.RasterSource),
                WmsSource = ToJsModel(mapStyle.WmsSource),
            };

    private static object? ToJsModel(RasterTileSource? rasterTileSource) =>
        rasterTileSource is null
            ? null
            : new
            {
                rasterTileSource.UrlTemplate,
                rasterTileSource.Attribution,
                rasterTileSource.TileSize,
                rasterTileSource.ReferrerPolicy,
            };

    private static object? ToJsModel(WmsTileSource? wmsTileSource) =>
        wmsTileSource is null
            ? null
            : new
            {
                wmsTileSource.BaseUrl,
                wmsTileSource.Layers,
                wmsTileSource.Attribution,
                wmsTileSource.Format,
                wmsTileSource.Transparent,
                wmsTileSource.Version,
                wmsTileSource.TileSize,
                wmsTileSource.ReferrerPolicy,
            };

    private static object ToJsModel(Polyline polyline) =>
        new
        {
            polyline.Id,
            Coordinates = polyline.Coordinates.ToArray(),
            polyline.Color,
            polyline.Width,
            polyline.Opacity,
            polyline.Popup,
        };

    private static object ToJsModel(TileOverlay tileOverlay) =>
        new
        {
            tileOverlay.Id,
            tileOverlay.UrlTemplate,
            tileOverlay.Attribution,
            tileOverlay.TileSize,
            tileOverlay.Opacity,
            tileOverlay.ReferrerPolicy,
        };

    private static object ToJsModel(MapImageDefinition mapImageDefinition) =>
        new
        {
            mapImageDefinition.Name,
            mapImageDefinition.Url,
            mapImageDefinition.Width,
            mapImageDefinition.Height,
            mapImageDefinition.PixelRatio,
            mapImageDefinition.Sdf,
        };

    private static object? ToJsModel(FitBoundsOptions? fitBoundsOptions) =>
        fitBoundsOptions is null
            ? null
            : new
            {
                FeatureIds = fitBoundsOptions.FeatureIds.ToArray(),
                fitBoundsOptions.Padding,
                fitBoundsOptions.TopLeftPadding,
                fitBoundsOptions.BottomRightPadding,
            };

    internal static ValueTask<T> SafeInvokeAsync<T>(
        this IJSRuntime jsRuntime,
        ILogger logger,
        string identifier,
        params object?[] args
    )
    {
        try
        {
            return jsRuntime.InvokeAsync<T>(identifier, args);
        }
        catch (JSDisconnectedException exception)
        {
            logger.LogTrace(exception, "JS interop skipped for {Identifier}, circuit disconnected.", identifier);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogTrace(exception, "JS interop cancelled for {Identifier}.", identifier);
        }

        return ValueTask.FromResult(default(T)!);
    }
}
