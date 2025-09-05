namespace Spillgebees.Blazor.Map.Models;

public record TileLayer(
    string UrlTemplate,
    string Attribution,
    bool? DetectRetina = null,
    int? TileSize = null);
