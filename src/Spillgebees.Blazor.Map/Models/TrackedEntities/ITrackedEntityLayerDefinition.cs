namespace Spillgebees.Blazor.Map;

/// <summary>
/// Non-generic tracked entity layer contract for heterogeneous map layer collections.
/// </summary>
public interface ITrackedEntityLayerDefinition
{
    /// <summary>
    /// Stable tracked layer source ID.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Runtime item type used by this tracked layer definition.
    /// </summary>
    Type ItemType { get; }
}
