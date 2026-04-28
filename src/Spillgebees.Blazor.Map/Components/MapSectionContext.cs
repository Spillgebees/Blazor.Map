namespace Spillgebees.Blazor.Map.Components;

internal sealed class MapSectionContext(MapContentSectionKind kind)
{
    public MapContentSectionKind Kind { get; } = kind;
}
