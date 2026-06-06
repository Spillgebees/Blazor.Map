namespace Spillgebees.Blazor.Map;

internal sealed class MapSectionContext(MapContentSectionKind kind)
{
    public MapContentSectionKind Kind { get; } = kind;
}
