import { expect, type Page, test } from "@playwright/test";
import { evaluateOnMap } from "./helpers";

// Functional coverage for the engine visibility system: display items toggling runtime
// layers and feature filters, plus overlays composing overlay-style layers and runtime
// parts (visible = original AND groups AND overlay AND part).

const PAGE_ROUTE = "/engine-display-functional-test";
const POINTS_LAYER_ID = "disp-points";
const RUNTIME_PART_LAYER_ID = "overlay-runtime-circle";
const COMPOSED_LAYER_PREFIX = "sgb-overlay-style-annotations-";

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

test.describe("engine display + overlays", () => {
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

  test("overlay toggles hide composed style layers and runtime parts together", async ({ page }) => {
    await openFixture(page);
    const composed = await composedLayerIds(page);

    await page.getByTestId("toggle-overlay").click();
    await expect.poll(() => layerVisibility(page, composed[0]), { timeout: 10000 }).toBe("none");
    await expect.poll(() => layerVisibility(page, RUNTIME_PART_LAYER_ID), { timeout: 10000 }).toBe("none");

    await page.getByTestId("toggle-overlay").click();
    await expect.poll(() => layerVisibility(page, composed[0]), { timeout: 10000 }).toBe("visible");
    await expect.poll(() => layerVisibility(page, RUNTIME_PART_LAYER_ID), { timeout: 10000 }).toBe("visible");
  });

  test("part toggles affect only their own layers", async ({ page }) => {
    await openFixture(page);
    const composed = await composedLayerIds(page);

    await page.getByTestId("toggle-part").click();
    await expect.poll(() => layerVisibility(page, RUNTIME_PART_LAYER_ID), { timeout: 10000 }).toBe("none");
    expect(await layerVisibility(page, composed[0])).toBe("visible");

    await page.getByTestId("toggle-part").click();
    await expect.poll(() => layerVisibility(page, RUNTIME_PART_LAYER_ID), { timeout: 10000 }).toBe("visible");
  });
});
