using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public sealed record TrainSampleDefinition(
    string Id,
    string ServiceNumber,
    string Route,
    string Operator,
    string Color,
    double Speed,
    IReadOnlyList<Coordinate> Waypoints,
    int InitialWaypointIndex = 0,
    double InitialProgress = 0
);
