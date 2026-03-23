using Spillgebees.Blazor.Map.Components.Layers;

namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed class MapLogicalLayerGroupRegistry
{
    private int _nextDeclarationOrder;
    private readonly Dictionary<string, MapLogicalLayerGroup> _groups = new(StringComparer.Ordinal);

    internal LayerOrderRegistration ReserveLayerOrderRegistration(
        string groupId,
        MapLayerOrderOptions layerOrder,
        MapLayerOrderOptions inheritedOrder
    )
    {
        var existingGroup = GetOrCreateGroup(groupId);
        if (existingGroup.Ordering is not null)
        {
            var updatedRegistration = LayerOrderRegistration.Create(
                layerOrder,
                inheritedOrder,
                existingGroup.DeclarationOrder
            );

            if (existingGroup.Ordering != updatedRegistration)
            {
                _groups[groupId] = existingGroup with { Ordering = updatedRegistration };
            }

            return updatedRegistration;
        }

        var registration = LayerOrderRegistration.Create(layerOrder, inheritedOrder, existingGroup.DeclarationOrder);
        _groups[groupId] = existingGroup with { Ordering = registration };
        return registration;
    }

    private MapLogicalLayerGroup GetOrCreateGroup(string groupId)
    {
        if (_groups.TryGetValue(groupId, out var existingGroup))
        {
            return existingGroup;
        }

        _nextDeclarationOrder++;

        var created = new MapLogicalLayerGroup(groupId, _nextDeclarationOrder);
        _groups[groupId] = created;
        return created;
    }
}
