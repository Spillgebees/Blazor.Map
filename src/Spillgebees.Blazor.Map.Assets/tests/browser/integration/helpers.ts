import type { Page } from "@playwright/test";

// Runs a snippet against the first registered engine map and returns its result. `body` is a function
// body string with `map` in scope (e.g. "return map.getZoom();"). Shared by the integration specs,
// which each drive a single map.
export function evaluateOnMap<T>(page: Page, body: string): Promise<T> {
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
