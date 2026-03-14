using System.Collections.Immutable;

namespace Spillgebees.Blazor.Map.Utilities;

/// <summary>
/// The result of diffing two layer collections.
/// </summary>
/// <typeparam name="T">The layer type.</typeparam>
/// <param name="Added">Layers present in the new collection but not in the old.</param>
/// <param name="Removed">IDs of layers present in the old collection but not in the new.</param>
/// <param name="Updated">Layers present in both collections where the value has changed.</param>
public record LayerDiffResult<T>(ImmutableArray<T> Added, ImmutableArray<string> Removed, ImmutableArray<T> Updated)
{
    public bool HasChanges => Added.Length > 0 || Removed.Length > 0 || Updated.Length > 0;
}

/// <summary>
/// Computes the diff between two layer collections by ID.
/// </summary>
public static class LayerDiffer
{
    /// <summary>
    /// Diffs two layer collections, producing added, removed, and updated buckets.
    /// Uses record value equality to detect updates.
    /// </summary>
    /// <remarks>
    /// Layer IDs are expected to be unique within each collection.
    /// If duplicates exist, later entries silently overwrite earlier ones.
    /// </remarks>
    /// <param name="oldLayers">The previous layer collection.</param>
    /// <param name="newLayers">The current layer collection.</param>
    /// <param name="idSelector">A function that extracts the string ID from a layer.</param>
    /// <returns>A <see cref="LayerDiffResult{T}"/> with the changes.</returns>
    public static LayerDiffResult<T> Diff<T>(
        IReadOnlyList<T> oldLayers,
        IReadOnlyList<T> newLayers,
        Func<T, string> idSelector
    )
    {
        if (ReferenceEquals(oldLayers, newLayers))
        {
            return EmptyResult<T>();
        }

        if (oldLayers.Count == 0)
        {
            return newLayers.Count == 0 ? EmptyResult<T>() : new LayerDiffResult<T>([.. newLayers], [], []);
        }

        if (newLayers.Count == 0)
        {
            var allRemovedIds = ImmutableArray.CreateBuilder<string>(oldLayers.Count);
            for (var i = 0; i < oldLayers.Count; i++)
            {
                allRemovedIds.Add(idSelector(oldLayers[i]));
            }

            return new LayerDiffResult<T>([], allRemovedIds.MoveToImmutable(), []);
        }

        var oldById = new Dictionary<string, T>(oldLayers.Count);
        for (var i = 0; i < oldLayers.Count; i++)
        {
            oldById[idSelector(oldLayers[i])] = oldLayers[i];
        }

        var added = ImmutableArray.CreateBuilder<T>();
        var updated = ImmutableArray.CreateBuilder<T>();
        var survivingIds = new HashSet<string>(newLayers.Count);

        for (var i = 0; i < newLayers.Count; i++)
        {
            var newLayer = newLayers[i];
            var id = idSelector(newLayer);
            survivingIds.Add(id);

            if (oldById.TryGetValue(id, out var oldLayer))
            {
                if (!EqualityComparer<T>.Default.Equals(oldLayer, newLayer))
                {
                    updated.Add(newLayer);
                }
            }
            else
            {
                added.Add(newLayer);
            }
        }

        var removed = ImmutableArray.CreateBuilder<string>();
        for (var i = 0; i < oldLayers.Count; i++)
        {
            var id = idSelector(oldLayers[i]);
            if (!survivingIds.Contains(id))
            {
                removed.Add(id);
            }
        }

        if (added.Count == 0 && removed.Count == 0 && updated.Count == 0)
        {
            return EmptyResult<T>();
        }

        return new LayerDiffResult<T>(added.ToImmutable(), removed.ToImmutable(), updated.ToImmutable());
    }

    private static LayerDiffResult<T> EmptyResult<T>() => new([], [], []);
}
