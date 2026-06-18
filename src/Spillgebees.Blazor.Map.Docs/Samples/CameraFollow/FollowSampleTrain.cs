namespace Spillgebees.Blazor.Map.Docs.Samples.CameraFollow;

// A lightweight stand-in for the full train-tracking sample: a few trains looping preset paths,
// just enough to drive the camera-follow showcase without its catalog, overlays, or glyph plumbing.
public sealed class FollowSampleTrain
{
    private readonly IReadOnlyList<Coordinate> _path;
    private readonly double _speed;
    private int _segment;
    private double _progress;

    private FollowSampleTrain(string id, string label, string color, double speed, IReadOnlyList<Coordinate> path)
    {
        Id = id;
        Label = label;
        Color = color;
        _speed = speed;
        _path = path;
        Position = path[0];
        Heading = Bearing(path[0], path[1]);
    }

    public string Id { get; }
    public string Label { get; }
    public string Color { get; }
    public Coordinate Position { get; private set; }
    public double Heading { get; private set; }

    // Moves the train a step along its loop, wrapping at the end.
    public void Advance()
    {
        _progress += _speed;
        while (_progress >= 1.0)
        {
            _progress -= 1.0;
            _segment = (_segment + 1) % _path.Count;
        }

        var from = _path[_segment];
        var to = _path[(_segment + 1) % _path.Count];
        Position = new Coordinate(
            from.Latitude + (to.Latitude - from.Latitude) * _progress,
            from.Longitude + (to.Longitude - from.Longitude) * _progress
        );
        Heading = Bearing(from, to);
    }

    public static List<FollowSampleTrain> CreateFleet() =>
        [
            new FollowSampleTrain(
                "rb-1",
                "RB 1",
                "#2563eb",
                0.06,
                [new(49.60, 6.10), new(49.63, 6.12), new(49.62, 6.17), new(49.59, 6.15)]
            ),
            new FollowSampleTrain(
                "ic-2",
                "IC 2",
                "#dc2626",
                0.045,
                [new(49.64, 6.08), new(49.66, 6.14), new(49.63, 6.18), new(49.61, 6.11)]
            ),
            new FollowSampleTrain(
                "re-3",
                "RE 3",
                "#16a34a",
                0.08,
                [new(49.58, 6.16), new(49.60, 6.21), new(49.56, 6.22), new(49.55, 6.17)]
            ),
        ];

    // Compass bearing in degrees (0 = north, clockwise). The icon points up, so this rotation aligns it
    // with the direction of travel.
    private static double Bearing(Coordinate from, Coordinate to)
    {
        var lat1 = from.Latitude * Math.PI / 180.0;
        var lat2 = to.Latitude * Math.PI / 180.0;
        var deltaLon = (to.Longitude - from.Longitude) * Math.PI / 180.0;

        var y = Math.Sin(deltaLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);

        var degrees = Math.Atan2(y, x) * 180.0 / Math.PI;
        return (degrees + 360) % 360;
    }
}
