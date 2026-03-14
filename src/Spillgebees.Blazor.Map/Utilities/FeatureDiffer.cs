using System.Collections.Immutable;

namespace Spillgebees.Blazor.Map.Utilities;

/// <summary>
/// The result of diffing two feature collections.
/// </summary>
/// <typeparam name="T">The feature type.</typeparam>
/// <param name="Added">Features present in the new collection but not in the old.</param>
/// <param name="Removed">IDs of features present in the old collection but not in the new.</param>
/// <param name="Updated">Features present in both collections where the value has changed.</param>
public record FeatureDiffResult<T>(ImmutableArray<T> Added, ImmutableArray<string> Removed, ImmutableArray<T> Updated)
{
    /// <summary>
    /// Whether the diff contains any changes.
    /// </summary>
    public bool HasChanges => Added.Length > 0 || Removed.Length > 0 || Updated.Length > 0;
}

/// <summary>
/// Computes the diff between two feature collections by ID.
/// </summary>
public static class FeatureDiffer
{
    /// <summary>
    /// Diffs two feature collections, producing added, removed, and updated buckets.
    /// Uses record value equality to detect updates.
    /// </summary>
    /// <remarks>
    /// Feature IDs are expected to be unique within each collection.
    /// If duplicates exist, later entries silently overwrite earlier ones.
    /// </remarks>
    /// <param name="oldFeatures">The previous feature collection.</param>
    /// <param name="newFeatures">The current feature collection.</param>
    /// <param name="idSelector">A function that extracts the string ID from a feature.</param>
    /// <returns>A <see cref="FeatureDiffResult{T}"/> with the changes.</returns>
    public static FeatureDiffResult<T> Diff<T>(
        IReadOnlyList<T> oldFeatures,
        IReadOnlyList<T> newFeatures,
        Func<T, string> idSelector
    )
    {
        if (ReferenceEquals(oldFeatures, newFeatures))
        {
            return EmptyResult<T>();
        }

        if (oldFeatures.Count == 0)
        {
            return newFeatures.Count == 0 ? EmptyResult<T>() : new FeatureDiffResult<T>([.. newFeatures], [], []);
        }

        if (newFeatures.Count == 0)
        {
            var allRemovedIds = ImmutableArray.CreateBuilder<string>(oldFeatures.Count);
            for (var i = 0; i < oldFeatures.Count; i++)
            {
                allRemovedIds.Add(idSelector(oldFeatures[i]));
            }

            return new FeatureDiffResult<T>([], allRemovedIds.MoveToImmutable(), []);
        }

        var oldById = new Dictionary<string, T>(oldFeatures.Count);
        for (var i = 0; i < oldFeatures.Count; i++)
        {
            oldById[idSelector(oldFeatures[i])] = oldFeatures[i];
        }

        var added = ImmutableArray.CreateBuilder<T>();
        var updated = ImmutableArray.CreateBuilder<T>();
        var survivingIds = new HashSet<string>(newFeatures.Count);

        for (var i = 0; i < newFeatures.Count; i++)
        {
            var newFeature = newFeatures[i];
            var id = idSelector(newFeature);
            survivingIds.Add(id);

            if (oldById.TryGetValue(id, out var oldFeature))
            {
                if (!EqualityComparer<T>.Default.Equals(oldFeature, newFeature))
                {
                    updated.Add(newFeature);
                }
            }
            else
            {
                added.Add(newFeature);
            }
        }

        var removed = ImmutableArray.CreateBuilder<string>();
        for (var i = 0; i < oldFeatures.Count; i++)
        {
            var id = idSelector(oldFeatures[i]);
            if (!survivingIds.Contains(id))
            {
                removed.Add(id);
            }
        }

        if (added.Count == 0 && removed.Count == 0 && updated.Count == 0)
        {
            return EmptyResult<T>();
        }

        return new FeatureDiffResult<T>(added.ToImmutable(), removed.ToImmutable(), updated.ToImmutable());
    }

    private static FeatureDiffResult<T> EmptyResult<T>() => new([], [], []);
}
