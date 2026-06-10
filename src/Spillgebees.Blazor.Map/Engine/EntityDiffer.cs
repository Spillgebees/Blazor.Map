namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Result of one diff pass. <see cref="UpsertInputPositions"/> holds positions into the
/// input list for entities needing a structural upsert (new entity or structural hash
/// change) — the layer builds the full upsert payload only for those.
/// The lists are buffers reused by the next <see cref="EntityDiffer.Diff"/> call;
/// consume the result before diffing again.
/// </summary>
internal sealed record EntityDiffResult(
    uint Epoch,
    bool HasStructuralChanges,
    IReadOnlyList<int> UpsertInputPositions,
    IReadOnlyList<uint> RemovedIndices,
    IReadOnlyList<EntityMotionRecord> Moved
);

/// <summary>
/// Owns the entity identity table (docs/plans/map-engine-protocol.md §3.1 and §3.5):
/// assigns recycled u32 indices, keeps one ~40-byte snapshot per entity, and splits each
/// update into motion records (binary fast path) and structural upserts (JSON op).
/// Structural changes bump the epoch; motion frames carry the epoch they were computed
/// against so the TS side can drop stale frames.
/// </summary>
internal sealed class EntityDiffer
{
    private struct EntitySnapshot
    {
        public uint Index;
        public double Lng;
        public double Lat;
        public float Rotation;
        public float SortKey;
        public int StructuralHash;
        public uint SeenAtVersion;
    }

    private readonly Dictionary<string, EntitySnapshot> _snapshots = [];
    private readonly Stack<uint> _freeIndices = [];
    private readonly List<int> _upsertPositions = [];
    private readonly List<uint> _removedIndices = [];
    private readonly List<EntityMotionRecord> _moved = [];
    private readonly List<string> _removedIds = [];
    private uint _nextIndex;
    private uint _version;

    public uint Epoch { get; private set; }

    /// <summary>Resolves the index assigned to an entity id; throws for unknown ids.</summary>
    public uint IndexOf(string id) => _snapshots[id].Index;

    public bool TryGetIndex(string id, out uint index)
    {
        if (_snapshots.TryGetValue(id, out var snapshot))
        {
            index = snapshot.Index;
            return true;
        }

        index = 0;
        return false;
    }

    public EntityDiffResult Diff(IReadOnlyList<EntityInput> inputs)
    {
        _upsertPositions.Clear();
        _removedIndices.Clear();
        _moved.Clear();
        _removedIds.Clear();
        _version++;

        for (var position = 0; position < inputs.Count; position++)
        {
            var input = inputs[position];

            if (!_snapshots.TryGetValue(input.Id, out var snapshot))
            {
                _snapshots[input.Id] = new EntitySnapshot
                {
                    Index = AllocateIndex(),
                    Lng = input.Lng,
                    Lat = input.Lat,
                    Rotation = input.Rotation,
                    SortKey = input.SortKey,
                    StructuralHash = input.StructuralHash,
                    SeenAtVersion = _version,
                };
                _upsertPositions.Add(position);
                continue;
            }

            if (snapshot.SeenAtVersion == _version)
            {
                throw new InvalidOperationException($"Duplicate tracked entity id '{input.Id}' in update.");
            }

            snapshot.SeenAtVersion = _version;

            if (snapshot.StructuralHash != input.StructuralHash)
            {
                _upsertPositions.Add(position);
            }
            else if (
                snapshot.Lng != input.Lng
                || snapshot.Lat != input.Lat
                || snapshot.Rotation != input.Rotation
                || snapshot.SortKey != input.SortKey
            )
            {
                _moved.Add(new EntityMotionRecord(snapshot.Index, input.Lng, input.Lat, input.Rotation, input.SortKey));
            }

            snapshot.Lng = input.Lng;
            snapshot.Lat = input.Lat;
            snapshot.Rotation = input.Rotation;
            snapshot.SortKey = input.SortKey;
            snapshot.StructuralHash = input.StructuralHash;
            _snapshots[input.Id] = snapshot;
        }

        foreach (var (id, snapshot) in _snapshots)
        {
            if (snapshot.SeenAtVersion != _version)
            {
                _removedIds.Add(id);
                _removedIndices.Add(snapshot.Index);
            }
        }

        foreach (var id in _removedIds)
        {
            _freeIndices.Push(_snapshots[id].Index);
            _snapshots.Remove(id);
        }

        var hasStructuralChanges = _upsertPositions.Count > 0 || _removedIndices.Count > 0;
        if (hasStructuralChanges)
        {
            Epoch++;
        }

        return new EntityDiffResult(Epoch, hasStructuralChanges, _upsertPositions, _removedIndices, _moved);
    }

    private uint AllocateIndex() => _freeIndices.Count > 0 ? _freeIndices.Pop() : _nextIndex++;
}
