namespace Spillgebees.Blazor.Map.Components.Layers;

internal sealed record LayerOrderRegistration(
    int DeclarationOrder,
    string? LayerGroup,
    string? BeforeLayerGroup,
    string? AfterLayerGroup
)
{
    internal static LayerOrderRegistration Create(
        MapLayerOrderOptions layerOrder,
        MapLayerOrderOptions inheritedOrder,
        int declarationOrder
    ) =>
        new(
            declarationOrder,
            layerOrder.LayerGroup ?? inheritedOrder.LayerGroup,
            layerOrder.BeforeLayerGroup ?? inheritedOrder.BeforeLayerGroup,
            layerOrder.AfterLayerGroup ?? inheritedOrder.AfterLayerGroup
        );
}
