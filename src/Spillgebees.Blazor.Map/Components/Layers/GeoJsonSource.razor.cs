using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// A GeoJSON source component that manages MapLibre sources and their child layers.
/// Place <see cref="LayerBase"/>-derived components (e.g., <see cref="LineLayer"/>,
/// <see cref="CircleLayer"/>) as children to create layers that reference this source.
/// </summary>
public partial class GeoJsonSource : ComponentBase, IMapSource, IAsyncDisposable
{
    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = null!;

    /// <summary>
    /// The parent map component.
    /// </summary>
    [CascadingParameter]
    public BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    /// <summary>
    /// A unique identifier for this source.
    /// </summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>
    /// The GeoJSON data for this source. Can be a GeoJSON object or a URL string.
    /// </summary>
    [Parameter, EditorRequired]
    public object? Data { get; set; }

    /// <summary>
    /// Child content (layer components).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool AllowOutsideMapSources { get; set; }

    [Parameter]
    public string? Stack { get; set; }

    [Parameter]
    public string? BeforeStack { get; set; }

    [Parameter]
    public string? AfterStack { get; set; }

    // Clustering

    /// <summary>
    /// Whether to cluster point features in this source.
    /// </summary>
    [Parameter]
    public bool Cluster { get; set; }

    /// <summary>
    /// The radius of each cluster in pixels. Default is 50.
    /// </summary>
    [Parameter]
    public int ClusterRadius { get; set; } = 50;

    /// <summary>
    /// The maximum zoom level at which clustering is applied.
    /// </summary>
    [Parameter]
    public int? ClusterMaxZoom { get; set; }

    /// <summary>
    /// The minimum number of points required to form a cluster.
    /// </summary>
    [Parameter]
    public int? ClusterMinPoints { get; set; }

    /// <summary>
    /// Custom properties to aggregate across clustered features using MapLibre reduce expressions.
    /// Keys are the output property names available on cluster features. Values are MapLibre
    /// <a href="https://maplibre.org/maplibre-style-spec/sources/#geojson-clusterProperties">accumulator expressions</a>.
    /// </summary>
    /// <example>
    /// <code>
    /// ClusterProperties="@(new Dictionary&lt;string, object&gt; {
    ///     ["totalValue"] = new object[] { "+", new object[] { "get", "value" } },
    ///     ["hasAlert"] = new object[] { "any", new object[] { "get", "alert" } },
    /// })"
    /// </code>
    /// </example>
    [Parameter]
    public IDictionary<string, object>? ClusterProperties { get; set; }

    // Options

    /// <summary>
    /// The maximum zoom level for the source. Default is 18.
    /// </summary>
    [Parameter]
    public int MaxZoom { get; set; } = 18;

    /// <summary>
    /// Whether to auto-generate unique feature IDs.
    /// </summary>
    [Parameter]
    public bool GenerateId { get; set; }

    /// <summary>
    /// A property to use as the feature ID.
    /// </summary>
    [Parameter]
    public string? PromoteId { get; set; }

    /// <summary>
    /// Attribution text for the source.
    /// </summary>
    [Parameter]
    public string? Attribution { get; set; }

    /// <summary>
    /// Whether to calculate line metrics for gradient lines.
    /// </summary>
    [Parameter]
    public bool LineMetrics { get; set; }

    /// <summary>
    /// When set, position changes in the source data are smoothly interpolated
    /// over the specified duration. Only affects Point geometry features.
    /// </summary>
    [Parameter]
    public AnimationOptions? Animation { get; set; }

    private bool _isInitialized;
    private object? _previousData;
    private readonly List<LayerBase> _pendingLayers = [];
    private readonly List<LayerBase> _registeredLayers = [];
    private MapLayerOrderOptions _previousOrderOptions = MapLayerOrderOptions.Empty;

