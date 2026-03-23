import { expect, test } from "@playwright/test";
import type { Page } from "@playwright/test";

test.describe.configure({ mode: "serial" });

type BrowserLogSnapshot = {
  consoleErrors: string[];
  failedResponses: string[];
  pageErrors: string[];
};

type TrainShowcaseLayerSnapshot = {
  composedStyleCount: number;
  decorationOpacityExpression: unknown;
  overlayStyleUrl: string | null;
  overlayStationLayerId: string | null;
  overlayStationCount: number;
  overlayStationLabelLayerId: string | null;
  overlayStationLabelCount: number;
  clusterColorExpression: unknown;
  clusterCountPitchAlignment: unknown;
  clusterCountRotationAlignment: unknown;
  clusterHitAreaPitchAlignment: unknown;
  clusterPitchAlignment: unknown;
  clusterProperties: unknown;
  hasClusterHitArea: boolean;
  hasTrainIconsLayer: boolean;
  iconSizeExpression: unknown;
  serviceLabelsPitchAlignment: unknown;
  serviceLabelsRotationAlignment: unknown;
  serviceTextOffset: unknown;
  serviceFeatureCountInSource: number;
  textRotationExpression: unknown;
  overlayTopOrder: number;
  trainClusterOrder: number;
  trainClusterVisibility: unknown;
  trainRouteVisibility: unknown;
  trainServiceVisibility: unknown;
  trainOperatorVisibility: unknown;
  trainIconsVisibility: unknown;
  trainHitAreaRadius: unknown;
};

type ToggleVisibilitySnapshot = {
  tracksVisibility: unknown;
  tramVisibility: unknown;
  stationsVisibility: unknown;
  platformsVisibility: unknown;
  routesVisibility: unknown;
  lifecycleVisibility: unknown;
  infrastructureVisibility: unknown;
  trainClusterVisibility: unknown;
};

type ToggleVisibilityKey = Exclude<keyof ToggleVisibilitySnapshot, "trainClusterVisibility">;

type OverlayGroupExpectation = {
  checkedByDefault: boolean;
  label: string;
  keys: ToggleVisibilityKey[];
};

const overlayGroups = [
  { checkedByDefault: true, label: "Tracks & tunnels", keys: ["tracksVisibility"] },
  { checkedByDefault: false, label: "Tram & metro", keys: ["tramVisibility"] },
  { checkedByDefault: true, label: "Stations & borders", keys: ["stationsVisibility"] },
  { checkedByDefault: true, label: "Platforms", keys: ["platformsVisibility"] },
  { checkedByDefault: true, label: "Routes", keys: ["routesVisibility"] },
  { checkedByDefault: true, label: "Lifecycle", keys: ["lifecycleVisibility"] },
  { checkedByDefault: false, label: "Infrastructure", keys: ["infrastructureVisibility"] },
] satisfies OverlayGroupExpectation[];

function buildToggleSnapshotExpectation(overrides: Partial<ToggleVisibilitySnapshot> = {}): ToggleVisibilitySnapshot {
  return {
    tracksVisibility: "visible",
    tramVisibility: "none",
    stationsVisibility: "visible",
    platformsVisibility: "visible",
    routesVisibility: "visible",
    lifecycleVisibility: "visible",
    infrastructureVisibility: "none",
    trainClusterVisibility: "visible",
    ...overrides,
  };
}

type RenderedFeatureSnapshot = {
  clusterCount: number;
  serviceCount: number;
  trainIconCount: number;
};

type ProjectedLayerFeaturePoint = {
  entityId: string | null;
  layerId: string;
  x: number;
  y: number;
};

const trainInteractiveLayerIds = [
  "train-source-operator-right",
  "train-source-route-left",
  "train-source-service-left",
  "train-source-hit-area",
  "train-source-symbols",
] as const;

type TrainInteractionSnapshot = {
  hoveredDecorationCount: number;
  hoveredOperatorLabelCount: number;
  hoveredRouteLabelCount: number;
  operatorLabelCount: number;
  routeLabelCount: number;
  selectedOperatorLabelCount: number;
  selectedTrainPanelVisible: boolean;
  zoom: number;
};

const trainShowcaseMapSelector = ".train-showcase-map .sgb-map-container";

