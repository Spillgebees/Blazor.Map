namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public sealed class TrainSampleState(
    string id,
    string serviceNumber,
    string route,
    string @operator,
    string color,
    double speed,
    IReadOnlyList<Coordinate> waypoints
    )
{
    public string Id { get; } = id;

    public string ServiceNumber { get; } = serviceNumber;

    public string Route { get; } = route;

    public string Operator { get; } = @operator;

    public string Color { get; } = color;

    public double Speed { get; } = speed;

    public IReadOnlyList<Coordinate> Waypoints { get; } = waypoints;

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
