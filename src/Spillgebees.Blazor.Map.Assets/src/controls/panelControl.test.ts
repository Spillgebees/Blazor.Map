import type { DotNet } from "@microsoft/dotnet-js-interop";
import type { Map as MapLibreMap } from "maplibre-gl";
import { describe, expect, it, vi } from "vitest";
import "../../tests/unit/maplibreMock";
import type { IPanelMapControl } from "../interfaces/controls";
import { PanelControl } from "./panelControl";

function createPanelOptions(overrides?: Partial<IPanelMapControl>): IPanelMapControl {
  return {
    kind: "panel",
    controlId: "filters",
    order: 500,
    visible: true,
    position: "top-right",
    label: "Filters",
    title: "Map filters",
    initiallyOpen: false,
    isOpen: null,
    maxWidth: null,
    className: null,
    ...overrides,
  };
}

function createMapContainer(): HTMLElement {
  const mapContainer = document.createElement("div");
  Object.defineProperty(mapContainer, "clientHeight", { configurable: true, value: 320 });
  mapContainer.getBoundingClientRect = () =>
    ({
      height: 320,
      width: 480,
      top: 0,
      right: 480,
      bottom: 320,
      left: 0,
      x: 0,
      y: 0,
      toJSON: () => ({}),
    }) as DOMRect;
  return mapContainer;
}

function createPanelControl(options?: Partial<IPanelMapControl>, stateReference?: DotNet.DotNetObject) {
  const placeholder = document.createElement("div");
  const content = document.createElement("div");
  content.hidden = true;
  content.textContent = "Layer filters";
  placeholder.appendChild(content);

  const control = new PanelControl(createPanelOptions(options), placeholder, content, stateReference);
  const mapContainer = createMapContainer();
  const container = control.onAdd({ getContainer: () => mapContainer } as MapLibreMap);

  return { container, content, placeholder, control };
}

