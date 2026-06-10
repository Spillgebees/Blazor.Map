using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Source-generated serialization for the ops channel: no reflection, camelCase, nulls
/// omitted — the wire shape consumed by <c>engine/ops.ts</c>.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(IReadOnlyList<EngineOp>))]
internal sealed partial class MapEngineJsonContext : JsonSerializerContext;
