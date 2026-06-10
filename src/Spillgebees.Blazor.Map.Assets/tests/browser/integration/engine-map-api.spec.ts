import { expect, type Page, test } from "@playwright/test";

// Map options, camera methods, transient popups, view reads, and tile overlays —
// the full map options/camera/query surface of the engine map.

const PAGE_ROUTE = "/engine-map-api-functional-test";

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

async function openFixture(page: Page): Promise<void> {
  await page.goto(PAGE_ROUTE, { waitUntil: "domcontentloaded" });
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });
}

test.describe("engine map API", () => {
  test("applies map options at create time", async ({ page }) => {
    await openFixture(page);

    await expect.poll(() => evaluateOnMap<number>(page, "return map.getPitch();"), { timeout: 20000 }).toBe(30);
    expect(await evaluateOnMap<number>(page, "return map.getBearing();")).toBe(15);
    expect(await evaluateOnMap<number>(page, "return map.getMinZoom();")).toBe(3);
    expect(await evaluateOnMap<number>(page, "return map.getMaxZoom();")).toBe(18);

    const container = page.locator("#map-api-container");
    await expect(container).toHaveClass(/map-api-extra-class/);
  });

  test("reacts to pitch parameter changes through map.configure", async ({ page }) => {
    await openFixture(page);
    await expect.poll(() => evaluateOnMap<number>(page, "return map.getPitch();"), { timeout: 20000 }).toBe(30);

    await page.getByTestId("toggle-pitch").click();
    await expect.poll(() => evaluateOnMap<number>(page, "return map.getPitch();"), { timeout: 10000 }).toBe(60);
  });

  test("flies the camera and raises moveend callbacks", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("fly-to").click();
    await expect
      .poll(() => evaluateOnMap<number>(page, "return map.getCenter().lng;"), { timeout: 20000 })
      .toBeCloseTo(7.0, 1);
    await expect
      .poll(async () => Number(await page.getByTestId("moveend-log").textContent()), { timeout: 10000 })
      .toBeGreaterThan(0);
  });

  test("fits the viewport around marker and shape features", async ({ page }) => {
    await openFixture(page);
    await page.getByTestId("fly-to").click();
    await expect
      .poll(() => evaluateOnMap<number>(page, "return map.getCenter().lng;"), { timeout: 20000 })
      .toBeCloseTo(7.0, 1);

    await page.getByTestId("fit-features").click();
    // m1 (49.62, 6.10) + c1 (49.58, 6.20) → center near (49.60, 6.15)
    await expect
      .poll(() => evaluateOnMap<number>(page, "return map.getCenter().lng;"), { timeout: 20000 })
      .toBeCloseTo(6.15, 1);
  });

  test("shows and closes transient popups", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("show-popup").click();
    await expect(page.locator(".maplibregl-popup-content")).toContainText("Transient popup", { timeout: 10000 });

    await page.getByTestId("close-popup").click();
    await expect(page.locator(".maplibregl-popup-content")).toHaveCount(0, { timeout: 10000 });
  });

  test("reads center, zoom, and bounds back into Blazor", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("read-view").click();
    await expect(page.getByTestId("view-log")).toHaveText(/center=49\.61,6\.14 zoom=12\.0 bounds=ok/, {
      timeout: 10000,
    });
  });

  test("adds and removes raster tile overlays", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("toggle-overlay").click();
    await expect
      .poll(() => evaluateOnMap<boolean>(page, 'return Boolean(map.getLayer("sgb-overlay-test-overlay"));'), {
        timeout: 10000,
      })
      .toBe(true);

    await page.getByTestId("toggle-overlay").click();
    await expect
      .poll(() => evaluateOnMap<boolean>(page, 'return Boolean(map.getLayer("sgb-overlay-test-overlay"));'), {
        timeout: 10000,
      })
      .toBe(false);
    await expect(page.getByTestId("map-error")).toHaveCount(0);
  });
});
