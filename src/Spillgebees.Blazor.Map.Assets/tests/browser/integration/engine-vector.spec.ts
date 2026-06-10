import { expect, type Page, test } from "@playwright/test";

// Functional coverage for VectorTileSource: vector tiles decode and render through
// the engine path, and layer paint updates apply at runtime.

const PAGE_ROUTE = "/engine-vector-functional-test";
const FILL_LAYER_ID = "vector-fill";

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

test.describe("engine vector tile sources", () => {
  test("renders vector tile features and applies paint updates", async ({ page }) => {
    await page.goto(PAGE_ROUTE, { waitUntil: "domcontentloaded" });
    await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });

    await expect
      .poll(
        () =>
          evaluateOnMap<number>(
            page,
            `
            if (!map.getLayer(${JSON.stringify(FILL_LAYER_ID)})) return 0;
            return map.queryRenderedFeatures({ layers: [${JSON.stringify(FILL_LAYER_ID)}] }).length;
            `,
          ),
        { timeout: 60000 },
      )
      .toBeGreaterThan(0);

    await page.getByTestId("change-paint").click();

    await expect
      .poll(
        () =>
          evaluateOnMap<unknown>(page, `return map.getPaintProperty(${JSON.stringify(FILL_LAYER_ID)}, "fill-color");`),
        { timeout: 10000 },
      )
      .toBe("#ea580c");
    await expect(page.getByTestId("map-error")).toHaveCount(0);
  });
});
