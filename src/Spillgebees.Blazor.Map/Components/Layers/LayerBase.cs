using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// Base class for MapLibre layer components. Layers can either be nested inside a
/// <see cref="IMapSource"/> (which provides the source automatically), or used
/// standalone with an explicit <see cref="SourceId"/> to reference an existing source
/// from the map style (e.g., the vector tile source in OpenFreeMap styles).
/// </summary>
public abstract class LayerBase : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = null!;

    [CascadingParameter]
    public IMapSource? Source { get; set; }

    [CascadingParameter]
    public BaseMap? Map { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    [Parameter]
    public string? SourceId { get; set; }

    [Parameter]
    public string? SourceLayerId { get; set; }

    [Parameter]
    public object? Filter { get; set; }

    [Parameter]
    public int? MinZoom { get; set; }

    [Parameter]
    public int? MaxZoom { get; set; }

    [Parameter]
    public string? BeforeLayerId { get; set; }

    [Parameter]
    public string? LayerGroup { get; set; }

    [Parameter]
    public string? BeforeLayerGroup { get; set; }

    [Parameter]
    public string? AfterLayerGroup { get; set; }

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public EventCallback<LayerFeatureEventArgs> OnClick { get; set; }

    [Parameter]
    public EventCallback<LayerFeatureEventArgs> OnMouseEnter { get; set; }

    [Parameter]
    public EventCallback OnMouseLeave { get; set; }

    private bool _isInitialized;
    private DotNetObjectReference<LayerBase>? _dotNetRef;

    // Previous state for diffing
    private Dictionary<string, object?>? _previousPaint;
    private Dictionary<string, object?>? _previousLayout;
    private object? _previousFilter;
    private int? _previousMinZoom;
    private int? _previousMaxZoom;
    private bool _previousVisible;
    private EventCallback<LayerFeatureEventArgs> _previousOnClick;
    private EventCallback<LayerFeatureEventArgs> _previousOnMouseEnter;
    private EventCallback _previousOnMouseLeave;
    private bool _eventsRequireWire;
    private string? _previousBeforeLayerId;
    private string? _previousLayerGroup;
    private string? _previousBeforeLayerGroup;
    private string? _previousAfterLayerGroup;

    private string? _resolvedSourceId => SourceId ?? Source?.Id;
    private BaseMap? _resolvedMap => Source?.Map ?? Map;
    private bool _hasEvents => OnClick.HasDelegate || OnMouseEnter.HasDelegate || OnMouseLeave.HasDelegate;
    internal MapLayerOrderOptions _orderOptions => new(LayerGroup, BeforeLayerGroup, AfterLayerGroup);

    internal abstract string _layerType { get; }
    internal abstract Dictionary<string, object?> GetPaintProperties();
    internal abstract Dictionary<string, object?> GetLayoutProperties();

    internal Dictionary<string, object?> BuildLayerSpec()
    {
        var spec = new Dictionary<string, object?>
        {
            ["id"] = Id,
            ["type"] = _layerType,
            ["source"] = _resolvedSourceId,
        };

        if (SourceLayerId is not null)
        {
            spec["source-layer"] = SourceLayerId;
        }

        if (Filter is not null)
        {
            spec["filter"] = Filter;
        }

        if (MinZoom.HasValue)
        {
            spec["minzoom"] = MinZoom.Value;
        }

        if (MaxZoom.HasValue)
        {
            spec["maxzoom"] = MaxZoom.Value;
        }

        var paint = GetPaintProperties();
        var nonNullPaint = paint.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value);
        if (nonNullPaint.Count > 0)
        {
            spec["paint"] = nonNullPaint;
        }

        var layout = GetLayoutProperties();
        layout["visibility"] = Visible ? "visible" : "none";
        var nonNullLayout = layout.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value);
        if (nonNullLayout.Count > 0)
        {
            spec["layout"] = nonNullLayout;
        }

        return spec;
    }

    internal virtual string GetLogicalOrderingIdentity() => $"layer:{Id}";

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        if (Source is not null)
        {
            await Source.RegisterLayerAsync(this);
        }
        else if (SourceId is not null && Map is not null)
        {
            var mapReady = await Map.WhenReadyAsync();
            if (!mapReady)
            {
                return;
            }

            await Map.SceneRegistry.RegisterLayerAsync(
                new MapLayerDescriptor(Id, BuildLayerSpec(), BeforeLayerId, GetLayerOrderRegistration())
            );
        }

        _isInitialized = true;
        SnapshotState();
        _eventsRequireWire = _hasEvents;

        await EnsureEventsWiredAsync();
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (!_isInitialized)
        {
            return;
        }

        var map = _resolvedMap;
        if (map is null)
        {
            return;
        }

        await DiffAndApplyPaintAsync(map);
        await DiffAndApplyLayoutAsync(map);
        await DiffAndApplyFilterAsync(map);
        await DiffAndApplyZoomRangeAsync(map);
        await DiffAndApplyOrderingAsync();
        await DiffAndApplyEventBindingsAsync(map);
    }

    private void SnapshotState()
    {
        _previousPaint = GetPaintProperties();
        _previousLayout = GetLayoutProperties();
        _previousLayout["visibility"] = Visible ? "visible" : "none";
        _previousFilter = Filter;
        _previousMinZoom = MinZoom;
        _previousMaxZoom = MaxZoom;
        _previousVisible = Visible;
        _previousOnClick = OnClick;
        _previousOnMouseEnter = OnMouseEnter;
        _previousOnMouseLeave = OnMouseLeave;
        _previousBeforeLayerId = BeforeLayerId;
        _previousLayerGroup = LayerGroup;
        _previousBeforeLayerGroup = BeforeLayerGroup;
        _previousAfterLayerGroup = AfterLayerGroup;
    }

    internal LayerOrderRegistration GetLayerOrderRegistration()
    {
        var map = _resolvedMap;
        if (map is null)
        {
            throw new InvalidOperationException($"Layer '{Id}' is not associated with a map yet.");
        }

        var inheritedOrder = Source?.OrderOptions ?? MapLayerOrderOptions.Empty;
        return map.SceneRegistry.ReserveLayerOrderRegistration(
            GetLogicalOrderingIdentity(),
            _orderOptions,
            inheritedOrder
        );
    }

    private async Task DiffAndApplyPaintAsync(BaseMap map)
    {
        var currentPaint = GetPaintProperties();
        var batch = map.SceneRegistry.CreateBatchBuilder();

        if (_previousPaint is null)
        {
            return;
        }

        foreach (var (key, currentValue) in currentPaint)
        {
            _previousPaint.TryGetValue(key, out var previousValue);

            if (!ValuesEqual(currentValue, previousValue))
            {
                batch.SetPaintProperty(Id, key, currentValue);
            }
        }

        await map.SceneRegistry.ApplyBatchAsync(batch);

        _previousPaint = currentPaint;
    }

    private async Task DiffAndApplyLayoutAsync(BaseMap map)
    {
        var currentLayout = GetLayoutProperties();
        var batch = map.SceneRegistry.CreateBatchBuilder();

        // Handle Visible separately — it maps to the "visibility" layout property
        if (_previousVisible != Visible)
        {
            batch.SetLayoutProperty(Id, "visibility", Visible ? "visible" : "none");
            _previousVisible = Visible;
        }

        if (_previousLayout is null)
        {
            return;
        }

        foreach (var (key, currentValue) in currentLayout)
        {
            if (key == "visibility")
            {
                continue; // handled above
            }

            _previousLayout.TryGetValue(key, out var previousValue);

            if (!ValuesEqual(currentValue, previousValue))
            {
                batch.SetLayoutProperty(Id, key, currentValue);
            }
        }

        await map.SceneRegistry.ApplyBatchAsync(batch);

        currentLayout["visibility"] = Visible ? "visible" : "none";
        _previousLayout = currentLayout;
    }

    private async Task DiffAndApplyFilterAsync(BaseMap map)
    {
        if (!ValuesEqual(Filter, _previousFilter))
        {
            var batch = map.SceneRegistry.CreateBatchBuilder();
            batch.SetFilter(Id, Filter);
            await map.SceneRegistry.ApplyBatchAsync(batch);
            _previousFilter = Filter;
        }
    }

    private async Task DiffAndApplyZoomRangeAsync(BaseMap map)
    {
        if (_previousMinZoom != MinZoom || _previousMaxZoom != MaxZoom)
        {
            var batch = map.SceneRegistry.CreateBatchBuilder();
            batch.SetLayerZoomRange(Id, MinZoom ?? 0, MaxZoom ?? 24);
            await map.SceneRegistry.ApplyBatchAsync(batch);
            _previousMinZoom = MinZoom;
            _previousMaxZoom = MaxZoom;
        }
    }

    private async Task DiffAndApplyOrderingAsync()
    {
        if (
            _previousBeforeLayerId == BeforeLayerId
            && _previousLayerGroup == LayerGroup
            && _previousBeforeLayerGroup == BeforeLayerGroup
            && _previousAfterLayerGroup == AfterLayerGroup
        )
        {
            return;
        }

        var map = _resolvedMap;
        if (map is null)
        {
            return;
        }

        await map.SceneRegistry.RegisterLayerAsync(
            new MapLayerDescriptor(Id, BuildLayerSpec(), BeforeLayerId, GetLayerOrderRegistration())
        );

        _previousBeforeLayerId = BeforeLayerId;
        _previousLayerGroup = LayerGroup;
        _previousBeforeLayerGroup = BeforeLayerGroup;
        _previousAfterLayerGroup = AfterLayerGroup;
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        // Handle arrays (expressions, DashArray, etc.)
        if (a is object[] arrA && b is object[] arrB)
        {
            return arrA.Length == arrB.Length && arrA.Zip(arrB).All(pair => ValuesEqual(pair.First, pair.Second));
        }

        if (a is double[] dArrA && b is double[] dArrB)
        {
            return dArrA.SequenceEqual(dArrB);
        }

        if (a is string[] sArrA && b is string[] sArrB)
        {
            return sArrA.SequenceEqual(sArrB);
        }

        return a.Equals(b);
    }

    private async Task WireEventsAsync()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = DotNetObjectReference.Create(this);
        var map = _resolvedMap!;

        if (!map.RuntimeIsInitialized)
        {
            return;
        }

        await map.SceneRegistry.WireLayerEventsAsync(
            new LayerEventDescriptor(
                Id,
                _dotNetRef,
                OnClick.HasDelegate,
                OnMouseEnter.HasDelegate,
                OnMouseLeave.HasDelegate
            )
        );
    }

    internal async Task NotifyLayerAddedAsync()
    {
        _eventsRequireWire = true;
        await EnsureEventsWiredAsync();
    }

    private async Task EnsureEventsWiredAsync()
    {
        if (!_eventsRequireWire || !_isInitialized || !_hasEvents || _resolvedMap is null)
        {
            return;
        }

        await WireEventsAsync();
        _eventsRequireWire = false;
    }

    private async Task DiffAndApplyEventBindingsAsync(BaseMap map)
    {
        if (
            _previousOnClick.Equals(OnClick)
            && _previousOnMouseEnter.Equals(OnMouseEnter)
            && _previousOnMouseLeave.Equals(OnMouseLeave)
        )
        {
            return;
        }

        var previouslyHadEvents =
            _previousOnClick.HasDelegate || _previousOnMouseEnter.HasDelegate || _previousOnMouseLeave.HasDelegate;

        if (_hasEvents)
        {
            await WireEventsAsync();
            _eventsRequireWire = false;
        }
        else if (previouslyHadEvents)
        {
            await map.SceneRegistry.UnregisterLayerEventsAsync(Id);
            _dotNetRef?.Dispose();
            _dotNetRef = null;
            _eventsRequireWire = false;
        }

        _previousOnClick = OnClick;
        _previousOnMouseEnter = OnMouseEnter;
        _previousOnMouseLeave = OnMouseLeave;
    }

    [JSInvokable("OnLayerClickAsync")]
    public async Task OnLayerClickAsync(double latitude, double longitude, JsonElement? properties)
    {
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(new LayerFeatureEventArgs(Id, new Coordinate(latitude, longitude), properties));
        }
    }

    [JSInvokable("OnLayerMouseEnterAsync")]
    public async Task OnLayerMouseEnterAsync(double latitude, double longitude, JsonElement? properties)
    {
        if (OnMouseEnter.HasDelegate)
        {
            await OnMouseEnter.InvokeAsync(
                new LayerFeatureEventArgs(Id, new Coordinate(latitude, longitude), properties)
            );
        }
    }

    [JSInvokable("OnLayerMouseLeaveAsync")]
    public async Task OnLayerMouseLeaveAsync()
    {
        if (OnMouseLeave.HasDelegate)
        {
            await OnMouseLeave.InvokeAsync();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_isInitialized && _resolvedMap is not null)
            {
                await _resolvedMap.SceneRegistry.UnregisterLayerEventsAsync(Id);
            }

            if (Source is not null)
            {
                await Source.UnregisterLayerAsync(this);
            }
            else if (_isInitialized && Map is not null)
            {
                await Map.SceneRegistry.UnregisterLayerAsync(Id);
            }
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }

        _dotNetRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
