import { describe, expect, it } from "vitest";
import { applyControlIcon } from "./icon";

const ICON_A = '<svg data-testid="icon-a" viewBox="0 0 24 24"><path d="M1 1" /></svg>';
const ICON_B = '<svg data-testid="icon-b" viewBox="0 0 24 24"><path d="M2 2" /></svg>';

describe("applyControlIcon", () => {
  it("renders the SVG markup into the button", () => {
    const button = document.createElement("button");

    applyControlIcon(button, ICON_A);

    expect(button.querySelector("svg")).not.toBeNull();
    expect(button.querySelector("[data-testid=icon-a]")).not.toBeNull();
  });

  it("replaces the DOM when the markup changes", () => {
    const button = document.createElement("button");
    applyControlIcon(button, ICON_A);
    applyControlIcon(button, ICON_B);

    expect(button.querySelector("[data-testid=icon-a]")).toBeNull();
    expect(button.querySelector("[data-testid=icon-b]")).not.toBeNull();
  });
});