async function mockLuxembourgOverlayStyle(page: Page): Promise<void> {
  await page.route(/.*\/fonts\/.*\.pbf(?:\?.*)?$/, async (route) => {
    // arrange

    // act
    await route.fulfill({
      status: 200,
      contentType: "application/x-protobuf",
      body: "",
    });

    // assert
  });

  await page.route(/.*(?:style\.json)$/, async (route) => {
    // arrange
    const requestUrl = route.request().url();

    // act
    if (requestUrl.includes("openfreemap.org") || requestUrl.includes("openmaptiles") || requestUrl.includes("maptiler") || requestUrl.includes("tiles.spillgebees.dev")) {
      await route.fallback();
      return;
    }

    // assert
    await route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({
        version: 8,
        sources: {
          railway: {
            type: "geojson",
            data: {
              type: "FeatureCollection",
              features: [
                {
                  type: "Feature",
                  properties: { railway: "rail", service: "" },
                  geometry: {
                    type: "LineString",
                    coordinates: [
                      [6.08, 49.61],
                      [6.16, 49.69],
                    ],
                  },
                },
                {
                  type: "Feature",
                  properties: {
                    railway: "station",
                    name: "Luxembourg",
                    railway_ref: "LUX",
                    operator: "CFL",
                  },
                  geometry: {
                    type: "Point",
                    coordinates: [6.1333, 49.6],
                  },
                },
                {
                  type: "Feature",
                  properties: {
                    railway: "halt",
                    name: "Ettelbruck",
                    railway_ref: "ETT",
                    operator: "CFL",
                  },
                  geometry: {
                    type: "Point",
                    coordinates: [6.1069, 49.8478],
                  },
                },
                {
                  type: "Feature",
                  properties: {
                    ref: "1",
                  },
                  geometry: {
                    type: "Polygon",
                    coordinates: [
                      [
                        [6.1315, 49.5994],
                        [6.1341, 49.5994],
                        [6.1341, 49.6001],
                        [6.1315, 49.6001],
                        [6.1315, 49.5994],
                      ],
                    ],
                  },
                },
                {
                  type: "Feature",
                  properties: {
                    railway: "tram",
                    service: "",
                  },
                  geometry: {
                    type: "LineString",
                    coordinates: [
                      [6.11, 49.62],
                      [6.18, 49.67],
                    ],
                  },
                },
                {
                  type: "Feature",
                  properties: {
                    railway: "railway_crossing",
                  },
                  geometry: {
                    type: "Point",
                    coordinates: [6.0815, 49.6115],
                  },
                },
                {
                  type: "Feature",
                  properties: {
                    railway: "level_crossing",
                  },
                  geometry: {
                    type: "Point",
                    coordinates: [6.0855, 49.613],
                  },
                },
                {
                  type: "Feature",
                  properties: {
                    railway: "switch",
                  },
                  geometry: {
                    type: "Point",
                    coordinates: [6.1088, 49.844],
                  },
                },
              ],
            },
          },
        },
        layers: [
          {
            id: "railway-line-rail",
            type: "line",
            source: "railway",
            filter: ["all", ["!=", ["get", "railway"], "tram"], ["==", ["get", "service"], ""]],
            layout: {
              visibility: "visible",
            },
            paint: {
              "line-color": "#334155",
              "line-width": 2,
            },
          },
          {
            id: "tram-line-fill",
            type: "line",
            source: "railway",
            filter: ["==", ["get", "railway"], "tram"],
            layout: {
              visibility: "none",
            },
            paint: {
              "line-color": "#dc2626",
              "line-width": 2,
            },
          },
          {
            id: "railway-platforms-fill",
            type: "fill",
            source: "railway",
            layout: {
              visibility: "visible",
            },
            paint: {
              "fill-color": "#a78bfa",
              "fill-opacity": 0.5,
            },
          },
          {
            id: "railway-platforms-3d",
            type: "fill-extrusion",
            source: "railway",
            layout: {
              visibility: "visible",
            },
            paint: {
              "fill-extrusion-color": "#a78bfa",
              "fill-extrusion-opacity": 0.7,
              "fill-extrusion-height": 1.5,
            },
          },
          {
            id: "railway-platforms-label",
            type: "symbol",
            source: "railway",
            layout: {
              visibility: "visible",
              "text-field": ["get", "ref"],
            },
            paint: {
              "text-color": "#7c3aed",
            },
          },
          {
            id: "railway-routes-casing",
            type: "line",
            source: "railway",
            layout: {
              visibility: "visible",
            },
            paint: {
              "line-color": "#ffffff",
              "line-width": 4,
            },
          },
          {
            id: "railway-routes",
            type: "line",
            source: "railway",
            layout: {
              visibility: "visible",
            },
            paint: {
              "line-color": "#4f46e5",
              "line-width": 2,
            },
          },
          {
            id: "railway-routes-label",
            type: "symbol",
            source: "railway",
            layout: {
              visibility: "visible",
              "text-field": "RE10",
            },
            paint: {
              "text-color": "#4f46e5",
            },
          },
          {
            id: "railway-border-circle",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "border"],
            layout: {
              visibility: "visible",
            },
            paint: {
              "circle-color": "#ffffff",
              "circle-radius": 4,
              "circle-stroke-color": "#1e293b",
              "circle-stroke-width": 2,
              "circle-pitch-alignment": "viewport",
            },
          },
          {
            id: "railway-border-label",
            type: "symbol",
            source: "railway",
            filter: ["==", ["get", "railway"], "border"],
            layout: {
              visibility: "visible",
              "text-field": ["coalesce", ["get", "uic_name"], ["get", "name"]],
            },
            paint: {
              "text-color": "#64748b",
            },
          },
          {
            id: "railway-stations-circle",
            type: "circle",
            source: "railway",
            filter: ["in", ["get", "railway"], ["literal", ["station", "halt"]]],
            layout: {
              visibility: "visible",
            },
            paint: {
              "circle-color": "#e74c3c",
              "circle-radius": 5,
              "circle-stroke-color": "#ffffff",
              "circle-stroke-width": 2,
              "circle-pitch-alignment": "viewport",
            },
          },
          {
            id: "railway-stations-label",
            type: "symbol",
            source: "railway",
            filter: ["in", ["get", "railway"], ["literal", ["station", "halt"]]],
            layout: {
              visibility: "visible",
              "text-field": ["get", "name"],
            },
            paint: {
              "text-color": "#1e3a5f",
            },
          },
          {
            id: "tram-stations-circle",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "tram_stop"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#e74c3c",
              "circle-radius": 4,
            },
          },
          {
            id: "railway-tram-stops-label",
            type: "symbol",
            source: "railway",
            filter: ["==", ["get", "railway"], "tram_stop"],
            layout: {
              visibility: "none",
              "text-field": ["get", "name"],
            },
            paint: {
              "text-color": "#475569",
            },
          },
          {
            id: "railway-switches",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "switch"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#f59e0b",
              "circle-radius": 3,
            },
          },
          {
            id: "railway-signals",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "signal"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#dc2626",
              "circle-radius": 2,
            },
          },
          {
            id: "railway-buffer-stops",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "buffer_stop"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#334155",
              "circle-radius": 2,
            },
          },
          {
            id: "railway-milestones",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "milestone"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#94a3b8",
              "circle-radius": 2,
            },
          },
          {
            id: "railway-turntables",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "turntable"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#92400e",
              "circle-radius": 3,
            },
          },
          {
            id: "railway-derails",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "derail"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#ea580c",
              "circle-radius": 2,
            },
          },
          {
            id: "railway-owner-change",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "owner_change"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#1d4ed8",
              "circle-radius": 3,
            },
          },
          {
            id: "railway-crossings-track",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "railway_crossing"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#475569",
              "circle-radius": 3,
            },
          },
          {
            id: "railway-crossings-circle",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "level_crossing"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#ffffff",
              "circle-radius": 4,
              "circle-stroke-color": "#dc2626",
              "circle-stroke-width": 2,
            },
          },
          {
            id: "railway-tram-crossings-circle",
            type: "circle",
            source: "railway",
            filter: ["==", ["get", "railway"], "tram_crossing"],
            layout: {
              visibility: "none",
            },
            paint: {
              "circle-color": "#ffffff",
              "circle-radius": 3,
              "circle-stroke-color": "#d97706",
              "circle-stroke-width": 1.5,
            },
          },
          {
            id: "railway-areas-fill",
            type: "fill",
            source: "railway",
            layout: {
              visibility: "none",
            },
            paint: {
              "fill-color": "#f5f0e8",
              "fill-opacity": 0.3,
            },
          },
          {
            id: "railway-areas-outline",
            type: "line",
            source: "railway",
            layout: {
              visibility: "none",
            },
            paint: {
              "line-color": "#d6cebe",
              "line-width": 1,
            },
          },
          {
            id: "railway-line-service",
            type: "line",
            source: "railway",
            filter: ["has", "service"],
            layout: {
              visibility: "none",
            },
            paint: {
              "line-color": "#64748b",
              "line-width": 1,
            },
          },
          {
            id: "railway-line-tunnel",
            type: "line",
            source: "railway",
            filter: ["==", ["get", "tunnel"], "yes"],
            layout: {
              visibility: "none",
            },
            paint: {
              "line-color": "#7c8db5",
              "line-width": 2,
            },
          },
          {
            id: "railway-tunnel-label",
            type: "symbol",
            source: "railway",
            layout: {
              visibility: "none",
              "text-field": ["get", "tunnel_name"],
            },
            paint: {
              "text-color": "#5b6e8a",
            },
          },
          {
            id: "tram-line-tunnel",
            type: "line",
            source: "railway",
            filter: ["all", ["==", ["get", "railway"], "tram"], ["==", ["get", "tunnel"], "yes"]],
            layout: {
              visibility: "none",
            },
            paint: {
              "line-color": "#f87171",
              "line-width": 2,
            },
          },
          {
            id: "railway-lifecycle-fill",
            type: "line",
            source: "railway",
            layout: {
              visibility: "visible",
            },
            paint: {
              "line-color": "#a8a29e",
              "line-width": 2,
            },
          },
          {
            id: "tram-lifecycle-fill",
            type: "line",
            source: "railway",
            layout: {
              visibility: "none",
            },
            paint: {
              "line-color": "#a8a29e",
              "line-width": 2,
            },
          },
        ],
      }),
    });
  });
}

