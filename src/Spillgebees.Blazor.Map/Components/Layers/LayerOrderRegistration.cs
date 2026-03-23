namespace Spillgebees.Blazor.Map.Components.Layers;

internal sealed record LayerOrderRegistration(
    int DeclarationOrder,
    string? Stack,
    string? BeforeStack,
    string? AfterStack
)
{
    internal static LayerOrderRegistration Create(
        MapLayerOrderOptions layerOrder,
        MapLayerOrderOptions inheritedOrder,
        int declarationOrder
    ) =>
        new(
            declarationOrder,
            layerOrder.Stack ?? inheritedOrder.Stack,
            layerOrder.BeforeStack ?? inheritedOrder.BeforeStack,
            layerOrder.AfterStack ?? inheritedOrder.AfterStack
        );
}
