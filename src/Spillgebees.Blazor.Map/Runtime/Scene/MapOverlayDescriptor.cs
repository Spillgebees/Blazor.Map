namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapOverlayDescriptor(
    string OverlayId,
    bool Visible,
    IReadOnlyList<MapVisibilityGroupTargetDescriptor> Targets,
    IReadOnlyList<MapOverlayPartDescriptor> Parts
);
