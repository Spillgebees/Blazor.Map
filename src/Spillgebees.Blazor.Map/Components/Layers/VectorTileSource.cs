using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Engine-backed vector tile source. Nested layers set <see cref="LayerBase.SourceLayerId"/>
/// to select the vector layer within the tiles.
/// </summary>
public sealed class VectorTileSource : ComponentBase, IAsyncDisposable, IEngineSource
{
    /// <summary>Owning <see cref="SgbMap"/>, provided as a cascading parameter.</summary>
    [CascadingParameter]
    public SgbMap? Map { get; set; }

    /// <summary>Unique source id within the map style.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>TileJSON endpoint URL. Mutually exclusive with <see cref="Tiles"/>.</summary>
    [Parameter]
    public string? Url { get; set; }

    /// <summary>Tile URL templates (<c>{z}/{x}/{y}</c>).</summary>
    [Parameter]
    public string[]? Tiles { get; set; }

    /// <summary>Attribution text displayed on the map for this source.</summary>
    [Parameter]
    public string? Attribution { get; set; }

    /// <summary>Minimum zoom level for which tiles are available (MapLibre <c>minzoom</c>).</summary>
    [Parameter]
    public int? MinZoom { get; set; }

    /// <summary>Maximum zoom level for which tiles are available (MapLibre <c>maxzoom</c>).</summary>
    [Parameter]
    public int? MaxZoom { get; set; }

    /// <summary>Layer components bound to this source.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool _isInitialized;

    /// <summary>Cascades the source to child layers and renders <see cref="ChildContent"/>.</summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<IEngineSource>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<>.Value), this);
        builder.AddComponentParameter(2, nameof(CascadingValue<>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<>.ChildContent), ChildContent);
        builder.CloseComponent();
    }

    /// <summary>Validates the map cascade.</summary>
    protected override void OnParametersSet()
    {
        if (Map is null)
        {
            throw new InvalidOperationException(
                $"{nameof(VectorTileSource)} must be nested inside a {nameof(SgbMap)}."
            );
        }
    }

    /// <summary>On first render, adds the source to the map.</summary>
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            EnsureInitialized();
        }
    }

    void IEngineSource.EnsureInitialized() => EnsureInitialized();

    internal void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        var spec = new JsonObject { ["type"] = "vector" };
        if (Url is not null)
        {
            spec["url"] = Url;
        }

        if (Tiles is { Length: > 0 })
        {
            var tiles = new JsonArray();
            foreach (var tile in Tiles)
            {
                tiles.Add(tile);
            }

            spec["tiles"] = tiles;
        }

        if (Attribution is not null)
        {
            spec["attribution"] = Attribution;
        }

        if (MinZoom is { } minZoom)
        {
            spec["minzoom"] = minZoom;
        }

        if (MaxZoom is { } maxZoom)
        {
            spec["maxzoom"] = maxZoom;
        }

        Map!.Channel.Queue(new SourceAddOp(Id, spec));
    }

    /// <summary>Removes the source from the map.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Map is null || !_isInitialized)
        {
            return;
        }

        await Map.Channel.QueueAndFlushAsync(new SourceRemoveOp(Id));
        GC.SuppressFinalize(this);
    }
}
