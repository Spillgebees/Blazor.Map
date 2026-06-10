import { expect, type Page, test } from "@playwright/test";

// Functional coverage for V2 typed styles: overlay composition, base style switching
// with full scene replay (entities, images, overlays survive), and theme switching.

const PAGE_ROUTE = "/engine-style-functional-test";
const SYMBOL_LAYER_ID = "entities-symbols";
const OVERLAY_LAYER_PREFIX = "sgb-overlay-style-test-overlay-";

function evaluateOnMap<T>(page: Page, body: string): Promise<T> {
  return page.evaluate(
    ({ evalBody }) => {
      const maps = window.Spillgebees?.Map?.maps;
      const map = maps ? [...maps.values()][0] : undefined;
      if (!map) {
        throw new Error("map not found");
      }

      // biome-ignore lint/security/noGlobalEval: test-only helper running fixed strings
      return new Function("map", evalBody)(map) as T;
    },
    { evalBody: body },
  );
}

function renderedEntityCount(page: Page): Promise<number> {
  return evaluateOnMap<number>(
    page,
    `
    if (!map.getLayer(${JSON.stringify(SYMBOL_LAYER_ID)})) return 0;
    const features = map.queryRenderedFeatures({ layers: [${JSON.stringify(SYMBOL_LAYER_ID)}] });
    return new Set(features.map((f) => f.properties?.entityId)).size;
    `,
  );
}

function overlayLayerCount(page: Page): Promise<number> {
  return evaluateOnMap<number>(
    page,
    `return (map.getStyle()?.layers ?? []).filter((l) => l.id.startsWith(${JSON.stringify(OVERLAY_LAYER_PREFIX)})).length;`,
  );
}

async function openFixture(page: Page): Promise<void> {
  await page.goto(PAGE_ROUTE, { waitUntil: "domcontentloaded" });
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });
  await expect.poll(() => renderedEntityCount(page), { timeout: 60000 }).toBe(2);
}

test.describe("engine styles", () => {
  test("composes overlay styles on load", async ({ page }) => {
    await openFixture(page);

    await expect.poll(() => overlayLayerCount(page), { timeout: 20000 }).toBeGreaterThan(0);
  });

  test("switching the base style replays entities, images, and overlays", async ({ page }) => {
    await openFixture(page);
    await expect.poll(() => overlayLayerCount(page), { timeout: 20000 }).toBeGreaterThan(0);

    await page.getByTestId("switch-style").click();

    await expect(page.getByTestId("reload-count")).toHaveText("1", { timeout: 20000 });
    await expect(page.getByTestId("active-style")).toHaveText("blue");
    // the engine scene survives the style swap
    await expect.poll(() => renderedEntityCount(page), { timeout: 20000 }).toBe(2);
    await expect.poll(() => evaluateOnMap<boolean>(page, `return map.hasImage("style-test-dot");`)).toBe(true);
    await expect.poll(() => overlayLayerCount(page), { timeout: 20000 }).toBeGreaterThan(0);
    await expect(page.getByTestId("map-error")).toHaveCount(0);

    // and switching back works too
    await page.getByTestId("switch-style").click();
    await expect(page.getByTestId("reload-count")).toHaveText("2", { timeout: 20000 });
    await expect.poll(() => renderedEntityCount(page), { timeout: 20000 }).toBe(2);
  });

  test("theme switches toggle the dark class without touching the map", async ({ page }) => {
    await openFixture(page);
    const container = page.locator(".sgb-map-container");

    await page.getByTestId("toggle-theme").click();
    await expect(container).toHaveClass(/sgb-map-dark/);

    await page.getByTestId("toggle-theme").click();
    await expect(container).not.toHaveClass(/sgb-map-dark/);

    await expect.poll(() => renderedEntityCount(page)).toBe(2);
  });
});
