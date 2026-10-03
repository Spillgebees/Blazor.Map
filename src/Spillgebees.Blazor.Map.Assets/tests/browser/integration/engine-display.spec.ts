import { expect, type Page, test } from "@playwright/test";
import { evaluateOnMap } from "./helpers";

// Functional coverage for the engine visibility system: display items toggling runtime
// layers, feature filters, base-style layers and composed overlay-style layers. Layers
// start as their style has them; an item that is on shows the layers it names, one that
// is off hides everything it targets, and off wins.

const PAGE_ROUTE = "/engine-display-functional-test";
const POINTS_LAYER_ID = "disp-points";
const BASE_LAYER_ID = "raster-layer";
const COMPOSED_LAYER_PREFIX = "sgb-overlay-style-annotations-";
const PRIMARY_STYLE_LAYER_ID = "overlay-circle";
const SECONDARY_STYLE_LAYER_ID = "overlay-secondary-circle";
const EXTRA_STYLE_LAYER_ID = "overlay-extra-circle";

function layerVisibility(page: Page, layerId: string): Promise<string> {
  return evaluateOnMap<string>(
    page,
    `return map.getLayoutProperty(${JSON.stringify(layerId)}, "visibility") ?? "visible";`,
  );
}

function composedLayerIds(page: Page): Promise<string[]> {
  return evaluateOnMap<string[]>(
    page,
    `return (map.getStyle()?.layers ?? []).map((l) => l.id).filter((id) => id.startsWith(${JSON.stringify(COMPOSED_LAYER_PREFIX)}));`,
  );
}

function composedLayerId(page: Page, originalLayerId: string): Promise<string | null> {
  return evaluateOnMap<string | null>(
    page,
    `
    const maps = window.Spillgebees?.Map;
    const key = ${JSON.stringify("annotations")} + String.fromCharCode(0) + ${JSON.stringify(originalLayerId)};
    const registration = maps?.composedStyleLayerIds?.get(map)?.get(key);
    return registration?.runtimeLayerId ?? null;
    `,
  );
}

function renderedFeatureCount(page: Page, layerId: string): Promise<number> {
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
  await expect
    .poll(
      () =>
        evaluateOnMap<number>(
          page,
          `
          if (!map.getLayer(${JSON.stringify(POINTS_LAYER_ID)})) return 0;
          return map.queryRenderedFeatures({ layers: [${JSON.stringify(POINTS_LAYER_ID)}] }).length;
          `,
        ),
      { timeout: 60000 },
    )
    .toBeGreaterThan(0);
  // overlay style composes asynchronously
  await expect.poll(() => composedLayerIds(page), { timeout: 30000 }).not.toHaveLength(0);
}

