// Shared icon slot for the controls we own (center, fullscreen, …). A single chokepoint for
// rendering SVG markup into a control button, so the two controls stay consistent and there is
// one place to evolve icon handling. Icons are only (re)applied on control add and on a genuine
// glyph swap (enter↔exit) — never on a hot path.

/**
 * Renders SVG markup into a control button.
 *
 * SECURITY: `svg` is assigned via `innerHTML`. Pass only trusted, author-controlled markup —
 * never user-supplied content — or it becomes an XSS sink. Callers surface the same warning on
 * their public icon parameters.
 */
export function applyControlIcon(button: HTMLElement, svg: string): void {
  button.innerHTML = svg;
}
