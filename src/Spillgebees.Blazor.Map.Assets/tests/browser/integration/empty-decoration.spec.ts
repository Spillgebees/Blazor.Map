import { expect, test } from "@playwright/test";

test("tracked entity decorations with null text do not log browser errors", async ({ page }) => {
  const browserEvents: string[] = [];
  const pageErrors: string[] = [];

  page.on("console", (message) => {
    browserEvents.push(`console.${message.type()}: ${message.text()}`);
  });

  page.on("pageerror", (error) => {
    pageErrors.push(error.message);
  });

  page.on("requestfailed", (request) => {
    browserEvents.push(`requestfailed: ${request.url()} ${request.failure()?.errorText ?? ""}`);
  });

  page.on("response", (response) => {
    if (response.status() >= 400) {
      browserEvents.push(`response.${response.status()}: ${response.url()}`);
    }
  });

  await page.goto("/empty-decoration-test");

  await expect(page.getByTestId("apply-selected-state")).toBeVisible({ timeout: 15_000 });
  await expect(page.locator(".sgb-map-container canvas")).toBeVisible({ timeout: 15_000 });

  await page.waitForFunction(() => {
    const map = Array.from(window.Spillgebees?.Map?.maps?.values() ?? [])[0];

    return Boolean(map);
  });
  await page.waitForFunction(() => {
    const map = Array.from(window.Spillgebees?.Map?.maps?.values() ?? [])[0];

    try {
      return Boolean(map?.getSource("tracked-empty-decoration"));
    } catch {
      return false;
    }
  });

  await page.evaluate(() => {
    const map = Array.from(window.Spillgebees.Map.maps.values())[0];
    map?.on("error", (event) => {
      console.error(`[maplibre:error] ${event.error?.message ?? String(event.error)}`);
    });
  });

  await page.getByTestId("apply-selected-state").click();
  await page.waitForTimeout(500);

  const unexpectedBrowserEvents = browserEvents.filter(isUnexpectedBrowserEvent);
  const unexpectedPageErrors = pageErrors.filter(isUnexpectedPageError);

  expect({ browserEvents: unexpectedBrowserEvents, pageErrors: unexpectedPageErrors }).toEqual({
    browserEvents: [],
    pageErrors: [],
  });
});

function isUnexpectedBrowserEvent(event: string): boolean {
  return (
    event.startsWith("console.error:") ||
    event.startsWith("requestfailed:") ||
    event.includes("[maplibre:error]") ||
    event.includes("tracked-empty-decoration-decorations")
  );
}

function isUnexpectedPageError(error: string): boolean {
  return error.includes("tracked-empty-decoration-decorations");
}
