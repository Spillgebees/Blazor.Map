using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Tests.Components;

internal static class TrackedEntityTestData
{
    public static TrackedEntityLayerDefinition<Vehicle> CreateLayer()
    {
        // arrange
        var vehicles = new[] { new Vehicle("vehicle-1", new Coordinate(49.6, 6.1), "train") };

        // act
        var layer = new TrackedEntityLayerDefinition<Vehicle>(
            Id: "vehicles",
            Items: vehicles,
            IdOptions: new TrackedEntityIdOptions<Vehicle>(vehicle => vehicle.Id),
            Visual: new TrackedEntityVisualOptions<Vehicle>(
                Symbol: new TrackedEntitySymbolOptions<Vehicle>(
                    vehicle => vehicle.Position,
                    vehicle => vehicle.IconImage
                ),
                Decorations: [],
                Cluster: new TrackedEntityClusterOptions(),
                Animation: null,
                Visible: true,
                PrimaryIconOpacity: null
            ),
            Behavior: new TrackedEntityBehaviorOptions<Vehicle>(),
            Callbacks: new TrackedEntityCallbacks<Vehicle>()
        );

        // assert
        return layer;
    }

    public sealed record Vehicle(string Id, Coordinate Position, string IconImage);
}
