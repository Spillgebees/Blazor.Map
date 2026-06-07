import { expect, test } from "@playwright/test";

test("DisplayMapControl reflects initial state and toggles runtime layer visibility", async ({ page }) => {
  // arrange
  await page.goto("/display-control-test");
  const displayToggle = page.getByTestId("map-display-toggle-points");
  const displayToggleItem = page.locator("label.sgb-map-layer-control-item").filter({ hasText: "Points" });

  await expect(displayToggle).toBeAttached({ timeout: 15_000 });
  await expect(displayToggle).not.toBeChecked();
  await expect.poll(() => hasDisplayRuntimeLayer(), { timeout: 15_000 }).toBe(true);
  await expect.poll(() => readDisplayLayerVisibility(), { timeout: 15_000 }).toBe("none");

  // act
  await displayToggleItem.click();

  // assert
  await expect(displayToggle).toBeChecked();
  await expect.poll(() => readDisplayLayerVisibility(), { timeout: 15_000 }).toBe("visible");

  async function hasDisplayRuntimeLayer(): Promise<boolean> {
    return page.evaluate(() => {
      const mapElement = document.querySelector(".sgb-map-container") as HTMLElement | null;
      const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

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

  async function readDisplayLayerVisibility(): Promise<unknown> {
    return page.evaluate(() => {
      const mapElement = document.querySelector(".sgb-map-container") as HTMLElement | null;
      const map = mapElement ? window.Spillgebees.Map.maps.get(mapElement) ?? null : null;

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
});
