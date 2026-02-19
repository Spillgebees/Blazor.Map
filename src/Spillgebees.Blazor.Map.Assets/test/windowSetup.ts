/**
 * Resets `window.Spillgebees` and the idempotency flags between tests.
 * Call inside `beforeEach` in every test file that touches window globals.
 */
export function resetWindowGlobals(): void {
  // Remove the namespace entirely so bootstrap() can recreate it
  // @ts-expect-error - cleanup for test isolation requires removing required property
  delete window.Spillgebees;

  window.hasBeforeStartBeenCalledForSpillgebeesMap = false;
  window.hasAfterStartedBeenCalledForSpillgebeesMap = false;
}
