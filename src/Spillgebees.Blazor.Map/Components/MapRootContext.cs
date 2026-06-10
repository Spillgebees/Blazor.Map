namespace Spillgebees.Blazor.Map;

/// <summary>Marks content as being nested inside a <see cref="SgbMap"/>.</summary>
internal sealed class MapRootContext(object map)
{
    public object Map { get; } = map;
}
