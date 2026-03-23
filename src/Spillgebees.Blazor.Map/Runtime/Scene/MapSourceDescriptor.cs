namespace Spillgebees.Blazor.Map.Runtime.Scene;

internal sealed record MapSourceDescriptor(string SourceId, IReadOnlyDictionary<string, object?> SourceSpec);
