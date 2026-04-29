using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Utilities;

/// <summary>
/// A JSON converter that converts enum values to lowercase strings.
/// </summary>
internal class LowerCaseJsonStringEnumConverter : JsonStringEnumConverter
{
    private static readonly JsonNamingPolicy _namingPolicy = new LowercaseNamingPolicy();

    public LowerCaseJsonStringEnumConverter()
        : base(namingPolicy: _namingPolicy, allowIntegerValues: false) { }
}

internal class LowercaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToLowerInvariant();
}
