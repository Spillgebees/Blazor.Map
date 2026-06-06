namespace Spillgebees.Blazor.Map;

internal static class ClusterLayerDefinitionHelpers
{
    internal static StyleValue<string> GetSymbolTextField(ClusterSymbolLayerDefinition definition) =>
        definition.TextField ?? Expr.Get("point_count_abbreviated");
}
