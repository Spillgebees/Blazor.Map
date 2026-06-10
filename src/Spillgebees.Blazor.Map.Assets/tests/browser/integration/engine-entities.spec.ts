import { expect, type Page, test } from "@playwright/test";

// Functional coverage for the engine tracked entity layer: rendering, motion,
// membership, click/hover events, selection, decorations, clustering, and
// animation — all against the real map.

const PAGE_ROUTE = "/engine-entity-functional-test";
const SOURCE_ID = "entities";
const DECORATION_SOURCE_ID = "entities-decorations";
const SYMBOL_LAYER_ID = "entities-symbols";
const CLUSTER_LAYER_ID = "entities-clusters";
const DECORATION_LAYER_ID = "entities-decoration-badge";

async function openFixture(page: Page, query = "", readyLayerId = SYMBOL_LAYER_ID): Promise<void> {
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

function uniqueEntityIds(page: Page, layerId: string): Promise<string[]> {
  return evaluateOnMap<string[]>(
    page,
    `
    if (!map.getLayer(${JSON.stringify(layerId)})) return [];
    const features = map.queryRenderedFeatures({ layers: [${JSON.stringify(layerId)}] });
    return [...new Set(features.map((f) => f.properties?.entityId).filter(Boolean))].sort();
    `,
  );
}

function sourceEntityCoordinates(page: Page, entityId: string): Promise<[number, number] | null> {
  return evaluateOnMap<[number, number] | null>(
    page,
    `
    const features = map.querySourceFeatures(${JSON.stringify(SOURCE_ID)});
    const feature = features.find((f) => f.properties?.entityId === ${JSON.stringify(entityId)} && f.properties?.kind === "primary");
    return feature ? feature.geometry.coordinates : null;
    `,
  );
}

function featureState(page: Page, featureId: number, sourceId = SOURCE_ID): Promise<Record<string, unknown>> {
  return evaluateOnMap<Record<string, unknown>>(
    page,
    `return map.getFeatureState({ source: ${JSON.stringify(sourceId)}, id: ${featureId} });`,
  );
}

async function screenPositionOf(page: Page, lng: number, lat: number): Promise<{ x: number; y: number }> {
  const projected = await evaluateOnMap<{ x: number; y: number }>(
    page,
    `const p = map.project([${lng}, ${lat}]); return { x: p.x, y: p.y };`,
  );
  const box = await page.locator(".sgb-map-container canvas").boundingBox();
  expect(box).not.toBeNull();
  if (!box) {
    throw new Error("canvas not visible");
  }

  return { x: box.x + projected.x, y: box.y + projected.y };
}

test.describe("engine tracked entities", () => {
  test("renders primary symbols and decoration features", async ({ page }) => {
    await openFixture(page);

    expect(await uniqueEntityIds(page, SYMBOL_LAYER_ID)).toEqual(["e1", "e2", "e3"]);

    // decorations live in a sibling source so cluster counts stay entity-based
    const decorationIds = await evaluateOnMap<string[]>(
      page,
      `
      const features = map.querySourceFeatures(${JSON.stringify(DECORATION_SOURCE_ID)});
      return [...new Set(features.filter((f) => f.properties?.kind === "decoration").map((f) => f.properties?.entityId))].sort();
      `,
    );
    expect(decorationIds).toEqual(["e1", "e2", "e3"]);

    // decoration text travels in feature properties even when the style has no glyphs
    const decorationText = await evaluateOnMap<string | undefined>(
      page,
      `
      const features = map.querySourceFeatures(${JSON.stringify(DECORATION_SOURCE_ID)});
      return features.find((f) => f.properties?.entityId === "e1")?.properties?.text;
      `,
    );
    expect(decorationText).toBe("Vehicle 1");

    await expect.poll(() => uniqueEntityIds(page, DECORATION_LAYER_ID)).toContain("e1");
  });

  test("moves an entity through the motion path", async ({ page }) => {
    await openFixture(page);
    const before = await sourceEntityCoordinates(page, "e1");
    expect(before?.[0]).toBeCloseTo(6.1, 5);

    await page.getByTestId("move-e1").click();

    await expect
      .poll(async () => (await sourceEntityCoordinates(page, "e1"))?.[0], { timeout: 10000 })
      .toBeCloseTo(6.12, 5);
  });

  test("adds and removes entities", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("add-entity").click();
    await expect.poll(() => uniqueEntityIds(page, SYMBOL_LAYER_ID), { timeout: 10000 }).toContain("e4");

    await page.getByTestId("remove-e2").click();
    await expect.poll(() => uniqueEntityIds(page, SYMBOL_LAYER_ID), { timeout: 10000 }).not.toContain("e2");
  });

  test("click hands the domain item back to .NET", async ({ page }) => {
    await openFixture(page);

    const position = await screenPositionOf(page, 6.1, 49.6);
    await page.mouse.click(position.x, position.y);

    await expect(page.getByTestId("last-clicked")).toHaveText("e1", { timeout: 10000 });
  });

  test("hover applies feature-state locally and raises the enter callback", async ({ page }) => {
    await openFixture(page);

    const position = await screenPositionOf(page, 6.1, 49.6);
    await page.mouse.move(position.x, position.y);

    // e1 is the first entity → index 0 → primary feature id 0
    await expect.poll(() => featureState(page, 0).then((state) => state.hover), { timeout: 10000 }).toBe(true);
    await expect(page.getByTestId("last-hovered")).toHaveText("e1");

    await page.mouse.move(position.x + 200, position.y + 200);
    await expect
      .poll(() => featureState(page, 0).then((state) => state.hover ?? false), { timeout: 10000 })
      .toBe(false);
  });

  test("hovering a decoration label triggers the entity hover behavior", async ({ page }) => {
    await openFixture(page);

    // the "badge" decoration renders ~22px above the entity (offset 0,-20 → -2em at
    // text size 11); aim right of its center so the point lies OUTSIDE the 24px
    // hit-area radius — only the decoration layer itself can produce this hover
    // the invisible hit-area layer must mount (an invalid spec fails validation
    // silently from .NET's perspective — pin its existence here)
    const hasHitArea = await page.evaluate(() => {
      const map = [...(window.Spillgebees?.Map?.maps?.values() ?? [])][0];
      return Boolean(map?.getLayer("entities-hit-area"));
    });
    expect(hasHitArea).toBe(true);

    const position = await screenPositionOf(page, 6.1, 49.6);
    await page.mouse.move(position.x + 20, position.y - 22);

    await expect.poll(() => featureState(page, 0).then((state) => state.hover ?? false), { timeout: 10000 }).toBe(true);
    await expect(page.getByTestId("last-hovered")).toHaveText("e1");
  });

  test("selection applies and clears feature-state", async ({ page }) => {
    await openFixture(page);

    await page.getByTestId("select-e1").click();
    await expect.poll(() => featureState(page, 0).then((state) => state.selected), { timeout: 10000 }).toBe(true);
    // decorations carry the same state in their own source (id 1 = slot 0 of entity 0)
    await expect.poll(() => featureState(page, 1, DECORATION_SOURCE_ID).then((state) => state.selected)).toBe(true);

    await page.getByTestId("clear-selection").click();
    await expect
      .poll(() => featureState(page, 0).then((state) => state.selected ?? false), { timeout: 10000 })
      .toBe(false);
  });

  test("clusters render and clicking zooms toward expansion", async ({ page }) => {
    await openFixture(page, "?cluster=1", CLUSTER_LAYER_ID);

    const clusterCount = () =>
      evaluateOnMap<number>(
        page,
        `
        if (!map.getLayer(${JSON.stringify(CLUSTER_LAYER_ID)})) return 0;
        return map.queryRenderedFeatures({ layers: [${JSON.stringify(CLUSTER_LAYER_ID)}] }).length;
        `,
      );
    await expect.poll(clusterCount, { timeout: 20000 }).toBeGreaterThan(0);

    // point_count counts entities, not features — decorations must not inflate it
    // (the fixture has 50 entities, each with one decoration).
    const totalCounted = await evaluateOnMap<number>(
      page,
      `
      const features = map.querySourceFeatures(${JSON.stringify(SOURCE_ID)});
      const seen = new Set();
      let total = 0;
      for (const feature of features) {
        const key = feature.id ?? feature.properties?.cluster_id;
        if (seen.has(key)) continue;
        seen.add(key);
        total += feature.properties?.point_count ?? 1;
      }
      return total;
      `,
    );
    expect(totalCounted).toBe(50);

    const clusterCenter = await evaluateOnMap<[number, number]>(
      page,
      `return map.queryRenderedFeatures({ layers: [${JSON.stringify(CLUSTER_LAYER_ID)}] })[0].geometry.coordinates;`,
    );

    const zoomBefore = await evaluateOnMap<number>(page, "return map.getZoom();");
    const position = await screenPositionOf(page, clusterCenter[0], clusterCenter[1]);
    await page.mouse.click(position.x, position.y);

    await expect
      .poll(() => evaluateOnMap<number>(page, "return map.getZoom();"), { timeout: 10000 })
      .toBeGreaterThan(zoomBefore + 0.5);
  });

  test("animates positions toward motion targets", async ({ page }) => {
    await openFixture(page, "?animate=1");

    await page.getByTestId("move-e1").click();

    // mid-flight the longitude sits strictly between start (6.10) and target (6.12)
    await expect
      .poll(
        async () => {
          const coordinates = await sourceEntityCoordinates(page, "e1");
          const lng = coordinates?.[0] ?? 0;
          return lng > 6.1005 && lng < 6.1195;
        },
        { timeout: 5000, intervals: [50] },
      )
      .toBe(true);

    await expect
      .poll(async () => (await sourceEntityCoordinates(page, "e1"))?.[0], { timeout: 10000 })
      .toBeCloseTo(6.12, 5);
  });
});

