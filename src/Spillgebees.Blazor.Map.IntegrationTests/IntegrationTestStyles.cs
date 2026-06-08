using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.IntegrationTests;

internal static class IntegrationTestStyles
{
    public static MapStyle Base { get; } =
        MapStyle.FromRasterUrl(
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVQYV2NgAAIAAAUAAarVyFEAAAAASUVORK5CYII=",
            "Integration test"
        );
}
