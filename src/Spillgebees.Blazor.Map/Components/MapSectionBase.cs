using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map.Components;

public abstract class MapSectionBase : ComponentBase
{
    private MapSectionContext SectionContext => new(SectionKind);

    [CascadingParameter]
    private MapRootContext? RootContext { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    internal abstract MapContentSectionKind SectionKind { get; }

    protected override void OnParametersSet()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside SgbMap.");
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
