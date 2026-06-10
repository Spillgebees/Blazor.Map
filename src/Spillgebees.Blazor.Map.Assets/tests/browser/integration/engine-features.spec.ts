import { expect, type Page, test } from "@playwright/test";

// Functional coverage for markers/circles/polylines hosted on SgbMap — the component
// family (feature-host seam) plus the Markers/Circles/Polylines parameters, including
// draggable-marker click/dragend callbacks through the engine event router.

const PAGE_ROUTE = "/engine-features-functional-test";
const CIRCLES_LAYER_ID = "sgb-circles-layer";
const POLYLINES_LAYER_ID = "sgb-polylines-layer";

function evaluateOnMap<T>(page: Page, body: string): Promise<T> {
  return page.evaluate(
    ({ evalBody }) => {
      const maps = window.Spillgebees?.Map?.maps;
      const map = maps ? [...maps.values()][0] : undefined;
      if (!map) {
        throw new Error("map not found");
      }

      return new Function("map", evalBody)(map) as T;
    },
    { evalBody: body },
  );
}

function renderedCount(page: Page, layerId: string): Promise<number> {
  return evaluateOnMap<number>(
    page,
    `
    if (!map.getLayer(${JSON.stringify(layerId)})) return 0;
    return map.queryRenderedFeatures({ layers: [${JSON.stringify(layerId)}] }).length;
    `,
  );
}

async function openFixture(page: Page): Promise<void> {
  await page.goto(PAGE_ROUTE, { waitUntil: "domcontentloaded" });
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });
}

test.describe("engine markers and shapes", () => {
  test("renders a DOM marker that moves and toggles", async ({ page }) => {
    await openFixture(page);

    // m1 (component) + drag-me (parameter)
    const allMarkers = page.locator(".maplibregl-marker");
    await expect(allMarkers).toHaveCount(2, { timeout: 30000 });

    const marker = page.locator(".maplibregl-marker:not(.test-draggable-marker)");
    const before = await marker.boundingBox();
    await page.getByTestId("move-marker").click();
    await expect
      .poll(async () => (await marker.boundingBox())?.x ?? 0, { timeout: 10000 })
      .toBeGreaterThan((before?.x ?? 0) + 10);

    await page.getByTestId("remove-marker").click();
    await expect(marker).toHaveCount(0, { timeout: 10000 });

    await page.getByTestId("remove-marker").click();
    await expect(marker).toHaveCount(1, { timeout: 10000 });
  });

  test("renders circles and polylines through the engine shape pipeline", async ({ page }) => {
    await openFixture(page);

    // c1+pc1 circles, p1+pp1 polylines — components and parameters share the layers.
    await expect.poll(() => renderedCount(page, CIRCLES_LAYER_ID), { timeout: 30000 }).toBeGreaterThanOrEqual(2);
    await expect.poll(() => renderedCount(page, POLYLINES_LAYER_ID), { timeout: 30000 }).toBeGreaterThanOrEqual(2);
    await expect(page.getByTestId("map-error")).toHaveCount(0);
  });

  test("raises click and dragend callbacks for a draggable parameter marker", async ({ page }) => {
    await openFixture(page);

    const draggable = page.locator(".maplibregl-marker.test-draggable-marker");
    await expect(draggable).toHaveCount(1, { timeout: 30000 });

    await draggable.click();
    await expect(page.getByTestId("marker-click-log")).toHaveText("click:drag-me", { timeout: 10000 });

    const box = await draggable.boundingBox();
    if (!box) {
      throw new Error("draggable marker has no bounding box");
    }

    // MapLibre default pins anchor at the bottom tip; grab the visible pin body.
    const grabX = box.x + box.width / 2;
    const grabY = box.y + box.height / 2;
    await page.mouse.move(grabX, grabY);
    await page.mouse.down();
    await page.mouse.move(grabX + 60, grabY + 40, { steps: 10 });
    await page.mouse.up();

    await expect(page.getByTestId("marker-drag-log")).toHaveText(/^dragend:drag-me:/, { timeout: 10000 });

    // the marker actually moved on screen
    const after = await draggable.boundingBox();
    expect(Math.abs((after?.x ?? 0) - box.x)).toBeGreaterThan(30);
  });
});
