namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapLogicalLayerGroup(
    string GroupId,
    int DeclarationOrder,
    LayerOrderRegistration? Ordering = null
);
