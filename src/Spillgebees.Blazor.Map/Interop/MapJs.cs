using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Components;

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
        Coordinate center,
        int zoom)
        => jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.createMap",
            dotNetObjectReference,
            onAfterCreateMapCallback,
            mapReference,
            center, zoom);

    internal static ValueTask DisposeMapAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference)
        => jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.disposeMap",
            mapReference);

    internal static ValueTask InvalidateSizeAsync(
        IJSRuntime jsRuntime,
        ILogger logger,
        ElementReference mapReference)
        => jsRuntime.SafeInvokeVoidAsync(
            logger,
            $"{JsNamespace}.invalidateSize",
            mapReference);

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
