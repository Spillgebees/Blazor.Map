import { expect, type Page, test } from "@playwright/test";
import {
  assertBudgets,
  ENGINE_FRAME_ID,
  ENGINE_GEOJSON_FRAME_ID,
  ENGINE_SHAPES_FRAME_ID,
  ENGINE_VECTOR_FRAME_ID,
  reportResults,
  runStressScenario,
  type StressScenario,
} from "./helpers";

// Benchmarks with enforced budgets (see helpers.ts). Scenarios run serially —
// they measure main-thread health and must not share the worker with other tests.
test.describe.configure({ mode: "serial" });
test.describe("map update benchmarks", () => {
  test.setTimeout(180000);

  const scenarios: StressScenario[] = [
    // Entity scenarios on the engine path (snapshot diff + binary motion frames).
    // pre-rewrite baseline for comparison: 412 ms ticks at 2 000 entities —
    // the main-thread freeze these budgets exist to prevent.
    {
      name: "e-engine-steady-state",
      route: "/engine-entity-stress-test",
      frameId: ENGINE_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent" },
    },
    {
      name: "e-engine-full-churn",
      route: "/engine-entity-stress-test",
      frameId: ENGINE_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "AllFeatures" },
    },
    {
      name: "e-engine-membership-churn",
      route: "/engine-entity-stress-test",
      frameId: ENGINE_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "Membership" },
    },
    {
      name: "e-engine-interaction-under-load",
      route: "/engine-entity-stress-test",
      frameId: ENGINE_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", interactivity: "1" },
      duringMeasure: sweepPointerAcrossMap,
    },
    // Feature cost matrix: scenario A with each tracked-entity feature toggled on
    // individually, then everything at once. Catches per-feature perf regressions.
    {
      name: "f-engine-decorations",
      route: "/engine-entity-stress-test",
      frameId: ENGINE_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", decorations: "1" },
    },
    {
      name: "f-engine-clustering",
      route: "/engine-entity-stress-test",
      frameId: ENGINE_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", cluster: "1" },
    },
    {
      name: "f-engine-animation",
      route: "/engine-entity-stress-test",
      frameId: ENGINE_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", animation: "1" },
    },
    {
      // zoom 15: at the default zoom 11 the whole stress spiral fits inside a few
      // clusters, so no individual symbols or decorations render and the variant
      // silently measures an idle map. Zoomed in, singles + clusters + labels all
      // render and every feature's pipeline is actually exercised.
      name: "f-engine-all-features",
      route: "/engine-entity-stress-test",
      frameId: ENGINE_FRAME_ID,
      query: {
        entities: "2000",
        interval: "100",
        pattern: "TenPercent",
        decorations: "1",
        cluster: "1",
        animation: "1",
        interactivity: "1",
        zoom: "15",
      },
      duringMeasure: sweepPointerAcrossMap,
    },
    // Raw GeoJsonSource per layer type — the second data pipeline (full document
    // rebuild + setData via the scheduler, no delta protocol).
    {
      name: "g-geojsonv2-circle-steady",
      route: "/engine-geojson-stress-test",
      frameId: ENGINE_GEOJSON_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", mode: "Circle" },
    },
    {
      name: "g-geojsonv2-circle-full-churn",
      route: "/engine-geojson-stress-test",
      frameId: ENGINE_GEOJSON_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "AllFeatures", mode: "Circle" },
    },
    {
      name: "g-geojsonv2-symbol-steady",
      route: "/engine-geojson-stress-test",
      frameId: ENGINE_GEOJSON_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", mode: "Symbol" },
    },
    {
      name: "g-geojsonv2-line-steady",
      route: "/engine-geojson-stress-test",
      frameId: ENGINE_GEOJSON_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", mode: "Line" },
    },
    {
      name: "g-geojsonv2-fill-steady",
      route: "/engine-geojson-stress-test",
      frameId: ENGINE_GEOJSON_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", mode: "Fill" },
    },
    {
      name: "g-geojsonv2-extrusion-steady",
      route: "/engine-geojson-stress-test",
      frameId: ENGINE_GEOJSON_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", mode: "FillExtrusion" },
    },
    // Convenience shape parameters (Circles/Polylines) — C#-built FeatureCollection
    // through the engine's raw-source lane. Markers are DOM elements (one per
    // feature), so they run at a deliberately small count: they are a labelling
    // tool, not a bulk-rendering pipeline.
    {
      name: "h-shapes-circles-steady",
      route: "/engine-shapes-stress-test",
      frameId: ENGINE_SHAPES_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", mode: "Circles" },
    },
    {
      name: "h-shapes-circles-full-churn",
      route: "/engine-shapes-stress-test",
      frameId: ENGINE_SHAPES_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "AllFeatures", mode: "Circles" },
    },
    {
      name: "h-shapes-polylines-steady",
      route: "/engine-shapes-stress-test",
      frameId: ENGINE_SHAPES_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", mode: "Polylines" },
    },
    {
      name: "h-markers-dom-steady",
      route: "/engine-shapes-stress-test",
      frameId: ENGINE_SHAPES_FRAME_ID,
      query: { entities: "200", interval: "100", pattern: "TenPercent", mode: "Markers" },
      rendersGlFeatures: false, // DOM markers are invisible to queryRenderedFeatures
    },
    // Vector tiles don't have a data-update path; the meaningful churn is runtime
    // paint updates over the tiled layer.
    {
      name: "v-vector-paint-churn",
      route: "/engine-vector-functional-test",
      frameId: ENGINE_VECTOR_FRAME_ID,
      query: { interval: "100" },
    },
    // Pan smoothness — the everyday "moving around the map" use-case. The NoChanges
    // pattern rebuilds value-identical feature lists every tick (the idiomatic Blazor
    // "list recreated on every render" shape), so the only legitimate map work during
    // the drag-pans is camera rendering. Redundant shape re-uploads show up both as
    // frame gaps and as a climbing setData counter, so the counter ceiling pins the
    // coordinator's unchanged-payload skip.
    {
      name: "i-pan-circles-rerender-only",
      route: "/engine-shapes-stress-test",
      frameId: ENGINE_SHAPES_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "NoChanges", mode: "Circles" },
      duringMeasure: dragPanAcrossMap,
      maxCounters: { setData: 2 },
    },
    {
      name: "i-pan-polylines-rerender-only",
      route: "/engine-shapes-stress-test",
      frameId: ENGINE_SHAPES_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "NoChanges", mode: "Polylines" },
      duringMeasure: dragPanAcrossMap,
      maxCounters: { setData: 2 },
    },
    // Pan smoothness with genuinely changing data — the legitimate update cost that
    // must coexist with camera movement.
    {
      name: "i-pan-circles-live-data",
      route: "/engine-shapes-stress-test",
      frameId: ENGINE_SHAPES_FRAME_ID,
      query: { entities: "2000", interval: "100", pattern: "TenPercent", mode: "Circles" },
      duringMeasure: dragPanAcrossMap,
    },
  ];

  for (const scenario of scenarios) {
    test(scenario.name, async ({ page }, testInfo) => {
      const snapshot = await runStressScenario(page, scenario);
      await reportResults(testInfo, scenario, snapshot);
      assertBudgets(snapshot, scenario);
    });
  }
});

