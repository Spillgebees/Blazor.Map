using System.Reflection;
using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Resolves the JSON wire name of enum values.
/// </summary>
public static class EnumJsonName
{
    /// <summary>
    /// Returns the value's <see cref="JsonStringEnumMemberNameAttribute"/> name,
    /// or its <c>ToString()</c> representation when no attribute is present.
    /// </summary>
    public static string Get(Enum value)
    {
        var enumName = Enum.GetName(value.GetType(), value);
        if (enumName is null)
        {
            return value.ToString();
        }

        var member = value.GetType().GetMember(enumName).FirstOrDefault();
        return member?.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? value.ToString();
    }
}