describe("PanelControl", () => {
  it("should attach Blazor content inside a collapsed accessible panel shell", () => {
    // arrange & act
    const { container, content } = createPanelControl({ className: "filters-shell" });
    const button = container.querySelector("button") as HTMLButtonElement;
    const panel = container.querySelector(".sgb-map-panel") as HTMLDivElement;

    // assert
    expect(container.classList.contains("maplibregl-ctrl")).toBe(true);
    expect(container.classList.contains("sgb-map-panel-control")).toBe(true);
    expect(container.classList.contains("filters-shell")).toBe(true);
    expect(button.getAttribute("aria-label")).toBe("Filters");
    expect(button.title).toBe("Filters");
    expect(button.getAttribute("aria-expanded")).toBe("false");
    expect(button.getAttribute("aria-controls")).toBe(panel.id);
    expect(panel.hidden).toBe(true);
    expect(panel.textContent).toContain("Layer filters");
    expect(content.hidden).toBe(false);
  });

  it("should open and close from the toggle button", () => {
    // arrange
    const { container } = createPanelControl();
    const button = container.querySelector("button") as HTMLButtonElement;
    const panel = container.querySelector(".sgb-map-panel") as HTMLDivElement;

    // act
    button.click();

    // assert
    expect(panel.hidden).toBe(false);
    expect(button.getAttribute("aria-expanded")).toBe("true");
    expect(button.title).toBe("Close Filters");

    // act
    button.click();

    // assert
    expect(panel.hidden).toBe(true);
    expect(button.getAttribute("aria-expanded")).toBe("false");
  });

  it("should close on Escape without removing Blazor content", () => {
    // arrange
    const { container, content } = createPanelControl({ initiallyOpen: true });
    const panel = container.querySelector(".sgb-map-panel") as HTMLDivElement;

    // act
    panel.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true }));

    // assert
    expect(panel.hidden).toBe(true);
    expect(panel.contains(content)).toBe(true);
  });

  it("should leave Escape from nested content to the nested element", () => {
    // arrange
    const { container } = createPanelControl({ initiallyOpen: true });
    const panel = container.querySelector(".sgb-map-panel") as HTMLDivElement;
    const nestedInput = document.createElement("input");
    panel.appendChild(nestedInput);

    // act
    nestedInput.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true }));

    // assert
    expect(panel.hidden).toBe(false);
  });

  it("should not close when Escape was already prevented", () => {
    // arrange
    const { container } = createPanelControl({ initiallyOpen: true });
    const panel = container.querySelector(".sgb-map-panel") as HTMLDivElement;
    const event = new KeyboardEvent("keydown", { key: "Escape", bubbles: true, cancelable: true });
    event.preventDefault();

    // act
    panel.dispatchEvent(event);

    // assert
    expect(panel.hidden).toBe(false);
  });

  it("should preserve open state and content when options update", () => {
    // arrange
    const { container, content, control } = createPanelControl();
    const button = container.querySelector("button") as HTMLButtonElement;
    button.click();

    // act
    control.update(createPanelOptions({ title: "Updated filters", maxWidth: "18rem", className: "updated" }));

    // assert
    expect(container.classList.contains("updated")).toBe(true);
    expect(container.querySelector(".sgb-map-panel-title")?.textContent).toBe("Updated filters");
    expect((container.querySelector(".sgb-map-panel") as HTMLDivElement).hidden).toBe(false);
    expect(container.querySelector(".sgb-map-panel-body")?.firstElementChild).toBe(content);
  });

  it("should rebuild title header when title presence changes", () => {
    // arrange
    const { container, content, control } = createPanelControl({ title: null });
    expect(container.querySelector(".sgb-map-panel-title")).toBeNull();

    // act
    control.update(createPanelOptions({ title: "Updated filters" }));

    // assert
    expect(container.querySelector(".sgb-map-panel-title")?.textContent).toBe("Updated filters");
    expect(container.querySelector(".sgb-map-panel-body")?.firstElementChild).toBe(content);

    // act
    control.update(createPanelOptions({ title: null }));

    // assert
    expect(container.querySelector(".sgb-map-panel-title")).toBeNull();
    expect(container.querySelector(".sgb-map-panel-body")?.firstElementChild).toBe(content);
  });

  it("should apply controlled open state from updated options", () => {
    // arrange
    const { container, control } = createPanelControl({ isOpen: false });

    // act
    control.update(createPanelOptions({ isOpen: true }));

    // assert
    expect((container.querySelector(".sgb-map-panel") as HTMLDivElement).hidden).toBe(false);
    expect(container.querySelector("button")?.getAttribute("aria-expanded")).toBe("true");
  });

  it("should notify .NET when user changes panel open state", () => {
    // arrange
    const invokeMethodAsync = vi.fn().mockResolvedValue(undefined);
    const { container } = createPanelControl({ isOpen: false }, {
      invokeMethodAsync,
    } as unknown as DotNet.DotNetObject);
    const button = container.querySelector("button") as HTMLButtonElement;

    // act
    button.click();

    // assert
    // biome-ignore lint/security/noSecrets: JSInvokable method identifier, not a secret
    expect(invokeMethodAsync).toHaveBeenCalledWith("OnPanelOpenChangedAsync", true);
  });

  it("should log .NET callback failures with panel context", async () => {
    // arrange
    const invokeMethodAsync = vi.fn().mockRejectedValue(new Error("disposed"));
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);
    const { container } = createPanelControl({ isOpen: false }, {
      invokeMethodAsync,
    } as unknown as DotNet.DotNetObject);
    const button = container.querySelector("button") as HTMLButtonElement;

    // act
    button.click();
    await Promise.resolve();

    // assert
    expect(consoleError).toHaveBeenCalledWith(
      "[Spillgebees.Map] panel control 'filters' failed to report open state 'true'.",
      expect.any(Error),
    );

    consoleError.mockRestore();
  });

  it("should not mutate controlled open state until options update", () => {
    // arrange
    const invokeMethodAsync = vi.fn().mockResolvedValue(undefined);
    const { container } = createPanelControl({ isOpen: false }, {
      invokeMethodAsync,
    } as unknown as DotNet.DotNetObject);
    const button = container.querySelector("button") as HTMLButtonElement;
    const panel = container.querySelector(".sgb-map-panel") as HTMLDivElement;

    // act
    button.click();

    // assert
    expect(panel.hidden).toBe(true);
    expect(button.getAttribute("aria-expanded")).toBe("false");
    // biome-ignore lint/security/noSecrets: JSInvokable method identifier, not a secret
    expect(invokeMethodAsync).toHaveBeenCalledWith("OnPanelOpenChangedAsync", true);
  });

  it("should return content to placeholder when removed", () => {
    // arrange
    const { control, content, placeholder } = createPanelControl({ initiallyOpen: true });

    // act
    control.onRemove();

    // assert
    expect(placeholder.firstElementChild).toBe(content);
    expect(content.hidden).toBe(true);
  });
});