/** Out-and-back drag-pans around the feature field, with a settle pause per gesture. */
async function dragPanAcrossMap(page: Page): Promise<void> {
  const box = await page.locator(".sgb-map-container canvas").boundingBox();
  expect(box, "map canvas must be visible for drag pans").not.toBeNull();
  if (!box) {
    return;
  }

  const centerX = box.x + box.width / 2;
  const centerY = box.y + box.height / 2;
  for (const direction of [1, -1]) {
    await page.mouse.move(centerX, centerY);
    await page.mouse.down();
    for (let step = 1; step <= 10; step++) {
      await page.mouse.move(centerX + direction * step * 18, centerY + direction * step * 6, { steps: 2 });
      await page.waitForTimeout(16);
    }
    await page.mouse.up();
    // let inertia finish so moveend (and the re-render it triggers) lands in-measure
    await page.waitForTimeout(150);
  }
}

async function sweepPointerAcrossMap(page: Page): Promise<void> {
  const box = await page.locator(".sgb-map-container canvas").boundingBox();
  expect(box, "map canvas must be visible for pointer sweeps").not.toBeNull();
  if (!box) {
    return;
  }

  const steps = 12;
  for (let step = 0; step <= steps; step++) {
    const fraction = step / steps;
    await page.mouse.move(box.x + box.width * fraction, box.y + box.height * (0.3 + 0.4 * fraction));
    await page.waitForTimeout(40);
  }
}
