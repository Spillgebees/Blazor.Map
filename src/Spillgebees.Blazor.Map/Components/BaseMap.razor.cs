using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Interop;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Base map component providing core map functionality.
/// </summary>
public abstract partial class BaseMap : ComponentBase, IAsyncDisposable
{
    [Inject]
    protected IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private ILoggerFactory _loggerFactory { get; set; } = null!;

    protected Lazy<ILogger> Logger => new(() => _loggerFactory.CreateLogger(GetType()));

    /// <summary>
    /// Options for the map (center, zoom, style, pitch, bearing, etc.).
    /// </summary>
    [Parameter]
    public MapOptions MapOptions { get; set; } = MapOptions.Default;

    /// <summary>
    /// Options for the map controls (navigation, scale, fullscreen, etc.).
    /// </summary>
    [Parameter]
    public MapControlOptions ControlOptions { get; set; } = MapControlOptions.Default;

    /// <summary>
    /// The visual theme for UI controls, popups, and attribution.
    /// This does NOT affect the map tiles — use <see cref="MapOptions.Style"/> for that.
    /// </summary>
    [Parameter]
    public MapTheme Theme { get; set; } = MapTheme.Light;

    /// <summary>
    /// The markers to display on the map.
    /// </summary>
    [Parameter]
    public List<Marker> Markers { get; set; } = [];

    /// <summary>
    /// The circles to display on the map.
    /// </summary>
    [Parameter]
    public List<Circle> Circles { get; set; } = [];

    /// <summary>
    /// The polylines to display on the map.
    /// </summary>
    [Parameter]
    public List<Polyline> Polylines { get; set; } = [];

    /// <summary>
    /// Additional raster tile overlays to render on top of the base map style.
    /// </summary>
    [Parameter]
    public List<TileOverlay> Overlays { get; set; } = [];

    /// <summary>
    /// The width of the map. If not set, the map will take the full width of its container.
    /// </summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>
    /// The height of the map. Default is "500px". A fixed height is required for the map to be displayed.
    /// </summary>
    [Parameter]
    public string? Height { get; set; } = "500px";

    /// <summary>
    /// The HTML id attribute for the map container. If not set, a unique id will be generated.
    /// </summary>
    [Parameter]
    public string ContainerId { get; set; } = $"map-container-{Guid.NewGuid()}";

    /// <summary>
    /// Additional CSS classes for the map container.
    /// </summary>
    [Parameter]
    public string ContainerClass { get; set; } = string.Empty;

    /// <summary>
    /// Callback invoked when the user clicks on the map.
    /// </summary>
    [Parameter]
    public EventCallback<MapClickEventArgs> OnMapClick { get; set; }

    /// <summary>
    /// Callback invoked when the map finishes a move (pan) transition.
    /// </summary>
    [Parameter]
    public EventCallback<MapViewEventArgs> OnMoveEnd { get; set; }

    /// <summary>
    /// Callback invoked when the map finishes a zoom transition.
    /// </summary>
    [Parameter]
    public EventCallback<MapViewEventArgs> OnZoomEnd { get; set; }

    /// <summary>
    /// Callback invoked when a marker is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<MarkerClickEventArgs> OnMarkerClick { get; set; }

    /// <summary>
    /// Callback invoked when a draggable marker is released after dragging.
    /// </summary>
    [Parameter]
    public EventCallback<MarkerDragEventArgs> OnMarkerDragEnd { get; set; }

    protected MapOptions InternalMapOptions = null!;
    protected MapControlOptions InternalControlOptions = null!;
    protected MapTheme InternalTheme;
    protected List<Marker> InternalMarkers { get; set; } = [];
    protected List<Circle> InternalCircles { get; set; } = [];
    protected List<Polyline> InternalPolylines { get; set; } = [];
    protected List<TileOverlay> InternalOverlays { get; set; } = [];

    protected string InternalContainerClass =>
        new CssBuilder()
            .AddClass("sgb-map-container")
            .AddClass("sgb-map-dark", Theme == MapTheme.Dark)
            .AddClass(ContainerClass)
            .Build();