async function readTrainShowcaseLayerSnapshot(page: Page): Promise<TrainShowcaseLayerSnapshot> {
  return page.evaluate<TrainShowcaseLayerSnapshot>(() => {
    const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

    if (!map) {
      throw new Error("Train showcase map was not initialized.");
    }

    const sourceRegistrations = window.Spillgebees.Map.sourceSpecs.get(map);
    const orderedLayerIds = map.getStyle().layers.map((layer) => layer.id);
    const mapOptions = window.Spillgebees.Map.mapOptions.get(map);
    const overlayStyleUrl = window.Spillgebees.Map.overlayStyleUrls.get(map)?.at(0) ?? null;
    const overlayTopOrder = orderedLayerIds.reduce((currentMax, layerId, index) => {
      return layerId.startsWith("sgb-overlay-style-") ? Math.max(currentMax, index) : currentMax;
    }, -1);
    const overlayStationLayerId = orderedLayerIds.find((layerId) => layerId.includes("railway-stations-circle")) ?? null;
    const overlayStationLabelLayerId = orderedLayerIds.find((layerId) => layerId.includes("railway-stations-label")) ?? null;
    const canvas = map.getCanvas();
    const bounds: [[number, number], [number, number]] = [[0, 0], [canvas.clientWidth, canvas.clientHeight]];

    return {
      composedStyleCount: mapOptions?.styles?.length ?? 0,
      decorationOpacityExpression: map.getPaintProperty("train-source-service-left", "text-opacity"),
      overlayStyleUrl,
      overlayStationLayerId,
      overlayStationCount:
        overlayStationLayerId === null ? 0 : map.queryRenderedFeatures(bounds, { layers: [overlayStationLayerId] }).length,
      overlayStationLabelLayerId,
      overlayStationLabelCount:
        overlayStationLabelLayerId === null
          ? 0
          : map.queryRenderedFeatures(bounds, { layers: [overlayStationLabelLayerId] }).length,
      clusterColorExpression: map.getPaintProperty("train-source-clusters", "circle-color"),
      clusterCountPitchAlignment: map.getLayoutProperty("train-source-cluster-count", "text-pitch-alignment"),
      clusterCountRotationAlignment: map.getLayoutProperty("train-source-cluster-count", "text-rotation-alignment"),
      clusterHitAreaPitchAlignment: map.getPaintProperty("train-source-cluster-hit-area", "circle-pitch-alignment"),
      clusterPitchAlignment: map.getPaintProperty("train-source-clusters", "circle-pitch-alignment"),
      clusterProperties: sourceRegistrations?.get("train-source")?.sourceSpec.clusterProperties ?? null,
      hasClusterHitArea: map.getLayer("train-source-cluster-hit-area") !== undefined,
      hasTrainIconsLayer: map.getLayer("train-source-symbols") !== undefined,
      iconSizeExpression: map.getLayoutProperty("train-source-symbols", "icon-size"),
      serviceLabelsPitchAlignment: map.getLayoutProperty("train-source-service-left", "text-pitch-alignment"),
      serviceLabelsRotationAlignment: map.getLayoutProperty("train-source-service-left", "text-rotation-alignment"),
      serviceTextOffset: map.getLayoutProperty("train-source-service-left", "text-offset"),
      serviceFeatureCountInSource: Array.from(
        ((sourceRegistrations?.get("train-source-decorations")?.sourceSpec.data as { features?: Array<{ properties?: Record<string, unknown> }> })
          ?.features ?? []),
      ).filter((feature) => feature.properties?.decorationId === "service").length,
      textRotationExpression: map.getLayoutProperty("train-source-service-left", "text-rotate"),
      overlayTopOrder,
      trainClusterOrder: orderedLayerIds.indexOf("train-source-clusters"),
      trainClusterVisibility: map.getLayoutProperty("train-source-clusters", "visibility"),
      trainRouteVisibility: map.getLayoutProperty("train-source-route-left", "visibility"),
      trainServiceVisibility: map.getLayoutProperty("train-source-service-left", "visibility"),
      trainOperatorVisibility: map.getLayoutProperty("train-source-operator-right", "visibility"),
      trainIconsVisibility: map.getLayoutProperty("train-source-symbols", "visibility"),
      trainHitAreaRadius: map.getPaintProperty("train-source-hit-area", "circle-radius"),
    };
  });
}

async function waitForTrainShowcaseReady(page: Page): Promise<void> {
  await expect(page.getByRole("heading", { name: "Live network snapshot" })).toBeVisible({ timeout: 15_000 });
  await expect(page.locator(".train-showcase-map .maplibregl-canvas")).toBeVisible({ timeout: 15_000 });
  await expect
    .poll(
      () =>
        page.evaluate(() => {
          const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
          const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

          if (!map) {
            return {
              hasClusterLayer: false,
              hasHitAreaLayer: false,
              hasIconLayer: false,
            };
          }

          return {
            hasClusterLayer: map.getLayer("train-source-clusters") !== undefined,
            hasHitAreaLayer: map.getLayer("train-source-hit-area") !== undefined,
            hasIconLayer: map.getLayer("train-source-symbols") !== undefined,
          };
        }),
      { timeout: 20_000 },
    )
    .toEqual({
      hasClusterLayer: true,
      hasHitAreaLayer: true,
      hasIconLayer: true,
    });
}

async function readToggleVisibilitySnapshot(page: Page): Promise<ToggleVisibilitySnapshot> {
  return page.evaluate<ToggleVisibilitySnapshot>(() => {
    const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

    if (!map) {
      throw new Error("Train showcase map was not initialized.");
    }

    return {
      tracksVisibility: map.getLayoutProperty("sgb-overlay-style-sgb-train-tracking-overlay-railway-line-rail", "visibility"),
      tramVisibility: map.getLayoutProperty("sgb-overlay-style-sgb-train-tracking-overlay-tram-line-fill", "visibility"),
      stationsVisibility: map.getLayoutProperty("sgb-overlay-style-sgb-train-tracking-overlay-railway-stations-circle", "visibility"),
      platformsVisibility: map.getLayoutProperty("sgb-overlay-style-sgb-train-tracking-overlay-railway-platforms-fill", "visibility"),
      routesVisibility: map.getLayoutProperty("sgb-overlay-style-sgb-train-tracking-overlay-railway-routes", "visibility"),
      lifecycleVisibility: map.getLayoutProperty("sgb-overlay-style-sgb-train-tracking-overlay-railway-lifecycle-fill", "visibility"),
      infrastructureVisibility: map.getLayoutProperty("sgb-overlay-style-sgb-train-tracking-overlay-railway-switches", "visibility"),
      trainClusterVisibility: map.getLayoutProperty("train-source-clusters", "visibility"),
    };
  });
}

async function readRenderedFeatureSnapshot(page: Page): Promise<RenderedFeatureSnapshot> {
  return page.evaluate<RenderedFeatureSnapshot>(() => {
    const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

    if (!map) {
      throw new Error("Train showcase map was not initialized.");
    }

    const canvas = map.getCanvas();
    const width = canvas.clientWidth || canvas.width;
    const height = canvas.clientHeight || canvas.height;
    const bounds: [[number, number], [number, number]] = [[0, 0], [width, height]];

    return {
      clusterCount: map.queryRenderedFeatures(bounds, { layers: ["train-source-clusters"] }).length,
      serviceCount: map.queryRenderedFeatures(bounds, { layers: ["train-source-service-left"] }).length,
      trainIconCount: map.queryRenderedFeatures(bounds, { layers: ["train-source-symbols"] }).length,
    };
  });
}