test.describe("engine display", () => {
  test("display items toggle runtime layer visibility", async ({ page }) => {
    await openFixture(page);
    expect(await layerVisibility(page, POINTS_LAYER_ID)).toBe("visible");

    await page.getByTestId("toggle-points").click();
    await expect.poll(() => layerVisibility(page, POINTS_LAYER_ID), { timeout: 10000 }).toBe("none");

    await page.getByTestId("toggle-points").click();
    await expect.poll(() => layerVisibility(page, POINTS_LAYER_ID), { timeout: 10000 }).toBe("visible");
  });

  test("feature display items compose negated filters onto the baseline", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("toggle-express").click();
    await expect
      .poll(() => evaluateOnMap<unknown>(page, `return map.getFilter(${JSON.stringify(POINTS_LAYER_ID)});`), {
        timeout: 10000,
      })
      .toEqual(["all", ["has", "name"], ["!", ["==", ["get", "kind"], "express"]]]);

    await page.getByTestId("toggle-express").click();
    await expect
      .poll(() => evaluateOnMap<unknown>(page, `return map.getFilter(${JSON.stringify(POINTS_LAYER_ID)});`))
      .toEqual(["has", "name"]);
  });

  test("unset items follow the style and can show layers the style hides", async ({ page }) => {
    await openFixture(page);
    const extraLayerId = await composedLayerId(page, EXTRA_STYLE_LAYER_ID);
    expect(extraLayerId).not.toBeNull();
    expect(await layerVisibility(page, extraLayerId!)).toBe("none");

    // the display control reflects the style default once the map reported it
    const toggle = page.getByTestId("map-display-toggle-extras");
    await expect(toggle).toBeEnabled({ timeout: 20000 });
    await expect(toggle).not.toBeChecked();

    await page.locator('label:has([data-testid="map-display-toggle-extras"])').click();
    await expect.poll(() => layerVisibility(page, extraLayerId!), { timeout: 10000 }).toBe("visible");
    await expect(toggle).toBeChecked();

    // a whole-style switch hides it, and switching the style back on keeps it shown
    await page.getByTestId("toggle-display-overlay-style").click();
    await expect.poll(() => layerVisibility(page, extraLayerId!), { timeout: 10000 }).toBe("none");
    await page.getByTestId("toggle-display-overlay-style").click();
    await expect.poll(() => layerVisibility(page, extraLayerId!), { timeout: 10000 }).toBe("visible");

    await page.locator('label:has([data-testid="map-display-toggle-extras"])').click();
    await expect.poll(() => layerVisibility(page, extraLayerId!), { timeout: 10000 }).toBe("none");
  });

  test("base-style layers come back after the overlay styles change", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("toggle-base").click();
    await expect.poll(() => layerVisibility(page, BASE_LAYER_ID), { timeout: 10000 }).toBe("none");

    await page.getByTestId("add-overlay-style").click();
    await expect
      .poll(
        () =>
          evaluateOnMap<number>(
            page,
            `return (map.getStyle()?.layers ?? []).filter((l) => l.id.startsWith("sgb-overlay-style-annotations-2-")).length;`,
          ),
        { timeout: 30000 },
      )
      .toBeGreaterThan(0);

    await page.getByTestId("toggle-base").click();
    await expect.poll(() => layerVisibility(page, BASE_LAYER_ID), { timeout: 10000 }).toBe("visible");
  });

  test("display hierarchy composes whole overlay style and individual style layer toggles visually", async ({
    page,
  }) => {
    await openFixture(page);
    const primaryLayerId = await composedLayerId(page, PRIMARY_STYLE_LAYER_ID);
    const secondaryLayerId = await composedLayerId(page, SECONDARY_STYLE_LAYER_ID);
    expect(primaryLayerId).not.toBeNull();
    expect(secondaryLayerId).not.toBeNull();

    await expect.poll(() => renderedFeatureCount(page, primaryLayerId!), { timeout: 10000 }).toBeGreaterThan(0);
    await expect.poll(() => renderedFeatureCount(page, secondaryLayerId!), { timeout: 10000 }).toBeGreaterThan(0);

    await page.getByTestId("toggle-display-overlay-style").click();
    await expect.poll(() => renderedFeatureCount(page, primaryLayerId!), { timeout: 10000 }).toBe(0);
    await expect.poll(() => renderedFeatureCount(page, secondaryLayerId!), { timeout: 10000 }).toBe(0);

    await page.getByTestId("toggle-display-overlay-style").click();
    await expect.poll(() => renderedFeatureCount(page, primaryLayerId!), { timeout: 10000 }).toBeGreaterThan(0);
    await expect.poll(() => renderedFeatureCount(page, secondaryLayerId!), { timeout: 10000 }).toBeGreaterThan(0);

    await page.getByTestId("toggle-display-overlay-secondary").click();
    await expect.poll(() => renderedFeatureCount(page, primaryLayerId!), { timeout: 10000 }).toBeGreaterThan(0);
    await expect.poll(() => renderedFeatureCount(page, secondaryLayerId!), { timeout: 10000 }).toBe(0);

    await page.getByTestId("toggle-display-overlay-style").click();
    await expect.poll(() => renderedFeatureCount(page, primaryLayerId!), { timeout: 10000 }).toBe(0);
    await expect.poll(() => renderedFeatureCount(page, secondaryLayerId!), { timeout: 10000 }).toBe(0);

    await page.getByTestId("toggle-display-overlay-style").click();
    await expect.poll(() => renderedFeatureCount(page, primaryLayerId!), { timeout: 10000 }).toBeGreaterThan(0);
    await expect.poll(() => renderedFeatureCount(page, secondaryLayerId!), { timeout: 10000 }).toBe(0);

    await page.getByTestId("toggle-display-overlay-secondary").click();
    await expect.poll(() => renderedFeatureCount(page, primaryLayerId!), { timeout: 10000 }).toBeGreaterThan(0);
    await expect.poll(() => renderedFeatureCount(page, secondaryLayerId!), { timeout: 10000 }).toBeGreaterThan(0);
  });
});
