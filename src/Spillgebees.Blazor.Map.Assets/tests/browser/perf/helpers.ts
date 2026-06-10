import * as fs from "node:fs";
import * as path from "node:path";
import { expect, type Page, type TestInfo } from "@playwright/test";

// Enforced perf budgets. PERF_BUDGET_SCALE relaxes them on
// noisy runners (CI uses 2); PERF_RECORD=1 records numbers without asserting at all.
const BUDGET_SCALE = Number(process.env.PERF_BUDGET_SCALE ?? "1");
export const RECORD_ONLY = process.env.PERF_RECORD === "1";

export const BUDGETS = {
  tickP95Ms: 50 * BUDGET_SCALE,
  longTaskMaxMs: 100 * BUDGET_SCALE,
  // frame gaps are quantized to 16.7ms steps at 60Hz; 55 allows a 3-frame hitch at
  // p95 (50.0ms lands exactly on the boundary) and fails on 4 frames (66.7ms).
  frameGapP95Ms: 55 * BUDGET_SCALE,
};

export interface MetricSummary {
  count: number;
  p50: number;
  p95: number;
  max: number;
  totalMs: number;
}

export interface StressSnapshot {
  elapsedMs: number;
  renderedFeatures: number;
  frameGaps: MetricSummary & { over50: number };
  tickDurations: MetricSummary;
  longTasks: MetricSummary;
  counters: Record<string, number>;
  mapEvents: { renders: number; sourcedata: number; styledata: number };
}

export interface StressScenario {
  name: string;
  route:
    | "/engine-entity-stress-test"
    | "/engine-geojson-stress-test"
    | "/engine-shapes-stress-test"
    | "/engine-vector-functional-test";
  frameId: string;
  query: Record<string, string>;
  warmupMs?: number;
  measureMs?: number;
  duringMeasure?: (page: Page) => Promise<void>;
  /**
   * DOM-marker scenarios render no GL features; everything else must render
   * something or the scenario is silently measuring an empty map.
   */
  rendersGlFeatures?: boolean;
}

export const ENGINE_FRAME_ID = "engine-entity-stress-frame";
export const ENGINE_GEOJSON_FRAME_ID = "engine-geojson-stress-frame";
export const ENGINE_VECTOR_FRAME_ID = "engine-vector-frame";
export const ENGINE_SHAPES_FRAME_ID = "engine-shapes-stress-frame";

export async function runStressScenario(page: Page, scenario: StressScenario): Promise<StressSnapshot> {
  const warmupMs = scenario.warmupMs ?? 4000;
  const measureMs = scenario.measureMs ?? 10000;
  const query = new URLSearchParams({ ...scenario.query, autostart: "1" });

  const pageErrors: string[] = [];
  page.on("pageerror", (error) => pageErrors.push(String(error)));

  await page.goto(`${scenario.route}?${query.toString()}`, { waitUntil: "domcontentloaded" });

  // WASM boot + map style load: wait until the map canvas exists and the loop ticks.
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });
  await expect
    .poll(
      () =>
        page.evaluate(
          (frameId) => window.MapStressDiagnostics?.snapshot(frameId)?.tickDurations.count ?? -1,
          scenario.frameId,
        ),
      { timeout: 60000, message: "stress loop should start producing ticks" },
    )
    .toBeGreaterThan(0);

  await page.waitForTimeout(warmupMs);
  await page.evaluate((frameId) => window.MapStressDiagnostics.reset(frameId), scenario.frameId);

  if (scenario.duringMeasure) {
    const deadline = Date.now() + measureMs;
    while (Date.now() < deadline) {
      await scenario.duringMeasure(page);
    }
  } else {
    await page.waitForTimeout(measureMs);
  }

  const snapshot = (await page.evaluate(
    (frameId) => window.MapStressDiagnostics.snapshot(frameId),
    scenario.frameId,
  )) as StressSnapshot | null;

  expect(snapshot, "diagnostics snapshot should exist").not.toBeNull();
  expect(pageErrors, "no unhandled page errors during the run").toEqual([]);
  await expect(page.locator("[data-testid=stress-error]")).toHaveCount(0);
  if (scenario.rendersGlFeatures !== false) {
    expect(
      (snapshot as StressSnapshot).renderedFeatures,
      "scenario must actually render features — 0 means the page is broken and the budgets are measuring an empty map",
    ).toBeGreaterThan(0);
  }

  return snapshot as StressSnapshot;
}

export async function reportResults(
  testInfo: TestInfo,
  scenario: StressScenario,
  snapshot: StressSnapshot,
): Promise<void> {
  const result = {
    scenario: scenario.name,
    route: scenario.route,
    query: scenario.query,
    recordedAt: new Date().toISOString(),
    budgets: BUDGETS,
    recordOnly: RECORD_ONLY,
    snapshot,
  };

  const resultsDir = path.join(testInfo.project.testDir, "..", "perf-results");
  fs.mkdirSync(resultsDir, { recursive: true });
  fs.writeFileSync(path.join(resultsDir, `${scenario.name}.json`), `${JSON.stringify(result, null, 2)}\n`);
  await testInfo.attach(`${scenario.name}.json`, {
    body: JSON.stringify(result, null, 2),
    contentType: "application/json",
  });
}

export function assertBudgets(snapshot: StressSnapshot): void {
  if (RECORD_ONLY) {
    return;
  }

  expect
    .soft(snapshot.tickDurations.p95, `tick duration p95 must stay under ${BUDGETS.tickP95Ms}ms`)
    .toBeLessThan(BUDGETS.tickP95Ms);
  expect
    .soft(snapshot.longTasks.max, `longest main-thread task must stay under ${BUDGETS.longTaskMaxMs}ms`)
    .toBeLessThan(BUDGETS.longTaskMaxMs);
  expect
    .soft(snapshot.frameGaps.p95, `frame gap p95 must stay under ${BUDGETS.frameGapP95Ms}ms`)
    .toBeLessThan(BUDGETS.frameGapP95Ms);
}

declare global {
  interface Window {
    MapStressDiagnostics: {
      snapshot(containerId: string): StressSnapshot | null;
      reset(containerId: string): void;
    };
  }
}
