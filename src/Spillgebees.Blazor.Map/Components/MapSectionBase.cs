using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Base for the section wrapper components (<see cref="MapControls"/>,
/// <see cref="MapFeatures"/>, …): validates placement inside a map and cascades the
/// section kind so children can verify they are in the right slot.
/// </summary>
internal abstract class MapSectionBase : ComponentBase
{
    private MapSectionContext _sectionContext => field ??= new MapSectionContext(SectionKind);

    [CascadingParameter]
    private MapRootContext? _rootContext { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    internal abstract MapContentSectionKind SectionKind { get; }

    protected override void OnParametersSet()
    {
        if (_rootContext is null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside SgbMap.");
        }
    }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MapSectionContext>>(0);
        builder.AddAttribute(1, nameof(CascadingValue<>.Value), _sectionContext);
        builder.AddAttribute(2, nameof(CascadingValue<>.ChildContent), ChildContent);
        builder.CloseComponent();
    }
}
