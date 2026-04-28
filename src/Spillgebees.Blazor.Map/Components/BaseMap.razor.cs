using System.Diagnostics.CodeAnalysis;
using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Interop;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.TrackedEntities;
using Spillgebees.Blazor.Map.Runtime.Scene;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Components;

/// <summary>
/// Base map component providing core map functionality.
/// </summary>
public abstract partial class BaseMap : ComponentBase, IAsyncDisposable
{
    protected BaseMap()
    {
        SceneRegistry = new MapSceneRegistry(this);
        ControlRegistry = new MapControlRegistryContext(this);
    }

    [Inject]
    protected IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private ILoggerFactory _loggerFactory { get; set; } = null!;

    protected Lazy<ILogger> Logger => new(() => _loggerFactory.CreateLogger(GetType()));
    internal MapSceneRegistry SceneRegistry { get; }
    internal MapControlRegistryContext ControlRegistry { get; }
    internal IJSRuntime Runtime => JsRuntime;
    internal ILogger RuntimeLogger => Logger.Value;

    /// <summary>
    /// Options for the map (center, zoom, style, pitch, bearing, etc.).
    /// </summary>
    [Parameter]
    public MapOptions MapOptions { get; set; } = MapOptions.Default;

    /// <summary>
    /// Declarative map controls (built-in, legend, and content controls). Empty by default. Prefer control subcomponents.
    /// </summary>
    [Parameter]
    public IReadOnlyList<MapControl> Controls { get; set; } = [];

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
    /// Declarative map images registered and replayed automatically across map lifecycle events.
    /// </summary>
    [Parameter]
    public List<MapImage> Images { get; set; } = [];

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
    /// Child content rendered inside the map's cascading value scope.
    /// Use this to place <see cref="Layers.GeoJsonSource"/> and layer components.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

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
    protected List<MapControl> InternalControls { get; set; } = [];
    protected MapTheme InternalTheme;
    protected List<Marker> InternalMarkers { get; set; } = [];
    protected List<Circle> InternalCircles { get; set; } = [];
    protected List<Polyline> InternalPolylines { get; set; } = [];
    protected List<TileOverlay> InternalOverlays { get; set; } = [];
    protected List<MapImage> InternalImages { get; set; } = [];

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

    internal ElementReference MapReference;
    protected DotNetObjectReference<BaseMap>? DotNetObjectReference;
    protected bool IsInitialized;
    protected bool IsDisposing;
    internal bool RuntimeIsInitialized => IsInitialized;
    internal bool RuntimeIsReady { get; private set; }

    internal event Func<Task>? StyleReloaded;

    private readonly TaskCompletionSource<bool> _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<RegisteredControl> _registeredControls = [];
    private readonly Dictionary<string, IReadOnlyList<Marker>> _registeredOverlayMarkers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<Circle>> _registeredOverlayCircles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<Polyline>> _registeredOverlayPolylines = new(
        StringComparer.Ordinal
    );

    /// <summary>
    /// Returns a task that completes when the map has been initialized and is ready
    /// for interop calls (sources, layers, etc.).
    /// </summary>
    internal Task<bool> WhenReadyAsync()
    {
        if (IsDisposing)
        {
            return Task.FromResult(false);
        }

        return _readyTcs.Task;
    }

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

    /// <summary>
    /// Gets the current map center.
    /// </summary>
    public ValueTask<Coordinate?> GetCenterAsync() => MapJs.GetCenterAsync(JsRuntime, Logger.Value, MapReference);

    /// <summary>
    /// Gets the current map zoom level.
    /// </summary>
    public ValueTask<double?> GetZoomAsync() => MapJs.GetZoomAsync(JsRuntime, Logger.Value, MapReference);

    /// <summary>
    /// Returns whether a layer currently exists in the map style.
    /// </summary>
    public ValueTask<bool> HasLayerAsync(string layerId) =>
        MapJs.HasLayerAsync(JsRuntime, Logger.Value, MapReference, layerId);

    /// <summary>
    /// Returns whether a layer exists within a composed style using the style's stable ID and original layer ID.
    /// </summary>
    public ValueTask<bool> HasStyleLayerAsync(string styleId, string layerId) =>
        MapJs.HasStyleLayerAsync(JsRuntime, Logger.Value, MapReference, styleId, layerId);

    /// <summary>
    /// Gets the current map bounds.
    /// </summary>
    public ValueTask<MapBounds?> GetBoundsAsync() => MapJs.GetBoundsAsync(JsRuntime, Logger.Value, MapReference);

    /// <summary>
    /// Queries rendered features at a screen point.
    /// </summary>
    public ValueTask<List<object>> QueryRenderedFeaturesAsync(Point point, IReadOnlyList<string>? layerIds = null) =>
        MapJs.QueryRenderedFeaturesAsync(JsRuntime, Logger.Value, MapReference, point, layerIds);

