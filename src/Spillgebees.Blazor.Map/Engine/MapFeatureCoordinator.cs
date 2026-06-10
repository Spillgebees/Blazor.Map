using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Markers/circles/polylines → ops. Everything rides the ops channel: markers ship as
/// marker.set/marker.remove ops (DOM elements survive style changes, so they need no
/// replay); circles/polylines are GeoJSON features on engine-owned sources/layers,
/// with their collections shipped through the raw-text lane + rAF scheduler — which
/// also gets them style-replay for free. Features aggregate across owners (components
/// and the map's convenience parameters).
/// </summary>
internal sealed class MapFeatureCoordinator(MapEngineChannel channel)
{
    private const string CirclesSourceId = "sgb-circles-source";
    private const string CirclesLayerId = "sgb-circles-layer";
    private const string PolylinesSourceId = "sgb-polylines-source";
    private const string PolylinesLayerId = "sgb-polylines-layer";
    private const string ParameterFeaturesOwnerId = "map-parameters";

    private static readonly JsonSerializerOptions _popupJsonOptions = new(JsonSerializerDefaults.Web);

    // reused across flushes — a 2,000-circle collection is ~300 KB of JSON per tick
    private readonly ArrayBufferWriter<byte> _flushBuffer = new(512 * 1024);

    private readonly Dictionary<string, IReadOnlyList<Marker>> _markersByOwner = [];
    private readonly Dictionary<string, IReadOnlyList<Circle>> _circlesByOwner = [];
    private readonly Dictionary<string, IReadOnlyList<Polyline>> _polylinesByOwner = [];
    private readonly Dictionary<string, Marker> _syncedMarkersById = [];
    private IReadOnlyList<Marker>? _appliedMarkersParameter;
    private IReadOnlyList<Circle>? _appliedCirclesParameter;
    private IReadOnlyList<Polyline>? _appliedPolylinesParameter;
    private bool _shapeInfrastructureQueued;

    public void SetMarkers(string ownerId, IReadOnlyList<Marker> markers)
    {
        SetOwnerFeatures(_markersByOwner, ownerId, markers);
        SyncMarkers();
    }

    public void SetCircles(string ownerId, IReadOnlyList<Circle> circles)
    {
        SetOwnerFeatures(_circlesByOwner, ownerId, circles);
        FlushCircles();
    }

    public void SetPolylines(string ownerId, IReadOnlyList<Polyline> polylines)
    {
        SetOwnerFeatures(_polylinesByOwner, ownerId, polylines);
        FlushPolylines();
    }

    public void RemoveOwner(string ownerId)
    {
        _markersByOwner.Remove(ownerId);
        _circlesByOwner.Remove(ownerId);
        _polylinesByOwner.Remove(ownerId);
        FlushCircles();
        FlushPolylines();
        SyncMarkers();
    }

    /// <summary>Applies the map's convenience parameters, reference-diffed per list.</summary>
    public void SyncParameters(
        IReadOnlyList<Marker>? markers,
        IReadOnlyList<Circle>? circles,
        IReadOnlyList<Polyline>? polylines
    )
    {
        if (!ReferenceEquals(markers, _appliedMarkersParameter))
        {
            _appliedMarkersParameter = markers;
            SetMarkers(ParameterFeaturesOwnerId, markers ?? []);
        }

        if (!ReferenceEquals(circles, _appliedCirclesParameter))
        {
            _appliedCirclesParameter = circles;
            SetCircles(ParameterFeaturesOwnerId, circles ?? []);
        }

        if (!ReferenceEquals(polylines, _appliedPolylinesParameter))
        {
            _appliedPolylinesParameter = polylines;
            SetPolylines(ParameterFeaturesOwnerId, polylines ?? []);
        }
    }

    private static void SetOwnerFeatures<T>(
        Dictionary<string, IReadOnlyList<T>> store,
        string ownerId,
        IReadOnlyList<T> features
    )
    {
        if (features.Count == 0)
        {
            store.Remove(ownerId);
        }
        else
        {
            store[ownerId] = features;
        }
    }

    private void SyncMarkers()
    {
        var desired = new Dictionary<string, Marker>();
        foreach (var marker in _markersByOwner.Values.SelectMany(markers => markers))
        {
            desired[marker.Id] = marker;
        }

        List<string>? removedIds = null;
        foreach (var id in _syncedMarkersById.Keys)
        {
            if (!desired.ContainsKey(id))
            {
                (removedIds ??= []).Add(id);
            }
        }

        foreach (var id in removedIds ?? [])
        {
            _syncedMarkersById.Remove(id);
            channel.Queue(new MarkerRemoveOp(id));
        }

        foreach (var (id, marker) in desired)
        {
            // records compare by value, so only genuinely changed markers cross the wire
            if (!_syncedMarkersById.TryGetValue(id, out var synced) || synced != marker)
            {
                _syncedMarkersById[id] = marker;
                channel.Queue(new MarkerSetOp(marker));
            }
        }
    }

