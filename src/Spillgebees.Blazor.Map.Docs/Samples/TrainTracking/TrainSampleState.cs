using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public sealed class TrainSampleState
{
    public TrainSampleState(
        string id,
        string serviceNumber,
        string route,
        string @operator,
        string color,
        double speed,
        IReadOnlyList<Coordinate> waypoints
    )
    {
        Id = id;
        ServiceNumber = serviceNumber;
        Route = route;
        Operator = @operator;
        Color = color;
        Speed = speed;
        Waypoints = waypoints;
    }

    public string Id { get; }

    public string ServiceNumber { get; }

    public string Route { get; }

    public string Operator { get; }

    public string Color { get; }

    public double Speed { get; }

    public IReadOnlyList<Coordinate> Waypoints { get; }

    public int WaypointIndex { get; set; }

    public double Progress { get; set; }

    public Coordinate CurrentPosition { get; set; } = new(0, 0);

    public Coordinate NextPosition { get; set; } = new(0, 0);

    public static TrainSampleState FromDefinition(TrainSampleDefinition definition) =>
        new(
            definition.Id,
            definition.ServiceNumber,
            definition.Route,
            definition.Operator,
            definition.Color,
            definition.Speed,
            definition.Waypoints
        )
        {
            WaypointIndex = definition.InitialWaypointIndex,
            Progress = definition.InitialProgress,
        };
}
