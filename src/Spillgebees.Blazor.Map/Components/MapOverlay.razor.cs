using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map;

public partial class MapOverlay : ComponentBase, IDisposable
{
    private MapOverlayContext? _context;
    private BaseMap? _registeredMap;
    private string? _registeredId;

    [CascadingParameter]
    private BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

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
    public int Order { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        if (Map is null)
        {
            throw new InvalidOperationException("MapOverlay must be placed inside SgbMap.");
        }

        if (SectionContext?.Kind is not MapContentSectionKind.Overlays)
        {
            throw new InvalidOperationException("MapOverlay must be placed inside MapOverlays.");
        }

        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("MapOverlay requires a non-empty Id.");
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("MapOverlay requires a non-empty Label.");
        }

        if (
            _registeredMap is not null
            && _registeredId is not null
            && (!ReferenceEquals(_registeredMap, Map) || !string.Equals(_registeredId, Id, StringComparison.Ordinal))
        )
        {
            _registeredMap.UnregisterOverlayDefinition(_registeredId);
        }

        _context = new MapOverlayContext(Map, Id);
        Map.RegisterOverlayDefinition(Id, Label, Description, InitiallyVisible, Symbol, Order);
        _registeredMap = Map;
        _registeredId = Id;
    }

    public void Dispose()
    {
        if (_registeredMap is not null && _registeredId is not null)
        {
            _registeredMap.UnregisterOverlayDefinition(_registeredId);
        }
    }
}