async function readProjectedLayerFeaturePoint(page: Page, layerId: string): Promise<ProjectedLayerFeaturePoint> {
  return page.evaluate<ProjectedLayerFeaturePoint, string>((requestedLayerId) => {
    const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

    if (!map) {
      throw new Error("Train showcase map was not initialized.");
    }

    const canvas = map.getCanvas();
    const features = map.queryRenderedFeatures([[0, 0], [canvas.clientWidth, canvas.clientHeight]], {
      layers: [requestedLayerId],
    });
    const feature = features.at(0);

    if (!feature || feature.geometry.type !== "Point") {
      throw new Error(`No rendered point feature was found for layer '${requestedLayerId}'.`);
    }

    const [longitude, latitude] = feature.geometry.coordinates as [number, number];
    const point = map.project([longitude, latitude]);
    const candidateOffsets: Array<[number, number]> = [
      [0, 0],
      [0, -8],
      [0, 8],
      [-8, 0],
      [8, 0],
      [-6, -6],
      [6, -6],
      [-6, 6],
      [6, 6],
      [0, -12],
      [0, 12],
      [-12, 0],
      [12, 0],
    ];

    const resolvedPoint =
      candidateOffsets
        .map(([offsetX, offsetY]) => ({ x: point.x + offsetX, y: point.y + offsetY }))
        .find((candidatePoint) => {
          const topFeature = map.queryRenderedFeatures(
            [
              [candidatePoint.x, candidatePoint.y],
              [candidatePoint.x, candidatePoint.y],
            ],
          )[0];

          return topFeature?.layer.id === requestedLayerId;
        }) ?? point;

    return {
      entityId: typeof feature.properties?.entityId === "string" ? feature.properties.entityId : null,
      layerId: requestedLayerId,
      x: resolvedPoint.x,
      y: resolvedPoint.y,
    };
  }, layerId);
}

async function readProjectedTrainInteractionPoint(page: Page): Promise<ProjectedLayerFeaturePoint> {
  return page.evaluate<ProjectedLayerFeaturePoint, readonly string[]>((requestedLayerIds) => {
    const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

    if (!map) {
      throw new Error("Train showcase map was not initialized.");
    }

    const canvas = map.getCanvas();
    const candidateFeatures = map.queryRenderedFeatures([[0, 0], [canvas.clientWidth, canvas.clientHeight]], {
      layers: [...requestedLayerIds],
    });
    const feature = candidateFeatures.find(
      (candidate) => candidate.geometry.type === "Point" && typeof candidate.properties?.entityId === "string",
    );

    if (!feature || feature.geometry.type !== "Point") {
      throw new Error("No rendered interactive train feature was found.");
    }

    const [longitude, latitude] = feature.geometry.coordinates as [number, number];
    const point = map.project([longitude, latitude]);
    const candidateOffsets: Array<[number, number]> = [
      [0, 0],
      [0, -8],
      [0, 8],
      [-8, 0],
      [8, 0],
      [-6, -6],
      [6, -6],
      [-6, 6],
      [6, 6],
      [0, -12],
      [0, 12],
      [-12, 0],
      [12, 0],
    ];

    const resolvedPoint = candidateOffsets
      .map(([offsetX, offsetY]) => ({ x: point.x + offsetX, y: point.y + offsetY }))
      .find((candidatePoint) => {
        const topFeature = map.queryRenderedFeatures(
          [
            [candidatePoint.x, candidatePoint.y],
            [candidatePoint.x, candidatePoint.y],
          ],
          { layers: [...requestedLayerIds] },
        )[0];

        return (
          topFeature?.geometry.type === "Point"
          && typeof topFeature.properties?.entityId === "string"
          && requestedLayerIds.includes(topFeature.layer.id)
        );
      });

    if (!resolvedPoint) {
      throw new Error("Could not resolve a stable interactive train point.");
    }

    const topFeature = map.queryRenderedFeatures(
      [
        [resolvedPoint.x, resolvedPoint.y],
        [resolvedPoint.x, resolvedPoint.y],
      ],
      { layers: [...requestedLayerIds] },
    )[0];

    if (
      !topFeature
      || topFeature.geometry.type !== "Point"
      || typeof topFeature.properties?.entityId !== "string"
      || !requestedLayerIds.includes(topFeature.layer.id)
    ) {
      throw new Error("Resolved train interaction point did not remain on a train-owned interactive feature.");
    }

    return {
      entityId: topFeature.properties.entityId,
      layerId: topFeature.layer.id,
      x: resolvedPoint.x,
      y: resolvedPoint.y,
    };
  }, trainInteractiveLayerIds);
}

async function readTrainInteractionSnapshot(page: Page): Promise<TrainInteractionSnapshot> {
  return page.evaluate<TrainInteractionSnapshot>(() => {
    const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

    if (!map) {
      throw new Error("Train showcase map was not initialized.");
    }

    const decorationSourceRegistration = window.Spillgebees.Map.sourceSpecs.get(map)?.get("train-source-decorations");
    const decorationData = decorationSourceRegistration?.sourceSpec.data as
      | { features?: Array<{ id?: string | number; properties?: Record<string, unknown> }> }
      | undefined;
    const decorationFeatures = decorationData?.features ?? [];
    // With promoteId="entityId", feature-state is keyed by entityId.
    // Extract unique entity IDs and check their state directly.
    const entityIds = new Set(
      decorationFeatures
        .map((f) => f.properties?.entityId)
        .filter((id): id is string => typeof id === "string"),
    );

    const getEntityState = (entityId: string) =>
      map.getFeatureState({ source: "train-source-decorations", id: entityId }) as {
        hover?: boolean;
        selected?: boolean;
      };

    const hoveredEntityIds = new Set([...entityIds].filter((id) => getEntityState(id).hover === true));
    const selectedEntityIds = new Set([...entityIds].filter((id) => getEntityState(id).selected === true));

    const hoveredDecorationCount = [...entityIds].reduce((count, entityId) => {
      if (!getEntityState(entityId).hover) return count;
      return count + decorationFeatures.filter((f) => f.properties?.entityId === entityId).length;
    }, 0);

    const hoveredRouteLabelCount = new Set(
      decorationFeatures
        .filter((f) => f.properties?.decorationId === "route" && typeof f.properties?.entityId === "string" && hoveredEntityIds.has(f.properties.entityId as string))
        .map((f) => f.properties?.entityId),
    ).size;

    const hoveredOperatorLabelCount = new Set(
      decorationFeatures
        .filter((f) => f.properties?.decorationId === "operator" && typeof f.properties?.entityId === "string" && hoveredEntityIds.has(f.properties.entityId as string))
        .map((f) => f.properties?.entityId),
    ).size;

    const selectedOperatorLabelCount = new Set(
      decorationFeatures
        .filter((f) => f.properties?.decorationId === "operator" && typeof f.properties?.entityId === "string" && selectedEntityIds.has(f.properties.entityId as string))
        .map((f) => f.properties?.entityId),
    ).size;
    return {
      hoveredDecorationCount,
      hoveredOperatorLabelCount,
      hoveredRouteLabelCount,
      operatorLabelCount: map.queryRenderedFeatures([[0, 0], [map.getCanvas().clientWidth, map.getCanvas().clientHeight]], {
        layers: ["train-source-operator-right"],
      }).length,
      routeLabelCount: map.queryRenderedFeatures([[0, 0], [map.getCanvas().clientWidth, map.getCanvas().clientHeight]], {
        layers: ["train-source-route-left"],
      }).length,
      selectedOperatorLabelCount,
      selectedTrainPanelVisible:
        Array.from(document.querySelectorAll(".train-card-label")).some((element) => element.textContent?.includes("Selected train") ?? false),
      zoom: map.getZoom(),
    };
  });
}

async function zoomShowcaseMapTo(page: Page, zoom: number): Promise<void> {
  await page.evaluate<number>((targetZoom) => {
    const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;
    map?.jumpTo({ zoom: targetZoom });
  }, zoom);
}

