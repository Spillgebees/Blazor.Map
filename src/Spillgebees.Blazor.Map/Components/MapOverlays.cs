using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Section wrapper that scopes its children as toggleable overlays
/// (<see cref="MapOverlay"/>). Must be placed inside <see cref="SgbMap"/>.
/// </summary>
public sealed class MapOverlays : ComponentBase
{
    private MapSectionContext _sectionContext => field ??= new MapSectionContext(MapContentSectionKind.Overlays);

    [CascadingParameter]
    private MapRootContext? _rootContext { get; set; }

    /// <summary>The overlay components scoped to this section.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Validates that the section is placed inside a <see cref="SgbMap"/>.</summary>
    protected override void OnParametersSet()
    {
        if (_rootContext is null)
        {
            throw new InvalidOperationException("MapOverlays must be placed inside SgbMap.");
        }
    }

    /// <summary>Cascades the section context to its child content.</summary>
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MapSectionContext>>(0);
        builder.AddAttribute(1, nameof(CascadingValue<>.Value), _sectionContext);
        builder.AddAttribute(2, nameof(CascadingValue<>.ChildContent), ChildContent);
        builder.CloseComponent();
    }
}
