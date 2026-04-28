import type { Map as MapLibreMap } from "maplibre-gl";
import { describe, expect, it, vi } from "vitest";
import "../../test/maplibreMock";
import type { IContentMapControl } from "../interfaces/controls";
import { ContentControl } from "./contentControl";

function createDefaultContentOptions(overrides?: Partial<IContentMapControl>): IContentMapControl {
  return {
    kind: "content",
    controlId: "actions",
    order: 500,
    enabled: true,
    position: "top-right",
    className: null,
    ...overrides,
  };
}

describe("ContentControl", () => {
  it("should attach Blazor content to the MapLibre control shell", () => {
    // arrange
    const placeholder = document.createElement("div");
    const content = document.createElement("button");
    content.hidden = true;
    content.textContent = "Refresh";
    placeholder.appendChild(content);
    const control = new ContentControl(
      createDefaultContentOptions({ className: "sample-control" }),
      placeholder,
      content,
    );

    // act
    const container = control.onAdd({} as MapLibreMap);

    // assert
    expect(container.classList.contains("maplibregl-ctrl")).toBe(true);
    expect(container.classList.contains("sgb-map-custom-control")).toBe(true);
    expect(container.classList.contains("sample-control")).toBe(true);
    expect(container.textContent).toBe("Refresh");
    expect(content.hidden).toBe(false);
  });

  it("should return content to placeholder when removed", () => {
    // arrange
    const placeholder = document.createElement("div");
    const content = document.createElement("div");
    const control = new ContentControl(createDefaultContentOptions(), placeholder, content);
    control.onAdd({} as MapLibreMap);

    // act
    control.onRemove();

    // assert
    expect(placeholder.firstElementChild).toBe(content);
    expect(content.hidden).toBe(true);
  });

  it("should update shell classes without replacing Blazor content", () => {
    // arrange
    const placeholder = document.createElement("div");
    const content = document.createElement("div");
    placeholder.appendChild(content);
    const control = new ContentControl(createDefaultContentOptions(), placeholder, content);
    const container = control.onAdd({} as MapLibreMap);

    // act
    control.update(createDefaultContentOptions({ className: "updated-control" }));

    // assert
    expect(container.classList.contains("updated-control")).toBe(true);
    expect(container.firstElementChild).toBe(content);
  });

  it("should preserve DOM event handlers when content is relocated", () => {
    // arrange
    const placeholder = document.createElement("div");
    const content = document.createElement("button");
    const onClick = vi.fn();
    content.addEventListener("click", onClick);
    placeholder.appendChild(content);
    const control = new ContentControl(createDefaultContentOptions(), placeholder, content);

    // act
    const container = control.onAdd({} as MapLibreMap);
    content.click();
    control.onRemove();
    content.click();

    // assert
    expect(container.firstElementChild).not.toBe(content);
    expect(placeholder.firstElementChild).toBe(content);
    expect(onClick).toHaveBeenCalledTimes(2);
  });
});
