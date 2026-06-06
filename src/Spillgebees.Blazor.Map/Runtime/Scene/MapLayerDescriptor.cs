using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapLayerDescriptor(
    string LayerId,
    IReadOnlyDictionary<string, object?> LayerSpec,
    string? BeforeLayerId,
    LayerOrderRegistration Ordering
);
