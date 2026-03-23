using System.Text.Json.Serialization;

namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public sealed record TrainFeatureCollection(
    [property: JsonPropertyName("features")] IReadOnlyList<TrainFeature> Features
)
{
    [JsonPropertyName("type")]
    public string Type => "FeatureCollection";
}

public sealed record TrainFeature(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("geometry")] GeoJsonPointGeometry Geometry,
    [property: JsonPropertyName("properties")] TrainFeatureProperties Properties
)
{
    [JsonPropertyName("type")]
    public string Type => "Feature";
}

public sealed record TrainFeatureProperties(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("service")] string Service,
    [property: JsonPropertyName("route")] string Route,
    [property: JsonPropertyName("operator_")] string Operator,
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("bearing")] double Bearing,
    [property: JsonPropertyName("hovered")] bool Hovered
);

public sealed record StationFeatureCollection(
    [property: JsonPropertyName("features")] IReadOnlyList<StationFeature> Features
)
{
    [JsonPropertyName("type")]
    public string Type => "FeatureCollection";
}

public sealed record StationFeature(
    [property: JsonPropertyName("geometry")] GeoJsonPointGeometry Geometry,
    [property: JsonPropertyName("properties")] StationFeatureProperties Properties
)
{
    [JsonPropertyName("type")]
    public string Type => "Feature";
}

public sealed record StationFeatureProperties(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("operator_")] string Operator,
    [property: JsonPropertyName("stype")] string StationType
);

public sealed record GeoJsonPointGeometry([property: JsonPropertyName("coordinates")] double[] Coordinates)
{
    [JsonPropertyName("type")]
    public string Type => "Point";
}
