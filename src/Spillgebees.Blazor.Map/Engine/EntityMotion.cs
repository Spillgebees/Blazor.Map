using System.Buffers.Binary;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Per-entity state extracted by the layer's selectors, in the shape the differ compares.
/// </summary>
internal readonly record struct EntityInput(
    string Id,
    double Lng,
    double Lat,
    float Rotation,
    float SortKey,
    int StructuralHash
);

/// <summary>
/// A motion-only change for one entity, destined for a binary motion frame.
/// </summary>
internal readonly record struct EntityMotionRecord(uint Index, double Lng, double Lat, float Rotation, float SortKey);

/// <summary>
/// Encodes motion frames in the binary layout decoded by <c>engine/motion.ts</c>
/// (docs/plans/map-engine-protocol.md §3.4). Little-endian, columnar.
/// </summary>
internal static class MotionFrameEncoder
{
    internal const uint MagicV1 = 0x53474201;

    [Flags]
    internal enum Columns : uint
    {
        Coords = 1,
        Rotation = 2,
        SortKey = 4,
    }

    private const int HeaderBytes = 16;

    public static byte[] Encode(
        uint epoch,
        IReadOnlyList<EntityMotionRecord> records,
        bool includeRotation,
        bool includeSortKey
    )
    {
        var count = records.Count;
        var byteLength = HeaderBytes + (count * 4) + (count * 16);
        if (includeRotation)
        {
            byteLength += count * 4;
        }

        if (includeSortKey)
        {
            byteLength += count * 4;
        }

        var columns = Columns.Coords;
        if (includeRotation)
        {
            columns |= Columns.Rotation;
        }

        if (includeSortKey)
        {
            columns |= Columns.SortKey;
        }

        var buffer = new byte[byteLength];
        var span = buffer.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(span, MagicV1);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], epoch);
        BinaryPrimitives.WriteUInt32LittleEndian(span[8..], (uint)count);
        BinaryPrimitives.WriteUInt32LittleEndian(span[12..], (uint)columns);

        var offset = HeaderBytes;
        for (var i = 0; i < count; i++, offset += 4)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], records[i].Index);
        }

        for (var i = 0; i < count; i++, offset += 16)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(span[offset..], records[i].Lng);
            BinaryPrimitives.WriteDoubleLittleEndian(span[(offset + 8)..], records[i].Lat);
        }

        if (includeRotation)
        {
            for (var i = 0; i < count; i++, offset += 4)
            {
                BinaryPrimitives.WriteSingleLittleEndian(span[offset..], records[i].Rotation);
            }
        }

        if (includeSortKey)
        {
            for (var i = 0; i < count; i++, offset += 4)
            {
                BinaryPrimitives.WriteSingleLittleEndian(span[offset..], records[i].SortKey);
            }
        }

        return buffer;
    }
}
