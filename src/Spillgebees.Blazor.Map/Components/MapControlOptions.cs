using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Components;

public record MapControlOptions(
    ZoomControlOptions ZoomControlOptions,
    ScaleControlOptions ScaleControlOptions,
    CenterControlOptions CenterControlOptions)
{
    public static MapControlOptions Default => new(
        ZoomControlOptions.Default,
        ScaleControlOptions.Default,
        CenterControlOptions.Default);
}

public record ZoomControlOptions(bool Enable, ControlPosition Position, bool ShowZoomInButton, bool ShowZoomOutButton)
{
    public static ZoomControlOptions Default => new(true, ControlPosition.TopRight, true, true);
}

public record ScaleControlOptions(bool Enable, ControlPosition Position, bool? ShowMetric, bool? ShowImperial)
{
    public static ScaleControlOptions Default => new(false, ControlPosition.BottomLeft, true, false);
}

public record CenterControlOptions(bool Enable, ControlPosition Position, Coordinate Center, int Zoom)
{
    public static CenterControlOptions Default => new(true, ControlPosition.TopRight, new Coordinate(49.751667, 6.101667), 9);
}
