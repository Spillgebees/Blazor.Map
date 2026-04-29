using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// A vector tile source component that loads tiles from a TileJSON URL.
/// Place <see cref="LayerBase"/>-derived components as children to create layers
/// that reference this source. Each child layer must specify a <see cref="LayerBase.SourceLayerId"/>
/// to select which sub-layer of the vector tiles to render.
/// </summary>
/// <example>
/// <code>
/// &lt;VectorTileSource Id="railway" Url="https://server.com/railway-tiles"&gt;
///     &lt;LineLayer Id="tracks" SourceLayerId="railway_lines" Color="#555" Width="2" /&gt;
///     &lt;CircleLayer Id="stations" SourceLayerId="stops" Radius="5" Color="#333" /&gt;
/// &lt;/VectorTileSource&gt;
/// </code>
/// </example>
public partial class VectorTileSource : ComponentBase, IMapSource, IAsyncDisposable
{
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
    /// The TileJSON URL for the vector tile source.
    /// MapLibre resolves tile endpoints, bounds, min/max zoom, and attribution from the TileJSON response.
    /// </summary>
    [Parameter, EditorRequired]
    public string Url { get; set; } = "";

    /// <summary>
    /// Child content (layer components).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Allows this source to be declared outside <see cref="MapSources" /> when advanced composition owns placement.
    /// </summary>
    [Parameter]
    public bool AllowOutsideMapSources { get; set; }

    protected override void OnParametersSet()
    {
        if (!AllowOutsideMapSources && SectionContext?.Kind is not MapContentSectionKind.Sources)
        {
            throw new InvalidOperationException("VectorTileSource must be placed inside MapSources.");
        }
    }

    [Parameter]
    public string? LayerGroup { get; set; }

    [Parameter]
    public string? BeforeLayerGroup { get; set; }

    [Parameter]
    public string? AfterLayerGroup { get; set; }

    /// <summary>
    /// Attribution text for the source. Overrides attribution from TileJSON if set.
    /// </summary>
    [Parameter]
    public string? Attribution { get; set; }

    /// <summary>
    /// Minimum zoom level for the source. Overrides the TileJSON value if set.
    /// </summary>
    [Parameter]
    public int? MinZoom { get; set; }

    /// <summary>
    /// Maximum zoom level for the source. Overrides the TileJSON value if set.
    /// </summary>
    [Parameter]
    public int? MaxZoom { get; set; }

    /// <summary>
    /// The referrer policy to apply to MapLibre-managed requests for this source.
    /// </summary>
    [Parameter]
    public ReferrerPolicy? ReferrerPolicy { get; set; }

    private bool _isInitialized;
    private readonly List<LayerBase> _pendingLayers = [];
    private readonly List<LayerBase> _registeredLayers = [];
    private MapLayerOrderOptions _previousOrderOptions = MapLayerOrderOptions.Empty;

    /// <inheritdoc/>
    MapLayerOrderOptions IMapSource.OrderOptions => new(LayerGroup, BeforeLayerGroup, AfterLayerGroup);

    private MapLayerOrderOptions OrderOptions => new(LayerGroup, BeforeLayerGroup, AfterLayerGroup);

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
            var mapReady = await Map.WhenReadyAsync();
            if (!mapReady)
            {
                return;
            }

            await AddSourceToMapAsync();
            _isInitialized = true;

            if (_pendingLayers.Count > 0)
            {
                await Map.SceneRegistry.RegisterLayersAsync(
                    _pendingLayers.Select(layer => new MapLayerDescriptor(
                        layer.Id,
                        layer.BuildLayerSpec(),
                        layer.BeforeLayerId,
                        layer.GetLayerOrderRegistration()
                    ))
                );

                foreach (var layer in _pendingLayers)
                {
                    await layer.NotifyLayerAddedAsync();
                }
            }

            _pendingLayers.Clear();
            _previousOrderOptions = OrderOptions;
        }
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (!_isInitialized || _previousOrderOptions == OrderOptions)
        {
            return;
        }

        await Map!.SceneRegistry.RegisterLayersAsync(
            _registeredLayers.Select(layer => new MapLayerDescriptor(
                layer.Id,
                layer.BuildLayerSpec(),
                layer.BeforeLayerId,
                layer.GetLayerOrderRegistration()
            ))
        );

        foreach (var layer in _registeredLayers)
        {
            await layer.NotifyLayerAddedAsync();
        }

        _previousOrderOptions = OrderOptions;
    }

    private async Task AddSourceToMapAsync()
    {
        var sourceSpec = new Dictionary<string, object?> { ["type"] = "vector", ["url"] = Url };

        if (Attribution is not null)
        {
            sourceSpec["attribution"] = Attribution;
        }

        if (MinZoom.HasValue)
        {
            sourceSpec["minzoom"] = MinZoom.Value;
        }

        if (MaxZoom.HasValue)
        {
            sourceSpec["maxzoom"] = MaxZoom.Value;
        }

        if (ReferrerPolicy.HasValue)
        {
            sourceSpec["referrerPolicy"] = ReferrerPolicy.Value;
        }

        await Map!.SceneRegistry.RegisterSourceAsync(new MapSourceDescriptor(Id, sourceSpec));
    }

    private async Task AddLayerToMapAsync(LayerBase layer)
    {
        await Map!.SceneRegistry.RegisterLayerAsync(
            new MapLayerDescriptor(
                layer.Id,
                layer.BuildLayerSpec(),
                layer.BeforeLayerId,
                layer.GetLayerOrderRegistration()
            )
        );
        await layer.NotifyLayerAddedAsync();
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
