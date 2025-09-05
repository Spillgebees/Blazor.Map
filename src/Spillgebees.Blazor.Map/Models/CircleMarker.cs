namespace Spillgebees.Blazor.Map.Models;

public record CircleMarker(
    string Id,
    Coordinate Coordinate,
    int Radius = 6,
    bool Stroke = false,
    string? StrokeColor = null,
    int? StrokeWeight = null,
    int? StrokeOpacity = null,
    bool Fill = false,
    string? FillColor = null,
    int? FillOpacity = null) : IPath;
