using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Converts the library's expression values (<see cref="StyleValue{T}"/> serializables:
/// literals, MapLibre expression object arrays, dictionaries) into <see cref="JsonNode"/>
/// trees for the ops channel.
/// </summary>
internal static class EngineJson
{
    public static JsonNode? ToNode<T>(StyleValue<T>? style) => style is { } value ? ToNode(value.ToSerializable()) : null;

    public static JsonNode? ToNode(object? value) =>
        value switch
        {
            null => null,
            JsonNode node => node.DeepClone(),
            string text => JsonValue.Create(text),
            bool flag => JsonValue.Create(flag),
            int number => JsonValue.Create(number),
            long number => JsonValue.Create(number),
            double number => JsonValue.Create(number),
            float number => JsonValue.Create(number),
            decimal number => JsonValue.Create(number),
            Enum enumValue => JsonValue.Create(EnumJsonName.Get(enumValue)),
            IReadOnlyDictionary<string, object> dictionary => ToObject(dictionary),
            IEnumerable enumerable => ToArray(enumerable),
            // arbitrary POCOs/anonymous GeoJSON documents (the public Data shape)
            _ => JsonSerializer.SerializeToNode(value, JsonSerializerOptions.Web),
        };

    private static JsonObject ToObject(IReadOnlyDictionary<string, object> dictionary)
    {
        var result = new JsonObject();
        foreach (var (key, entry) in dictionary)
        {
            result[key] = ToNode(entry);
        }

        return result;
    }

    private static JsonArray ToArray(IEnumerable enumerable)
    {
        var result = new JsonArray();
        foreach (var entry in enumerable)
        {
            result.Add(ToNode(entry));
        }

        return result;
    }
}
