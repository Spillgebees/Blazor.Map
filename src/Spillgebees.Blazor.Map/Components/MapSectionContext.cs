namespace Spillgebees.Blazor.Map;

/// <summary>Cascaded by section wrappers; identifies which content slot a child is in.</summary>
internal sealed class MapSectionContext(MapContentSectionKind kind)
{
    public MapContentSectionKind Kind { get; } = kind;
}
