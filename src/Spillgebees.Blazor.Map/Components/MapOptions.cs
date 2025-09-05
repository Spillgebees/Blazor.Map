using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Components;

public record MapOptions(
    Coordinate Center,
    int Zoom,
    bool ShowLeafletPrefix,
    string? FitToLayerId = null)
{
    public static MapOptions Default => new(
        new Coordinate(49.751667, 6.101667),
        9,
        true);
}
