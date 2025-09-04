namespace Spillgebees.Blazor.Map.Models;

public record Polyline(
    List<Coordinate> Coordinates,
    int? SmoothFactor = null,
    bool NoClip = false,
    bool Stroke = false,
    string? StrokeColor = null,
    int? StrokeWeight = null,
    int? StrokeOpacity = null,
    bool Fill = false,
    string? FillColor = null,
    int? FillOpacity = null) : IPath;
