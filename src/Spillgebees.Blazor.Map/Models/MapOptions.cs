namespace Spillgebees.Blazor.Map.Models;

public record MapOptions(
    Coordinate Center,
    int Zoom,
    bool ShowLeafletPrefix,
    List<string>? FitToLayerIds = null)
{
    public static MapOptions Default => new(
        new Coordinate(49.751667, 6.101667),
        9,
        true);
}
