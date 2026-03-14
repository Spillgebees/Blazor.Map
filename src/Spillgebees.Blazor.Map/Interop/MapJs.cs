using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Interop;

/// <summary>
/// Static helper for invoking map-related JavaScript interop functions.
/// </summary>
internal static class MapJs
{
    /// <summary>
    /// The protocol version this C# library expects from the JS module.
    /// Bumped whenever the JS interop contract changes (function names, parameter shapes, return types).
    /// </summary>
    internal const int ProtocolVersion = 1;

    private const string JsNamespace = "Spillgebees.Map.mapFunctions";
    private const string JsProtocolVersionFunction = "Spillgebees.Map.getProtocolVersion";

    /// <summary>
    /// Retrieves the protocol version from the loaded JavaScript module.
    /// </summary>
    internal static ValueTask<int> GetProtocolVersionAsync(IJSRuntime jsRuntime, ILogger logger) =>
        jsRuntime.SafeInvokeAsync<int>(logger, JsProtocolVersionFunction);

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
        MapControlOptions mapControlOptions,
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
            mapOptions,
            mapControlOptions,
            theme,
            markers,
            circles,
            polylines,
            overlays
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
                    added = markerDiff.Added,
                    updated = markerDiff.Updated,
                    removedIds = markerDiff.Removed,
                },
                circles = new
                {
                    added = circleDiff.Added,
                    updated = circleDiff.Updated,
                    removedIds = circleDiff.Removed,
                },
                polylines = new
                {
                    added = polylineDiff.Added,
                    updated = polylineDiff.Updated,
                    removedIds = polylineDiff.Removed,
                },
            }
        );

    /// <summary>
    /// Sets the raster tile overlays on the map.
    /// </summary>
    internal static ValueTask SetOverlaysAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        List<TileOverlay> overlays
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.setOverlays", mapReference, overlays);

    /// <summary>
    /// Sets the map controls.
    /// </summary>
    internal static ValueTask SetControlsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        MapControlOptions mapControlOptions
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.setControls", mapReference, mapControlOptions);

    /// <summary>
    /// Updates map options (style, pitch, bearing, terrain, projection).
    /// </summary>
    internal static ValueTask SetMapOptionsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        MapOptions mapOptions
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.setMapOptions", mapReference, mapOptions);

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
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.fitBounds", mapReference, fitBoundsOptions);

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
