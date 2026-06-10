using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Engine-backed GeoJSON source. <see cref="Data"/> accepts a URL string, a raw GeoJSON
/// string, or a <see cref="JsonNode"/>; updates are reference-diffed and flushed through
/// the rAF-coalesced scheduler. Clustering reuses <see cref="ClusterOptions"/> including
/// generated cluster layers and click-to-expand zoom.
/// </summary>
public sealed class GeoJsonSource : ComponentBase, IAsyncDisposable, IEngineSource
{
    /// <summary>Owning <see cref="SgbMap"/>, provided as a cascading parameter.</summary>
    [CascadingParameter]
    public SgbMap? Map { get; set; }

    /// <summary>Unique source id within the map style.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>A URL string, raw GeoJSON string, or <see cref="JsonNode"/> document.</summary>
    [Parameter]
    public object? Data { get; set; }

    /// <summary>Clustering configuration, including generated cluster layers and click behavior.</summary>
    [Parameter]
    public ClusterOptions? Cluster { get; set; }

    /// <summary>Auto-assigns feature ids based on index (MapLibre <c>generateId</c>); required for feature-state.</summary>
    [Parameter]
    public bool GenerateIds { get; set; }

    /// <summary>Feature property to use as the feature id (MapLibre <c>promoteId</c>).</summary>
    [Parameter]
    public string? PromoteId { get; set; }

    /// <summary>Attribution text displayed on the map for this source.</summary>
    [Parameter]
    public string? Attribution { get; set; }

    /// <summary>Calculates line distance metrics, enabling <c>line-gradient</c> styling (MapLibre <c>lineMetrics</c>).</summary>
    [Parameter]
    public bool LineMetrics { get; set; }

    /// <summary>Interpolates Point feature positions on data updates.</summary>
    [Parameter]
    public TimeSpan? AnimateUpdates { get; set; }

    /// <summary>Easing applied to <see cref="AnimateUpdates"/> interpolation.</summary>
    [Parameter]
    public AnimationEasing AnimateEasing { get; set; } = AnimationEasing.Linear;

    /// <summary>Layer components bound to this source.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool _isInitialized;
    private object? _appliedData;

    private IReadOnlyList<ClusterLayerDefinition> _clusterLayerDefinitions =>
        Cluster is { Enabled: true, LayerSet: { Enabled: true } layerSet } ? layerSet.Layers : [];

    private string ClusterLayerId(ClusterLayerDefinition definition) => $"{Id}-{definition.IdSuffix}";

    // unlike TrackedEntityLayer (whose children are capture-once config), child layers
    // must re-render so their parameter updates flow into per-property ops.

    /// <summary>Cascades the source to child layers and renders <see cref="ChildContent"/>.</summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<IEngineSource>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<>.Value), this);
        builder.AddComponentParameter(2, nameof(CascadingValue<>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<>.ChildContent), ChildContent);
        builder.CloseComponent();
    }

    /// <summary>Validates the map cascade and, once initialized, pushes <see cref="Data"/> changes to the engine.</summary>
    protected override void OnParametersSet()
    {
        if (Map is null)
        {
            throw new InvalidOperationException($"{nameof(GeoJsonSource)} must be nested inside a {nameof(SgbMap)}.");
        }

        if (_isInitialized && !ReferenceEquals(Data, _appliedData))
        {
            _appliedData = Data;
            var animateMs = AnimateUpdates is { } animate ? (int?)animate.TotalMilliseconds : null;
            var easing = AnimateEasing == AnimationEasing.EaseInOut ? "easeInOut" : "linear";
            if (Data is string rawText)
            {
                // raw text takes the pass-through lane: parsed once on the JS side
                Map.Channel.PushSourceData(Id, rawText, animateMs, animateMs is null ? null : easing);
            }
            else
            {
                Map.Channel.Queue(
                    new SourceSetDataOp(Id, BuildDataNode(), animateMs is { } ms ? new EngineAnimation(ms, easing) : null)
                );
            }
        }
    }

    /// <summary>On first render, adds the source (and any cluster layers) to the map.</summary>
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            EnsureInitialized();
        }
    }

    void IEngineSource.EnsureInitialized() => EnsureInitialized();

    /// <summary>
    /// Queues source creation; child layers call this before adding themselves so the
    /// source op always precedes layer ops regardless of OnAfterRender ordering.
    /// </summary>
    internal void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _appliedData = Data;

        var spec = new JsonObject
        {
            ["type"] = "geojson",
            ["data"] = BuildDataNode() ?? new JsonObject { ["type"] = "FeatureCollection", ["features"] = new JsonArray() },
        };
        if (GenerateIds)
        {
            spec["generateId"] = true;
        }

        if (PromoteId is not null)
        {
            spec["promoteId"] = PromoteId;
        }

        if (Attribution is not null)
        {
            spec["attribution"] = Attribution;
        }

        if (LineMetrics)
        {
            spec["lineMetrics"] = true;
        }

        if (EngineSpec.BuildClusterSourceOptions(Cluster) is JsonObject clusterOptions)
        {
            foreach (var (key, value) in clusterOptions.ToList())
            {
                clusterOptions.Remove(key);
                spec[key] = value;
            }
        }

        Map!.Channel.Queue(new SourceAddOp(Id, spec));

        foreach (var definition in _clusterLayerDefinitions)
        {
            Map.Channel.Queue(new LayerAddOp(ClusterLayerId(definition), EngineSpec.BuildClusterLayerSpec(ClusterLayerId(definition), Id, definition)));
        }

        if (Cluster is { ClickBehavior: ClusterClickBehavior.ZoomToDissolve })
        {
            var zoomLayerIds = _clusterLayerDefinitions
                .Where(definition => definition.Interactive)
                .Select(ClusterLayerId)
                .ToArray();
            if (zoomLayerIds.Length > 0)
            {
                Map.Channel.Queue(new SourceClusterZoomOp(Id, zoomLayerIds));
            }
        }
    }

    private JsonNode? BuildDataNode() =>
        Data switch
        {
            null => null,
            JsonNode node => node.DeepClone(),
            string text => text.TrimStart().StartsWith('{') ? JsonNode.Parse(text) : JsonValue.Create(text),
            _ => EngineJson.ToNode(Data),
        };

    /// <summary>Removes the source and its generated cluster layers from the map.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Map is null || !_isInitialized)
        {
            return;
        }

        foreach (var definition in _clusterLayerDefinitions)
        {
            Map.Channel.Queue(new LayerRemoveOp(ClusterLayerId(definition)));
        }

        await Map.Channel.QueueAndFlushAsync(new SourceRemoveOp(Id));
        GC.SuppressFinalize(this);
    }
}
