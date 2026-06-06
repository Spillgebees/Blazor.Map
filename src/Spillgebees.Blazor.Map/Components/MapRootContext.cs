namespace Spillgebees.Blazor.Map;

internal sealed class MapRootContext(BaseMap map)
{
    public BaseMap Map { get; } = map;
}
