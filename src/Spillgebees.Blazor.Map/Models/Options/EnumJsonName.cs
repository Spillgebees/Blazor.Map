using System.Reflection;
using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models.Options;

public static class EnumJsonName
{
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
