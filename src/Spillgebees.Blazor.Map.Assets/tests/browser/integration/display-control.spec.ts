import { expect, test } from "@playwright/test";
import type { Page } from "@playwright/test";

const runtimeReadinessTimeout = 30_000;
const runtimeLayerTimeout = 45_000;
const displayTestLayerId = "display-test-layer";

type LayerVisibility = "none" | "visible";

type DisplayControlWindow = Window & {
  __displayControlMapLibreErrorForwardingAttached?: boolean;
  __displayControlMapLibreErrors?: string[];
};

test("DisplayMapControl reflects initial state and toggles display item", async ({ page }) => {
  test.setTimeout(90_000);

  // arrange
  await page.goto("/display-control-test");
  await waitForDisplayMapAndForwardMapLibreErrors(page);
  await waitForDisplayMapRuntimeReady(page);
  await waitForDisplayRuntimeLayer(page);

  const displayToggle = page.locator(".sgb-map-panel:not([hidden])").getByTestId("map-display-toggle-points");
  const displayToggleControl = page
    .locator(".sgb-map-panel:not([hidden]) .sgb-map-layer-control-item")
    .filter({ hasText: "Points" });

  await expect(displayToggle).toBeAttached({ timeout: 15_000 });
  await expect(displayToggleControl).toBeVisible();
  await expect(displayToggle).not.toBeChecked();
  await expectLayerVisibility(page, "none");

  // act
  await displayToggleControl.click();

  // assert
  await expect(displayToggle).toBeChecked();
  await expectLayerVisibility(page, "visible");

  // act
  await displayToggleControl.click();

  // assert
  await expect(displayToggle).not.toBeChecked();
  await expectLayerVisibility(page, "none");
});

async function waitForDisplayMapAndForwardMapLibreErrors(page: Page): Promise<void> {
  try {
    await page.waitForFunction(
      () => {
        const map = Array.from(window.Spillgebees?.Map?.maps?.values() ?? [])[0] ?? null;

        return Boolean(map);
      },
      undefined,
      { timeout: runtimeReadinessTimeout },
    );
  } catch (error) {
    throw new Error(`Display map object was not registered. ${await readDisplayMapDiagnostics(page)}`, { cause: error });
  }

  await page.evaluate(() => {
    const currentWindow = window as DisplayControlWindow;
    const map = Array.from(currentWindow.Spillgebees?.Map?.maps?.values() ?? [])[0] ?? null;

    currentWindow.__displayControlMapLibreErrors ??= [];

    if (!map || currentWindow.__displayControlMapLibreErrorForwardingAttached) {
      return;
    }

    currentWindow.__displayControlMapLibreErrorForwardingAttached = true;
    map.on("error", (event) => {
      const message = event.error?.message ?? String(event.error);
      currentWindow.__displayControlMapLibreErrors?.push(message);
      console.error(`[maplibre:error] ${message}`);
    });
  });
}

async function waitForDisplayMapRuntimeReady(page: Page): Promise<void> {
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: runtimeReadinessTimeout });

  try {
    await page.waitForFunction(
      () => {
        const mapElement = document.querySelector(".sgb-map-container") as HTMLElement | null;
        const map = mapElement ? window.Spillgebees?.Map?.maps?.get(mapElement) ?? null : null;

        try {
          return Boolean(map?.isStyleLoaded?.());
        } catch {
          return false;
        }
      },
      undefined,
      { timeout: runtimeReadinessTimeout },
    );
  } catch (error) {
    throw new Error(`Display map runtime was not ready. ${await readDisplayMapDiagnostics(page)}`, { cause: error });
  }
}

async function waitForDisplayRuntimeLayer(page: Page): Promise<void> {
  try {
    await expect.poll(async () => hasDisplayRuntimeLayer(page), { timeout: runtimeLayerTimeout }).toBe(true);
  } catch (error) {
    throw new Error(`Display runtime source/layer was not registered. ${await readDisplayMapDiagnostics(page)}`, {
      cause: error,
    });
  }
}

async function hasDisplayRuntimeLayer(page: Page): Promise<boolean> {
  return await page.evaluate((layerId) => {
    const mapElement = document.querySelector(".sgb-map-container") as HTMLElement | null;
    const map = mapElement ? window.Spillgebees?.Map?.maps?.get(mapElement) ?? null : null;

    if (!map) {
      return false;
    }

    try {
      return Boolean(map.getSource("display-test-source") && map.getLayer(layerId));
    } catch {
      return false;
    }
  }, displayTestLayerId);
}

async function expectLayerVisibility(page: Page, expectedVisibility: LayerVisibility): Promise<void> {
  try {
    await expect.poll(async () => readLayerVisibility(page), { timeout: runtimeReadinessTimeout }).toBe(expectedVisibility);
  } catch (error) {
    throw new Error(`Expected ${displayTestLayerId} visibility to be ${expectedVisibility}. ${await readDisplayMapDiagnostics(page)}`, {
      cause: error,
    });
  }
}

async function readLayerVisibility(page: Page): Promise<LayerVisibility | null> {
  return await page.evaluate((layerId) => {
    const map = Array.from(window.Spillgebees?.Map?.maps?.values() ?? [])[0] ?? null;

    if (!map) {
      return null;
    }

    try {
      const visibility = map.getLayoutProperty(layerId, "visibility");

      return visibility === "none" ? "none" : "visible";
    } catch {
      return null;
    }
  }, displayTestLayerId);
}

async function readDisplayMapDiagnostics(page: Page): Promise<string> {
  const diagnostics = await page.evaluate(() => {
    const currentWindow = window as DisplayControlWindow;
    const mapElement = document.querySelector(".sgb-map-container") as HTMLElement | null;
    const map = Array.from(currentWindow.Spillgebees?.Map?.maps?.values() ?? [])[0] ?? null;

    if (!map) {
      return {
        hasNamespace: Boolean(currentWindow.Spillgebees?.Map),
        hasMapElement: Boolean(mapElement),
        hasMap: false,
        mapLibreErrors: currentWindow.__displayControlMapLibreErrors ?? [],
      };
    }

    try {
      const style = map.getStyle?.();

      return {
        hasNamespace: Boolean(currentWindow.Spillgebees?.Map),
        hasMapElement: Boolean(mapElement),
        hasMap: true,
        isStyleLoaded: map.isStyleLoaded?.(),
        loaded: map.loaded?.(),
        mapOptions: currentWindow.Spillgebees?.Map?.mapOptions?.get(map),
        style: currentWindow.Spillgebees?.Map?.styles?.get(map),
        sourceIds: Object.keys(style?.sources ?? {}),
        layerIds: style?.layers?.map((layer) => layer.id) ?? [],
        mapLibreErrors: currentWindow.__displayControlMapLibreErrors ?? [],
      };
    } catch (error) {
      return {
        hasNamespace: Boolean(currentWindow.Spillgebees?.Map),
        hasMapElement: Boolean(mapElement),
        hasMap: true,
        error: error instanceof Error ? error.message : String(error),
        mapLibreErrors: currentWindow.__displayControlMapLibreErrors ?? [],
      };
    }
  });

  return `Diagnostics: ${JSON.stringify(diagnostics)}`;
}