    /// <inheritdoc/>
    public MapLayerOrderOptions OrderOptions => new(Stack, BeforeStack, AfterStack);

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (!AllowOutsideMapSources && SectionContext?.Kind is not MapContentSectionKind.Sources)
        {
            throw new InvalidOperationException("GeoJsonSource must be placed inside MapSources.");
        }
    }

    /// <inheritdoc/>
    public async Task RegisterLayerAsync(LayerBase layer)
    {
        if (!_registeredLayers.Contains(layer))
        {
            _registeredLayers.Add(layer);
        }

        if (!_isInitialized)
        {
            _pendingLayers.Add(layer);
            return;
        }

        await AddLayerToMapAsync(layer);
    }

    /// <inheritdoc/>
    public async Task UnregisterLayerAsync(LayerBase layer)
    {
        _registeredLayers.Remove(layer);
        _pendingLayers.Remove(layer);

        if (!_isInitialized)
        {
            return;
        }

        try
        {
            await Map!.SceneRegistry.UnregisterLayerAsync(layer.Id);
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Map is not null)
        {
            // Wait for map to be ready
            var mapReady = await Map.WhenReadyAsync();
            if (!mapReady)
            {
                return;
            }

            await AddSourceToMapAsync();
            _isInitialized = true;

            // Add any layers that registered before the source was ready
            if (_pendingLayers.Count > 0)
            {
                await Map.SceneRegistry.RegisterLayersAsync(
                    _pendingLayers.Select(layer => new MapLayerDescriptor(
                        layer.Id,
                        layer.BuildLayerSpec(),
                        layer.BeforeId,
                        layer.GetLayerOrderRegistration()
                    ))
                );

                foreach (var layer in _pendingLayers)
                {
                    await layer.NotifyLayerAddedAsync();
                }
            }

            _pendingLayers.Clear();
        }
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (_isInitialized && Data != _previousData && Data is not null)
        {
            _previousData = Data;
            var batch = Map!.SceneRegistry.CreateBatchBuilder();
            batch.SetSourceData(Id, Data, Animation);
            await Map.SceneRegistry.ApplyBatchAsync(batch);
        }

        if (_isInitialized && _previousOrderOptions != OrderOptions)
        {
            await Map!.SceneRegistry.RegisterLayersAsync(
                _registeredLayers.Select(layer => new MapLayerDescriptor(
                    layer.Id,
                    layer.BuildLayerSpec(),
                    layer.BeforeId,
                    layer.GetLayerOrderRegistration()
                ))
            );

            foreach (var layer in _registeredLayers)
            {
                await layer.NotifyLayerAddedAsync();
            }

            _previousOrderOptions = OrderOptions;
        }
    }

    private async Task AddSourceToMapAsync()
    {
        var sourceSpec = new Dictionary<string, object?>
        {
            ["type"] = "geojson",
            ["data"] = Data,
            ["maxzoom"] = MaxZoom,
            ["cluster"] = Cluster ? true : null,
            ["generateId"] = GenerateId ? true : null,
            ["lineMetrics"] = LineMetrics ? true : null,
        };

        if (Cluster)
        {
            sourceSpec["clusterRadius"] = ClusterRadius;
            if (ClusterMaxZoom.HasValue)
            {
                sourceSpec["clusterMaxZoom"] = ClusterMaxZoom.Value;
            }

            if (ClusterMinPoints.HasValue)
            {
                sourceSpec["clusterMinPoints"] = ClusterMinPoints.Value;
            }

            if (ClusterProperties is not null)
            {
                sourceSpec["clusterProperties"] = ClusterProperties;
            }
        }

        if (PromoteId is not null)
        {
            sourceSpec["promoteId"] = PromoteId;
        }

        if (Attribution is not null)
        {
            sourceSpec["attribution"] = Attribution;
        }

        // Remove null entries
        var cleanSpec = sourceSpec.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value);

        _previousData = Data;
        _previousOrderOptions = OrderOptions;
        await Map!.SceneRegistry.RegisterSourceAsync(new MapSourceDescriptor(Id, cleanSpec));
    }

    private async Task AddLayerToMapAsync(LayerBase layer)
    {
        await Map!.SceneRegistry.RegisterLayerAsync(
            new MapLayerDescriptor(layer.Id, layer.BuildLayerSpec(), layer.BeforeId, layer.GetLayerOrderRegistration())
        );
        await layer.NotifyLayerAddedAsync();
    }

    /// <summary>
    /// Gets the zoom level at which a cluster expands into its children.
    /// Use this with <see cref="BaseMap.FlyToAsync"/> to zoom into a cluster on click.
    /// </summary>
    /// <param name="clusterId">The cluster's ID (from the <c>cluster_id</c> feature property).</param>
    /// <returns>The zoom level at which the cluster expands.</returns>
    public async ValueTask<double> GetClusterExpansionZoomAsync(int clusterId)
    {
        return await _jsRuntime.InvokeAsync<double>(
            "Spillgebees.Map.mapFunctions.getClusterExpansionZoom",
            Map!.MapReference,
            Id,
            clusterId
        );
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_isInitialized && Map is not null)
        {
            try
            {
                await Map.SceneRegistry.UnregisterSourceAsync(Id);
            }
            catch (JSDisconnectedException) { }
            catch (ObjectDisposedException) { }
        }

        GC.SuppressFinalize(this);
    }
}
