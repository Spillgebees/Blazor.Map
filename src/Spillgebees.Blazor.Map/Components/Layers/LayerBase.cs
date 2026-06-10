using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Base class for engine-backed MapLibre layer components. Layers nest inside a
/// <see cref="GeoJsonSource"/> (which provides the source), or stand alone with an
/// explicit <see cref="SourceId"/> referencing a style source. Specs cross the interop
/// boundary once; parameter changes diff into per-property ops.
/// </summary>
public abstract class LayerBase : ComponentBase, IAsyncDisposable
{
    /// <summary>Owning <see cref="SgbMap"/>, provided as a cascading parameter.</summary>
    [CascadingParameter]
    public SgbMap? Map { get; set; }

    [CascadingParameter]
    internal IEngineSource? Source { get; set; }

    [CascadingParameter]
    internal MapOverlayPart? OverlayPart { get; set; }

    /// <summary>Unique layer id within the map style.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>Explicit source id; defaults to the enclosing source component's id when nested.</summary>
    [Parameter]
    public string? SourceId { get; set; }

    /// <summary>Vector layer to use within a vector tile source (MapLibre <c>source-layer</c>).</summary>
    [Parameter]
    public string? SourceLayerId { get; set; }

    /// <summary>MapLibre filter expression (object array or <see cref="JsonNode"/>).</summary>
    [Parameter]
    public object? Filter { get; set; }

    /// <summary>Minimum zoom level at which the layer is visible (MapLibre <c>minzoom</c>).</summary>
    [Parameter]
    public double? MinZoom { get; set; }

    /// <summary>Maximum zoom level at which the layer is visible (MapLibre <c>maxzoom</c>).</summary>
    [Parameter]
    public double? MaxZoom { get; set; }

    /// <summary>Ordering slot defined on the map (see engine slot ops).</summary>
    [Parameter]
    public string? Slot { get; set; }

    /// <summary>Explicit before-layer id; takes precedence over <see cref="Slot"/>.</summary>
    [Parameter]
    public string? Before { get; set; }

    /// <summary>Fires when a feature of this layer is clicked; args carry the layer id, click coordinate, and the feature's properties.</summary>
    [Parameter]
    public EventCallback<LayerFeatureEventArgs> OnClick { get; set; }

    /// <summary>Fires when the pointer enters a feature of this layer; args carry the layer id, pointer coordinate, and the feature's properties.</summary>
    [Parameter]
    public EventCallback<LayerFeatureEventArgs> OnMouseEnter { get; set; }

    /// <summary>Fires when the pointer leaves the layer's features.</summary>
    [Parameter]
    public EventCallback OnMouseLeave { get; set; }

    private bool _isInitialized;
    private JsonObject _appliedPaint = [];
    private JsonObject _appliedLayout = [];
    private string? _appliedFilterJson;
    private double? _appliedMinZoom;
    private double? _appliedMaxZoom;
    private int _clickHandlerId;
    private int _enterHandlerId;
    private int _leaveHandlerId;

    internal abstract string LayerType { get; }
    internal abstract Dictionary<string, object?> GetPaintProperties();
    internal abstract Dictionary<string, object?> GetLayoutProperties();

    private string _resolvedSourceId =>
        SourceId
        ?? Source?.Id
        ?? throw new InvalidOperationException(
            $"Layer '{Id}' must be nested inside a GeoJsonSource or set {nameof(SourceId)}."
        );

    /// <summary>Always returns <c>false</c>; the layer has no render output and updates flow through engine ops.</summary>
    protected override bool ShouldRender() => false;

    /// <summary>Validates the map cascade and, once initialized, diffs parameter changes into per-property ops.</summary>
    protected override void OnParametersSet()
    {
        if (Map is null)
        {
            throw new InvalidOperationException($"Layer '{Id}' must be nested inside a {nameof(SgbMap)}.");
        }

        if (_isInitialized)
        {
            DiffAndApply();
        }
    }

