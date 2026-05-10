namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapOverlayPartDescriptor(
    string PartId,
    bool Visible,
    IReadOnlyList<MapVisibilityGroupTargetDescriptor> Targets
);
