namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapVisibilityGroupDescriptor(
    string GroupId,
    bool Visible,
    IReadOnlyList<MapVisibilityGroupTargetDescriptor> Targets
);
