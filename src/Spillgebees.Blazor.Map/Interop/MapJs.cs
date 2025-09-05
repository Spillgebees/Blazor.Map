using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;

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
        List<Polyline> polylines)
        => jsRuntime.SafeInvokeVoidAsync(
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
            polylines);

    public static ValueTask SetLayersAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        List<Marker> markers,
        List<CircleMarker> circleMarkers,
        List<Polyline> polylines)
        => jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.setLayers",
            mapReference,
            markers,
            circleMarkers,
            polylines);

    public static ValueTask SetTileLayersAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        List<TileLayer> tileLayers)
        => jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.setTileLayers",
            mapReference,
            tileLayers);

    internal static ValueTask InvalidateSizeAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference)
        => jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.invalidateSize",
            mapReference);

    internal static ValueTask DisposeMapAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference)
        => jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.disposeMap",
            mapReference);

    public static ValueTask FitToLayerAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference,
        string layerId)
        => jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.fitToLayer",
            mapReference,
            layerId);

    private static ValueTask SafeInvokeVoidAsync(
        this IJSRuntime jsRuntime,
        ILogger logger,
        string identifier,
        params object?[] args)
    {
        try
        {
            return jsRuntime.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Invocation of {identifier} was cancelled.", identifier);
        }

        return ValueTask.CompletedTask;
    }

    private static ValueTask<T> SafeInvokeAsync<T>(
        this IJSRuntime jsRuntime,
        ILogger logger,
        string identifier,
        params object?[] args)
    {
        try
        {
            return jsRuntime.InvokeAsync<T>(identifier, args);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Invocation of {identifier} was cancelled.", identifier);
        }

        return ValueTask.FromResult(default(T)!);
    }
}