    /// <summary>On first render, ensures the enclosing source exists and adds the layer to the map.</summary>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // sources must exist before layers that reference them; child OnAfterRender
        // ordering is not guaranteed, so the source initializes on demand.
        Source?.EnsureInitialized();
        Initialize();
        _isInitialized = true;
    }

    private void Initialize()
    {
        _appliedPaint = EngineSpec.FromProperties(GetPaintProperties());
        _appliedLayout = EngineSpec.FromProperties(GetLayoutProperties());
        _appliedFilterJson = Filter is null ? null : EngineJson.ToNode(Filter)?.ToJsonString();
        _appliedMinZoom = MinZoom;
        _appliedMaxZoom = MaxZoom;

        var spec = new JsonObject
        {
            ["id"] = Id,
            ["type"] = LayerType,
            ["source"] = _resolvedSourceId,
        };
        if (SourceLayerId is not null)
        {
            spec["source-layer"] = SourceLayerId;
        }

        if (Filter is not null)
        {
            spec["filter"] = EngineJson.ToNode(Filter);
        }

        if (MinZoom is { } minZoom)
        {
            spec["minzoom"] = minZoom;
        }

        if (MaxZoom is { } maxZoom)
        {
            spec["maxzoom"] = maxZoom;
        }

        if (_appliedPaint.Count > 0)
        {
            spec["paint"] = _appliedPaint.DeepClone();
        }

        if (_appliedLayout.Count > 0)
        {
            spec["layout"] = _appliedLayout.DeepClone();
        }

        Map!.Channel.Queue(new LayerAddOp(Id, spec, Slot, Before));
        RegisterEventHandlers();
        OverlayPart?.RegisterRuntimeLayer(Id);
    }

    private void RegisterEventHandlers()
    {
        if (OnClick.HasDelegate)
        {
            _clickHandlerId = Map!.Router.Register(payload => HandleEventAsync(payload, OnClick));
        }

        if (OnMouseEnter.HasDelegate)
        {
            _enterHandlerId = Map!.Router.Register(payload => HandleEventAsync(payload, OnMouseEnter));
        }

        if (OnMouseLeave.HasDelegate)
        {
            _leaveHandlerId = Map!.Router.Register(_ => OnMouseLeave.InvokeAsync());
        }

        if (_clickHandlerId != 0 || _enterHandlerId != 0 || _leaveHandlerId != 0)
        {
            Map!.Channel.Queue(
                new EventsSetOp(
                    Id,
                    new EngineEventHandlers(
                        Click: _clickHandlerId == 0 ? null : _clickHandlerId,
                        Enter: _enterHandlerId == 0 ? null : _enterHandlerId,
                        Leave: _leaveHandlerId == 0 ? null : _leaveHandlerId
                    )
                )
            );
        }
    }

    private async Task HandleEventAsync(JsonElement payload, EventCallback<LayerFeatureEventArgs> callback)
    {
        var lng = payload.TryGetProperty("lng", out var lngProperty) ? lngProperty.GetDouble() : 0;
        var lat = payload.TryGetProperty("lat", out var latProperty) ? latProperty.GetDouble() : 0;
        JsonElement? properties = payload.TryGetProperty("properties", out var propertiesProperty)
            ? propertiesProperty
            : null;
        await callback.InvokeAsync(new LayerFeatureEventArgs(Id, new Coordinate(lat, lng), properties));
    }

    private void DiffAndApply()
    {
        var channel = Map!.Channel;
        _appliedPaint = DiffSection(
            channel,
            _appliedPaint,
            EngineSpec.FromProperties(GetPaintProperties()),
            (name, value) => new LayerSetPaintOp(Id, name, value)
        );
        _appliedLayout = DiffSection(
            channel,
            _appliedLayout,
            EngineSpec.FromProperties(GetLayoutProperties()),
            (name, value) => new LayerSetLayoutOp(Id, name, value)
        );

        var filterNode = Filter is null ? null : EngineJson.ToNode(Filter);
        var filterJson = filterNode?.ToJsonString();
        if (filterJson != _appliedFilterJson)
        {
            _appliedFilterJson = filterJson;
            channel.Queue(new LayerSetFilterOp(Id, filterNode));
        }

        if (MinZoom != _appliedMinZoom || MaxZoom != _appliedMaxZoom)
        {
            _appliedMinZoom = MinZoom;
            _appliedMaxZoom = MaxZoom;
            channel.Queue(new LayerSetZoomOp(Id, MinZoom ?? 0, MaxZoom ?? 24));
        }
    }

    private static JsonObject DiffSection(
        MapEngineChannel channel,
        JsonObject applied,
        JsonObject current,
        Func<string, JsonNode?, EngineOp> createOp
    )
    {
        foreach (var (name, value) in current)
        {
            if (!applied.TryGetPropertyValue(name, out var previous) || !JsonNode.DeepEquals(previous, value))
            {
                channel.Queue(createOp(name, value?.DeepClone()));
            }
        }

        foreach (var (name, _) in applied)
        {
            if (!current.ContainsKey(name))
            {
                channel.Queue(createOp(name, null));
            }
        }

        return current;
    }

    /// <summary>Unregisters event handlers and removes the layer from the map.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Map is null || !_isInitialized)
        {
            return;
        }

        foreach (var handlerId in (int[])[_clickHandlerId, _enterHandlerId, _leaveHandlerId])
        {
            if (handlerId != 0)
            {
                Map.Router.Unregister(handlerId);
            }
        }

        if (_clickHandlerId != 0 || _enterHandlerId != 0 || _leaveHandlerId != 0)
        {
            Map.Channel.Queue(new EventsClearOp(Id));
        }

        await Map.Channel.QueueAndFlushAsync(new LayerRemoveOp(Id));
        GC.SuppressFinalize(this);
    }
}
