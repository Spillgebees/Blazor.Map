using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Models;

[JsonConverter(typeof(ControlPositionJsonConverter))]
public enum ControlPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public class ControlPositionJsonConverter : JsonStringEnumConverter
{
    private static readonly JsonNamingPolicy _namingPolicy = new LowercaseNamingPolicy();

    public ControlPositionJsonConverter() : base(namingPolicy: _namingPolicy, allowIntegerValues: false)
    {
    }
}

public class LowercaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
        => name.ToLower();
}