async function zoomShowcaseIntoUnclusteredMode(page: Page): Promise<void> {
  const zoomInButton = page.locator(".train-showcase-map").getByRole("button", { name: "Zoom in" }).first();

  await expect(zoomInButton).toBeVisible({ timeout: 15_000 });

  for (let index = 0; index < 6; index++) {
    const snapshot = await readRenderedFeatureSnapshot(page);
    const zoom = await page.evaluate(() => {
      const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
      const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

      return map?.getZoom() ?? 0;
    });

    if (snapshot.clusterCount === 0 && snapshot.trainIconCount > 0 && zoom >= 13) {
      return;
    }

    await zoomInButton.click();
  }

  await expect
    .poll(
      async () => ({
        ...(await readRenderedFeatureSnapshot(page)),
        zoom: await page.evaluate(() => {
          const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
          const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

          return map?.getZoom() ?? 0;
        }),
      }),
      { timeout: 20_000 },
    )
    .toMatchObject({
      clusterCount: 0,
      trainIconCount: expect.any(Number),
      zoom: expect.any(Number),
    });

  await expect
    .poll(() => page.evaluate(() => {
      const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
      const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

      return map?.getZoom() ?? 0;
    }), {
      timeout: 20_000,
    })
    .toBeGreaterThanOrEqual(13);
}

async function hoverRenderedTrain(page: Page): Promise<string> {
  const canvas = page.locator(".train-showcase-map .maplibregl-canvas");
  const point = await readProjectedTrainInteractionPoint(page);

  if (!point.entityId) {
    throw new Error("Expected a rendered train icon and canvas bounds.");
  }

  await canvas.hover({ position: { x: 2, y: 2 } });
  await canvas.hover({ position: { x: point.x, y: point.y } });

  return point.entityId;
}

async function movePointerOffTrain(page: Page): Promise<void> {
  const canvas = page.locator(".train-showcase-map .maplibregl-canvas");

  await canvas.hover({ position: { x: 2, y: 2 } });
}

async function clickRenderedTrain(page: Page): Promise<string> {
  const canvas = page.locator(".train-showcase-map .maplibregl-canvas");
  let point = await readProjectedTrainInteractionPoint(page);

  if (!point.entityId) {
    throw new Error("Expected a rendered train icon and canvas bounds.");
  }

  await canvas.hover({ position: { x: point.x, y: point.y } });

  point = await readProjectedTrainInteractionPoint(page);
  if (!point.entityId) {
    throw new Error("Expected a rendered train icon before clicking.");
  }

  await canvas.click({ position: { x: point.x, y: point.y } });

  return point.entityId;
}

async function expectNoUnexpectedBrowserErrors(page: Page): Promise<BrowserLogSnapshot> {
  // arrange
  const consoleErrors: string[] = [];
  const failedResponses: string[] = [];
  const pageErrors: string[] = [];

  page.on("console", (message) => {
    if (message.type() === "error") {
      consoleErrors.push(message.text());
    }
  });

  page.on("pageerror", (error) => {
    pageErrors.push(error.message);
  });

  page.on("response", (response) => {
    if (response.status() >= 400) {
      failedResponses.push(`${response.status()} ${response.url()}`);
    }
  });

  // act
  await mockLuxembourgOverlayStyle(page);
  await page.goto("/");
  await waitForTrainShowcaseReady(page);

  // assert
  await expect
    .poll(() => ({ consoleErrors: [...consoleErrors], failedResponses: [...failedResponses], pageErrors: [...pageErrors] }))
    .toEqual({
      consoleErrors: [],
      failedResponses: [],
      pageErrors: [],
    });

  return {
    consoleErrors,
    failedResponses,
    pageErrors,
  };
}

async function ensureOverlayGroupTogglesReady(page: Page): Promise<void> {
  await expect.poll(async () => readToggleVisibilitySnapshot(page), { timeout: 20_000 }).toEqual(buildToggleSnapshotExpectation());
}

async function bringLegendToggleIntoView(toggle: ReturnType<Page["locator"]>): Promise<void> {
  const label = toggle.locator("xpath=ancestor::label[1]");
  await expect(label).toBeVisible({ timeout: 20_000 });
  await label.evaluate((element) => {
    const body = element.closest(".sgb-map-legend")?.querySelector(".sgb-map-legend-body") as HTMLElement | null;

    if (body) {
      const labelRect = element.getBoundingClientRect();
      const bodyRect = body.getBoundingClientRect();
      const offsetTop = labelRect.top - bodyRect.top + body.scrollTop;
      const targetScrollTop = offsetTop - body.clientHeight / 2 + labelRect.height / 2;
      body.scrollTop = Math.max(0, targetScrollTop);
    }

    element.scrollIntoView({ block: "center", inline: "nearest" });
  });
}

async function setLegendToggleChecked(toggle: ReturnType<Page["locator"]>, checked: boolean): Promise<void> {
  await bringLegendToggleIntoView(toggle);
  const label = toggle.locator("xpath=ancestor::label[1]");
  await label.evaluate((element, nextChecked) => {
    const input = element.querySelector("input[type='checkbox']") as HTMLInputElement | null;
    if (!input) {
      throw new Error("Legend toggle input was not found inside its label.");
    }

    input.checked = nextChecked;
    input.dispatchEvent(new Event("change", { bubbles: true }));
  }, checked);
}

async function ensureLegendExpanded(page: Page): Promise<void> {
  const legendToggle = page.getByRole("button", { name: /show legend|hide legend/i }).first();
  await expect(legendToggle).toBeVisible({ timeout: 20_000 });

  if ((await legendToggle.getAttribute("aria-expanded")) !== "true") {
    await legendToggle.click();
  }
}

