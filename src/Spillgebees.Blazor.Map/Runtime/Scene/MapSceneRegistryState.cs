namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapSceneRegistryState(
    IReadOnlyDictionary<string, MapSourceDescriptor> Sources,
    IReadOnlyDictionary<string, MapLayerDescriptor> Layers,
    IReadOnlyDictionary<string, LayerEventDescriptor> LayerEvents
);
