import { expect, test } from "@playwright/test";

const PAGE_ROUTE = "/engine-percent-sizing-functional-test";

test.describe("engine percent sizing", () => {
  test("Height and Width 100 percent fill a fixed-size parent", async ({ page }) => {
    // arrange
    await page.goto(PAGE_ROUTE, { waitUntil: "domcontentloaded" });
    const parent = page.getByTestId("percent-map-parent");
    const map = page.locator(".sgb-map-container");

    // act
    await expect(map).toBeAttached({ timeout: 60000 });

    // assert
    await expect
      .poll(() => parent.boundingBox(), { timeout: 60000 })
      .toMatchObject({
        width: 640,
        height: 360,
      });
    await expect
      .poll(() => map.boundingBox(), { timeout: 60000 })
      .toMatchObject({
        width: 640,
        height: 360,
      });
  });

  test("root wrapper does not create a sizing boundary", async ({ page }) => {
    // arrange
    await page.goto(PAGE_ROUTE, { waitUntil: "domcontentloaded" });
    const root = page.locator(".sgb-map-root");

    // act
    await expect(root).toBeAttached({ timeout: 60000 });
    const display = await root.evaluate((element) => getComputedStyle(element).display);

    // assert
    expect(display).toBe("contents");
  });
});
