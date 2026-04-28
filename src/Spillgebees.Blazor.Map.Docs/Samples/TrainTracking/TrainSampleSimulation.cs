using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Options;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public static class TrainSampleSimulation
{
    public static List<TrainSampleState> CreateStates()
    {
        var states = TrainSampleCatalog.Definitions.Select(TrainSampleState.FromDefinition).ToList();
        InitializeStates(states);
        return states;
    }

    public static void Advance(TrainSampleState train)
    {
        train.Progress += train.Speed;

        while (train.Progress >= 1.0)
        {
            train.Progress -= 1.0;
            train.WaypointIndex++;

            if (train.WaypointIndex >= train.Waypoints.Count - 1)
            {
                train.WaypointIndex = 0;
                train.Progress = 0;
            }
        }

        UpdateTrainPositions(train);
    }

    public static TrainFeatureCollection BuildGeoJson(IEnumerable<TrainSampleState> trains, string? hoveredTrainId)
    {
        var features = trains
            .Select(train =>
            {
                ValidateTrain(train);
                var bearing = CalculateBearing(train.CurrentPosition, train.NextPosition);

                return new TrainFeature(
                    train.Id,
                    new GeoJsonPointGeometry([train.CurrentPosition.Longitude, train.CurrentPosition.Latitude]),
                    new TrainFeatureProperties(
                        train.Id,
                        train.ServiceNumber,
                        train.Route,
                        train.Operator,
                        train.Color,
                        $"train-{train.Color.TrimStart('#')}",
                        bearing,
                        hoveredTrainId == train.Id
                    )
                );
            })
            .ToArray();

        return new TrainFeatureCollection(features);
    }

    public static IReadOnlyList<TrackedEntity<TrainSampleState>> BuildTrackedEntities(
        IEnumerable<TrainSampleState> trains
    )
    {
        return trains
            .Select(train =>
            {
                ValidateTrain(train);
                var bearing = CalculateBearing(train.CurrentPosition, train.NextPosition);
                var isInternational = IsInternational(train);

                return new TrackedEntity<TrainSampleState>(
                    train.Id,
                    train.CurrentPosition,
                    new TrackedEntitySymbol($"train-{train.Color.TrimStart('#')}", size: 1.0, rotation: bearing),
                    color: train.Color,
                    hover: new TrackedEntityHoverIntent(1.2, true),
                    renderOrder: 100,
                    decorations:
                    [
                        new TrackedEntityDecoration(
                            "service",
                            text: train.ServiceNumber,
                            offset: new PixelPoint(1.3, -0.3),
                            anchor: SymbolAnchor.Left,
                            color: "#1e293b",
                            textSize: 11,
                            rotation: bearing,
                            renderOrder: 110
                        ),
                        new TrackedEntityDecoration(
                            "route",
                            text: train.Route,
                            offset: new PixelPoint(1.8, 1.2),
                            anchor: SymbolAnchor.Left,
                            displayMode: TrackedEntityDecorationDisplayMode.Hover,
                            color: "#64748b",
                            textSize: 9,
                            rotation: bearing,
                            renderOrder: 105
                        ),
                        new TrackedEntityDecoration(
                            "operator",
                            text: train.Operator,
                            offset: new PixelPoint(-1.3, 0.0),
                            anchor: SymbolAnchor.Right,
                            displayMode: TrackedEntityDecorationDisplayMode.Selected,
                            color: train.Color,
                            textSize: 8,
                            rotation: null,
                            renderOrder: 108
                        ),
                    ],
                    item: train,
                    properties: new Dictionary<string, object?> { ["internationalPresence"] = isInternational ? 1 : 0 }
                );
            })
            .ToArray();
    }

    public static string BuildIconSvg(string color) =>
        $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="32" height="32">
              <defs>
                <filter id="s" x="-50%" y="-50%" width="200%" height="200%">
                  <feDropShadow dx="0" dy="1" stdDeviation="1.1" flood-color="#0f172a" flood-opacity="0.25"/>
                </filter>
              </defs>
              <path
                d="M27.5 16
                   C24.5 12.5 19 7.8 13.2 7.8
                   C7.5 7.8 4.5 11.8 4.5 16
                   C4.5 20.2 7.5 24.2 13.2 24.2
                   C19 24.2 24.5 19.5 27.5 16Z"
                fill="{color}"
                stroke="white"
                stroke-width="2"
                stroke-linejoin="round"
                filter="url(#s)"/>
            </svg>
            """;

    private static void InitializeStates(IEnumerable<TrainSampleState> trains)
    {
        foreach (var train in trains)
        {
            UpdateTrainPositions(train);
        }
    }

    private static void UpdateTrainPositions(TrainSampleState train)
    {
        ValidateTrain(train);

        var from = train.Waypoints[train.WaypointIndex];
        var to = train.Waypoints[train.WaypointIndex + 1];

        train.CurrentPosition = Interpolate(from, to, train.Progress);
        train.NextPosition = to;
    }

    private static Coordinate Interpolate(Coordinate from, Coordinate to, double progress) =>
        new(
            Latitude: from.Latitude + (to.Latitude - from.Latitude) * progress,
            Longitude: from.Longitude + (to.Longitude - from.Longitude) * progress
        );

    private static double CalculateBearing(Coordinate from, Coordinate to)
    {
        var lat1 = from.Latitude * Math.PI / 180.0;
        var lat2 = to.Latitude * Math.PI / 180.0;
        var deltaLon = (to.Longitude - from.Longitude) * Math.PI / 180.0;

        var y = Math.Sin(deltaLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);

        var bearingRadians = Math.Atan2(y, x);
        var bearingDegrees = bearingRadians * 180.0 / Math.PI;

        return (bearingDegrees - 90 + 360) % 360;
    }

    public static bool IsInternational(TrainSampleState train)
    {
        const double minLatitude = 49.44;
        const double maxLatitude = 50.19;
        const double minLongitude = 5.73;
        const double maxLongitude = 6.53;

        return train.Waypoints.Any(waypoint =>
            waypoint.Latitude < minLatitude
            || waypoint.Latitude > maxLatitude
            || waypoint.Longitude < minLongitude
            || waypoint.Longitude > maxLongitude
        );
    }

    private static void ValidateTrain(TrainSampleState train)
    {
        if (train.Waypoints.Count < 2)
        {
            throw new InvalidOperationException($"Train '{train.Id}' must define at least two waypoints.");
        }

        if (train.WaypointIndex < 0 || train.WaypointIndex >= train.Waypoints.Count - 1)
        {
            throw new InvalidOperationException(
                $"Train '{train.Id}' has an invalid waypoint index {train.WaypointIndex}."
            );
        }
    }
}
