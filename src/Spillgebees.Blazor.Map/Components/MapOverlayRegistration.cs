namespace Spillgebees.Blazor.Map;

/// <summary>
/// Shared plumbing for marker/circle/polyline components: pushes the component's
/// current features to the host under a stable owner id and removes them on dispose.
/// </summary>
internal sealed class MapOverlayRegistration<TFeature>
{
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private IMapFeatureHost? _registeredMap;
    private TFeature? _currentFeature;
    private bool _hasCurrentFeature;

    public async ValueTask RegisterAsync(
        IMapFeatureHost? map,
        MapSectionContext? sectionContext,
        string componentName,
        TFeature feature,
        Func<IMapFeatureHost, string, IReadOnlyList<TFeature>, ValueTask> setOverlayFeaturesAsync
    )
    {
        ValidatePlacement(map, sectionContext, componentName);

        if (
            ReferenceEquals(_registeredMap, map)
            && _hasCurrentFeature
            && EqualityComparer<TFeature>.Default.Equals(_currentFeature, feature)
        )
        {
            return;
        }

        if (_registeredMap is not null && !ReferenceEquals(_registeredMap, map))
        {
            await RemoveRegisteredOverlayFeaturesAsync();
        }

        _currentFeature = feature;
        _hasCurrentFeature = true;
        _registeredMap = map;
        await setOverlayFeaturesAsync(map!, _ownerId, [feature]);
    }

    public async ValueTask DisposeAsync()
    {
        await RemoveRegisteredOverlayFeaturesAsync();
    }

    private async ValueTask RemoveRegisteredOverlayFeaturesAsync()
    {
        if (_registeredMap is not null)
        {
            await _registeredMap.RemoveOverlayFeaturesAsync(_ownerId);
        }

        _registeredMap = null;
        _currentFeature = default;
        _hasCurrentFeature = false;
    }

    private static void ValidatePlacement(IMapFeatureHost? map, MapSectionContext? sectionContext, string componentName)
    {
        if (map is null)
        {
            throw new InvalidOperationException($"{componentName} must be placed inside SgbMap.");
        }

        if (sectionContext?.Kind is not MapContentSectionKind.Features)
        {
            throw new InvalidOperationException($"{componentName} must be placed inside MapFeatures.");
        }
    }
}
