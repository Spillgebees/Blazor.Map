using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Models.TrackedData;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public static class TrainTrackingPresentation
{
    public const string OverlayStyleId = "sgb-train-tracking-overlay";
    public const string OverlayStyleUrlConfigurationKey = "Samples:TrainTracking:OverlayStyleUrl";
    public const string ComposedGlyphsUrlConfigurationKey = "Samples:TrainTracking:ComposedGlyphsUrl";
    public const string DefaultOverlayStyleUrl = "traintracking/style.json";
    public const string CodeSnippet =
        @"<SgbMap MapOptions=""@_mapOptions""
        Controls=""@_controls""
        OnMoveEnd=""@HandleMapViewChangedAsync""
        OnZoomEnd=""@HandleMapViewChangedAsync"">
    <TrackedDataSource @ref=""_trainSource""
                       Id=""train-source""
                       Items=""@_trains""
                       Identity=""@_trainIdentity""
                       Symbol=""@_trainSymbol""
                       Decorations=""@_trainDecorations""
                       Cluster=""@_trainClusterOptions""
                       Interaction=""@_trainInteraction""
                       Animation=""@(new AnimationOptions(Duration: 2000, Easing: AnimationEasing.EaseInOut))""
                       Visible=""@_visibility.ShowTrains""
                       PrimaryIconOpacity=""@_trainIconOpacityExpr""
                       OnItemClick=""@HandleTrainClick""
                       OnItemMouseEnter=""@HandleTrainHover""
                       OnItemMouseLeave=""@HandleTrainLeave"" />
</SgbMap>

// hover and selection use feature-state, labels stay screen-facing, and supplementary labels stay hidden while clustered";

    public static MapLegendDefinition OverlayLegendDefinition { get; } =
        new(
            [
                new MapLegendSectionDefinition(
                    "Map layers",
                    [
                        new MapLegendItemDefinition(
                            "3d-buildings",
                            "3D Buildings",
                            "Extruded building footprints.",
                            IsVisibleByDefault: true,
                            IsToggleable: true
                        ),
                        new MapLegendItemDefinition(
                            "trains",
                            "Trains",
                            "Live tracked train icons, labels, and clusters.",
                            IsVisibleByDefault: true,
                            IsToggleable: true
                        ),
                    ]
                ),
                new MapLegendSectionDefinition(
                    "Railway overlay",
                    [
                        new MapLegendItemDefinition(
                            "tracks",
                            "Tracks & tunnels",
                            "Rail lines, service tracks, tunnels, and railway areas.",
                            [
                                new MapLegendTargetDefinition(
                                    OverlayStyleId,
                                    [
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
                                        "railway-areas-outline",
                                    ]
                                ),
                            ],
                            true,
                            IsToggleable: true
                        ),
                        new MapLegendItemDefinition(
                            "tram",
                            "Tram & metro",
                            "Tram lines, stops, subway entrances, and crossings.",
                            [
                                new MapLegendTargetDefinition(
                                    OverlayStyleId,
                                    [
                                        "tram-line-fill",
                                        "tram-line-tunnel",
                                        "tram-stations-icon",
                                        "subway-entrance-icon",
                                        "tram-lifecycle-fill",
                                        "railway-tram-crossings-circle",
                                    ]
                                ),
                            ],
                            false,
                            IsToggleable: true
                        ),
                        new MapLegendItemDefinition(
                            "stations",
                            "Stations & borders",
                            "Railway stations, border crossings, and labels.",
                            [
                                new MapLegendTargetDefinition(
                                    OverlayStyleId,
                                    [
                                        "railway-stations-circle",
                                        "railway-stations-label",
                                        "railway-border-circle",
                                        "railway-border-label",
                                    ]
                                ),
                            ],
                            true,
                            IsToggleable: true
                        ),
                        new MapLegendItemDefinition(
                            "platforms",
                            "Platforms",
                            "Platform areas, 3D extrusions, and labels.",
                            [
                                new MapLegendTargetDefinition(
                                    OverlayStyleId,
                                    [
                                        "railway-platforms-fill",
                                        "railway-platforms-3d",
                                        "railway-platforms-label",
                                        "railway-platform-refs-label",
                                        "railway-platform-names-label",
                                    ]
                                ),
                            ],
                            true,
                            IsToggleable: true
                        ),
                        new MapLegendItemDefinition(
                            "routes",
                            "Routes",
                            "Named railway routes with color-coded lines.",
                            [
                                new MapLegendTargetDefinition(
                                    OverlayStyleId,
                                    ["railway-routes-casing", "railway-routes", "railway-routes-label"]
                                ),
                            ],
                            true,
                            IsToggleable: true
                        ),
                        new MapLegendItemDefinition(
                            "lifecycle",
                            "Lifecycle",
                            "Construction, proposed, disused, and preserved railways.",
                            [
                                new MapLegendTargetDefinition(
                                    OverlayStyleId,
                                    [
                                        "railway-lifecycle-construction",
                                        "railway-lifecycle-proposed",
                                        "railway-lifecycle-disused",
                                        "railway-lifecycle-abandoned",
                                        "railway-lifecycle-preserved",
                                        "railway-lifecycle-razed",
                                    ]
                                ),
                            ],
                            true,
                            IsToggleable: true
                        ),
                        new MapLegendItemDefinition(
                            "infrastructure",
                            "Infrastructure",
                            "Signals, switches, crossings, and track furniture.",
                            [
                                new MapLegendTargetDefinition(
                                    OverlayStyleId,
                                    [
                                        "railway-switches",
                                        "railway-signals",
                                        "railway-buffer-stops",
                                        "railway-milestones",
                                        "railway-turntables",
                                        "railway-derails",
                                        "railway-crossings-track",
                                        "railway-owner-change",
                                        "railway-crossings-circle",
                                    ]
                                ),
                            ],
                            false,
                            IsToggleable: true
                        ),
                    ]
                ),
            ],
            ClassName: "train-overlay-legend-content"
        );

    public static LegendMapControl OverlayLegendControl { get; } =
        new("overlay-legend", Position: ControlPosition.TopLeft);

    public static MapOptions BuildMapOptions(string? overlayStyleUrl, string? composedGlyphsUrl)
    {
        var resolvedOverlayStyleUrl = string.IsNullOrWhiteSpace(overlayStyleUrl)
            ? DefaultOverlayStyleUrl
            : overlayStyleUrl;
        var styles = new[]
        {
            MapStyle.OpenFreeMap.Positron,
            MapStyle.FromUrl(resolvedOverlayStyleUrl).WithId(OverlayStyleId),
        };

        return new(
            Center: new Coordinate(49.75, 6.12),
            Zoom: 8,
            Styles: styles,
            ComposedGlyphsUrl: composedGlyphsUrl,
            Pitch: 45,
            WebFonts: ["11px 'Martian Mono'", "11px 'DM Sans'"]
        );
    }

    public static IReadOnlyList<MapControl> Controls { get; } = [new NavigationMapControl(), OverlayLegendControl];

    public static AnimationOptions TrainAnimation { get; } = new(Duration: 2000, Easing: AnimationEasing.EaseInOut);

    public static TrackedDataClusterOptions TrackedTrainClusterOptions { get; } =
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
