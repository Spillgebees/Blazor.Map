using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

public partial class MapOverlayPart : ComponentBase, IDisposable
{
    private MapOverlayPartContext? _context;
    private BaseMap? _registeredMap;
    private string? _registeredOverlayId;
    private string? _registeredPartId;

    [CascadingParameter]
    private MapOverlayContext? Overlay { get; set; }

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public bool InitiallyVisible { get; set; } = true;

    [Parameter]
    public MapLegendSymbol? Symbol { get; set; }

    [Parameter]
    public IReadOnlyList<string> LayerIds { get; set; } = [];

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        if (Overlay is null)
        {
            throw new InvalidOperationException("MapOverlayPart must be placed inside MapOverlay.");
        }

        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("MapOverlayPart requires a non-empty Id.");
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("MapOverlayPart requires a non-empty Label.");
        }

        if (
            _registeredMap is not null
            && _registeredOverlayId is not null
            && _registeredPartId is not null
            && (
                !ReferenceEquals(_registeredMap, Overlay.Map)
                || !string.Equals(_registeredOverlayId, Overlay.OverlayId, StringComparison.Ordinal)
                || !string.Equals(_registeredPartId, Id, StringComparison.Ordinal)
            )
        )
        {
            _registeredMap.UnregisterOverlayPartDefinition(_registeredOverlayId, _registeredPartId);
        }

        _context = new MapOverlayPartContext(Overlay.Map, Overlay.OverlayId, Id);
        Overlay.Map.RegisterOverlayPartDefinition(
            Overlay.OverlayId,
            Id,
            Label,
            Description,
            InitiallyVisible,
            Symbol,
            LayerIds
        );
        _registeredMap = Overlay.Map;
        _registeredOverlayId = Overlay.OverlayId;
        _registeredPartId = Id;
    }

    public void Dispose()
    {
        if (_registeredMap is not null && _registeredOverlayId is not null && _registeredPartId is not null)
        {
            _registeredMap.UnregisterOverlayPartDefinition(_registeredOverlayId, _registeredPartId);
        }
    }
}
