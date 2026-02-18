using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Interop;

internal static class MapJs
{
    private const string JsNamespace = "Spillgebees.Map.mapFunctions";

    internal static ValueTask CreateMapAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        DotNetObjectReference<BaseMap>? dotNetObjectReference,
        string onAfterCreateMapCallback,
        ElementReference mapReference,
        MapOptions mapOptions,
        MapControlOptions mapControlOptions,
        List<TileLayer> tileLayers,
        List<Marker> markers,
        List<CircleMarker> circleMarkers,
        List<Polyline> polylines
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.createMap",
            dotNetObjectReference,
            onAfterCreateMapCallback,
            mapReference,
            mapOptions,
            mapControlOptions,
            tileLayers,
            markers,
            circleMarkers,
            polylines
        );

    internal static ValueTask SetLayersAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        List<Marker> markers,
        List<CircleMarker> circleMarkers,
        List<Polyline> polylines
    ) =>
        jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.setLayers",
            mapReference,
            markers,
            circleMarkers,
            polylines
        );

    internal static ValueTask SetTileLayersAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        List<TileLayer> tileLayers
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.setTileLayers", mapReference, tileLayers);

    internal static ValueTask SetMapControlsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        MapControlOptions mapControlOptions
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.setMapControls", mapReference, mapControlOptions);

    internal static ValueTask SetMapOptionsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        MapOptions mapOptions
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.setMapOptions", mapReference, mapOptions);

    internal static ValueTask InvalidateSizeAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.invalidateSize", mapReference);

    internal static ValueTask FitBoundsAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        FitBoundsOptions fitBoundsOptions
    ) => jsRuntime.SafeInvokeVoidAsync(logger, $"{JsNamespace}.fitBounds", mapReference, fitBoundsOptions);

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

    private static ValueTask<T> SafeInvokeAsync<T>(
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
            logger.LogTrace(exception, "JS interop skipped for {Identifier} — circuit disconnected.", identifier);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogTrace(exception, "JS interop cancelled for {Identifier}.", identifier);
        }

        return ValueTask.FromResult(default(T)!);
    }
}
