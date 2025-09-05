namespace Spillgebees.Blazor.Map.Models.Layers;

public interface IPath
{
    public bool Stroke { get; init; }
    public string? StrokeColor { get; init; }
    public int? StrokeWeight { get; init; }
    public int? StrokeOpacity { get; init; }
    public bool Fill { get; init; }
    public string? FillColor { get; init; }
    public int? FillOpacity { get; init; }
}
