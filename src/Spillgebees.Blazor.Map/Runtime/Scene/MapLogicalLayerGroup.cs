namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapLogicalLayerGroup(
    string GroupId,
    int DeclarationOrder,
    Components.Layers.LayerOrderRegistration? Ordering = null
);