    /// <summary>
    /// Moves an existing layer relative to another layer.
    /// </summary>
    public ValueTask MoveLayerAsync(string layerId, string? beforeLayerId = null) =>
        MapJs.MoveLayerAsync(JsRuntime, Logger.Value, MapReference, layerId, beforeLayerId);

    /// <summary>
    /// Sets feature-state for a tracked entity across its primary and decoration sources.
    /// </summary>
    public ValueTask SetTrackedEntityFeatureStateAsync(
        string primarySourceId,
        string? decorationSourceId,
        string entityId,
        IReadOnlyDictionary<string, object> state
    ) =>
        MapJs.SetTrackedEntityFeatureStateAsync(
            JsRuntime,
            Logger.Value,
            MapReference,
            primarySourceId,
            decorationSourceId,
            entityId,
            state
        );

    /// <summary>
    /// Sets the visibility of an existing layer in the map style.
    /// Use this to show/hide built-in style layers (e.g., hide tram layers).
    /// </summary>
    /// <param name="layerId">The ID of the layer in the map style.</param>
    /// <param name="visible">Whether the layer should be visible.</param>
    public async ValueTask SetLayerVisibilityAsync(string layerId, bool visible)
    {
        await JsRuntime.InvokeVoidAsync(
            "Spillgebees.Map.mapFunctions.setLayerVisibility",
            MapReference,
            layerId,
            visible
        );
    }

    /// <summary>
    /// Sets the visibility of a composed style layer using the style's stable ID and the original layer ID.
    /// </summary>
    public ValueTask SetStyleLayerVisibilityAsync(string styleId, string layerId, bool visible) =>
        MapJs.SetStyleLayerVisibilityAsync(JsRuntime, Logger.Value, MapReference, styleId, layerId, visible);

    /// <summary>
    /// Shows a popup at the specified coordinate with HTML content.
    /// Only one programmatic popup is shown at a time — calling this again replaces the previous one.
    /// </summary>
    /// <param name="position">The geographic position for the popup.</param>
    /// <param name="html">The HTML content to display.</param>
    /// <param name="options">Optional popup configuration.</param>
    public async ValueTask ShowPopupAsync(Coordinate position, string html, Models.Popups.PopupOptions? options = null)
    {
        await JsRuntime.InvokeVoidAsync(
            "Spillgebees.Map.mapFunctions.showPopup",
            MapReference,
            position,
            html,
            options
        );
    }

    /// <summary>
    /// Closes any programmatic popup currently shown via <see cref="ShowPopupAsync"/>.
    /// </summary>
    public async ValueTask ClosePopupAsync()
    {
        await JsRuntime.InvokeVoidAsync("Spillgebees.Map.mapFunctions.closePopup", MapReference);
    }

    /// <summary>
    /// Sets the feature state for a specific feature in a source.
    /// Feature state can be read in paint/layout expressions via <c>["feature-state", "propertyName"]</c>
    /// for hover highlighting, selection, and other interactive styling without re-rendering the source data.
    /// </summary>
    /// <param name="sourceId">The source containing the feature.</param>
    /// <param name="featureId">The feature's ID (from the GeoJSON <c>id</c> field or <c>promoteId</c>).</param>
    /// <param name="state">A dictionary of state properties to set.</param>
    /// <param name="sourceLayerId">The source layer ID (required for vector tile sources).</param>
    public async ValueTask SetFeatureStateAsync(
        string sourceId,
        object featureId,
        IDictionary<string, object> state,
        string? sourceLayerId = null
    )
    {
        await JsRuntime.InvokeVoidAsync(
            "Spillgebees.Map.mapFunctions.setFeatureState",
            MapReference,
            sourceId,
            featureId,
            state,
            sourceLayerId
        );
    }

