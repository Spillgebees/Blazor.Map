using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map.Components;

public sealed class MapControls : ComponentBase
{
    private MapSectionContext? _sectionContext;

    private MapSectionContext SectionContext =>
        _sectionContext ??= new MapSectionContext(MapContentSectionKind.Controls);

    [CascadingParameter]
    private MapRootContext? RootContext { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException("MapControls must be placed inside SgbMap.");
        }
    }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MapSectionContext>>(0);
        builder.AddAttribute(1, nameof(CascadingValue<MapSectionContext>.Value), SectionContext);
        builder.AddAttribute(2, nameof(CascadingValue<MapSectionContext>.ChildContent), ChildContent);
        builder.CloseComponent();
    }
}
