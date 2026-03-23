using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models.Expressions;

/// <summary>
/// JSON converter factory for <see cref="StyleValue{T}"/>.
/// Serializes as either the literal value or the expression array.
/// </summary>
public class StyleValueConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(StyleValue<>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(StyleValueConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

internal sealed class StyleValueConverter<T> : JsonConverter<StyleValue<T>>
{
    public override StyleValue<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("StyleValue deserialization is not supported.");

    public override void Write(Utf8JsonWriter writer, StyleValue<T> value, JsonSerializerOptions options)
    {
        if (value.IsExpression)
        {
            JsonSerializer.Serialize(writer, value.Expression, options);
        }
        else if (value.Literal is not null)
        {
            JsonSerializer.Serialize(writer, value.Literal, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
