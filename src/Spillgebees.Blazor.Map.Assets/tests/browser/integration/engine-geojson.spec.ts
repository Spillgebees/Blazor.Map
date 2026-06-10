import { expect, type Page, test } from "@playwright/test";

// Functional coverage for raw GeoJSON sources + typed layers on the engine path:
// rendering, data updates, runtime paint changes, layer click events, and clustering
// with the full ClusterOptions model on a plain source.

const PAGE_ROUTE = "/engine-geojson-functional-test";
const CIRCLE_LAYER_ID = "places-circles";
const LINE_LAYER_ID = "places-lines";
const CLUSTER_LAYER_ID = "places-clusters";

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

function renderedNames(page: Page, layerId: string): Promise<string[]> {
  return evaluateOnMap<string[]>(
    page,
    `
    if (!map.getLayer(${JSON.stringify(layerId)})) return [];
    const features = map.queryRenderedFeatures({ layers: [${JSON.stringify(layerId)}] });
    return [...new Set(features.map((f) => f.properties?.name).filter(Boolean))].sort();
    `,
  );
}

async function openFixture(page: Page, query = "", readyLayerId = CIRCLE_LAYER_ID): Promise<void> {
  await page.goto(`${PAGE_ROUTE}${query}`, { waitUntil: "domcontentloaded" });
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });
  await expect
    .poll(
      () =>
        evaluateOnMap<number>(
          page,
          `
          if (!map.getLayer(${JSON.stringify(readyLayerId)})) return 0;
          return map.queryRenderedFeatures({ layers: [${JSON.stringify(readyLayerId)}] }).length;
          `,
        ),
      { timeout: 60000 },
    )
    .toBeGreaterThan(0);
}

test.describe("engine geojson sources", () => {
  test("renders typed circle and line layers from raw data", async ({ page }) => {
    await openFixture(page);

    expect(await renderedNames(page, CIRCLE_LAYER_ID)).toEqual(["place-1", "place-2", "place-3"]);
    await expect.poll(() => renderedNames(page, LINE_LAYER_ID)).toContain("track");
  });

  test("applies data updates through the scheduler", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("update-data").click();

    await expect
      .poll(() => renderedNames(page, CIRCLE_LAYER_ID), { timeout: 10000 })
      .toEqual(["place-1", "place-2", "place-3", "place-4"]);
  });

  test("applies paint changes at runtime", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("change-paint").click();

    await expect
      .poll(
        () =>
          evaluateOnMap<unknown>(
            page,
            `return map.getPaintProperty(${JSON.stringify(CIRCLE_LAYER_ID)}, "circle-radius");`,
          ),
        { timeout: 10000 },
      )
      .toBe(14);
    await expect
      .poll(() =>
        evaluateOnMap<unknown>(
          page,
          `return map.getPaintProperty(${JSON.stringify(CIRCLE_LAYER_ID)}, "circle-color");`,
        ),
      )
      .toBe("#16a34a");
  });

  test("routes layer clicks to .NET with feature properties", async ({ page }) => {
    await openFixture(page);

    const position = await evaluateOnMap<{ x: number; y: number }>(
      page,
      `const p = map.project([6.12, 49.6]); return { x: p.x, y: p.y };`,
    );
    const box = await page.locator(".sgb-map-container canvas").boundingBox();
    expect(box).not.toBeNull();
    if (!box) {
      return;
    }

    await page.mouse.click(box.x + position.x, box.y + position.y);

    await expect(page.getByTestId("last-clicked")).toHaveText("place-1", { timeout: 10000 });
  });

  test("clusters raw sources with generated layers and zooms on click", async ({ page }) => {
    await openFixture(page, "?cluster=1", CLUSTER_LAYER_ID);

    const zoomBefore = await evaluateOnMap<number>(page, "return map.getZoom();");
    const clusterCenter = await evaluateOnMap<[number, number]>(
      page,
      `return map.queryRenderedFeatures({ layers: [${JSON.stringify(CLUSTER_LAYER_ID)}] })[0].geometry.coordinates;`,
    );
    const position = await evaluateOnMap<{ x: number; y: number }>(
      page,
      `const p = map.project([${clusterCenter[0]}, ${clusterCenter[1]}]); return { x: p.x, y: p.y };`,
    );
    const box = await page.locator(".sgb-map-container canvas").boundingBox();
    expect(box).not.toBeNull();
    if (!box) {
      return;
    }

    await page.mouse.click(box.x + position.x, box.y + position.y);

    await expect
      .poll(() => evaluateOnMap<number>(page, "return map.getZoom();"), { timeout: 10000 })
      .toBeGreaterThan(zoomBefore + 0.5);
  });
});
