namespace Spillgebees.Blazor.Map.Models.TrackedData;

/// <summary>
/// Non-generic tracked data layer contract for heterogeneous map layer collections.
/// </summary>
public interface ITrackedDataLayer
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