    protected string InternalContainerStyle =>
        new StyleBuilder()
            .AddStyle("width", Width, Width is not null)
            .AddStyle("height", Height, Height is not null)
            .Build();

    protected ElementReference MapReference;
    protected DotNetObjectReference<BaseMap>? DotNetObjectReference;
    protected bool IsInitialized;
    protected bool IsDisposing;

    /// <summary>
    /// Performs an animated camera flight to the specified position.
    /// </summary>
    /// <param name="center">The target center coordinate.</param>
    /// <param name="zoom">Optional target zoom level.</param>
    /// <param name="bearing">Optional target bearing (rotation) in degrees.</param>
    /// <param name="pitch">Optional target pitch (tilt) in degrees.</param>
    public ValueTask FlyToAsync(Coordinate center, int? zoom = null, double? bearing = null, double? pitch = null) =>
        MapJs.FlyToAsync(JsRuntime, Logger.Value, MapReference, center, zoom, bearing, pitch);

    /// <summary>
    /// Changes the map view to fit the specified features.
    /// </summary>
    /// <param name="options">Options containing the features to fit to and other display options.</param>
    public ValueTask FitBoundsAsync(FitBoundsOptions options) =>
        MapJs.FitBoundsAsync(JsRuntime, Logger.Value, MapReference, options);

    /// <summary>
    /// Triggers a size recalculation on the map.
    /// Useful when dynamically changing the map container dimensions.
    /// </summary>
    public ValueTask ResizeAsync() => MapJs.ResizeAsync(JsRuntime, Logger.Value, MapReference);

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        if (IsDisposing)
        {
            return;
        }
        IsDisposing = true;

        try
        {
            await MapJs.DisposeMapAsync(JsRuntime, Logger.Value, MapReference);
        }
        catch (Exception exception) when (exception is JSDisconnectedException or OperationCanceledException)
        {
            // synchronous throws are already logged at trace level in SafeInvokeVoidAsync;
            // this catch handles the rare async propagation case
        }
        catch (Exception exception)
        {
            Logger.Value.LogError(exception, "Failed to dispose map");
        }
        finally
        {
            DotNetObjectReference?.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// This method is called from JavaScript when the map has been initialized. Don't call it manually.
    /// </summary>
    [JSInvokable]
    public async Task OnMapInitializedAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        IsInitialized = true;

        // the delay is required to ensure the page has rendered
        await Task.Delay(50);
        // since content may have shifted after rendering, we must tell the map to recalculate its size
        await ResizeAsync();
    }

    /// <summary>
    /// This method is called from JavaScript when the map is clicked. Don't call it manually.
    /// </summary>
    [JSInvokable]
    public async Task OnMapClickCallbackAsync(MapClickEventArgs args)
    {
        if (OnMapClick.HasDelegate)
        {
            await OnMapClick.InvokeAsync(args);
        }
    }

    /// <summary>
    /// This method is called from JavaScript when the map finishes moving. Don't call it manually.
    /// </summary>
    [JSInvokable]
    public async Task OnMoveEndCallbackAsync(MapViewEventArgs args)
    {
        if (OnMoveEnd.HasDelegate)
        {
            await OnMoveEnd.InvokeAsync(args);
        }
    }

    /// <summary>
    /// This method is called from JavaScript when the map finishes zooming. Don't call it manually.
    /// </summary>
    [JSInvokable]
    public async Task OnZoomEndCallbackAsync(MapViewEventArgs args)
    {
        if (OnZoomEnd.HasDelegate)
        {
            await OnZoomEnd.InvokeAsync(args);
        }
    }

    /// <summary>
    /// This method is called from JavaScript when a marker is clicked. Don't call it manually.
    /// </summary>
    [JSInvokable]
    public async Task OnMarkerClickCallbackAsync(MarkerClickEventArgs args)
    {
        if (OnMarkerClick.HasDelegate)
        {
            await OnMarkerClick.InvokeAsync(args);
        }
    }

