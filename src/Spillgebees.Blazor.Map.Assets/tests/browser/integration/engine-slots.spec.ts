import { expect, type Page, test } from "@playwright/test";

// Functional coverage for slots: the anchors sit below the base style's first label
// (skipping icon-only symbols), composed style layers go into slots by layer type,
// metadata and per-layer slots, layer components and tile overlays use their slots,
// and everything is re-anchored after a base style switch.

const PAGE_ROUTE = "/engine-slot-functional-test";
const COMPOSED_PREFIX = "sgb-overlay-style-test-slot-composed-";
const TILE_OVERLAY_LAYER = "sgb-overlay-slot-tiles";

const TILE_OVERLAYS_ANCHOR = "sgb-slot:sgb:tile-overlays";
const COMPOSED_GROUND_ANCHOR = "sgb-slot:sgb:composed-ground";
const BELOW_LABELS_ANCHOR = "sgb-slot:below-labels";
const COMPOSED_LABELS_ANCHOR = "sgb-slot:sgb:composed-labels";

function layersOrder(page: Page): Promise<string[]> {
  return page.evaluate(() => {
    const maps = window.Spillgebees?.Map?.maps;
    const map = maps ? [...maps.values()][0] : undefined;
    if (!map) {
      throw new Error("map not found");
    }

    return map.getLayersOrder();
  });
}

async function openFixture(page: Page): Promise<void> {
  await page.goto(PAGE_ROUTE, { waitUntil: "domcontentloaded" });
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });
  // wait until every producer has landed: composed style, tile overlay and layer components
  await expect
    .poll(
      async () => {
        const order = await layersOrder(page);
        const composedCount = order.filter((id) => id.startsWith(COMPOSED_PREFIX)).length;
        return (
          composedCount === 5 &&
          order.includes(TILE_OVERLAY_LAYER) &&
          order.includes("custom-below") &&
          order.includes("custom-top") &&
          order.includes("custom-before-top")
        );
      },
      { timeout: 60000 },
    )
    .toBe(true);
}

/** Asserts the full slot layout against the base style's last ground layer and first label. */
async function expectSlotOrder(page: Page, lastBaseGroundId: string, baseLabelsId: string): Promise<void> {
  const order = await layersOrder(page);
  const at = (id: string) => {
    const index = order.indexOf(id);
    expect(index, `layer '${id}' missing from ${JSON.stringify(order)}`).toBeGreaterThanOrEqual(0);
    return index;
  };

  const tileAnchor = at(TILE_OVERLAYS_ANCHOR);
  const groundAnchor = at(COMPOSED_GROUND_ANCHOR);
  const belowLabelsAnchor = at(BELOW_LABELS_ANCHOR);
  const labelsAnchor = at(COMPOSED_LABELS_ANCHOR);
  const baseLabels = at(baseLabelsId);

  // the whole base ground stays below the slots, including ground drawn after an
  // icon-only symbol layer
  expect(at(lastBaseGroundId)).toBeLessThan(at(TILE_OVERLAY_LAYER));

  // the tile overlay paints right above the base ground, below every vector layer we add
  expect(at(TILE_OVERLAY_LAYER)).toBeLessThan(tileAnchor);
  expect(tileAnchor).toBeLessThan(at(`${COMPOSED_PREFIX}composed-track`));

  // non-symbol composed layers go below labels, and so do symbols with sgb:slot metadata
  expect(at(`${COMPOSED_PREFIX}composed-track`)).toBeLessThan(groundAnchor);
  expect(at(`${COMPOSED_PREFIX}composed-caption-ground`)).toBeLessThan(groundAnchor);

  // below-labels layer components paint above composed ground, below the base labels
  expect(at("custom-below")).toBeGreaterThan(groundAnchor);
  expect(at("custom-below")).toBeLessThan(belowLabelsAnchor);
  expect(belowLabelsAnchor).toBeLessThan(baseLabels);

  // composed symbol layers go above the base labels...
  expect(at(`${COMPOSED_PREFIX}composed-label`)).toBeGreaterThan(baseLabels);
  expect(at(`${COMPOSED_PREFIX}composed-label`)).toBeLessThan(labelsAnchor);
  // ...together with circles placed there by sgb:slot metadata and by MapStyle.WithLayerSlot
  expect(at(`${COMPOSED_PREFIX}composed-dot-metadata`)).toBeGreaterThan(baseLabels);
  expect(at(`${COMPOSED_PREFIX}composed-dot-metadata`)).toBeLessThan(labelsAnchor);
  expect(at(`${COMPOSED_PREFIX}composed-dot-csharp`)).toBeGreaterThan(baseLabels);
  expect(at(`${COMPOSED_PREFIX}composed-dot-csharp`)).toBeLessThan(labelsAnchor);

  // above-labels layer components paint on top of everything
  expect(at("custom-top")).toBeGreaterThan(labelsAnchor);

  // Before beats Slot: this layer asks for below-labels, but its explicit target keeps
  // it at the top, right below custom-top
  expect(at("custom-before-top")).toBeGreaterThan(labelsAnchor);
  expect(at("custom-before-top")).toBeLessThan(at("custom-top"));
}

