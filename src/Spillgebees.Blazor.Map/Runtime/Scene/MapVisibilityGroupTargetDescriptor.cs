namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapVisibilityGroupTargetDescriptor(
    MapVisibilityGroupTargetKind Kind,
    IReadOnlyList<string> LayerIds,
    string? StyleId = null,
    IReadOnlyList<string>? Tags = null,
    object? Filter = null
);