test.describe("train tracking showcase", () => {
  test("should load without browser errors", async ({ page }) => {
    // arrange
    // act
    const browserLogSnapshot = await expectNoUnexpectedBrowserErrors(page);

    // assert
    expect(browserLogSnapshot.consoleErrors).toEqual([]);
    expect(browserLogSnapshot.failedResponses).toEqual([]);
    expect(browserLogSnapshot.pageErrors).toEqual([]);
  });

  test("should keep safe live map styling and ordering for the showcase", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);

    // act
    await expect
      .poll(async () => {
        const snapshot = await readTrainShowcaseLayerSnapshot(page);

        return {
          composedStyleCount: snapshot.composedStyleCount,
          overlayStyleUrl: snapshot.overlayStyleUrl,
          overlayStationLayerId: snapshot.overlayStationLayerId,
          overlayStationLabelLayerId: snapshot.overlayStationLabelLayerId,
          hasClusterHitArea: snapshot.hasClusterHitArea,
          hasTrainIconsLayer: snapshot.hasTrainIconsLayer,
          trainClusterOrder: snapshot.trainClusterOrder,
        };
      }, { timeout: 20_000 })
      .toMatchObject({
        composedStyleCount: 2,
        overlayStyleUrl: expect.stringContaining("style.json"),
        overlayStationLayerId: expect.stringContaining("railway-stations-circle"),
        overlayStationLabelLayerId: expect.stringContaining("railway-stations-label"),
        hasClusterHitArea: true,
        hasTrainIconsLayer: true,
        trainClusterOrder: expect.any(Number),
      });
    await expect
      .poll(async () => (await readTrainShowcaseLayerSnapshot(page)).overlayStationCount, { timeout: 20_000 })
      .toBeGreaterThan(0);
    const snapshot = await readTrainShowcaseLayerSnapshot(page);

    // assert
    expect(snapshot.trainClusterOrder).toBeGreaterThanOrEqual(0);
    expect(JSON.stringify(snapshot.iconSizeExpression)).not.toContain("feature-state");
    expect(snapshot.textRotationExpression).toBeUndefined();
    expect(snapshot.clusterColorExpression).toBe("#2563eb");
    expect(snapshot.clusterPitchAlignment).toBe("viewport");
    expect(snapshot.clusterHitAreaPitchAlignment).toBe("viewport");
    expect(snapshot.clusterCountPitchAlignment).toBe("viewport");
    expect(snapshot.clusterCountRotationAlignment).toBe("viewport");
    expect(JSON.stringify(snapshot.clusterProperties)).toContain("internationalPresence");
    expect(snapshot.overlayTopOrder).toBeGreaterThanOrEqual(0);
    expect(snapshot.overlayStationCount).toBeGreaterThan(0);
    expect(snapshot.overlayStationLabelLayerId).toContain("railway-stations-label");
    expect(snapshot.trainClusterVisibility).toBe("visible");
    expect(snapshot.trainServiceVisibility).toBe("visible");
    expect(snapshot.trainRouteVisibility).toBe("visible");
    expect(snapshot.trainOperatorVisibility).toBe("visible");
    expect(snapshot.trainIconsVisibility).toBe("visible");
    expect(snapshot.trainHitAreaRadius).toBe(24);
    expect(snapshot.serviceLabelsPitchAlignment).toBe("viewport");
    expect(snapshot.serviceLabelsRotationAlignment).toBe("viewport");
    expect(snapshot.trainClusterOrder).toBeGreaterThan(snapshot.overlayTopOrder);
  });

  test("should keep clustered decorations hidden until trains uncluster", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);

    // act
    const snapshot = await readTrainShowcaseLayerSnapshot(page);

    // assert
    expect(snapshot.trainClusterVisibility).toBe("visible");
    expect(snapshot.trainServiceVisibility).toBe("visible");
    expect(snapshot.trainRouteVisibility).toBe("visible");
    expect(snapshot.trainOperatorVisibility).toBe("visible");
    expect(JSON.stringify(snapshot.decorationOpacityExpression)).not.toContain('"zoom"');
    expect(JSON.stringify(snapshot.decorationOpacityExpression)).not.toContain('"step"');
    expect(JSON.stringify(snapshot.decorationOpacityExpression)).toContain('"hover"');
  });

  test("should keep showcase toggles wired to live layer visibility", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);
    await ensureOverlayGroupTogglesReady(page);

    const trainsToggle = page.locator("input[data-testid='map-legend-toggle-trains']");

    // act
    await setLegendToggleChecked(trainsToggle, false);

    await expect
      .poll(async () => {
        const toggleSnapshot = await readToggleVisibilitySnapshot(page);

        return {
          trainClusterVisibility: toggleSnapshot.trainClusterVisibility,
        };
      })
      .toEqual({
        trainClusterVisibility: "none",
      });
    const hiddenSnapshot = await readToggleVisibilitySnapshot(page);

    await setLegendToggleChecked(trainsToggle, true);

    await expect.poll(async () => readToggleVisibilitySnapshot(page)).toEqual({
      tracksVisibility: "visible",
      tramVisibility: "none",
      stationsVisibility: "visible",
      platformsVisibility: "visible",
      routesVisibility: "visible",
      lifecycleVisibility: "visible",
      infrastructureVisibility: "none",
      trainClusterVisibility: "visible",
    });
    const visibleSnapshot = await readToggleVisibilitySnapshot(page);

    // assert
    expect(hiddenSnapshot).toEqual({
      tracksVisibility: "visible",
      tramVisibility: "none",
      stationsVisibility: "visible",
      platformsVisibility: "visible",
      routesVisibility: "visible",
      lifecycleVisibility: "visible",
      infrastructureVisibility: "none",
      trainClusterVisibility: "none",
    });
    expect(visibleSnapshot).toEqual({
      tracksVisibility: "visible",
      tramVisibility: "none",
      stationsVisibility: "visible",
      platformsVisibility: "visible",
      routesVisibility: "visible",
      lifecycleVisibility: "visible",
      infrastructureVisibility: "none",
      trainClusterVisibility: "visible",
    });
  });

  test("should render overlay group toggles with default checked states", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);
    await ensureOverlayGroupTogglesReady(page);

    // act
    const snapshot = await readToggleVisibilitySnapshot(page);

    // assert
    for (const group of overlayGroups) {
      await ensureLegendExpanded(page);

      const toggleKey =
        group.label === "Tracks & tunnels"
          ? "tracks"
          : group.label === "Tram & metro"
            ? "tram"
            : group.label === "Stations & borders"
              ? "stations"
              : group.label.toLowerCase().replaceAll(" ", "-");
      const toggle = page.locator(`input[data-testid="map-legend-toggle-${toggleKey}"]`);

      if (group.label === "Tracks & tunnels") {
        await expect(page.getByText("Railway overlay")).toBeVisible();
      }

      if (group.checkedByDefault) {
        await expect(toggle).toBeChecked();
      } else {
        await expect(toggle).not.toBeChecked();
      }
    }

    expect(snapshot).toEqual(buildToggleSnapshotExpectation());
  });

  test("should keep the legend shell bounded and scroll tall content", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);
    await ensureOverlayGroupTogglesReady(page);

    // act
    const snapshot = await page.evaluate(() => {
      const mapContainer = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
      const legend = document.querySelector(".sgb-map-legend") as HTMLElement | null;
      const panel = legend?.querySelector(".sgb-map-legend-panel") as HTMLElement | null;
      const body = legend?.querySelector(".sgb-map-legend-body") as HTMLElement | null;

      if (!mapContainer || !legend || !panel || !body) {
        throw new Error("Legend shell was not rendered.");
      }

      const filler = document.createElement("div");
      filler.style.height = "1200px";
      filler.textContent = "legend filler";
      body.appendChild(filler);

      return {
        bodyClientHeight: body.clientHeight,
        bodyOverflowY: window.getComputedStyle(body).overflowY,
        bodyScrollHeight: body.scrollHeight,
        legendHeight: legend.getBoundingClientRect().height,
        legendMaxHeight: window.getComputedStyle(panel).maxHeight,
        mapHeight: mapContainer.getBoundingClientRect().height,
      };
    });

    // assert
    expect(snapshot.legendHeight).toBeLessThanOrEqual(snapshot.mapHeight);
    expect(snapshot.legendMaxHeight).not.toBe("none");
    expect(["auto", "scroll"]).toContain(snapshot.bodyOverflowY);
    expect(snapshot.bodyScrollHeight).toBeGreaterThan(snapshot.bodyClientHeight);
  });

  test("should keep the live legend clear of same-side controls and attribution", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);
    await ensureOverlayGroupTogglesReady(page);

    // act
    await page.evaluate(() => {
      const mapContainer = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
      const topLeftHost = mapContainer?.querySelector(".maplibregl-ctrl-top-left") as HTMLElement | null;
      const bottomLeftHost = mapContainer?.querySelector(".maplibregl-ctrl-bottom-left") as HTMLElement | null;
      const legend = document.querySelector(".sgb-map-legend") as HTMLElement | null;

      if (!mapContainer || !topLeftHost || !bottomLeftHost || !legend) {
        throw new Error("Map control hosts were not rendered.");
      }

      const syntheticAttribution = document.createElement("div");
      syntheticAttribution.className = "maplibregl-ctrl maplibregl-ctrl-attrib legend-test-attribution";
      syntheticAttribution.textContent = "Attribution";
      bottomLeftHost.appendChild(syntheticAttribution);

      const syntheticControl = document.createElement("div");
      syntheticControl.className = "maplibregl-ctrl maplibregl-ctrl-group legend-test-neighbor";
      syntheticControl.style.width = "30px";
      syntheticControl.style.height = "58px";
      topLeftHost.insertBefore(syntheticControl, legend);

      const body = legend.querySelector(".sgb-map-legend-body") as HTMLElement | null;
      if (!body) {
        throw new Error("Legend body was not rendered.");
      }

      const filler = document.createElement("div");
      filler.style.height = "1200px";
      filler.textContent = "legend filler";
      body.appendChild(filler);

      window.dispatchEvent(new Event("resize"));
    });

    // assert
    await expect
      .poll(
        () =>
          page.evaluate(() => {
            const mapContainer = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
            const legend = document.querySelector(".sgb-map-legend") as HTMLElement | null;
            const panel = legend?.querySelector(".sgb-map-legend-panel") as HTMLElement | null;
            const body = legend?.querySelector(".sgb-map-legend-body") as HTMLElement | null;
            const attribution = document.querySelector(".legend-test-attribution") as HTMLElement | null;
            const neighbor = document.querySelector(".legend-test-neighbor") as HTMLElement | null;

            if (!mapContainer || !legend || !panel || !body || !attribution || !neighbor) {
              throw new Error("Legend runtime test elements are missing.");
            }

            const style = window.getComputedStyle(panel);
            const legendRect = legend.getBoundingClientRect();
            const attributionRect = attribution.getBoundingClientRect();
            const controlRect = neighbor.getBoundingClientRect();
            const mapRect = mapContainer.getBoundingClientRect();

            return {
              attributionTop: attributionRect.top,
              bodyClientHeight: body.clientHeight,
              bodyScrollHeight: body.scrollHeight,
              controlBottom: controlRect.bottom,
              legendBottom: legendRect.bottom,
              legendMaxHeight: Number.parseFloat(style.maxHeight),
              legendTop: legendRect.top,
              mapBottom: mapRect.bottom,
              mapTop: mapRect.top,
            };
          }),
        { timeout: 20_000 },
      )
      .toEqual(
        expect.objectContaining({
          attributionTop: expect.any(Number),
          bodyClientHeight: expect.any(Number),
          bodyScrollHeight: expect.any(Number),
          controlBottom: expect.any(Number),
          legendBottom: expect.any(Number),
          legendMaxHeight: expect.any(Number),
          legendTop: expect.any(Number),
          mapBottom: expect.any(Number),
          mapTop: expect.any(Number),
        }),
      );

    const snapshot = await page.evaluate(() => {
      const mapContainer = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
      const legend = document.querySelector(".sgb-map-legend") as HTMLElement | null;
      const panel = legend?.querySelector(".sgb-map-legend-panel") as HTMLElement | null;
      const body = legend?.querySelector(".sgb-map-legend-body") as HTMLElement | null;
      const attribution = document.querySelector(".legend-test-attribution") as HTMLElement | null;
      const neighbor = document.querySelector(".legend-test-neighbor") as HTMLElement | null;

      if (!mapContainer || !legend || !panel || !body || !attribution || !neighbor) {
        throw new Error("Legend runtime test elements are missing.");
      }

      const style = window.getComputedStyle(panel);
      const legendRect = legend.getBoundingClientRect();
      const attributionRect = attribution.getBoundingClientRect();
      const controlRect = neighbor.getBoundingClientRect();
      const mapRect = mapContainer.getBoundingClientRect();

      return {
        attributionTop: attributionRect.top,
        bodyClientHeight: body.clientHeight,
        bodyScrollHeight: body.scrollHeight,
        controlBottom: controlRect.bottom,
        legendBottom: legendRect.bottom,
        legendMaxHeight: Number.parseFloat(style.maxHeight),
        legendTop: legendRect.top,
        mapBottom: mapRect.bottom,
        mapTop: mapRect.top,
      };
    });

    expect(snapshot.legendBottom).toBeLessThan(snapshot.attributionTop);
    expect(snapshot.legendTop).toBeGreaterThan(snapshot.controlBottom);
    expect(snapshot.bodyScrollHeight).toBeGreaterThan(snapshot.bodyClientHeight);
    expect(snapshot.legendMaxHeight).toBeGreaterThan(0);
    expect(snapshot.legendMaxHeight).toBeLessThan(snapshot.mapBottom - snapshot.mapTop);
  });

  test("should render Luxembourg legend swatches with the richer live-style structure", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);
    await ensureOverlayGroupTogglesReady(page);

    // act
    const snapshot = await page.evaluate(() => {
      const trackSwatch = document.querySelector(
        ".train-overlay-swatch-tracks",
      ) as HTMLElement | null;
      const infrastructureSwatch = document.querySelector(
        ".train-overlay-swatch-infrastructure",
      ) as HTMLElement | null;
      const stationSwatch = document.querySelector(
        ".train-overlay-swatch-stations",
      ) as HTMLElement | null;
      const tramSwatch = document.querySelector(
        ".train-overlay-swatch-tram",
      ) as HTMLElement | null;
      const lifecycleSwatch = document.querySelector(
        ".train-overlay-swatch-lifecycle",
      ) as HTMLElement | null;

      if (!trackSwatch || !infrastructureSwatch || !stationSwatch || !tramSwatch || !lifecycleSwatch) {
        throw new Error("Legend swatches were not rendered.");
      }

      return {
        infrastructureLightCount: infrastructureSwatch.querySelectorAll(".sig-red, .sig-off").length,
        stationDotCount: stationSwatch.querySelectorAll("circle").length,
        trackRailCount: trackSwatch.querySelectorAll(".tk-rail-top, .tk-rail-side").length,
        trackSleeperCount: trackSwatch.querySelectorAll(".tk-tie-top, .tk-tie-front, .tk-tie-left").length,
        tramCircleCount: tramSwatch.querySelectorAll("circle, rect").length,
        lifecycleLineCount: lifecycleSwatch.querySelectorAll("line").length,
      };
    });

    // assert
    expect(snapshot.stationDotCount).toBe(3);
    expect(snapshot.infrastructureLightCount).toBe(2);
    expect(snapshot.trackRailCount).toBeGreaterThan(0);
    expect(snapshot.trackSleeperCount).toBeGreaterThan(0);
    expect(snapshot.tramCircleCount).toBeGreaterThan(0);
    expect(snapshot.lifecycleLineCount).toBe(2);
  });

  test("should toggle all mapped overlay layers for every overlay group", async ({ page }) => {
    test.setTimeout(60_000);

    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);
    await ensureOverlayGroupTogglesReady(page);

    // act & assert
    for (const group of overlayGroups) {
      await ensureLegendExpanded(page);

      const toggleKey =
        group.label === "Tracks & tunnels"
          ? "tracks"
          : group.label === "Tram & metro"
            ? "tram"
            : group.label === "Stations & borders"
              ? "stations"
              : group.label.toLowerCase().replaceAll(" ", "-");
      const toggle = page.locator(`input[data-testid="map-legend-toggle-${toggleKey}"]`);
      const toggledState = Object.fromEntries(group.keys.map((key) => [key, group.checkedByDefault ? "none" : "visible"])) as Partial<ToggleVisibilitySnapshot>;
      const restoredState = Object.fromEntries(group.keys.map((key) => [key, group.checkedByDefault ? "visible" : "none"])) as Partial<ToggleVisibilitySnapshot>;

       if (group.checkedByDefault) {
        await setLegendToggleChecked(toggle, false);
       } else {
        await setLegendToggleChecked(toggle, true);
       }

        await expect.poll(async () => readToggleVisibilitySnapshot(page)).toEqual(buildToggleSnapshotExpectation(toggledState));

        await ensureLegendExpanded(page);

        if (group.checkedByDefault) {
         await setLegendToggleChecked(toggle, true);
        } else {
        await setLegendToggleChecked(toggle, false);
       }

      await expect.poll(async () => readToggleVisibilitySnapshot(page)).toEqual(buildToggleSnapshotExpectation(restoredState));
    }
  });

  test("should not register sample-owned station or infrastructure layers in the showcase", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);

    // act
    const manualLayerSnapshot = await page.evaluate(() => {
      const mapElement = document.querySelector(".train-showcase-map .sgb-map-container") as HTMLElement | null;
      const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

      if (!map) {
        throw new Error("Train showcase map was not initialized.");
      }

      return {
        hasStationsSource: map.getSource("stations") !== undefined,
        hasStationDots: map.getLayer("sgb-station-dots") !== undefined,
        hasStationLabels: map.getLayer("sgb-station-labels") !== undefined,
        hasRailMain: map.getLayer("sgb-rail-main") !== undefined,
        hasRailService: map.getLayer("sgb-rail-service") !== undefined,
        hasPlatforms: map.getLayer("sgb-platforms-3d") !== undefined,
        hasCrossings: map.getLayer("sgb-crossings") !== undefined,
        hasSwitches: map.getLayer("sgb-switches") !== undefined,
      };
    });

    // assert
    expect(manualLayerSnapshot).toEqual({
      hasStationsSource: false,
      hasStationDots: false,
      hasStationLabels: false,
      hasRailMain: false,
      hasRailService: false,
      hasPlatforms: false,
      hasCrossings: false,
      hasSwitches: false,
    });
  });

  test("should hide train number labels while clustering is active", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);

    await expect.poll(async () => (await readRenderedFeatureSnapshot(page)).clusterCount, { timeout: 20_000 }).toBeGreaterThan(0);

    // act
    const snapshot = await readRenderedFeatureSnapshot(page);

    // assert
    expect(snapshot.clusterCount).toBeGreaterThan(0);
    expect(snapshot.serviceCount).toBe(0);
  });

  test("should zoom into unclustered mode and render train icons", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);

    // act
    await zoomShowcaseIntoUnclusteredMode(page);

    const snapshot = await readRenderedFeatureSnapshot(page);

    // assert
    expect(snapshot.clusterCount).toBe(0);
    expect(snapshot.trainIconCount).toBeGreaterThan(0);
  });

  test("should render the train number label above and away from the train icon when unclustered", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);
    await zoomShowcaseIntoUnclusteredMode(page);

    // act
    const snapshot = await readTrainShowcaseLayerSnapshot(page);

    // assert
    expect(snapshot.serviceFeatureCountInSource).toBeGreaterThan(0);
    expect(snapshot.serviceTextOffset).toEqual([1.3, -2.2]);
  });

  test("should hover a rendered train and show hover-driven behavior", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);
    await zoomShowcaseIntoUnclusteredMode(page);

    // act
    const entityId = await hoverRenderedTrain(page);

    // assert
    await expect.poll(async () => (await readTrainInteractionSnapshot(page)).hoveredDecorationCount, { timeout: 20_000 }).toBeGreaterThan(0);

    const snapshot = await readTrainInteractionSnapshot(page);
    expect(entityId).not.toBe("");
    expect(snapshot.selectedTrainPanelVisible).toBe(false);
    expect(snapshot.hoveredDecorationCount).toBeGreaterThan(0);
    expect(snapshot.hoveredRouteLabelCount).toBe(1);
    expect(snapshot.hoveredOperatorLabelCount).toBe(1);

    // act
    await movePointerOffTrain(page);

    // assert
    await expect.poll(async () => (await readTrainInteractionSnapshot(page)).hoveredDecorationCount, { timeout: 20_000 }).toBe(0);
  });

  test("should click a rendered train and keep selection after the next update", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);
    await zoomShowcaseIntoUnclusteredMode(page);

    // act
    const entityId = await clickRenderedTrain(page);

    // assert
    await expect.poll(() => readTrainInteractionSnapshot(page), { timeout: 20_000 }).toMatchObject({
      selectedOperatorLabelCount: 1,
      selectedTrainPanelVisible: true,
    });
    const clickSnapshot = await readTrainInteractionSnapshot(page);
    expect(entityId).not.toBe("");
    expect(clickSnapshot.zoom).toBeGreaterThanOrEqual(13);
    expect(clickSnapshot.selectedOperatorLabelCount).toBe(1);

    await page.waitForTimeout(2_500);
    await expect.poll(() => readTrainInteractionSnapshot(page), { timeout: 20_000 }).toMatchObject({
      selectedOperatorLabelCount: 1,
      selectedTrainPanelVisible: true,
    });
    const persistedSnapshot = await readTrainInteractionSnapshot(page);
    expect(persistedSnapshot.zoom).toBeGreaterThanOrEqual(13);
  });

  test("should show hover and click labels at any unclustered zoom and hide them while clustered", async ({ page }) => {
    // arrange
    await mockLuxembourgOverlayStyle(page);
    await page.goto("/");
    await waitForTrainShowcaseReady(page);

    // act & assert
    await expect.poll(async () => (await readRenderedFeatureSnapshot(page)).clusterCount, { timeout: 20_000 }).toBeGreaterThan(0);

    const clusteredSnapshot = await readTrainInteractionSnapshot(page);
    expect(clusteredSnapshot.hoveredRouteLabelCount).toBe(0);
    expect(clusteredSnapshot.hoveredOperatorLabelCount).toBe(0);
    expect(clusteredSnapshot.selectedOperatorLabelCount).toBe(0);

    await zoomShowcaseMapTo(page, 12.5);
    await expect
      .poll(async () => {
        const snapshot = await readRenderedFeatureSnapshot(page);
        const interactionSnapshot = await readTrainInteractionSnapshot(page);

        return {
          clusterCount: snapshot.clusterCount,
          serviceCount: snapshot.serviceCount,
          zoom: interactionSnapshot.zoom,
        };
      }, { timeout: 20_000 })
      .toMatchObject({
        clusterCount: 0,
        serviceCount: expect.any(Number),
        zoom: expect.any(Number),
      });

    const unclusteredSnapshot = await readTrainInteractionSnapshot(page);
    expect(unclusteredSnapshot.zoom).toBeLessThan(13);
    expect(unclusteredSnapshot.hoveredRouteLabelCount).toBe(0);
    expect(unclusteredSnapshot.hoveredOperatorLabelCount).toBe(0);
    expect(unclusteredSnapshot.selectedOperatorLabelCount).toBe(0);

    await hoverRenderedTrain(page);

    await expect.poll(() => readTrainInteractionSnapshot(page), { timeout: 20_000 }).toMatchObject({
      hoveredRouteLabelCount: 1,
      hoveredOperatorLabelCount: 1,
    });

    const hoveredSnapshot = await readTrainInteractionSnapshot(page);
    expect(hoveredSnapshot.zoom).toBeLessThan(13);
    expect(hoveredSnapshot.hoveredRouteLabelCount).toBe(1);
    expect(hoveredSnapshot.hoveredOperatorLabelCount).toBe(1);

    await movePointerOffTrain(page);
    await expect.poll(async () => (await readTrainInteractionSnapshot(page)).hoveredRouteLabelCount, { timeout: 20_000 }).toBe(0);
    await expect.poll(async () => (await readTrainInteractionSnapshot(page)).hoveredOperatorLabelCount, { timeout: 20_000 }).toBe(0);

    await clickRenderedTrain(page);

    await expect.poll(() => readTrainInteractionSnapshot(page), { timeout: 20_000 }).toMatchObject({
      selectedOperatorLabelCount: 1,
      selectedTrainPanelVisible: true,
    });
  });

  test("should keep selected-train panel hidden until a train is chosen", async ({ page }) => {
    // arrange
    await page.goto("/");
    await waitForTrainShowcaseReady(page);

    // act
    const selectedPanel = page.getByText("Selected train");

    // assert
    await expect(selectedPanel).toBeHidden();
  });
});
