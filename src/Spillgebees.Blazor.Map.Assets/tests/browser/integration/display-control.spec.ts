import { expect, test } from "@playwright/test";
import type { Page } from "@playwright/test";

const runtimeReadinessTimeout = 30_000;
const runtimeLayerTimeout = 45_000;

test("DisplayMapControl reflects initial state and toggles runtime layer visibility", async ({ page }) => {
  test.setTimeout(90_000);

  // arrange
  await page.goto("/display-control-test");
  const displayToggle = page.getByTestId("map-display-toggle-points");
  const displayToggleItem = page.locator("label.sgb-map-layer-control-item").filter({ hasText: "Points" });

  await expect(displayToggle).toBeAttached({ timeout: 15_000 });
  await expect(displayToggle).not.toBeChecked();
  await waitForDisplayMapRuntimeReady(page);
  await waitForDisplayRuntimeLayer(page);
  await expect.poll(() => readDisplayLayerVisibility(page), { timeout: runtimeLayerTimeout }).toBe("none");

  // act
  await displayToggleItem.click();

  // assert
  await expect(displayToggle).toBeChecked();
  await expect.poll(() => readDisplayLayerVisibility(page), { timeout: runtimeLayerTimeout }).toBe("visible");
});

async function waitForDisplayMapRuntimeReady(page: Page): Promise<void> {
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: runtimeReadinessTimeout });

  try {
    await page.waitForFunction(
      () => {
        const mapElement = document.querySelector(".sgb-map-container") as HTMLElement | null;
        const map = mapElement ? window.Spillgebees?.Map?.maps?.get(mapElement) ?? null : null;

        try {
          return Boolean(map?.isStyleLoaded());
        } catch {
          return false;
        }
      },
      { timeout: runtimeReadinessTimeout },
    );
  } catch (error) {
    throw new Error(`Display map runtime was not ready. ${await readDisplayMapDiagnostics(page)}`, { cause: error });
  }
}

async function waitForDisplayRuntimeLayer(page: Page): Promise<void> {
  try {
    await expect.poll(() => hasDisplayRuntimeLayer(page), { timeout: runtimeLayerTimeout }).toBe(true);
  } catch (error) {
    throw new Error(`Display runtime source/layer was not registered. ${await readDisplayMapDiagnostics(page)}`, {
      cause: error,
    });
  }
}

async function hasDisplayRuntimeLayer(page: Page): Promise<boolean> {
  return page.evaluate(() => {
    const mapElement = document.querySelector(".sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees?.Map?.maps?.get(mapElement) ?? null : null;

    if (!map) {
      return false;
    }

    try {
      return Boolean(map.getSource("display-test-source") && map.getLayer("display-test-layer"));
    } catch {
      return false;
    }
  });
}

async function readDisplayLayerVisibility(page: Page): Promise<unknown> {
  return page.evaluate(() => {
    const mapElement = document.querySelector(".sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees?.Map?.maps?.get(mapElement) ?? null : null;

    try {
      if (!map?.getLayer("display-test-layer")) {
        return null;
      }

      return map.getLayoutProperty("display-test-layer", "visibility") ?? "visible";
    } catch {
      return null;
    }
  });
}

async function readDisplayMapDiagnostics(page: Page): Promise<string> {
  const diagnostics = await page.evaluate(() => {
    const mapElement = document.querySelector(".sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees?.Map?.maps?.get(mapElement) ?? null : null;

    if (!map) {
      return {
        hasNamespace: Boolean(window.Spillgebees?.Map),
        hasMapElement: Boolean(mapElement),
        hasMap: false,
      };
    }

    try {
      const style = map.getStyle();

      return {
        hasNamespace: Boolean(window.Spillgebees?.Map),
        hasMapElement: Boolean(mapElement),
        hasMap: true,
        isStyleLoaded: map.isStyleLoaded(),
        loaded: map.loaded(),
        sourceIds: Object.keys(style.sources ?? {}),
        layerIds: style.layers?.map((layer) => layer.id) ?? [],
      };
    } catch (error) {
      return {
        hasNamespace: Boolean(window.Spillgebees?.Map),
        hasMapElement: Boolean(mapElement),
        hasMap: true,
        error: error instanceof Error ? error.message : String(error),
      };
    }
  });

  return `Diagnostics: ${JSON.stringify(diagnostics)}`;
}
