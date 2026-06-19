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
});
