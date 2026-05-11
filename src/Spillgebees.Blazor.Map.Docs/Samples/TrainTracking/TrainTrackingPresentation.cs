using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.TrackedEntities;
using Spillgebees.Blazor.Map.Models.Visibility;

namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public static class TrainTrackingPresentation
{
    public const string RailwayOverlayId = "railway-overlay";
    public const string OverlayStyleId = "sgb-train-tracking-overlay";
    public const string OverlayStyleUrlConfigurationKey = "Samples:TrainTracking:OverlayStyleUrl";
    public const string ComposedGlyphsUrlConfigurationKey = "Samples:TrainTracking:ComposedGlyphsUrl";
    public const string DefaultOverlayStyleUrl = "traintracking/style.json";
    public const string CodeSnippet =
        @"<SgbMap MapOptions=""@_mapOptions""
          OnMoveEnd=""@HandleMapViewChangedAsync""
          OnZoomEnd=""@HandleMapViewChangedAsync"">
      <MapControls>
          <NavigationMapControl />
          <LayerMapControl Id=""layers"" GroupIds=""@(new[] { ""trains"", ""3d-buildings"" })"" />
          <OverlayMapControl Id=""overlays"" OverlayIds=""@(new[] { ""railway-overlay"" })"" />
      </MapControls>
      <MapOverlays>
          <MapOverlay Id=""railway-overlay"" Label=""Railway overlay"">
              <StyleOverlay Style='@MapStyle.FromUrl(""/traintracking/style.json"").WithId(""sgb-train-tracking-overlay"")' />
              <MapOverlayPart Id=""tracks"" Label=""Tracks & tunnels"" LayerIds='@(new[] { ""railway-line-rail"" })' />
          </MapOverlay>
      </MapOverlays>
      <MapFeatures>
          <TrackedEntityLayer TItem=""TrainSampleState"" Layer=""@_trackedEntityLayer"" />
      </MapFeatures>
    </SgbMap>

// hover and selection use feature-state, labels stay screen-facing, and supplementary labels stay hidden while clustered";

    public static MapLayerVisibilityState CreateLayerVisibility() =>
        new([
            new MapLayerVisibilityGroup(
                "3d-buildings",
                [MapLayerVisibilityTarget.Layer("sgb-buildings-3d")],
                Label: "3D Buildings"
            ),
            new MapLayerVisibilityGroup(
                "trains",
                [
                    MapLayerVisibilityTarget.Layer(
                        "train-source-cluster-hit-area",
                        "train-source-clusters",
                        "train-source-cluster-count",
                        "train-source-hit-area",
                        "train-source-symbols",
                        "train-source-cluster-sentinel",
                        "train-source-service-left",
                        "train-source-route-left",
                        "train-source-operator-right"
                    ),
                ],
                Label: "Trains"
            ),
        ]);

    public static MapOptions BuildMapOptions(string? composedGlyphsUrl)
    {
        return new(
            Center: new Coordinate(49.75, 6.12),
            Zoom: 8,
            Style: MapStyle.OpenFreeMap.Positron,
            ComposedGlyphsUrl: composedGlyphsUrl,
            Pitch: 45,
            WebFonts: ["11px 'Martian Mono'", "11px 'DM Sans'"]
        );
    }

    public static AnimationOptions TrainAnimation { get; } = new(Duration: 2000, Easing: AnimationEasing.EaseInOut);

    public static TrackedEntityClusterOptions TrackedTrainClusterOptions { get; } =
        new(
            Enabled: true,
            Radius: 64,
            MaxZoom: 12,
            MinPoints: 1,
            ClickBehavior: TrackedEntityClusterClickBehavior.ZoomToDissolve,
            Properties: new Dictionary<string, object>
            {
                ["internationalPresence"] = new object[] { "max", new object[] { "get", "internationalPresence" } },
            }
        );

    public static object[] TrainIconOpacityExpression { get; } =
    [
        "case",
        new object[] { "boolean", new object[] { "feature-state", TrackedEntityFeatureStates.Selected.Name }, false },
        1.0,
        new object[] { "boolean", new object[] { "feature-state", TrackedEntityFeatureStates.Hover.Name }, false },
        1.0,
        0.96,
    ];

    public static object[] OperatorOpacityExpression { get; } =
    [
        "case",
        new object[] { "boolean", new object[] { "feature-state", TrackedEntityFeatureStates.Selected.Name }, false },
        1.0,
        new object[] { "boolean", new object[] { "feature-state", TrackedEntityFeatureStates.Hover.Name }, false },
        1.0,
        0.0,
    ];

    public static double GetBearing(TrainSampleState train)
    {
        var lat1 = train.CurrentPosition.Latitude * Math.PI / 180.0;
        var lat2 = train.NextPosition.Latitude * Math.PI / 180.0;
        var deltaLon = (train.NextPosition.Longitude - train.CurrentPosition.Longitude) * Math.PI / 180.0;

        var y = Math.Sin(deltaLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);

        var bearingRadians = Math.Atan2(y, x);
        var bearingDegrees = bearingRadians * 180.0 / Math.PI;

        return (bearingDegrees - 90 + 360) % 360;
    }
}
