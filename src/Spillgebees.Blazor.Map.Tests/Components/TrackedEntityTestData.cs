using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.TrackedData;

namespace Spillgebees.Blazor.Map.Tests.Components;

internal static class TrackedEntityTestData
{
    public static TrackedDataLayer<Vehicle> CreateLayer()
    {
        // arrange
        var vehicles = new[] { new Vehicle("vehicle-1", new Coordinate(49.6, 6.1), "train") };

        // act
        var layer = new TrackedDataLayer<Vehicle>(
            Id: "vehicles",
            Items: vehicles,
            IdOptions: new TrackedDataIdOptions<Vehicle>(vehicle => vehicle.Id),
            Visual: new TrackedDataVisualOptions<Vehicle>(
                Symbol: new TrackedDataSymbolOptions<Vehicle>(
                    vehicle => vehicle.Position,
                    vehicle => vehicle.IconImage
                ),
                Decorations: [],
                Cluster: new TrackedDataClusterOptions(),
                Animation: null,
                Visible: true,
                PrimaryIconOpacity: null
            ),
            Behavior: new TrackedDataBehaviorOptions<Vehicle>(),
            Callbacks: new TrackedDataCallbacks<Vehicle>()
        );

        // assert
        return layer;
    }

    public sealed record Vehicle(string Id, Coordinate Position, string IconImage);
}