    /// <summary>
    /// Sets a single feature state value using a typed key.
    /// </summary>
    /// <param name="sourceId">The source containing the feature.</param>
    /// <param name="featureId">The feature's ID (from the GeoJSON <c>id</c> field or <c>promoteId</c>).</param>
    /// <param name="state">A single state entry created via <see cref="Models.Expressions.FeatureStateKey{T}.Set"/>.</param>
    /// <param name="sourceLayerId">The source layer ID (required for vector tile sources).</param>
    public async ValueTask SetFeatureStateAsync(
        string sourceId,
        object featureId,
        KeyValuePair<string, object> state,
        string? sourceLayerId = null
    )
    {
        await SetFeatureStateAsync(
            sourceId,
            featureId,
            new Dictionary<string, object> { [state.Key] = state.Value },
            sourceLayerId
        );
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        if (IsDisposing)
        {
            return;
        }
        IsDisposing = true;
        RuntimeIsReady = false;

        // Unblock any child components awaiting WhenReadyAsync()
        _readyTcs.TrySetResult(false);

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

        // apply fitBounds after resize so the container has its final dimensions
        if (MapOptions?.FitBoundsOptions is { } fitBoundsOptions)
        {
            await FitBoundsAsync(fitBoundsOptions);
        }

        await SyncImagesAsync(force: true);

        // Signal that the map is ready for child components (sources, layers)
        RuntimeIsReady = true;
        _readyTcs.TrySetResult(true);
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

    /// <summary>
    /// This method is called from JavaScript when the map style is reloaded. Don't call it manually.
    /// </summary>
    [JSInvokable]
    public async Task OnMapStyleReloadedAsync()
    {
        if (StyleReloaded is null)
        {
            return;
        }

        foreach (var handler in StyleReloaded.GetInvocationList().Cast<Func<Task>>())
        {
            await handler();
        }
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        MapOptionsCompositionValidator.Validate(MapOptions);
        ValidateControlIds(GetDesiredControls());

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

        var desiredImages = GetDesiredImages();
        await SyncImagesAsync(desiredImages);

        var desiredControls = GetDesiredControls();
        if (!InternalControls.SequenceEqual(desiredControls))
        {
            InternalControls = desiredControls;
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
    /// Initializes the map and creates the map instance.
    /// </summary>
    protected virtual async Task InitializeMapAsync()
    {
        MapOptionsCompositionValidator.Validate(MapOptions);

        DotNetObjectReference = Microsoft.JSInterop.DotNetObjectReference.Create(this);

        InternalMapOptions = MapOptions;

        InternalControls = GetDesiredControls();
        ValidateControlIds(InternalControls);
        InternalTheme = Theme;
        InternalMarkers = GetDesiredMarkers();
        InternalCircles = GetDesiredCircles();
        InternalPolylines = GetDesiredPolylines();
        InternalOverlays = [.. Overlays];
        InternalImages = [.. GetDesiredImages()];

        await MapJs.CreateMapAsync(
            JsRuntime,
            Logger.Value,
            DotNetObjectReference,
            nameof(OnMapInitializedAsync),
            MapReference,
            InternalMapOptions,
            InternalControls,
            InternalTheme,
            InternalMarkers,
            InternalCircles,
            InternalPolylines,
            InternalOverlays
        );
    }

    private async Task SyncFeaturesAsync()
    {
        var desiredMarkers = GetDesiredMarkers();
        var desiredCircles = GetDesiredCircles();
        var desiredPolylines = GetDesiredPolylines();
        var markerDiff = FeatureDiffer.Diff(InternalMarkers, desiredMarkers, static m => m.Id);
        var circleDiff = FeatureDiffer.Diff(InternalCircles, desiredCircles, static c => c.Id);
        var polylineDiff = FeatureDiffer.Diff(InternalPolylines, desiredPolylines, static p => p.Id);

        if (!markerDiff.HasChanges && !circleDiff.HasChanges && !polylineDiff.HasChanges)
        {
            return;
        }

        await MapJs.SyncFeaturesAsync(JsRuntime, Logger.Value, MapReference, markerDiff, circleDiff, polylineDiff);

        // snapshot new state
        InternalMarkers = desiredMarkers;
        InternalCircles = desiredCircles;
        InternalPolylines = desiredPolylines;
    }

    internal async ValueTask SetOverlayMarkersAsync(string ownerId, IReadOnlyList<Marker> markers)
    {
        _registeredOverlayMarkers[ownerId] = markers;
        if (IsInitialized)
        {
            await SyncFeaturesAsync();
        }
    }

    internal async ValueTask SetOverlayCirclesAsync(string ownerId, IReadOnlyList<Circle> circles)
    {
        _registeredOverlayCircles[ownerId] = circles;
        if (IsInitialized)
        {
            await SyncFeaturesAsync();
        }
    }

    internal async ValueTask SetOverlayPolylinesAsync(string ownerId, IReadOnlyList<Polyline> polylines)
    {
        _registeredOverlayPolylines[ownerId] = polylines;
        if (IsInitialized)
        {
            await SyncFeaturesAsync();
        }
    }

    internal async ValueTask RemoveOverlayFeaturesAsync(string ownerId)
    {
        var changed = _registeredOverlayMarkers.Remove(ownerId);
        changed |= _registeredOverlayCircles.Remove(ownerId);
        changed |= _registeredOverlayPolylines.Remove(ownerId);
        if (changed && IsInitialized)
        {
            await SyncFeaturesAsync();
        }
    }

    private List<Marker> GetDesiredMarkers() =>
        [.. Markers, .. _registeredOverlayMarkers.Values.SelectMany(markers => markers)];

    private List<Circle> GetDesiredCircles() =>
        [.. Circles, .. _registeredOverlayCircles.Values.SelectMany(circles => circles)];

    private List<Polyline> GetDesiredPolylines() =>
        [.. Polylines, .. _registeredOverlayPolylines.Values.SelectMany(polylines => polylines)];

    private ValueTask SetOverlaysAsync() =>
        MapJs.SetOverlaysAsync(JsRuntime, Logger.Value, MapReference, InternalOverlays);

    internal ValueTask SyncControlsAsync()
    {
        var desiredControls = GetDesiredControls();
        ValidateControlIds(desiredControls);

        if (InternalControls.SequenceEqual(desiredControls))
        {
            return ValueTask.CompletedTask;
        }

        InternalControls = desiredControls;
        return SetControlsAsync();
    }

    private ValueTask SetControlsAsync() =>
        MapJs.SetControlsAsync(JsRuntime, Logger.Value, MapReference, InternalControls);

    internal ValueTask SetControlContentAsync(
        string controlId,
        string kind,
        ElementReference placeholderReference,
        ElementReference contentReference
    ) =>
        MapJs.SetControlContentAsync(
            JsRuntime,
            Logger.Value,
            MapReference,
            controlId,
            kind,
            placeholderReference,
            contentReference
        );

    internal ValueTask RemoveControlContentAsync(string controlId) =>
        MapJs.RemoveControlContentAsync(JsRuntime, Logger.Value, MapReference, controlId);

    internal bool RegisterControl(string ownerId, MapControl control)
    {
        var existing = _registeredControls.FirstOrDefault(entry => entry.OwnerId == ownerId);
        if (existing is not null)
        {
            if (existing.Control == control)
            {
                return false;
            }

            existing.Control = control;
            return true;
        }

        _registeredControls.Add(new RegisteredControl(ownerId, control));
        return true;
    }

    internal bool UnregisterControl(string controlId)
    {
        var index = _registeredControls.FindIndex(entry =>
            string.Equals(entry.Control.ControlId, controlId, StringComparison.Ordinal)
        );
        if (index < 0)
        {
            return false;
        }

        _registeredControls.RemoveAt(index);
        return true;
    }

    internal bool UnregisterControlByOwner(string ownerId)
    {
        var index = _registeredControls.FindIndex(entry => entry.OwnerId == ownerId);
        if (index < 0)
        {
            return false;
        }

        _registeredControls.RemoveAt(index);
        return true;
    }

    protected List<MapControl> GetDesiredControls() =>
        [.. Controls, .. _registeredControls.Select(entry => entry.Control)];

    internal bool TryGetControl(string controlId, [NotNullWhen(true)] out MapControl? control)
    {
        control = InternalControls.FirstOrDefault(control => control.ControlId == controlId);
        return control is not null;
    }

    private static void ValidateControlIds(IEnumerable<MapControl> controls)
    {
        var duplicateControlId = controls
            .GroupBy(control => control.ControlId, StringComparer.Ordinal)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);

        if (duplicateControlId is null)
        {
            return;
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(duplicateControlId.Key)
                ? "Control IDs must be non-empty."
                : $"Control IDs must be unique. Duplicate ID: '{duplicateControlId.Key}'."
        );
    }

    private sealed class RegisteredControl(string ownerId, MapControl control)
    {
        public string OwnerId { get; } = ownerId;

        public MapControl Control { get; set; } = control;
    }

    private ValueTask SetMapOptionsAsync() =>
        MapJs.SetMapOptionsAsync(JsRuntime, Logger.Value, MapReference, InternalMapOptions);

    private ValueTask SetThemeAsync() => MapJs.SetThemeAsync(JsRuntime, Logger.Value, MapReference, InternalTheme);

    private async Task SyncImagesAsync(bool force = false)
    {
        var desiredImages = GetDesiredImages();

        if (!force && !ShouldSyncImages(desiredImages))
        {
            return;
        }

        InternalImages = [.. desiredImages];
        await MapJs.SetImagesAsync(JsRuntime, Logger.Value, MapReference, InternalImages);
    }

    private async Task SyncImagesAsync(IReadOnlyList<MapImage> desiredImages)
    {
        if (!ShouldSyncImages(desiredImages))
        {
            return;
        }

        InternalImages = [.. desiredImages];
        await MapJs.SetImagesAsync(JsRuntime, Logger.Value, MapReference, InternalImages);
    }

    private bool ShouldSyncImages(IReadOnlyList<MapImage> desiredImages)
    {
        if (InternalImages.Count != desiredImages.Count)
        {
            return true;
        }

        for (var i = 0; i < desiredImages.Count; i++)
        {
            if (InternalImages[i] != desiredImages[i])
            {
                return true;
            }
        }

        return false;
    }

    private IReadOnlyList<MapImage> GetDesiredImages()
    {
        return [.. Images];
    }
}
