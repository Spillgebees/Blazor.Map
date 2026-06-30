import { expect, type Page, test } from "@playwright/test";
import { evaluateOnMap } from "./helpers";

// Map-level camera follow against the real map: engaging on a tracked entity, tracking it as
// it moves, and clearing on a real user pan (with the .NET callback echo).

const PAGE_ROUTE = "/engine-follow-functional-test";
const SYMBOL_LAYER_ID = "vehicles-symbols";

async function openFixture(page: Page): Promise<void> {
  await page.goto(PAGE_ROUTE, { waitUntil: "domcontentloaded" });
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });
  await expect
    .poll(
      () =>
        evaluateOnMap<number>(
          page,
          `
          if (!map.getLayer(${JSON.stringify(SYMBOL_LAYER_ID)})) return 0;
          return map.queryRenderedFeatures({ layers: [${JSON.stringify(SYMBOL_LAYER_ID)}] }).length;
          `,
        ),
      { timeout: 60000 },
    )
    .toBeGreaterThan(0);
}

const centerLng = (page: Page) => evaluateOnMap<number>(page, "return map.getCenter().lng;");
const centerLat = (page: Page) => evaluateOnMap<number>(page, "return map.getCenter().lat;");

test.describe("engine camera follow", () => {
  test("engages on the entity and applies the held zoom", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("start-follow").click();

    // entity sits at (lng 6.14, lat 49.61); anchored zoom 14 from zoom 12
    await expect.poll(() => centerLng(page), { timeout: 20000 }).toBeCloseTo(6.14, 2);
    await expect.poll(() => centerLat(page), { timeout: 20000 }).toBeCloseTo(49.61, 2);
    await expect
      .poll(() => evaluateOnMap<number>(page, "return map.getZoom();"), { timeout: 20000 })
      .toBeCloseTo(14, 1);
    await expect(page.getByTestId("last-reason")).toHaveText("Started");
  });

  test("animates the engage move when a duration is set", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("start-follow-animated").click();

    // a 1500ms engage means the camera is mid-transition (isMoving) before it settles on the target
    await expect.poll(() => evaluateOnMap<boolean>(page, "return map.isMoving();"), { timeout: 5000 }).toBe(true);
    await expect.poll(() => centerLng(page), { timeout: 20000 }).toBeCloseTo(6.14, 2);
    await expect.poll(() => centerLat(page), { timeout: 20000 }).toBeCloseTo(49.61, 2);
  });

  test("reports Updated when the follow options change", async ({ page }) => {
    await openFixture(page);
    await page.getByTestId("start-follow").click();
    await expect(page.getByTestId("last-reason")).toHaveText("Started", { timeout: 20000 });

    // a second, differently-configured follow on the same target is an update, not a fresh start
    await page.getByTestId("start-follow-animated").click();
    await expect(page.getByTestId("last-reason")).toHaveText("Updated", { timeout: 20000 });
  });

  test("clears explicitly with the Cleared reason", async ({ page }) => {
    await openFixture(page);
    await page.getByTestId("start-follow").click();
    await expect.poll(() => centerLat(page), { timeout: 20000 }).toBeCloseTo(49.61, 2);

    await page.getByTestId("clear-follow").click();

    await expect(page.getByTestId("last-reason")).toHaveText("Cleared", { timeout: 20000 });
    await expect(page.getByTestId("follow-target")).toHaveText("none");
  });

  test("clears with FeatureMissing when the entity is removed", async ({ page }) => {
    await openFixture(page);
    await page.getByTestId("start-follow-clear-missing").click();
    await expect.poll(() => centerLat(page), { timeout: 20000 }).toBeCloseTo(49.61, 2);

    await page.getByTestId("remove-e1").click();

    await expect(page.getByTestId("last-reason")).toHaveText("FeatureMissing", { timeout: 20000 });
    await expect(page.getByTestId("follow-target")).toHaveText("none");
  });

  test("tracks the entity as it moves", async ({ page }) => {
    await openFixture(page);
    await page.getByTestId("start-follow").click();
    await expect.poll(() => centerLng(page), { timeout: 20000 }).toBeCloseTo(6.14, 2);

    await page.getByTestId("move-e1").click();

    // MoveE1 shifts the entity by (+0.03 lat, +0.05 lng)
    await expect.poll(() => centerLng(page), { timeout: 20000 }).toBeCloseTo(6.19, 2);
    await expect.poll(() => centerLat(page), { timeout: 20000 }).toBeCloseTo(49.64, 2);
  });

  test("eases tracking for jumped entity camera moves when configured", async ({ page }) => {
    await openFixture(page);
    await page.getByTestId("start-follow-tracking-animated").click();
    await expect.poll(() => centerLng(page), { timeout: 20000 }).toBeCloseTo(6.14, 2);

    await page.getByTestId("move-e1").click();

    // Tracking animation is opt-in, so a jumped entity starts a camera transition instead of snapping.
    await expect.poll(() => evaluateOnMap<boolean>(page, "return map.isMoving();"), { timeout: 5000 }).toBe(true);
    await expect.poll(() => centerLng(page), { timeout: 20000 }).toBeCloseTo(6.19, 2);
    await expect.poll(() => centerLat(page), { timeout: 20000 }).toBeCloseTo(49.64, 2);
  });

  test("holds a fixed zoom, restoring it after the camera zooms away", async ({ page }) => {
    await openFixture(page);
    await page.getByTestId("start-follow-fixed-zoom").click();
    await expect
      .poll(() => evaluateOnMap<number>(page, "return map.getZoom();"), { timeout: 20000 })
      .toBeCloseTo(15, 1);

    // move the camera off the fixed zoom; a programmatic zoom carries no originalEvent, so it does not
    // clear the follow, leaving the controller to pull the zoom back on a later frame
    await evaluateOnMap<void>(page, "map.setZoom(11); return undefined;");

    await expect
      .poll(() => evaluateOnMap<number>(page, "return map.getZoom();"), { timeout: 20000 })
      .toBeCloseTo(15, 1);
    await expect(page.getByTestId("follow-target")).toHaveText("e1");
  });

  test("clears on a real user pan and echoes the reason to .NET", async ({ page }) => {
    await openFixture(page);
    await page.getByTestId("start-follow").click();
    await expect.poll(() => centerLng(page), { timeout: 20000 }).toBeCloseTo(6.14, 2);

    // a genuine drag across the canvas carries an originalEvent, so it clears the follow
    const canvas = page.locator(".sgb-map-container canvas");
    const box = await canvas.boundingBox();
    if (!box) {
      throw new Error("canvas not found");
    }

    const cx = box.x + box.width / 2;
    const cy = box.y + box.height / 2;
    await page.mouse.move(cx, cy);
    await page.mouse.down();
    await page.mouse.move(cx - 120, cy - 80, { steps: 8 });
    await page.mouse.up();

    await expect(page.getByTestId("last-reason")).toHaveText("UserInteraction", { timeout: 20000 });
    await expect(page.getByTestId("follow-target")).toHaveText("none");

    // let drag inertia settle before sampling the baseline (the map glides after mouseup)
    await evaluateOnMap<void>(page, "return new Promise((resolve) => map.once('idle', resolve));");
    const lngAfterClear = await centerLng(page);

    // tracking has stopped: moving the entity no longer recentres the camera
    await page.getByTestId("move-e1").click();
    await page.waitForTimeout(500);
    expect(await centerLng(page)).toBeCloseTo(lngAfterClear, 3);
  });

  test("clears on a user pan even while the entity is continuously moving", async ({ page }) => {
    // regression: a moving entity recentres every frame, which previously swallowed the drag gesture
    // (dragstart never fired) so the follow never cleared
    await openFixture(page);
    await page.getByTestId("start-moving").click();
    await page.getByTestId("start-follow").click();
    await expect(page.getByTestId("last-reason")).toHaveText("Started", { timeout: 20000 });

    const canvas = page.locator(".sgb-map-container canvas");
    const box = await canvas.boundingBox();
    if (!box) {
      throw new Error("canvas not found");
    }

    const cx = box.x + box.width / 2;
    const cy = box.y + box.height / 2;
    await page.mouse.move(cx, cy);
    await page.mouse.down();
    for (let step = 1; step <= 10; step++) {
      await page.mouse.move(cx - step * 12, cy - step * 7);
      await page.waitForTimeout(16);
    }
    await page.mouse.up();

    await expect(page.getByTestId("last-reason")).toHaveText("UserInteraction", { timeout: 20000 });
    await expect(page.getByTestId("follow-target")).toHaveText("none");
  });
});
