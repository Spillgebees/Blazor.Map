using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Engine-backed map component. Declarative children
/// queue ops through a single channel; high-frequency entity data flows through binary
/// motion frames. The component owns lifecycle and parameters; model → ops translation
/// lives in the engine coordinators (controls, features, popups, display). Hosts the
/// shared map-control component family (controls render directly inside the map — no
/// <c>MapControls</c> wrapper needed).
/// </summary>
public partial class SgbMap
    : ComponentBase,
        IAsyncDisposable,
        IMapControlHost,
        IMapOverlayHost,
        IMapInteropHost,
        IMapFeatureHost
{
    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = null!;

    /// <summary>A MapLibre style URL, or a raw style JSON document. Lowest precedence.</summary>
    [Parameter]
    public string? StyleSpec { get; set; }

    /// <summary>Typed base map style; takes precedence over <see cref="StyleSpec"/>.</summary>
    [Parameter]
    public MapStyle? Style { get; set; }

    /// <summary>
    /// Composed styles: index 0 is the base map, the rest merge in as overlay styles
    /// (URL styles only). Takes precedence over <see cref="Style"/>.
    /// </summary>
    [Parameter]
    public IReadOnlyList<MapStyle>? Styles { get; set; }

    /// <summary>Shared glyph endpoint override for composed styles.</summary>
    [Parameter]
    public string? ComposedGlyphsUrl { get; set; }

    /// <summary>UI theme for controls/popups; does not affect map tiles.</summary>
    [Parameter]
    public MapTheme Theme { get; set; } = MapTheme.Light;

    /// <summary>
    /// Display toggles (the library's <see cref="MapDisplayState"/> model). Item changes
    /// apply as JS-local visibility/filter updates.
    /// </summary>
    [Parameter]
    public MapDisplayState? Display { get; set; }

    /// <summary>Initial map center; also the home view for <see cref="ReCenterAsync"/>. Defaults to (0, 0).</summary>
    [Parameter]
    public Coordinate Center { get; set; } = new(0, 0);

    /// <summary>Initial zoom level; also the home zoom for <see cref="ReCenterAsync"/>.</summary>
    [Parameter]
    public double Zoom { get; set; }

    /// <summary>Camera pitch in degrees.</summary>
    [Parameter]
    public double Pitch { get; set; }

    /// <summary>Camera bearing in degrees.</summary>
    [Parameter]
    public double Bearing { get; set; }

    /// <summary>Map projection. Defaults to <see cref="MapProjection.Mercator"/>.</summary>
    [Parameter]
    public MapProjection Projection { get; set; } = MapProjection.Mercator;

    /// <summary>Minimum allowed zoom level.</summary>
    [Parameter]
    public double? MinZoom { get; set; }

    /// <summary>Maximum allowed zoom level.</summary>
    [Parameter]
    public double? MaxZoom { get; set; }

    /// <summary>Constrains panning to the given bounds.</summary>
    [Parameter]
    public MapBounds? MaxBounds { get; set; }

    /// <summary>
    /// Fits the viewport around the referenced markers/circles/polylines. Re-applies
    /// whenever a new options instance is assigned.
    /// </summary>
    [Parameter]
    public FitBoundsOptions? FitBounds { get; set; }

    /// <summary>Disables all user interaction handlers when false (create-time only).</summary>
    [Parameter]
    public bool Interactive { get; set; } = true;

    /// <summary>Requires ctrl/cmd + scroll to zoom (create-time only).</summary>
    [Parameter]
    public bool CooperativeGestures { get; set; }

    /// <summary>CSS font shorthands preloaded for label rendering (create-time only).</summary>
    [Parameter]
    public IReadOnlyList<string>? WebFonts { get; set; }

    /// <summary>Canvas pixel ratio strategy. Defaults to <see cref="MapPixelRatioMode.BrowserDefault"/>.</summary>
    [Parameter]
    public MapPixelRatioMode PixelRatioMode { get; set; } = MapPixelRatioMode.BrowserDefault;

    /// <summary>Explicit canvas pixel ratio; wins over <see cref="PixelRatioMode"/>.</summary>
    [Parameter]
    public double? PixelRatio { get; set; }

    /// <summary>Raster tile overlays stacked above the base style.</summary>
    [Parameter]
    public IReadOnlyList<TileOverlay>? Overlays { get; set; }

    /// <summary>CSS height of the map container. Defaults to "400px".</summary>
    [Parameter]
    public string Height { get; set; } = "400px";

    /// <summary>CSS width of the map container. Defaults to "100%".</summary>
    [Parameter]
    public string Width { get; set; } = "100%";

    /// <summary>Optional id attribute for the map container element.</summary>
    [Parameter]
    public string? ContainerId { get; set; }

    /// <summary>Additional CSS class for the map container element.</summary>
    [Parameter]
    public string? ContainerClass { get; set; }

    /// <summary>Images registered with the map for use by layers and styles (e.g. icon-image).</summary>
    [Parameter]
    public IReadOnlyList<MapImage>? Images { get; set; }

    /// <summary>DOM markers (full <see cref="Marker"/> model, including Draggable).</summary>
    [Parameter]
    public IReadOnlyList<Marker>? Markers { get; set; }

    /// <summary>Circle features (the <see cref="Circle"/> model) rendered on the map.</summary>
    [Parameter]
    public IReadOnlyList<Circle>? Circles { get; set; }

    /// <summary>Polyline features (the <see cref="Polyline"/> model) rendered on the map.</summary>
    [Parameter]
    public IReadOnlyList<Polyline>? Polylines { get; set; }

    /// <summary>Fires when a marker is clicked; args carry the marker id and position.</summary>
    [Parameter]
    public EventCallback<MarkerClickEventArgs> OnMarkerClick { get; set; }

    /// <summary>Fires when a draggable marker is dropped; args carry the marker id and new position.</summary>
    [Parameter]
    public EventCallback<MarkerDragEventArgs> OnMarkerDragEnd { get; set; }

    /// <summary>Fires when the map itself is clicked; args carry the clicked coordinate.</summary>
    [Parameter]
    public EventCallback<MapClickEventArgs> OnMapClick { get; set; }

    /// <summary>Fires when a camera movement ends; args carry the new center, zoom, bearing and pitch.</summary>
    [Parameter]
    public EventCallback<MapViewEventArgs> OnMoveEnd { get; set; }

    /// <summary>Fires when a zoom animation ends; args carry the new center, zoom, bearing and pitch.</summary>
    [Parameter]
    public EventCallback<MapViewEventArgs> OnZoomEnd { get; set; }

    /// <summary>Declarative map content: controls, features, sources and overlays.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Fires once the underlying map has loaded and is ready for operations.</summary>
    [Parameter]
    public EventCallback OnMapReady { get; set; }

    /// <summary>Raised after a base style change once the scene has been re-applied.</summary>
    [Parameter]
    public EventCallback OnStyleReloaded { get; set; }

    /// <summary>Fires when the engine reports an error; args carry the exception.</summary>
    [Parameter]
    public EventCallback<Exception> OnMapError { get; set; }

    internal MapEngineChannel Channel { get; private set; } = null!;

    internal MapEngineEventRouter Router { get; private set; } = null!;

    internal bool IsReady { get; private set; }

    private MapControlCoordinator _controls = null!;
    private MapFeatureCoordinator _features = null!;
    private MapPopupCoordinator _popups = null!;
    private MapDisplayCoordinator _display = null!;
    private MapTileOverlayCoordinator _tileOverlays = null!;

    private ElementReference _container;
    private bool _isCreated;
    private string? _appliedStylesJson;
    private MapTheme _appliedTheme;
    private EngineMapConfig? _appliedConfig;
    private FitBoundsOptions? _appliedFitBounds;
    private MapDisplayState? _subscribedDisplay;
    private readonly List<MapStyle> _overlayStyles = [];
    private readonly List<MapOverlay> _overlays = [];
    private readonly TaskCompletionSource<bool> _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal MapControlRegistryContext ControlRegistry { get; private set; } = null!;

    internal MapRootContext RootContext { get; private set; } = null!;

    /// <summary>Sets up the ops channel, event router and engine coordinators.</summary>
    protected override void OnInitialized()
    {
        Channel = new MapEngineChannel(_jsRuntime);
        Channel.Error += exception => _ = OnMapError.InvokeAsync(exception);
        Router = new MapEngineEventRouter();
        Router.MapEvent += HandleMapEventAsync;
        Router.MarkerClick += args => OnMarkerClick.InvokeAsync(args);
        Router.MarkerDragEnd += args => OnMarkerDragEnd.InvokeAsync(args);
        _controls = new MapControlCoordinator(Channel, Router, ReCenterAsync);
        _features = new MapFeatureCoordinator(Channel);
        _popups = new MapPopupCoordinator(Channel, Router);
        _display = new MapDisplayCoordinator(Channel);
        _tileOverlays = new MapTileOverlayCoordinator(Channel);
        _follow = new MapFollowCoordinator(Channel);
        ControlRegistry = new MapControlRegistryContext(this);
        RootContext = new MapRootContext(this);
    }

    /// <summary>Creates the underlying map on first render.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        Channel.Attach(_container);
        QueueImages();
        _appliedStylesJson = BuildStylesNode().ToJsonString();
        _appliedTheme = Theme;
        _appliedConfig = BuildConfig();
        await MapEngineJs.CreateMapAsync(_jsRuntime, _container, BuildOptionsJson(), Router.Reference);
        _isCreated = true;
        // overlay styles registered by children while CreateMapAsync was in flight
        // missed both the create options and the runtime path — catch up now.
        await ApplyStylesIfChangedAsync();
        await SyncFollowAsync();
    }

    /// <summary>Applies parameter changes (config, features, overlays, styles, theme) to the running map.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (!ReferenceEquals(Display, _subscribedDisplay))
        {
            _subscribedDisplay?.Changed -= HandleDisplayChanged;

            _subscribedDisplay = Display;
            _subscribedDisplay?.Changed += HandleDisplayChanged;

            _display.Sync(_subscribedDisplay);
        }

        _features.SyncParameters(Markers, Circles, Polylines);
        _tileOverlays.SyncParameter(Overlays);

        if (!_isCreated)
        {
            return;
        }

        var config = BuildConfig();
        if (config != _appliedConfig)
        {
            _appliedConfig = config;
            Channel.Queue(new MapConfigureOp(config));
        }

        // initial fit runs from the load handler, once the feature data has landed
        if (IsReady)
        {
            SyncFitBounds();
        }

        var stylesJson = BuildStylesNode().ToJsonString();
        if (stylesJson != _appliedStylesJson)
        {
            _appliedStylesJson = stylesJson;
            await MapEngineJs.SetStylesAsync(_jsRuntime, _container, stylesJson);
        }

        if (Theme != _appliedTheme)
        {
            _appliedTheme = Theme;
            await MapEngineJs.SetThemeAsync(_jsRuntime, _container, EngineStyleJson.ThemeName(Theme));
        }

        await SyncFollowAsync();
    }

    private void QueueImages()
    {
        foreach (var image in Images ?? [])
        {
            Channel.Queue(
                new ImageAddOp(
                    image.Id,
                    image.Url,
                    new JsonObject
                    {
                        ["width"] = image.Width,
                        ["height"] = image.Height,
                        ["pixelRatio"] = image.PixelRatio,
                        ["sdf"] = image.IsSdf,
                    }
                )
            );
        }
    }

    private string BuildOptionsJson()
    {
        var options = BuildStylesNode();
        options["center"] = new JsonArray(Center.Longitude, Center.Latitude);
        options["zoom"] = Zoom;
        options["theme"] = EngineStyleJson.ThemeName(Theme);
        options["interactive"] = Interactive;
        options["cooperativeGestures"] = CooperativeGestures;
        if (WebFonts is { Count: > 0 })
        {
            options["webFonts"] = new JsonArray([.. WebFonts.Select(font => (JsonNode)font)]);
        }

        options["config"] = JsonSerializer.SerializeToNode(
            _appliedConfig ?? BuildConfig(),
            MapEngineJsonContext.Default.EngineMapConfig
        );
        return options.ToJsonString();
    }

    private EngineMapConfig BuildConfig() =>
        new(
            Pitch,
            Bearing,
            Projection,
            MinZoom,
            MaxZoom,
            MaxBounds,
            PixelRatioMode == MapPixelRatioMode.BrowserDefault ? null : PixelRatioMode,
            PixelRatio
        );

    private void SyncFitBounds()
    {
        if (ReferenceEquals(FitBounds, _appliedFitBounds))
        {
            return;
        }

        _appliedFitBounds = FitBounds;
        if (FitBounds is { FeatureIds.Count: > 0 })
        {
            Channel.Queue(
                new CameraFitFeaturesOp(
                    FitBounds.FeatureIds,
                    FitBounds.Padding,
                    FitBounds.TopLeftPadding,
                    FitBounds.BottomRightPadding
                )
            );
        }
    }

    private JsonObject BuildStylesNode() =>
        EngineStyleJson.BuildStylesNode(
            Styles,
            Style,
            StyleSpec,
            _overlayStyles,
            ComposedGlyphsUrl,
            exception => _ = OnMapError.InvokeAsync(exception)
        );

    private void HandleDisplayChanged(object? sender, MapDisplayChangedEventArgs args) =>
        _display.Sync(_subscribedDisplay);

    /// <summary>Registers an overlay style into the composed styles list.</summary>
    internal void RegisterOverlayStyle(MapStyle style)
    {
        _overlayStyles.Add(style);
        _ = ApplyStylesIfChangedAsync();
    }

    internal void RegisterOverlay(MapOverlay overlay) => _overlays.Add(overlay);

    internal void UnregisterOverlay(MapOverlay overlay) => _overlays.Remove(overlay);

    internal void NotifyOverlayChanged(string overlayId, string? partId = null) =>
        OverlayChanged?.Invoke(this, new MapOverlayChangedEventArgs(overlayId, partId));

    /// <summary>Toggleable overlay state, raised on registration and visibility changes.</summary>
    internal event EventHandler<MapOverlayChangedEventArgs>? OverlayChanged;

    /// <summary>Toggles an overlay declared via <see cref="MapOverlay"/>.</summary>
    public Task SetOverlayVisibleAsync(string overlayId, bool visible)
    {
        _overlays.FirstOrDefault(overlay => overlay.Id == overlayId)?.SetVisible(visible);
        return Task.CompletedTask;
    }

    /// <summary>Toggles a part of an overlay declared via <see cref="MapOverlayPart"/>.</summary>
    public Task SetOverlayPartVisibleAsync(string overlayId, string partId, bool visible)
    {
        _overlays.FirstOrDefault(overlay => overlay.Id == overlayId)?.SetPartVisible(partId, visible);
        return Task.CompletedTask;
    }

    private async Task ApplyStylesIfChangedAsync()
    {
        if (!_isCreated)
        {
            return;
        }

        var stylesJson = BuildStylesNode().ToJsonString();
        if (stylesJson == _appliedStylesJson)
        {
            return;
        }

        _appliedStylesJson = stylesJson;
        try
        {
            await MapEngineJs.SetStylesAsync(_jsRuntime, _container, stylesJson);
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    private async Task HandleMapEventAsync(string kind, JsonElement payload)
    {
        switch (kind)
        {
            case "load":
                IsReady = true;
                _readyTcs.TrySetResult(true);
                await Channel.MarkReadyAsync();
                _controls.Sync();
                // queue the initial fit after MarkReadyAsync so the feature data the
                // fit resolves against has already landed JS-side
                SyncFitBounds();
                await OnMapReady.InvokeAsync();
                break;
            case "stylereloaded":
                await OnStyleReloaded.InvokeAsync();
                break;
            case "moveend":
                await OnMoveEnd.InvokeAsync(ToViewEventArgs(payload));
                break;
            case "zoomend":
                await OnZoomEnd.InvokeAsync(ToViewEventArgs(payload));
                break;
            case "click":
                await OnMapClick.InvokeAsync(
                    new MapClickEventArgs(
                        new Coordinate(payload.GetProperty("lat").GetDouble(), payload.GetProperty("lng").GetDouble())
                    )
                );
                break;
            case "error":
                var message = payload.TryGetProperty("message", out var messageProperty)
                    ? messageProperty.GetString()
                    : null;
                await OnMapError.InvokeAsync(new InvalidOperationException(message ?? "Unknown map engine error."));
                break;
            case "followcleared":
                await HandleFollowClearedAsync(payload);
                break;
        }
    }

    private static MapViewEventArgs ToViewEventArgs(JsonElement payload) =>
        new(
            new Coordinate(payload.GetProperty("lat").GetDouble(), payload.GetProperty("lng").GetDouble()),
            payload.GetProperty("zoom").GetDouble(),
            payload.GetProperty("bearing").GetDouble(),
            payload.GetProperty("pitch").GetDouble()
        );

    // --- public map API ---

    /// <summary>Returns to the home view: the FitBounds features when set, else Center/Zoom.</summary>
    public Task ReCenterAsync() =>
        FitBounds is { FeatureIds.Count: > 0 } ? FitBoundsAsync(FitBounds) : FlyToAsync(Center, Zoom);

    /// <summary>Animates the camera to the given view.</summary>
    public Task FlyToAsync(Coordinate center, double? zoom = null, double? bearing = null, double? pitch = null) =>
        Channel.QueueAndFlushAsync(new CameraFlyToOp(center, zoom, bearing, pitch));

    /// <summary>Fits the viewport around the referenced markers/circles/polylines.</summary>
    public Task FitBoundsAsync(FitBoundsOptions options) =>
        Channel.QueueAndFlushAsync(
            new CameraFitFeaturesOp(
                options.FeatureIds,
                options.Padding,
                options.TopLeftPadding,
                options.BottomRightPadding
            )
        );

    /// <summary>Recalculates the map's size after its container changed.</summary>
    public Task ResizeAsync() => Channel.QueueAndFlushAsync(new MapResizeOp());

    /// <summary>Shows a transient text popup (replaces any previous transient popup).</summary>
    public Task ShowPopupAsync(Coordinate position, string text, PopupOptions? options = null) =>
        Channel.QueueAndFlushAsync(
            new PopupShowOp(position, (options ?? PopupOptions.FromText(text)) with { Content = text })
        );

    /// <summary>Shows a transient raw-HTML popup. Use only with trusted or sanitized content.</summary>
    public Task ShowRawHtmlPopupAsync(Coordinate position, string html, PopupOptions? options = null) =>
        Channel.QueueAndFlushAsync(
            new PopupShowOp(
                position,
                (options ?? PopupOptions.FromRawHtml(html)) with { Content = html, ContentMode = PopupContentMode.RawHtml }
            )
        );

    /// <summary>Closes the transient popup, if any.</summary>
    public Task ClosePopupAsync() => Channel.QueueAndFlushAsync(new PopupCloseOp());

    /// <summary>Moves an existing layer relative to another layer.</summary>
    public Task MoveLayerAsync(string layerId, string? beforeLayerId = null) =>
        Channel.QueueAndFlushAsync(new LayerMoveOp(layerId, Before: beforeLayerId));

    /// <summary>Sets MapLibre feature state on any source (e.g. for data-driven styling).</summary>
    public Task SetFeatureStateAsync(
        string sourceId,
        object featureId,
        IReadOnlyDictionary<string, object> state,
        string? sourceLayer = null
    )
    {
        var stateNode = new JsonObject();
        foreach (var (key, value) in state)
        {
            stateNode[key] = JsonSerializer.SerializeToNode(value);
        }

        return Channel.QueueAndFlushAsync(
            new SourceFeatureStateOp(sourceId, JsonSerializer.SerializeToNode(featureId)!, stateNode, sourceLayer)
        );
    }

    /// <summary>The current map center, or null before the map exists.</summary>
    public async ValueTask<Coordinate?> GetCenterAsync()
    {
        var view = await GetViewSafeAsync();
        return view is null ? null : new Coordinate(view.Lat, view.Lng);
    }

    /// <summary>The current zoom level, or null before the map exists.</summary>
    public async ValueTask<double?> GetZoomAsync() => (await GetViewSafeAsync())?.Zoom;

    /// <summary>The current viewport bounds, or null before the map exists.</summary>
    public async ValueTask<MapBounds?> GetBoundsAsync()
    {
        try
        {
            return await MapEngineJs.GetBoundsAsync(_jsRuntime, _container);
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    /// <summary>Whether a layer with the given id currently exists on the map.</summary>
    public ValueTask<bool> HasLayerAsync(string layerId) =>
        MapEngineJs.HasLayerAsync(_jsRuntime, _container, layerId);

    /// <summary>Whether a layer exists for the given composed style, by its original id.</summary>
    public ValueTask<bool> HasStyleLayerAsync(string styleId, string layerId) =>
        MapEngineJs.HasStyleLayerAsync(_jsRuntime, _container, styleId, layerId);

    /// <summary>Queries rendered features at a screen point, optionally limited to layers.</summary>
    public ValueTask<List<object>> QueryRenderedFeaturesAsync(
        PixelPoint point,
        IReadOnlyList<string>? layerIds = null
    ) => MapEngineJs.QueryRenderedFeaturesAsync(_jsRuntime, _container, point, layerIds);

    private async ValueTask<MapEngineJs.EngineView?> GetViewSafeAsync()
    {
        try
        {
            return await MapEngineJs.GetViewAsync(_jsRuntime, _container);
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    /// <summary>Disposes the underlying map and releases JS interop resources.</summary>
    public async ValueTask DisposeAsync()
    {
        _readyTcs.TrySetResult(false);
        _subscribedDisplay?.Changed -= HandleDisplayChanged;

        try
        {
            await MapEngineJs.DisposeMapAsync(_jsRuntime, _container);
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }

        Router.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- host interface forwarding (the shared component family binds to these) ---

    bool IMapControlHost.RegisterControl(string ownerId, MapControlDefinition control) =>
        _controls.Register(ownerId, control);

    bool IMapControlHost.UnregisterControl(string controlId) => _controls.Unregister(controlId);

    bool IMapControlHost.UnregisterControlByOwner(string ownerId) => _controls.UnregisterByOwner(ownerId);

    bool IMapControlHost.RuntimeIsReady => IsReady;

    Task<bool> IMapControlHost.WhenReadyAsync() => IsReady ? Task.FromResult(true) : _readyTcs.Task;

    ValueTask IMapControlHost.SyncControlsAsync()
    {
        _controls.Sync();
        return ValueTask.CompletedTask;
    }

    ValueTask IMapControlHost.SetControlContentAsync(
        string controlId,
        string kind,
        ElementReference placeholderReference,
        ElementReference contentReference,
        Func<bool, Task>? onPanelOpenChangedAsync
    )
    {
        // the engine resolves the content elements by DOM convention
        // (data-sgb-control-placeholder) and the shell kind from the registered
        // definition; the seam keeps references/kind so components stay host-agnostic.
        _ = kind;
        _ = placeholderReference;
        _ = contentReference;
        _controls.SetContent(controlId, onPanelOpenChangedAsync);
        return ValueTask.CompletedTask;
    }

    ValueTask IMapControlHost.RemoveControlContentAsync(string controlId)
    {
        _controls.RemoveContent(controlId);
        return ValueTask.CompletedTask;
    }

    ValueTask IMapFeatureHost.SetOverlayMarkersAsync(string ownerId, IReadOnlyList<Marker> markers)
    {
        _features.SetMarkers(ownerId, markers);
        return ValueTask.CompletedTask;
    }

    ValueTask IMapFeatureHost.SetOverlayCirclesAsync(string ownerId, IReadOnlyList<Circle> circles)
    {
        _features.SetCircles(ownerId, circles);
        return ValueTask.CompletedTask;
    }

    ValueTask IMapFeatureHost.SetOverlayPolylinesAsync(string ownerId, IReadOnlyList<Polyline> polylines)
    {
        _features.SetPolylines(ownerId, polylines);
        return ValueTask.CompletedTask;
    }

    ValueTask IMapFeatureHost.RemoveOverlayFeaturesAsync(string ownerId)
    {
        _features.RemoveOwner(ownerId);
        return ValueTask.CompletedTask;
    }

    bool IMapInteropHost.RuntimeIsReady => IsReady;

    Task<bool> IMapInteropHost.WhenReadyAsync() => IsReady ? Task.FromResult(true) : _readyTcs.Task;

    ValueTask IMapInteropHost.SetPopupContentAsync(
        string popupId,
        Coordinate position,
        PopupOptions options,
        ElementReference placeholderReference,
        ElementReference contentReference,
        Func<Task> onClosedAsync
    )
    {
        // content elements resolve by DOM convention (data-sgb-popup-placeholder)
        _ = placeholderReference;
        _ = contentReference;
        _popups.SetContent(popupId, position, options, onClosedAsync);
        return ValueTask.CompletedTask;
    }

    ValueTask IMapInteropHost.RemovePopupContentAsync(string popupId)
    {
        _popups.RemoveContent(popupId);
        return ValueTask.CompletedTask;
    }

    event EventHandler<MapOverlayChangedEventArgs>? IMapOverlayHost.OverlayChanged
    {
        add => OverlayChanged += value;
        remove => OverlayChanged -= value;
    }

    IReadOnlyList<MapOverlayItem> IMapOverlayHost.GetOverlayItems() =>
        [.. _overlays.Select(overlay => overlay.BuildItem())];

    void IMapOverlayHost.SetOverlayVisible(string overlayId, bool visible) =>
        _ = SetOverlayVisibleAsync(overlayId, visible);

    void IMapOverlayHost.SetOverlayPartVisible(string overlayId, string partId, bool visible) =>
        _ = SetOverlayPartVisibleAsync(overlayId, partId, visible);
}
