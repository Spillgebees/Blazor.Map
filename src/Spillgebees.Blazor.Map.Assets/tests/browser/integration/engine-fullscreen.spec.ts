import { expect, type Page, test } from "@playwright/test";

// Fullscreen control coverage: a custom UX SVG renders inside our first-class control, the
// stock control still renders by default, and a host-owned button drives the imperative API.
// True OS fullscreen transitions are unreliable under headless Chromium, so the deterministic
// enter/exit/toggle behaviour is covered by the engine/fullscreen.test.ts unit suite; here we
// assert the wiring is live and side-effect-free.

const PAGE_ROUTE = "/engine-fullscreen-functional-test";

async function openFixture(page: Page): Promise<void> {
  await page.goto(PAGE_ROUTE, { waitUntil: "domcontentloaded" });
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 60000 });
}

test.describe("engine fullscreen control", () => {
  test("renders the custom UX SVG inside the fullscreen control button", async ({ page }) => {
    await openFixture(page);

    const customControl = page.locator(".sgb-map-fullscreen-control");
    await expect(customControl.first()).toBeVisible({ timeout: 20000 });
    await expect(page.locator("[data-testid=fs-custom-enter]")).toBeVisible();
  });

  test("the default fullscreen control still renders a glyph button", async ({ page }) => {
    await openFixture(page);

    // two controls on the page; the un-customised one must still render an svg button
    const buttons = page.locator(".sgb-map-fullscreen-control button:has(svg)");
    await expect(buttons).toHaveCount(2, { timeout: 20000 });
  });

  test("the host toggle button drives the imperative API without erroring", async ({ page }) => {
    await openFixture(page);

    await expect(page.getByTestId("fullscreen-state")).toHaveText("fullscreen: false");
    await page.getByTestId("custom-toggle").click();

    // the call path must not surface a map error regardless of headless fullscreen support
    await expect(page.getByTestId("map-error")).toHaveCount(0);
    await expect(page.getByTestId("fullscreen-state")).toBeVisible();
  });

  test("renders overlay content inside the map container at a map-relative position", async ({ page }) => {
    // arrange
    await openFixture(page);
    const container = page.locator(".sgb-map-container").first();
    const overlay = page.getByTestId("fullscreen-overlay");

    // act
    const boxes = await Promise.all([container.boundingBox(), overlay.boundingBox()]);
    await overlay.click();

    // assert
    expect(boxes[0]).not.toBeNull();
    expect(boxes[1]).not.toBeNull();
    expect(Math.round((boxes[1]?.x ?? 0) - (boxes[0]?.x ?? 0))).toBe(24);
    expect(Math.round((boxes[1]?.y ?? 0) - (boxes[0]?.y ?? 0))).toBe(24);
    await expect(page.getByTestId("fullscreen-overlay-clicked")).toHaveText("overlay clicked: true");
    await expect(container.locator(".sgb-map-overlay-root [data-testid=fullscreen-overlay]")).toBeVisible();
  });

  test("pseudo fullscreen styles only promote the map container and keep overlay content map-relative", async ({
    page,
  }) => {
    // arrange
    await openFixture(page);
    const root = page.locator(".sgb-map-root").first();
    const container = page.locator(".sgb-map-container").first();
    const overlay = page.getByTestId("fullscreen-overlay");

    // act
    await root.evaluate((element) => element.classList.add("sgb-map-pseudo-fullscreen"));
    const rootOnly = await root.evaluate((element) => {
      const rootStyle = getComputedStyle(element);
      const containerStyle = getComputedStyle(element.querySelector(".sgb-map-container") as Element);

      return {
        display: rootStyle.display,
        position: containerStyle.position,
      };
    });
    await root.evaluate((element) => element.classList.remove("sgb-map-pseudo-fullscreen"));
    await container.evaluate((element) => element.classList.add("sgb-map-pseudo-fullscreen"));
    const containerPseudo = await container.evaluate((element) => ({
      height: getComputedStyle(element).height,
      position: getComputedStyle(element).position,
    }));
    const boxes = await Promise.all([container.boundingBox(), overlay.boundingBox()]);

    // assert
    expect(rootOnly).toEqual({ display: "contents", position: "relative" });
    expect(containerPseudo.position).toBe("fixed");
    expect(containerPseudo.height).toBe(`${page.viewportSize()?.height ?? 0}px`);
    expect(Math.round((boxes[1]?.x ?? 0) - (boxes[0]?.x ?? 0))).toBe(24);
    expect(Math.round((boxes[1]?.y ?? 0) - (boxes[0]?.y ?? 0))).toBe(24);
  });

  test("fullscreen styles cover WebKit native selectors", async ({ page }) => {
    // arrange
    await openFixture(page);

    // act
    const stylesheetText = await page.evaluate(() =>
      Array.from(document.styleSheets)
        .flatMap((sheet) => {
          try {
            return Array.from(sheet.cssRules).map((rule) => rule.cssText);
          } catch {
            return [];
          }
        })
        .join("\n"),
    );

    // assert
    expect(stylesheetText).toContain(".sgb-map-container:-webkit-full-screen");
  });
});
