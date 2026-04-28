using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map.Components;

public abstract class MapOverlayComponentBase : ComponentBase, IAsyncDisposable
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");

    [CascadingParameter]
    protected BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    protected string OwnerId => _ownerId;

    protected override async Task OnParametersSetAsync()
    {
        ValidatePlacement();
        await SetOverlayFeaturesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Map is not null)
        {
            await Map.RemoveOverlayFeaturesAsync(_ownerId);
        }
    }

    protected abstract ValueTask SetOverlayFeaturesAsync();

    private void ValidatePlacement()
    {
        if (SectionContext?.Kind is not MapContentSectionKind.Overlays)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside MapOverlays.");
        }
    }
}
