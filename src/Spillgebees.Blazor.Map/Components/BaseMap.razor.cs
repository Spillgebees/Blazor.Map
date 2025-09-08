using BlazorComponentUtilities;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Interop;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Components;

public abstract partial class BaseMap : ComponentBase, IAsyncDisposable
{
    [Inject]
    protected IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private ILoggerFactory _loggerFactory { get; set; } = null!;
    protected Lazy<ILogger> Logger => new(() => _loggerFactory.CreateLogger(GetType()));

    /// <summary>
    /// Options for the map.
    /// </summary>
    [Parameter]
    public MapOptions MapOptions { get; set; } = MapOptions.Default;

    /// <summary>
    /// Options for the map controls.
    /// </summary>
    [Parameter]
    public MapControlOptions MapControlOptions { get; set; } = MapControlOptions.Default;

    /// <summary>
    /// The tile layers to display on the map.
    /// </summary>
    [Parameter, EditorRequired]
    public required List<TileLayer> TileLayers { get; set; }

    /// <summary>
    /// The markers to display on the map.
    /// </summary>
    [Parameter]
    public List<Marker> Markers { get; set; } = [];

    /// <summary>
    /// The circle markers to display on the map.
    /// </summary>
    [Parameter]
    public List<CircleMarker> CircleMarkers { get; set; } = [];

    /// <summary>
    /// The polylines to display on the map.
    /// </summary>
    [Parameter]
    public List<Polyline> Polylines { get; set; } = [];

    /// <summary>
    /// The width of the map. If not set, the map will take the full width of its container.
    /// </summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>
    /// The height of the map. Default is "500px", a fixed height is required for the map to be displayed.
    /// </summary>
    [Parameter]
    public string? Height { get; set; } = "500px";

    /// <summary>
    /// The HTML id attribute for the map container. If not set, a unique id will be generated.
    /// </summary>
    [Parameter]
    public string MapContainerHtmlId { get; set; } = $"map-container-{Guid.NewGuid()}";

    /// <summary>
    /// Additional CSS classes for the map container.
    /// </summary>
    [Parameter]
    public string MapContainerClass { get; set; } = string.Empty;

    protected MapOptions InternalMapOptions = null!;
    protected MapControlOptions InternalMapControlOptions = null!;
    protected List<Marker> InternalMarkers { get; set; } = [];
    protected List<CircleMarker> InternalCircleMarkers { get; set; } = [];
    protected List<Polyline> InternalPolylines { get; set; } = [];
    protected List<TileLayer> InternalTileLayers { get; set; } = [];

    protected string InternalMapContainerClass => new CssBuilder()
        .AddClass("sgb-map-container")
        .AddClass(MapContainerClass)
        .Build();

    protected string InternalMapContainerStyle => new StyleBuilder()
        .AddStyle("width", Width, Width is not null)
        .AddStyle("height", Height, Height is not null)
        .Build();

    protected ElementReference MapReference;
    protected DotNetObjectReference<BaseMap>? DotNetObjectReference;
    protected bool IsInitialized;
    protected bool IsDisposing;

    private readonly TaskCompletionSource _initializationCompletionSource = new();

    /// <summary>
    /// Changes the map view to fit the provider layers.
    /// </summary>
    /// <param name="layerIds">The layer ids to fit the view to.</param>
    public ValueTask FitToLayersAsync(List<string> layerIds)
        => MapJs.FitToLayerAsync(JsRuntime, Logger.Value, MapReference, layerIds);

    public virtual async ValueTask DisposeAsync()
    {
        if (IsDisposing)
        {
            return;
        }
        IsDisposing = true;

        try
        {
            // ensure initialization completed to avoid DotNetObjectReference disposed exceptions
            await _initializationCompletionSource.Task;
            await MapJs.DisposeMapAsync(JsRuntime, Logger.Value, MapReference);
        }
        catch (Exception exception) when (exception is JSDisconnectedException or OperationCanceledException)
        {
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

        _initializationCompletionSource.TrySetResult();
        IsInitialized = true;

        // the delay is required to ensure the page has rendered
        await Task.Delay(50);
        // since content may have shifted after rendering, we must tell leaflet to check the map size
        await InvalidateMapSizeAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsInitialized is false)
        {
            return;
        }

        if (Markers.SequenceEqual(InternalMarkers) is false
            || CircleMarkers.SequenceEqual(InternalCircleMarkers) is false
            || Polylines.SequenceEqual(InternalPolylines) is false)
        {
            InternalMarkers = [..Markers];
            InternalCircleMarkers = [..CircleMarkers];
            InternalPolylines = [..Polylines];
            await SetMarkersAsync();
        }

        if (TileLayers != InternalTileLayers)
        {
            InternalTileLayers = TileLayers;
            await SetTileLayersAsync();
        }

        if (InternalMapControlOptions != MapControlOptions)
        {
            InternalMapControlOptions = MapControlOptions;
            await SetMapControlsAsync();
        }

        if (InternalMapOptions != MapOptions)
        {
            InternalMapOptions = MapOptions;
            await SetMapOptionsAsync();
        }

        await Task.CompletedTask;
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
        => firstRender
            ? InitializeMapAsync()
            : Task.CompletedTask;

    protected virtual async Task InitializeMapAsync()
    {
        DotNetObjectReference = Microsoft.JSInterop.DotNetObjectReference.Create(this);

        InternalMapOptions = MapOptions;
        InternalMapControlOptions = MapControlOptions;
        InternalTileLayers = TileLayers;
        InternalMarkers = [..Markers];
        InternalCircleMarkers = [..CircleMarkers];
        InternalPolylines = [..Polylines];

        await MapJs.CreateMapAsync(
            JsRuntime,
            Logger.Value,
            DotNetObjectReference,
            nameof(OnMapInitializedAsync),
            MapReference,
            MapOptions,
            InternalMapControlOptions,
            InternalTileLayers,
            InternalMarkers,
            InternalCircleMarkers,
            InternalPolylines);
    }

    private ValueTask SetMarkersAsync()
        => MapJs.SetLayersAsync(JsRuntime, Logger.Value, MapReference, InternalMarkers, InternalCircleMarkers, InternalPolylines);

    private ValueTask SetTileLayersAsync()
        => MapJs.SetTileLayersAsync(JsRuntime, Logger.Value, MapReference, InternalTileLayers);

    private ValueTask SetMapControlsAsync()
        => MapJs.SetMapControlsAsync(JsRuntime, Logger.Value, MapReference, InternalMapControlOptions);

    private ValueTask SetMapOptionsAsync()
        => MapJs.SetMapOptionsAsync(JsRuntime, Logger.Value, MapReference, InternalMapOptions);

    private ValueTask InvalidateMapSizeAsync()
        => MapJs.InvalidateSizeAsync(JsRuntime, Logger.Value, MapReference);
}