    /// <summary>
    /// This method is called from JavaScript when a marker is dragged. Don't call it manually.
    /// </summary>
    [JSInvokable]
    public async Task OnMarkerDragEndCallbackAsync(MarkerDragEventArgs args)
    {
        if (OnMarkerDragEnd.HasDelegate)
        {
            await OnMarkerDragEnd.InvokeAsync(args);
        }
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (IsInitialized is false)
        {
            return;
        }

        await SyncFeaturesAsync();

        if (Overlays != InternalOverlays)
        {
            InternalOverlays = Overlays;
            await SetOverlaysAsync();
        }

        if (InternalControlOptions != ControlOptions)
        {
            InternalControlOptions = ControlOptions;
            await SetControlsAsync();
        }

        if (InternalMapOptions != MapOptions)
        {
            InternalMapOptions = MapOptions;
            await SetMapOptionsAsync();
        }

        if (InternalTheme != Theme)
        {
            InternalTheme = Theme;
            await SetThemeAsync();
        }
    }

    /// <inheritdoc/>
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        firstRender ? InitializeMapAsync() : Task.CompletedTask;

    /// <summary>
    /// Initializes the map by validating the protocol version and creating the map instance.
    /// </summary>
    protected virtual async Task InitializeMapAsync()
    {
        DotNetObjectReference = Microsoft.JSInterop.DotNetObjectReference.Create(this);

        // Protocol version handshake — safety net for cached JS modules
        var jsProtocolVersion = await MapJs.GetProtocolVersionAsync(JsRuntime, Logger.Value);
        if (jsProtocolVersion != MapJs.ProtocolVersion)
        {
            throw new InvalidOperationException(
                $"Spillgebees.Blazor.Map: JavaScript/C# version mismatch. "
                    + $"The loaded JavaScript module is protocol version {jsProtocolVersion} "
                    + $"but the .NET library expects protocol version {MapJs.ProtocolVersion}. "
                    + "Clear your browser cache and reload the page."
            );
        }

        InternalMapOptions = MapOptions;
        InternalControlOptions = ControlOptions;
        InternalTheme = Theme;
        InternalMarkers = [.. Markers];
        InternalCircles = [.. Circles];
        InternalPolylines = [.. Polylines];
        InternalOverlays = [.. Overlays];

        await MapJs.CreateMapAsync(
            JsRuntime,
            Logger.Value,
            DotNetObjectReference,
            nameof(OnMapInitializedAsync),
            MapReference,
            InternalMapOptions,
            InternalControlOptions,
            InternalTheme,
            InternalMarkers,
            InternalCircles,
            InternalPolylines,
            InternalOverlays
        );
    }

    private async Task SyncFeaturesAsync()
    {
        var markerDiff = FeatureDiffer.Diff(InternalMarkers, Markers, static m => m.Id);
        var circleDiff = FeatureDiffer.Diff(InternalCircles, Circles, static c => c.Id);
        var polylineDiff = FeatureDiffer.Diff(InternalPolylines, Polylines, static p => p.Id);

        if (!markerDiff.HasChanges && !circleDiff.HasChanges && !polylineDiff.HasChanges)
        {
            return;
        }

        await MapJs.SyncFeaturesAsync(JsRuntime, Logger.Value, MapReference, markerDiff, circleDiff, polylineDiff);

        // snapshot new state
        InternalMarkers = [.. Markers];
        InternalCircles = [.. Circles];
        InternalPolylines = [.. Polylines];
    }

    private ValueTask SetOverlaysAsync() =>
        MapJs.SetOverlaysAsync(JsRuntime, Logger.Value, MapReference, InternalOverlays);

    private ValueTask SetControlsAsync() =>
        MapJs.SetControlsAsync(JsRuntime, Logger.Value, MapReference, InternalControlOptions);

    private ValueTask SetMapOptionsAsync() =>
        MapJs.SetMapOptionsAsync(JsRuntime, Logger.Value, MapReference, InternalMapOptions);

    private ValueTask SetThemeAsync() => MapJs.SetThemeAsync(JsRuntime, Logger.Value, MapReference, InternalTheme);
}