test.describe("engine entity structural reconfiguration", () => {
  // structural parameters (decorations, clustering) must apply at runtime by
  // transparently rebuilding the engine layer — not only at creation time
  test("toggling decorations and clustering rebuilds the layer in place", async ({ page }) => {
    await page.goto("/engine-entity-stress-test?entities=100", { waitUntil: "domcontentloaded" });
    await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });

    const hasLayer = (layerId: string) =>
      page.evaluate((id) => {
        const maps = window.Spillgebees?.Map?.maps;
        const map = maps ? [...maps.values()][0] : undefined;
        return Boolean(map?.getLayer(id));
      }, layerId);

    await expect.poll(() => hasLayer("engine-stress-symbols"), { timeout: 20000 }).toBe(true);
    expect(await hasLayer("engine-stress-decoration-label")).toBe(false);

    await page.getByTestId("stress-decorations").selectOption("True");
    await expect.poll(() => hasLayer("engine-stress-decoration-label"), { timeout: 10000 }).toBe(true);
    await expect.poll(() => hasLayer("engine-stress-symbols"), { timeout: 10000 }).toBe(true);

    await page.getByTestId("stress-clustering").selectOption("True");
    await expect.poll(() => hasLayer("engine-stress-clusters"), { timeout: 10000 }).toBe(true);

    await page.getByTestId("stress-decorations").selectOption("False");
    await expect.poll(() => hasLayer("engine-stress-decoration-label"), { timeout: 10000 }).toBe(false);
    await expect.poll(() => hasLayer("engine-stress-clusters"), { timeout: 10000 }).toBe(true);
  });
});
