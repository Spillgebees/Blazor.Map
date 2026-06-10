using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// A toggleable part of a <see cref="MapOverlay"/>: targets the runtime layers nested
/// inside it plus any composed style layers named in <see cref="LayerIds"/>.
/// </summary>
public sealed class MapOverlayPart : ComponentBase
{
    [CascadingParameter]
    internal MapOverlay? Overlay { get; set; }

    /// <summary>Part id, unique within the overlay.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>Display label for overlay UI (e.g. legends); falls back to <see cref="Id"/>.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Optional description for overlay UI.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Optional legend symbol for overlay UI.</summary>
    [Parameter]
    public MapLegendSymbol? Symbol { get; set; }

    /// <summary>Initial visibility, applied once on initialization. Defaults to true.</summary>
    [Parameter]
    public bool InitiallyVisible { get; set; } = true;

    /// <summary>Style layer ids within the overlay's composed style.</summary>
    [Parameter]
    public IReadOnlyList<string>? LayerIds { get; set; }

    /// <summary>The part's nested runtime layer components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private readonly List<string> _runtimeLayerIds = [];

    internal bool Visible { get; set; }

    /// <summary>Cascades this part to its child content.</summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MapOverlayPart>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<>.Value), this);
        builder.AddComponentParameter(2, nameof(CascadingValue<>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<>.ChildContent), ChildContent);
        builder.CloseComponent();
    }

    /// <summary>Applies <see cref="InitiallyVisible"/> and registers the part with its overlay.</summary>
    protected override void OnInitialized()
    {
        if (Overlay is null)
        {
            throw new InvalidOperationException(
                $"{nameof(MapOverlayPart)} must be nested inside a {nameof(MapOverlay)}."
            );
        }

        Visible = InitiallyVisible;
        Overlay.RegisterPart(this);
    }

    /// <summary>Called by nested layer components when they initialize.</summary>
    internal void RegisterRuntimeLayer(string layerId)
    {
        _runtimeLayerIds.Add(layerId);
        Overlay?.QueueOverlayOp();
    }

    internal IReadOnlyList<EngineVisibilityTarget> BuildTargets(string styleId)
    {
        var targets = new List<EngineVisibilityTarget>();
        if (_runtimeLayerIds.Count > 0)
        {
            targets.Add(new EngineVisibilityTarget("runtimeLayer", LayerIds: [.. _runtimeLayerIds]));
        }

        if (LayerIds is { Count: > 0 })
        {
            targets.Add(new EngineVisibilityTarget("styleLayer", LayerIds: LayerIds, StyleId: styleId));
        }

        return targets;
    }
}
