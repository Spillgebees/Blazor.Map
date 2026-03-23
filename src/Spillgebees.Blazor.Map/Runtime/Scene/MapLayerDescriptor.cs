using Spillgebees.Blazor.Map.Components.Layers;

namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapLayerDescriptor(
    string LayerId,
    IReadOnlyDictionary<string, object?> LayerSpec,
    string? BeforeId,
    LayerOrderRegistration Ordering
);
