namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapSceneRegistryEntry<T>(bool Exists, T? Value, long Version);
