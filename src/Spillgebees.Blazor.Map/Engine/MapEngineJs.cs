using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// The complete JS interop surface of the engine control plane: one ops call, one
/// motion call, plus map lifecycle.
/// </summary>
internal static class MapEngineJs
{
    private const string Namespace = "Spillgebees.Engine";

    public static ValueTask CreateMapAsync(
        IJSRuntime jsRuntime,
        ElementReference container,
        string optionsJson,
        DotNetObjectReference<MapEngineEventRouter> router
    ) => jsRuntime.InvokeVoidAsync($"{Namespace}.createMap", container, optionsJson, router);

    public static ValueTask ApplyOpsAsync(IJSRuntime jsRuntime, ElementReference container, string opsJson) =>
        jsRuntime.InvokeVoidAsync($"{Namespace}.applyOps", container, opsJson);

    public static ValueTask PushMotionAsync(
        IJSRuntime jsRuntime,
        ElementReference container,
        string layerId,
        byte[] frame
    ) => jsRuntime.InvokeVoidAsync($"{Namespace}.pushMotion", container, layerId, frame);

    public static ValueTask SetSourceDataAsync(
        IJSRuntime jsRuntime,
        ElementReference container,
        string sourceId,
        string dataJson,
        int? animateMs,
        string? animateEasing = null
    ) =>
        jsRuntime.InvokeVoidAsync(
            $"{Namespace}.setSourceData",
            container,
            sourceId,
            dataJson,
            animateMs,
            animateEasing
        );

    public static ValueTask SetStylesAsync(IJSRuntime jsRuntime, ElementReference container, string stylesJson) =>
        jsRuntime.InvokeVoidAsync($"{Namespace}.setStyles", container, stylesJson);

    public static ValueTask SetThemeAsync(IJSRuntime jsRuntime, ElementReference container, string theme) =>
        jsRuntime.InvokeVoidAsync($"{Namespace}.setTheme", container, theme);

    public static ValueTask DisposeMapAsync(IJSRuntime jsRuntime, ElementReference container) =>
        jsRuntime.InvokeVoidAsync($"{Namespace}.dispose", container);

    // --- read side: queries return values, so they cannot ride the one-way ops channel ---

    internal sealed record EngineView(double Lng, double Lat, double Zoom, double Bearing, double Pitch);

    public static ValueTask<EngineView?> GetViewAsync(IJSRuntime jsRuntime, ElementReference container) =>
        jsRuntime.InvokeAsync<EngineView?>($"{Namespace}.getView", container);

    public static ValueTask<MapBounds?> GetBoundsAsync(IJSRuntime jsRuntime, ElementReference container) =>
        jsRuntime.InvokeAsync<MapBounds?>($"{Namespace}.getBounds", container);

    public static ValueTask<bool> HasLayerAsync(IJSRuntime jsRuntime, ElementReference container, string layerId) =>
        jsRuntime.InvokeAsync<bool>($"{Namespace}.hasLayer", container, layerId);

    public static ValueTask<bool> HasStyleLayerAsync(
        IJSRuntime jsRuntime,
        ElementReference container,
        string styleId,
        string layerId
    ) => jsRuntime.InvokeAsync<bool>($"{Namespace}.hasStyleLayer", container, styleId, layerId);

    public static ValueTask<List<object>> QueryRenderedFeaturesAsync(
        IJSRuntime jsRuntime,
        ElementReference container,
        PixelPoint point,
        IReadOnlyList<string>? layerIds
    ) => jsRuntime.InvokeAsync<List<object>>($"{Namespace}.queryRenderedFeatures", container, point, layerIds);
}
