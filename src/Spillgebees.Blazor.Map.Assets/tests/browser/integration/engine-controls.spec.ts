import { expect, type Page, test } from "@playwright/test";
import { evaluateOnMap } from "./helpers";

// Functional coverage for the shared control component family hosted on SgbMap:
// native MapLibre controls, panel controls with Blazor-rendered content, and the
// display/overlay control panels driving the engine visibility system end to end.

const PAGE_ROUTE = "/engine-controls-functional-test";
const POINTS_LAYER_ID = "ctrl-points";
const NOTES_LAYER_ID = "notes-circle";

function layerVisibility(page: Page, layerId: string): Promise<string> {
  return evaluateOnMap<string>(
    page,
    `return map.getLayoutProperty(${JSON.stringify(layerId)}, "visibility") ?? "visible";`,
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
}

test.describe("engine controls", () => {
  test("MapPopup mounts Blazor content at its coordinate and closes", async ({ page }) => {
    await openFixture(page);

    const popupContent = page.locator(".maplibregl-popup [data-testid=popup-content]");
    await expect(popupContent).toBeVisible({ timeout: 20000 });
    await expect(popupContent).toHaveText("Popup from Blazor");

    await page.locator(".maplibregl-popup-close-button").click();
    await expect(popupContent).toBeHidden({ timeout: 10000 });
  });

  test("native navigation control mounts", async ({ page }) => {
    await openFixture(page);

    await expect(page.locator(".maplibregl-ctrl-zoom-in")).toBeVisible({ timeout: 20000 });
  });

  test("panel control mounts Blazor content and toggles open state", async ({ page }) => {
    await openFixture(page);

    const panelContent = page.getByTestId("panel-content");
    await expect(panelContent).toBeVisible({ timeout: 20000 });
    // content lives inside the panel control's body, not the hidden placeholder
    await expect(page.locator(".sgb-map-panel-control [data-testid=panel-content]")).toBeVisible();

    // closing via the panel toggle hides the content
    await page.locator(".sgb-map-panel-control .sgb-map-panel-toggle").first().click();
    await expect(panelContent).toBeHidden({ timeout: 10000 });
  });

  test("DisplayMapControl toggles drive engine layer visibility", async ({ page }) => {
    await openFixture(page);
    expect(await layerVisibility(page, POINTS_LAYER_ID)).toBe("visible");

    // the switch input is visually hidden behind a styled track — click its label
    const toggleLabel = page.locator('label:has([data-testid="map-display-toggle-points"])');
    await expect(toggleLabel).toBeVisible({ timeout: 20000 });
    await toggleLabel.click();
    await expect.poll(() => layerVisibility(page, POINTS_LAYER_ID), { timeout: 10000 }).toBe("none");

    await toggleLabel.click();
    await expect.poll(() => layerVisibility(page, POINTS_LAYER_ID), { timeout: 10000 }).toBe("visible");
  });

  test("LegendMapControl toggles display items through the binder", async ({ page }) => {
    await openFixture(page);
    expect(await layerVisibility(page, POINTS_LAYER_ID)).toBe("visible");

    const legendToggle = page.locator('label:has([data-testid="map-legend-toggle-points-legend"])');
    await expect(legendToggle).toBeVisible({ timeout: 20000 });
    await legendToggle.click();
    await expect.poll(() => layerVisibility(page, POINTS_LAYER_ID), { timeout: 10000 }).toBe("none");

    await legendToggle.click();
    await expect.poll(() => layerVisibility(page, POINTS_LAYER_ID), { timeout: 10000 }).toBe("visible");
  });

  test("OverlayMapControl toggles drive overlay visibility", async ({ page }) => {
    await openFixture(page);
    await expect.poll(() => layerVisibility(page, NOTES_LAYER_ID), { timeout: 20000 }).toBe("visible");

    const toggleLabel = page.locator('label:has([data-testid="map-overlay-toggle-notes"])');
    await expect(toggleLabel).toBeVisible({ timeout: 20000 });
    await toggleLabel.click();
    await expect.poll(() => layerVisibility(page, NOTES_LAYER_ID), { timeout: 10000 }).toBe("none");

    await toggleLabel.click();
    await expect.poll(() => layerVisibility(page, NOTES_LAYER_ID), { timeout: 10000 }).toBe("visible");

    // part-level toggle
    await page.locator('label:has([data-testid="map-overlay-toggle-notes-markers"])').click();
    await expect.poll(() => layerVisibility(page, NOTES_LAYER_ID), { timeout: 10000 }).toBe("none");
  });
});
