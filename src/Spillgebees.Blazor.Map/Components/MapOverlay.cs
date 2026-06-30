using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// A toggleable overlay on the engine path. Parts group runtime layers (nested layer
/// components) and/or composed style layers (<see cref="MapOverlayPart.LayerIds"/>
/// referencing <see cref="Style"/>). Visibility composes as overlay AND part,
/// applied JS-locally.
/// </summary>
public sealed class MapOverlay : ComponentBase, IAsyncDisposable
{
    /// <summary>The host map, supplied by the enclosing <see cref="SgbMap"/>.</summary>
    [CascadingParameter]
    public SgbMap? Map { get; set; }

    /// <summary>Unique overlay id.</summary>
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

    /// <summary>Optional overlay style (URL style) composed into the map.</summary>
    [Parameter]
    public MapStyle? Style { get; set; }

    /// <summary>The overlay's parts (<see cref="MapOverlayPart"/>) and/or layer components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private readonly List<MapOverlayPart> _parts = [];
    private bool _isInitialized;

    internal bool Visible { get; private set; }

    internal string StyleId => Style?.Id ?? Id;

    /// <summary>Cascades this overlay to its child content.</summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MapOverlay>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<>.Value), this);
        builder.AddComponentParameter(2, nameof(CascadingValue<>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<>.ChildContent), ChildContent);
        builder.CloseComponent();
    }

    /// <summary>Applies <see cref="InitiallyVisible"/>.</summary>
    protected override void OnInitialized()
    {
        Visible = InitiallyVisible;
    }

    /// <summary>Validates that the overlay is nested inside a <see cref="SgbMap"/>.</summary>
    protected override void OnParametersSet()
    {
        if (Map is null)
        {
            throw new InvalidOperationException($"{nameof(MapOverlay)} must be nested inside a {nameof(SgbMap)}.");
        }
    }

    /// <summary>Registers the overlay (and its style, if any) with the host map on first render.</summary>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        if (Style is not null)
        {
            Map!.RegisterOverlayStyle(Style.Id is null ? Style.WithId(Id) : Style);
        }

        Map!.RegisterOverlay(this);
        _isInitialized = true;
        QueueOverlayOp();
    }

    internal void RegisterPart(MapOverlayPart part)
    {
        _parts.Add(part);
        QueueOverlayOp();
    }

    /// <summary>Called by parts/layers when their membership or visibility changes.</summary>
    internal void QueueOverlayOp()
    {
        if (!_isInitialized || Map is null)
        {
            return;
        }

        Map.Channel.Queue(
            new OverlaySetOp(
                Id,
                Visible,
                Targets: [],
                Parts:
                [
                    .. _parts.Select(part => new EngineOverlayPart(part.Id, part.Visible, part.BuildTargets(StyleId))),
                ]
            )
        );
        Map.NotifyOverlayChanged(Id);
    }

    internal MapOverlayItem BuildItem() =>
        new(
            Id,
            Label ?? Id,
            Visible,
            Description,
            Symbol,
            [
                .. _parts.Select(part => new MapOverlayPartItem(
                    part.Id,
                    part.Label ?? part.Id,
                    part.Visible,
                    part.Description,
                    part.Symbol
                )),
            ]
        );

    internal void SetVisible(bool visible)
    {
        Visible = visible;
        QueueOverlayOp();
    }

    internal bool SetPartVisible(string partId, bool visible)
    {
        var part = _parts.FirstOrDefault(candidate => candidate.Id == partId);
        if (part is null)
        {
            return false;
        }

        part.Visible = visible;
        QueueOverlayOp();
        return true;
    }

    /// <summary>Unregisters the overlay and removes it from the host map.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Map is null || !_isInitialized)
        {
            return;
        }

        Map.UnregisterOverlay(this);
        await Map.Channel.QueueAndFlushAsync(new OverlayRemoveOp(Id));
        GC.SuppressFinalize(this);
    }
}