async function layerIndex(page: Page, id: string): Promise<number> {
  const order = await layersOrder(page);
  const index = order.indexOf(id);
  expect(index, `layer '${id}' missing from ${JSON.stringify(order)}`).toBeGreaterThanOrEqual(0);
  return index;
}

test.describe("engine slots", () => {
  test("places every layer in its slot around the base style's labels", async ({ page }) => {
    await openFixture(page);

    await expectSlotOrder(page, "base-bridges", "base-labels");
    await expect(page.getByTestId("map-error")).toHaveCount(0);
  });

  test("moves a layer between slots when its Slot parameter changes", async ({ page }) => {
    await openFixture(page);
    await expectSlotOrder(page, "base-bridges", "base-labels");

    // above labels moves the layer to the top of the map
    await page.getByTestId("toggle-custom-slot").click();
    await expect(page.getByTestId("custom-slot")).toHaveText("above-labels");
    await expect
      .poll(async () => {
        const order = await layersOrder(page);
        return order.indexOf("custom-below") > order.indexOf(COMPOSED_LABELS_ANCHOR);
      })
      .toBe(true);

    // below labels moves it back
    await page.getByTestId("toggle-custom-slot").click();
    await expect(page.getByTestId("custom-slot")).toHaveText("below-labels");
    await expect
      .poll(async () => {
        const order = await layersOrder(page);
        const index = order.indexOf("custom-below");
        return index > order.indexOf(COMPOSED_GROUND_ANCHOR) && index < order.indexOf(BELOW_LABELS_ANCHOR);
      })
      .toBe(true);
    await expect(page.getByTestId("map-error")).toHaveCount(0);
  });

  test("moves composed style layers when their slot changes without switching base style", async ({ page }) => {
    await openFixture(page);
    await expectSlotOrder(page, "base-bridges", "base-labels");

    await page.getByTestId("toggle-composed-track-slot").click();
    await expect(page.getByTestId("active-style")).toHaveText("slot-base");
    await expect(page.getByTestId("composed-track-slot")).toHaveText("above-labels");

    await expect
      .poll(async () => {
        const track = await layerIndex(page, `${COMPOSED_PREFIX}composed-track`);
        const baseLabels = await layerIndex(page, "base-labels");
        const labelsAnchor = await layerIndex(page, COMPOSED_LABELS_ANCHOR);
        return track > baseLabels && track < labelsAnchor;
      })
      .toBe(true);

    await page.getByTestId("toggle-composed-track-slot").click();
    await expect(page.getByTestId("composed-track-slot")).toHaveText("default");
    await expect
      .poll(async () => {
        const track = await layerIndex(page, `${COMPOSED_PREFIX}composed-track`);
        const groundAnchor = await layerIndex(page, COMPOSED_GROUND_ANCHOR);
        return track < groundAnchor;
      })
      .toBe(true);
    await expect(page.getByTestId("active-style")).toHaveText("slot-base");
    await expect(page.getByTestId("map-error")).toHaveCount(0);
  });

  test("honours Before over Slot with real layer order", async ({ page }) => {
    await openFixture(page);

    const belowLabelsAnchor = await layerIndex(page, BELOW_LABELS_ANCHOR);
    const beforeTop = await layerIndex(page, "custom-before-top");
    const customTop = await layerIndex(page, "custom-top");

    expect(beforeTop).toBeGreaterThan(belowLabelsAnchor);
    expect(beforeTop).toBeLessThan(customTop);
    await expect(page.getByTestId("map-error")).toHaveCount(0);
  });

  test("re-anchors every slot against the new base style after a style switch", async ({ page }) => {
    await openFixture(page);
    await expectSlotOrder(page, "base-bridges", "base-labels");

    await page.getByTestId("switch-style").click();
    await expect(page.getByTestId("reload-count")).toHaveText("1", { timeout: 20000 });

    // composed styles are recomposed asynchronously after the replay
    await expect
      .poll(async () => (await layersOrder(page)).filter((id) => id.startsWith(COMPOSED_PREFIX)).length, {
        timeout: 20000,
      })
      .toBe(5);

    await expectSlotOrder(page, "alt-ground", "alt-labels");
    await expect(page.getByTestId("map-error")).toHaveCount(0);

    // and switching back re-anchors against the original base
    await page.getByTestId("switch-style").click();
    await expect(page.getByTestId("reload-count")).toHaveText("2", { timeout: 20000 });
    await expect
      .poll(async () => (await layersOrder(page)).filter((id) => id.startsWith(COMPOSED_PREFIX)).length, {
        timeout: 20000,
      })
      .toBe(5);
    await expectSlotOrder(page, "base-bridges", "base-labels");
  });
});
