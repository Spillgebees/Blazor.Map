namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public static class TrainTrackingPresentation
{
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
         <LegendMapControl Id=""overlay-legend""
                           Position=""@ControlPosition.TopLeft""
                           Title=""Legend""
                           Definition=""@TrainTrackingPresentation.OverlayLegendDefinition"" />
     </MapControls>
     <MapFeatures>
         <TrackedEntityLayer TItem=""TrainSampleState"" Layer=""@_trackedEntityLayer"" />
     </MapFeatures>
   </SgbMap>

// hover and selection use feature-state, labels stay screen-facing, and supplementary labels stay hidden while clustered";

    public static MapLegend OverlayLegendDefinition { get; } =
        new(
            [
                new MapLegendSection(
                    "Map layers",
                    [
                        new MapLegendItem(
                            "3d-buildings",
                            "3D Buildings",
                            "Extruded building footprints.",
                            "3d-buildings"
                        ),
                        new MapLegendItem(
                            "trains",
                            "Trains",
                            "Live tracked train icons, labels, and clusters.",
                            "trains"
                        ),
                    ]
                ),
                new MapLegendSection(
                    "Railway overlay",
                    [
                        new MapLegendItem(
                            "tracks",
                            "Tracks & tunnels",
                            "Rail lines, service tracks, tunnels, and railway areas.",
                            "tracks"
                        ),
                        new MapLegendItem(
                            "tram",
                            "Tram & metro",
                            "Tram lines, stops, subway entrances, and crossings.",
                            "tram"
                        ),
                        new MapLegendItem(
                            "stations",
                            "Stations & borders",
                            "Railway stations, border crossings, and labels.",
                            "stations"
                        ),
                        new MapLegendItem(
                            "platforms",
                            "Platforms",
                            "Platform areas, 3D extrusions, and labels.",
                            "platforms"
                        ),
                        new MapLegendItem("routes", "Routes", "Named railway routes with color-coded lines.", "routes"),
                        new MapLegendItem(
                            "lifecycle",
                            "Lifecycle",
                            "Construction, proposed, disused, and preserved railways.",
                            "lifecycle"
                        ),
                        new MapLegendItem(
                            "infrastructure",
                            "Infrastructure",
                            "Signals, switches, crossings, and track furniture.",
                            "infrastructure"
                        ),
                    ]
                ),
            ],
            ClassName: "train-overlay-legend-content"
        );

    public static MapDisplayState CreateDisplay() =>
        new([
            new MapDisplayItem(
                "3d-buildings",
                [MapDisplayTarget.RuntimeLayers("sgb-buildings-3d")],
                Label: "3D Buildings"
            ),
            new MapDisplayItem(
                "trains",
                [
                    MapDisplayTarget.RuntimeLayers(
                        "train-source-clusters",
                        "train-source-cluster-count",
                        "train-source-symbols",
                        "train-source-decoration-service",
                        "train-source-decoration-route",
                        "train-source-decoration-operator"
                    ),
                ],
                Label: "Trains"
            ),
            new MapDisplayItem(
                "tracks",
                [
                    MapDisplayTarget.StyleLayers(
                        OverlayStyleId,
                        "railway-line-rail",
                        "railway-line-light-rail",
                        "railway-line-subway",
                        "railway-line-narrow-gauge",
                        "railway-line-funicular",
                        "railway-line-monorail",
                        "railway-line-miniature",
                        "railway-line-service",
                        "railway-line-tunnel",
                        "railway-tunnel-label",
                        "railway-areas-fill",
                        "railway-areas-outline"
                    ),
                ],
                Label: "Tracks & tunnels"
            ),
            new MapDisplayItem(
                "tram",
                [
                    MapDisplayTarget.StyleLayers(
                        OverlayStyleId,
                        "tram-line-fill",
                        "tram-line-tunnel",
                        "tram-stations-icon",
                        "subway-entrance-icon",
                        "tram-lifecycle-fill",
                        "railway-tram-crossings-circle"
                    ),
                ],
                IsOn: false,
                Label: "Tram & metro"
            ),
            new MapDisplayItem(
                "stations",
                [
                    MapDisplayTarget.StyleLayers(
                        OverlayStyleId,
                        "railway-stations-circle",
                        "railway-stations-label",
                        "railway-border-circle",
                        "railway-border-label"
                    ),
                ],
                Label: "Stations & borders"
            ),
            new MapDisplayItem(
                "platforms",
                [
                    MapDisplayTarget.StyleLayers(
                        OverlayStyleId,
                        "railway-platforms-fill",
                        "railway-platforms-3d",
                        "railway-platforms-label",
                        "railway-platform-refs-label",
                        "railway-platform-names-label"
                    ),
                ],
                Label: "Platforms"
            ),
            new MapDisplayItem(
                "routes",
                [
                    MapDisplayTarget.StyleLayers(
                        OverlayStyleId,
                        "railway-routes-casing",
                        "railway-routes",
                        "railway-routes-label"
                    ),
                ],
                Label: "Routes"
            ),
            new MapDisplayItem(
                "lifecycle",
                [
                    MapDisplayTarget.StyleLayers(
                        OverlayStyleId,
                        "railway-lifecycle-construction",
                        "railway-lifecycle-proposed",
                        "railway-lifecycle-disused",
                        "railway-lifecycle-abandoned",
                        "railway-lifecycle-preserved",
                        "railway-lifecycle-razed"
                    ),
                ],
                Label: "Lifecycle"
            ),
            new MapDisplayItem(
                "infrastructure",
                [
                    MapDisplayTarget.StyleLayers(
                        OverlayStyleId,
                        "railway-switches",
                        "railway-signals",
                        "railway-buffer-stops",
                        "railway-milestones",
                        "railway-turntables",
                        "railway-derails",
                        "railway-crossings-track",
                        "railway-owner-change",
                        "railway-crossings-circle"
                    ),
                ],
                IsOn: false,
                Label: "Infrastructure"
            ),
        ]);

    public static IReadOnlyList<MapStyle> BuildStyles(string? overlayStyleUrl)
    {
        var resolvedOverlayStyleUrl = string.IsNullOrWhiteSpace(overlayStyleUrl)
            ? DefaultOverlayStyleUrl
            : overlayStyleUrl;
        return [MapStyle.OpenFreeMap.Positron, MapStyle.FromUrl(resolvedOverlayStyleUrl).WithId(OverlayStyleId)];
    }

    public static IReadOnlyList<string> WebFonts { get; } = ["11px 'Martian Mono'", "11px 'DM Sans'"];

    public static TimeSpan TrainAnimation { get; } = TimeSpan.FromMilliseconds(2000);

    public static ClusterOptions TrackedTrainClusterOptions { get; } =
        ClusterOptions.Create(
            radius: 64,
            maxZoom: 12,
            minPoints: 1,
            properties: new Dictionary<string, object>
            {
                ["internationalPresence"] = new object[] { "max", new object[] { "get", "internationalPresence" } },
            },
            clickBehavior: ClusterClickBehavior.ZoomToDissolve
        );

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
