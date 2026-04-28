namespace Spillgebees.Blazor.Map.Components;

internal sealed class MapRootContext(BaseMap map)
{
    public BaseMap Map { get; } = map;
}
