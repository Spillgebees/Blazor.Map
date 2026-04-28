using System.Reflection;
using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models.Expressions;

/// <summary>
/// A styling value that can be either a literal or a MapLibre expression.
/// Implicit conversions make simple cases ergonomic.
/// </summary>
[JsonConverter(typeof(StyleValueConverterFactory))]
public readonly struct StyleValue<T>
{
    internal T? Literal { get; }
    internal object[]? Expression { get; }
    internal bool IsExpression => Expression is not null;

    private StyleValue(T? literal, object[]? expression)
    {
        Literal = literal;
        Expression = expression;
    }

    /// <summary>
    /// Returns the raw serializable value — either the literal or the expression array.
    /// Use this when storing in <c>Dictionary&lt;string, object?&gt;</c> to avoid
    /// boxed struct serialization issues with <c>System.Text.Json</c>.
    /// </summary>
    internal object? ToSerializable()
    {
        if (IsExpression)
        {
            return Expression;
        }

        if (Literal is null)
        {
            return null;
        }

        if (Literal is Enum enumValue)
        {
            return GetEnumJsonName(enumValue);
        }

        return Literal;
    }

    private static string GetEnumJsonName(Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).Single();
        return member.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? value.ToString();
    }

    /// <summary>
    /// Implicitly convert a literal value to a <see cref="StyleValue{T}"/>.
    /// </summary>
    public static implicit operator StyleValue<T>(T value) => new(value, null);

    /// <summary>
    /// Implicitly convert a MapLibre expression (object array) to a <see cref="StyleValue{T}"/>.
    /// </summary>
    public static implicit operator StyleValue<T>(object[] expression) => new(default, expression);
}