    private void EnsureShapeInfrastructure()
    {
        if (_shapeInfrastructureQueued)
        {
            return;
        }

        _shapeInfrastructureQueued = true;
        var emptyCollection = """{"type":"FeatureCollection","features":[]}""";
        channel.Queue(
            new SourceAddOp(
                CirclesSourceId,
                new JsonObject { ["type"] = "geojson", ["data"] = JsonNode.Parse(emptyCollection) }
            )
        );
        channel.Queue(
            new LayerAddOp(
                CirclesLayerId,
                new JsonObject
                {
                    ["id"] = CirclesLayerId,
                    ["type"] = "circle",
                    ["source"] = CirclesSourceId,
                    ["paint"] = new JsonObject
                    {
                        ["circle-radius"] = EngineSpec.Expr("coalesce", EngineSpec.Expr("get", "radius"), 8),
                        ["circle-color"] = EngineSpec.Expr("coalesce", EngineSpec.Expr("get", "color"), "#3388ff"),
                        ["circle-opacity"] = EngineSpec.Expr("coalesce", EngineSpec.Expr("get", "opacity"), 1),
                        ["circle-stroke-color"] = EngineSpec.Expr(
                            "coalesce",
                            EngineSpec.Expr("get", "strokeColor"),
                            "transparent"
                        ),
                        ["circle-stroke-width"] = EngineSpec.Expr(
                            "coalesce",
                            EngineSpec.Expr("get", "strokeWidth"),
                            0
                        ),
                        ["circle-stroke-opacity"] = EngineSpec.Expr(
                            "coalesce",
                            EngineSpec.Expr("get", "strokeOpacity"),
                            1
                        ),
                    },
                }
            )
        );
        channel.Queue(
            new SourceAddOp(
                PolylinesSourceId,
                new JsonObject { ["type"] = "geojson", ["data"] = JsonNode.Parse(emptyCollection) }
            )
        );
        channel.Queue(
            new LayerAddOp(
                PolylinesLayerId,
                new JsonObject
                {
                    ["id"] = PolylinesLayerId,
                    ["type"] = "line",
                    ["source"] = PolylinesSourceId,
                    ["layout"] = new JsonObject { ["line-join"] = "round", ["line-cap"] = "round" },
                    ["paint"] = new JsonObject
                    {
                        ["line-color"] = EngineSpec.Expr("coalesce", EngineSpec.Expr("get", "color"), "#3388ff"),
                        ["line-width"] = EngineSpec.Expr("coalesce", EngineSpec.Expr("get", "width"), 3),
                        ["line-opacity"] = EngineSpec.Expr("coalesce", EngineSpec.Expr("get", "opacity"), 1),
                    },
                }
            )
        );
    }

    // Shape flushes rebuild the whole FeatureCollection per change, so they write
    // straight to UTF-8 instead of going through the JsonNode DOM (~5x cheaper at
    // 2000 features, and this runs on every parameter tick).
    private void FlushCircles()
    {
        EnsureShapeInfrastructure();
        _flushBuffer.Clear();
        using (var writer = new Utf8JsonWriter(_flushBuffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            foreach (var circles in _circlesByOwner.Values)
            {
                foreach (var circle in circles)
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", "Feature");
                    writer.WriteString("id", circle.Id);
                    writer.WriteStartObject("geometry");
                    writer.WriteString("type", "Point");
                    writer.WriteStartArray("coordinates");
                    writer.WriteNumberValue(circle.Position.Longitude);
                    writer.WriteNumberValue(circle.Position.Latitude);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.WriteStartObject("properties");
                    writer.WriteString("id", circle.Id);
                    writer.WriteNumber("radius", circle.Radius);
                    WriteOptional(writer, "color", circle.Color);
                    WriteOptional(writer, "opacity", circle.Opacity);
                    WriteOptional(writer, "strokeColor", circle.StrokeColor);
                    WriteOptional(writer, "strokeWidth", circle.StrokeWidth);
                    WriteOptional(writer, "strokeOpacity", circle.StrokeOpacity);
                    WriteOptionalPopup(writer, circle.Popup);
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        channel.PushSourceData(CirclesSourceId, Encoding.UTF8.GetString(_flushBuffer.WrittenSpan), animateMs: null);
    }

    private void FlushPolylines()
    {
        EnsureShapeInfrastructure();
        _flushBuffer.Clear();
        using (var writer = new Utf8JsonWriter(_flushBuffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            foreach (var polylines in _polylinesByOwner.Values)
            {
                foreach (var polyline in polylines)
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", "Feature");
                    writer.WriteString("id", polyline.Id);
                    writer.WriteStartObject("geometry");
                    writer.WriteString("type", "LineString");
                    writer.WriteStartArray("coordinates");
                    foreach (var coordinate in polyline.Coordinates)
                    {
                        writer.WriteStartArray();
                        writer.WriteNumberValue(coordinate.Longitude);
                        writer.WriteNumberValue(coordinate.Latitude);
                        writer.WriteEndArray();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.WriteStartObject("properties");
                    writer.WriteString("id", polyline.Id);
                    WriteOptional(writer, "color", polyline.Color);
                    WriteOptional(writer, "width", polyline.Width);
                    WriteOptional(writer, "opacity", polyline.Opacity);
                    WriteOptionalPopup(writer, polyline.Popup);
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        channel.PushSourceData(PolylinesSourceId, Encoding.UTF8.GetString(_flushBuffer.WrittenSpan), animateMs: null);
    }

    private static void WriteOptional(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(name, value);
        }
    }

    private static void WriteOptional(Utf8JsonWriter writer, string name, double? value)
    {
        if (value is not null)
        {
            writer.WriteNumber(name, value.Value);
        }
    }

    private static void WriteOptionalPopup(Utf8JsonWriter writer, PopupOptions? popup)
    {
        if (popup is not null)
        {
            // engine/shape-popups.ts expects the popup options as a JSON string property
            writer.WriteString("popup", JsonSerializer.Serialize(popup, _popupJsonOptions));
        }
    }
}
