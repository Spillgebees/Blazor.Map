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

    [Parameter]
    public MapOptions MapOptions { get; set; } = MapOptions.Default;

    [Parameter]
    public MapControlOptions MapControlOptions { get; set; } = MapControlOptions.Default;

    [Parameter, EditorRequired]
    public required List<TileLayer> TileLayers { get; set; }

    [Parameter]
    public List<Marker> Markers { get; set; } = [];

    [Parameter]
    public List<CircleMarker> CircleMarkers { get; set; } = [];

    [Parameter]
    public List<Polyline> Polylines { get; set; } = [];

    [Parameter]
    public string? Width { get; set; }

    [Parameter]
    public string? Height { get; set; } = "500px";

    [Parameter]
    public string MapContainerHtmlId { get; set; } = $"map-container-{Guid.NewGuid()}";

    [Parameter]
    public string MapContainerClass { get; set; } = string.Empty;

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
            Logger.Value.LogError(exception, "Failed to dispose editor");
        }
        finally
        {
            DotNetObjectReference?.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    [JSInvokable]
    public async Task OnMapInitializedAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        _initializationCompletionSource.TrySetResult();
        IsInitialized = true;

        // ensure map initialized correctly
        await Task.Delay(50);
        await InvalidateMapSizeAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsInitialized is false)
        {
            return;
        }

        if (Markers != InternalMarkers
            || CircleMarkers != InternalCircleMarkers
            || Polylines != InternalPolylines)
        {
            InternalMarkers = Markers;
            InternalCircleMarkers = CircleMarkers;
            InternalPolylines = Polylines;
            await SetMarkersAsync();
        }

        if (TileLayers != InternalTileLayers)
        {
            InternalTileLayers = TileLayers;
            await SetTileLayersAsync();
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

        InternalTileLayers = TileLayers;
        InternalMarkers = Markers;
        InternalCircleMarkers = CircleMarkers;
        InternalPolylines = Polylines;

        await MapJs.CreateMapAsync(
            JsRuntime,
            Logger.Value,
            DotNetObjectReference,
            nameof(OnMapInitializedAsync),
            MapReference,
            MapOptions,
            MapControlOptions,
            InternalTileLayers,
            InternalMarkers,
            InternalCircleMarkers,
            InternalPolylines);
    }

    private ValueTask SetMarkersAsync()
        => MapJs.SetLayersAsync(JsRuntime, Logger.Value, MapReference, InternalMarkers, InternalCircleMarkers, InternalPolylines);

    private ValueTask SetTileLayersAsync()
        => MapJs.SetTileLayersAsync(JsRuntime, Logger.Value, MapReference, InternalTileLayers);

    private ValueTask InvalidateMapSizeAsync()
        => MapJs.InvalidateSizeAsync(JsRuntime, Logger.Value, MapReference);

    public ValueTask FitToLayerAsync(string layerId)
        => MapJs.FitToLayerAsync(JsRuntime, Logger.Value, MapReference, layerId);
}
