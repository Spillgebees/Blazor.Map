import { expect, test } from "@playwright/test";

test.describe("interaction legend guide", () => {
  test("opens and dismisses the page-level interaction reference", async ({ page }) => {
    const errors: string[] = [];
    page.on("console", (message) => {
      if (message.type() === "error") errors.push(message.text());
    });
    page.on("pageerror", (error) => errors.push(error.message));

    await page.goto("/guides/interaction-legend");

    const trigger = page.getByRole("button", { name: "Interaction guide" });
    const reference = page.getByRole("dialog", { name: "Move around the map" });

    await expect(reference).toBeHidden();
    await trigger.click();
    await expect(reference).toBeVisible();
    await expect(reference.locator(".interaction-reference-row")).toHaveCount(24);
    await expect(reference).toContainText("Ctrl/Cmd + scroll");
    await expect(reference).toContainText("Two-finger drag");
    await expect(reference).toContainText("Three-finger vertical drag");

    await page.keyboard.press("Escape");

    await expect(reference).toBeHidden();
    await expect(trigger).toBeFocused();
    expect(errors).toEqual([]);
  });

  test("keeps the interaction reference within a mobile viewport", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/guides/interaction-legend");
    await page.getByRole("button", { name: "Interaction guide" }).click();

    const reference = page.getByRole("dialog", { name: "Move around the map" });
    await expect(reference).toBeVisible();

    for (const viewport of [
      { width: 390, height: 844 },
      { width: 360, height: 640 },
      { width: 640, height: 360 },
    ]) {
      await page.setViewportSize(viewport);

      const dimensions = await reference.evaluate((element) => ({
        clientWidth: element.clientWidth,
        scrollWidth: element.scrollWidth,
      }));
      const bounds = await reference.boundingBox();

      expect(dimensions.scrollWidth).toBe(dimensions.clientWidth);
      expect(bounds).not.toBeNull();
      expect(bounds!.x).toBeGreaterThanOrEqual(0);
      expect(bounds!.y).toBeGreaterThanOrEqual(0);
      expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(viewport.width);
      expect(bounds!.y + bounds!.height).toBeLessThanOrEqual(viewport.height);
    }
  });

  test("shows the full interaction reference on common desktop displays", async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto("/guides/interaction-legend");
    await page.getByRole("button", { name: "Interaction guide" }).click();

    const reference = page.getByRole("dialog", { name: "Move around the map" });
    await expect(reference).toBeVisible();

    for (const viewport of [
      { width: 1920, height: 1080 },
      { width: 3840, height: 2160 },
    ]) {
      await page.setViewportSize(viewport);

      const dimensions = await reference.evaluate((element) => ({
        clientHeight: element.clientHeight,
        scrollHeight: element.scrollHeight,
      }));

      expect(dimensions.scrollHeight).toBe(dimensions.clientHeight);
    }
  });
});
