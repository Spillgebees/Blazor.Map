import type { Map as MapLibreMap } from "maplibre-gl";
import { describe, expect, it } from "vitest";
import "../../test/maplibreMock";
import type { ILegendControlOptions } from "../interfaces/controls";
import { LegendControl } from "./legendControl";

function setRect(
  element: HTMLElement,
  rectangle: Pick<DOMRect, "top" | "right" | "bottom" | "left" | "width" | "height">,
): void {
  Object.defineProperty(element, "getBoundingClientRect", {
    configurable: true,
    value: () => ({
      ...rectangle,
      x: rectangle.left,
      y: rectangle.top,
      toJSON: () => rectangle,
    }),
  });
}

function createControlHost(className: string, mapContainer: HTMLElement): HTMLDivElement {
  const host = document.createElement("div");
  host.className = className;
  mapContainer.appendChild(host);
  return host;
}

function createMockMap(): MapLibreMap {
  return {} as MapLibreMap;
}

function createDefaultLegendOptions(overrides?: Partial<ILegendControlOptions>): ILegendControlOptions {
  return {
    enable: true,
    position: "top-right",
    title: "Legend",
    collapsible: true,
    initiallyOpen: true,
    className: null,
    ...overrides,
  };
}

describe("LegendControl", () => {
  describe("onAdd", () => {
    it("should create the expected DOM structure for a collapsible legend", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      content.hidden = true;
      content.textContent = "Legend content";
      placeholder.appendChild(content);
      const control = new LegendControl(createDefaultLegendOptions(), placeholder, content);

      // act
      const container = control.onAdd(createMockMap());

      // assert
      expect(container.classList.contains("maplibregl-ctrl")).toBe(true);
      expect(container.classList.contains("sgb-map-ctrl-group")).toBe(true);
      expect(container.classList.contains("sgb-map-legend")).toBe(true);
      expect(container.classList.contains("sgb-map-legend-collapsible")).toBe(true);
      expect(container.classList.contains("sgb-map-legend-open")).toBe(true);

      // toggle button is the first child with an SVG icon
      const toggleButton = container.querySelector("button.sgb-map-legend-toggle");
      expect(toggleButton).not.toBeNull();
      expect(toggleButton?.querySelector("svg")).not.toBeNull();
      expect(toggleButton?.getAttribute("aria-label")).toBe("Hide legend");
      expect(toggleButton?.getAttribute("aria-expanded")).toBe("true");

      // panel is the second child
      const panel = container.querySelector(".sgb-map-legend-panel");
      expect(panel).not.toBeNull();

      // panel header with title (no close button; toggle button morphs icon instead)
      const header = panel?.querySelector(".sgb-map-legend-header");
      expect(header).not.toBeNull();
      expect(header?.querySelector(".sgb-map-legend-title")?.textContent).toBe("Legend");
      expect(header?.querySelector("button.sgb-map-legend-close")).toBeNull();

      // panel body with content
      const body = panel?.querySelector(".sgb-map-legend-body");
      expect(body).not.toBeNull();
      expect(body?.textContent).toContain("Legend content");
      expect(content.hidden).toBe(false);
    });

    it("should create the expected DOM structure for a non-collapsible legend", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      content.hidden = true;
      content.textContent = "Legend content";
      placeholder.appendChild(content);
      const control = new LegendControl(createDefaultLegendOptions({ collapsible: false }), placeholder, content);

      // act
      const container = control.onAdd(createMockMap());

      // assert
      expect(container.classList.contains("maplibregl-ctrl")).toBe(true);
      expect(container.classList.contains("sgb-map-legend")).toBe(true);
      expect(container.classList.contains("sgb-map-legend-open")).toBe(true);
      expect(container.classList.contains("sgb-map-ctrl-group")).toBe(false);
      expect(container.classList.contains("sgb-map-legend-collapsible")).toBe(false);

      // no toggle button, no panel wrapper
      expect(container.querySelector("button.sgb-map-legend-toggle")).toBeNull();
      expect(container.querySelector(".sgb-map-legend-panel")).toBeNull();
      expect(container.querySelector("button.sgb-map-legend-close")).toBeNull();

      // header with title directly in container
      expect(container.querySelector(".sgb-map-legend-header .sgb-map-legend-title")?.textContent).toBe("Legend");

      // body directly in container
      expect(container.querySelector(".sgb-map-legend-body")?.textContent).toContain("Legend content");
      expect(content.hidden).toBe(false);
    });

    it("should omit header for non-collapsible legend without title", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      content.textContent = "Legend content";
      placeholder.appendChild(content);
      const control = new LegendControl(
        createDefaultLegendOptions({ collapsible: false, title: null }),
        placeholder,
        content,
      );

      // act
      const container = control.onAdd(createMockMap());

      // assert
      expect(container.querySelector(".sgb-map-legend-header")).toBeNull();
      expect(container.querySelector(".sgb-map-legend-body")?.textContent).toContain("Legend content");
    });

    it("should bound legend height to the map container", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      const mapContainer = document.createElement("div");
      Object.defineProperty(mapContainer, "clientHeight", {
        configurable: true,
        value: 480,
      });
      setRect(mapContainer, { top: 0, right: 320, bottom: 480, left: 0, width: 320, height: 480 });
      placeholder.appendChild(content);
      const control = new LegendControl(createDefaultLegendOptions(), placeholder, content);

      // act
      const container = control.onAdd({
        getContainer: () => mapContainer,
      } as MapLibreMap);

      // assert
      expect(container.style.getPropertyValue("--sgb-map-legend-max-height")).toBe("456px");
    });

    it("should reserve space for bottom attribution on the same side", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      const mapContainer = document.createElement("div");
      const topRightHost = createControlHost("maplibregl-ctrl-top-right", mapContainer);
      const bottomRightHost = createControlHost("maplibregl-ctrl-bottom-right", mapContainer);
      const attribution = document.createElement("div");

      Object.defineProperty(mapContainer, "clientHeight", {
        configurable: true,
        value: 600,
      });

      setRect(mapContainer, { top: 0, right: 320, bottom: 600, left: 0, width: 320, height: 600 });
      setRect(attribution, { top: 552, right: 308, bottom: 588, left: 188, width: 120, height: 36 });
      attribution.className = "maplibregl-ctrl-attrib";
      bottomRightHost.appendChild(attribution);
      placeholder.appendChild(content);

      const control = new LegendControl(createDefaultLegendOptions(), placeholder, content);
      const container = control.onAdd({
        getContainer: () => mapContainer,
      } as MapLibreMap) as HTMLDivElement;

      setRect(container, { top: 40, right: 308, bottom: 280, left: 68, width: 240, height: 240 });
      topRightHost.appendChild(container);

      // act
      control.update(createDefaultLegendOptions());

      // assert
      expect(container.style.getPropertyValue("--sgb-map-legend-max-height")).toBe("500px");
    });

    it("should include neighboring controls in the same corner when calculating available height", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      const mapContainer = document.createElement("div");
      const topRightHost = createControlHost("maplibregl-ctrl-top-right", mapContainer);
      const bottomRightHost = createControlHost("maplibregl-ctrl-bottom-right", mapContainer);
      const navigationControl = document.createElement("div");
      const scaleControl = document.createElement("div");

      Object.defineProperty(mapContainer, "clientHeight", {
        configurable: true,
        value: 400,
      });

      setRect(mapContainer, { top: 0, right: 320, bottom: 400, left: 0, width: 320, height: 400 });
      setRect(navigationControl, { top: 12, right: 308, bottom: 66, left: 252, width: 56, height: 54 });
      setRect(scaleControl, { top: 342, right: 308, bottom: 374, left: 232, width: 76, height: 32 });
      topRightHost.appendChild(navigationControl);
      bottomRightHost.appendChild(scaleControl);
      placeholder.appendChild(content);

      const control = new LegendControl(createDefaultLegendOptions(), placeholder, content);
      const container = control.onAdd({
        getContainer: () => mapContainer,
      } as MapLibreMap) as HTMLDivElement;

      setRect(container, { top: 76, right: 308, bottom: 256, left: 68, width: 240, height: 180 });
      topRightHost.appendChild(container);

      // act
      control.update(createDefaultLegendOptions());

      // assert
      expect(container.style.getPropertyValue("--sgb-map-legend-max-height")).toBe("254px");
    });

    it("should ignore controls from the opposite side", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      const mapContainer = document.createElement("div");
      const topLeftHost = createControlHost("maplibregl-ctrl-top-left", mapContainer);
      const bottomRightHost = createControlHost("maplibregl-ctrl-bottom-right", mapContainer);
      const attribution = document.createElement("div");

      Object.defineProperty(mapContainer, "clientHeight", {
        configurable: true,
        value: 400,
      });

      setRect(mapContainer, { top: 0, right: 320, bottom: 400, left: 0, width: 320, height: 400 });
      setRect(attribution, { top: 350, right: 308, bottom: 388, left: 188, width: 120, height: 38 });
      attribution.className = "maplibregl-ctrl-attrib";
      bottomRightHost.appendChild(attribution);
      placeholder.appendChild(content);

      const control = new LegendControl(createDefaultLegendOptions({ position: "top-left" }), placeholder, content);
      const container = control.onAdd({
        getContainer: () => mapContainer,
      } as MapLibreMap) as HTMLDivElement;

      setRect(container, { top: 12, right: 252, bottom: 212, left: 12, width: 240, height: 200 });
      topLeftHost.appendChild(container);

      // act
      control.update(createDefaultLegendOptions({ position: "top-left" }));

      // assert
      expect(container.style.getPropertyValue("--sgb-map-legend-max-height")).toBe("374px");
    });

    it("should toggle the open state when the toggle button is clicked", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      placeholder.appendChild(content);
      const control = new LegendControl(createDefaultLegendOptions({ initiallyOpen: false }), placeholder, content);

      // act
      const container = control.onAdd(createMockMap());
      const toggleButton = container.querySelector("button.sgb-map-legend-toggle") as HTMLButtonElement;
      toggleButton.click();

      // assert
      expect(container.classList.contains("sgb-map-legend-open")).toBe(true);
      expect(toggleButton.getAttribute("aria-expanded")).toBe("true");
      expect(toggleButton.getAttribute("aria-label")).toBe("Hide legend");
      const panel = container.querySelector(".sgb-map-legend-panel") as HTMLDivElement;
      expect(panel.hidden).toBe(false);
    });

    it("should close the legend when the toggle button is clicked while open", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      placeholder.appendChild(content);
      const control = new LegendControl(createDefaultLegendOptions({ initiallyOpen: true }), placeholder, content);
      const container = control.onAdd(createMockMap());

      // act
      const toggleButton = container.querySelector("button.sgb-map-legend-toggle") as HTMLButtonElement;
      toggleButton.click();

      // assert
      expect(container.classList.contains("sgb-map-legend-closed")).toBe(true);
      expect(container.classList.contains("sgb-map-legend-open")).toBe(false);
      expect(toggleButton.getAttribute("aria-expanded")).toBe("false");
      expect(toggleButton.getAttribute("aria-label")).toBe("Show legend");
      const panel = container.querySelector(".sgb-map-legend-panel") as HTMLDivElement;
      expect(panel.hidden).toBe(true);
    });

    it("should set correct aria attributes when initially closed", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      placeholder.appendChild(content);
      const control = new LegendControl(createDefaultLegendOptions({ initiallyOpen: false }), placeholder, content);

      // act
      const container = control.onAdd(createMockMap());

      // assert
      const toggleButton = container.querySelector("button.sgb-map-legend-toggle") as HTMLButtonElement;
      expect(toggleButton.getAttribute("aria-expanded")).toBe("false");
      expect(toggleButton.getAttribute("aria-label")).toBe("Show legend");
      expect(container.classList.contains("sgb-map-legend-closed")).toBe(true);
    });

    it("should show close icon when open and legend icon when closed", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      placeholder.appendChild(content);
      const control = new LegendControl(createDefaultLegendOptions({ initiallyOpen: false }), placeholder, content);
      const container = control.onAdd(createMockMap());
      const toggleButton = container.querySelector("button.sgb-map-legend-toggle") as HTMLButtonElement;

      // act & assert - initially closed: should show legend icon (viewBox="0 0 24 24")
      const closedSvg = toggleButton.querySelector("svg") as SVGElement;
      expect(closedSvg).not.toBeNull();
      expect(closedSvg.getAttribute("viewBox")).toBe("0 0 24 24");

      // act - open the legend
      toggleButton.click();

      // assert - now open: should show close icon (viewBox="0 0 14 14")
      const openSvg = toggleButton.querySelector("svg") as SVGElement;
      expect(openSvg).not.toBeNull();
      expect(openSvg.getAttribute("viewBox")).toBe("0 0 14 14");

      // act - close the legend again
      toggleButton.click();

      // assert - closed again: should show legend icon
      const closedAgainSvg = toggleButton.querySelector("svg") as SVGElement;
      expect(closedAgainSvg).not.toBeNull();
      expect(closedAgainSvg.getAttribute("viewBox")).toBe("0 0 24 24");
    });

    it("should omit header for collapsible legend without title", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      content.textContent = "Legend content";
      placeholder.appendChild(content);
      const control = new LegendControl(
        createDefaultLegendOptions({ collapsible: true, title: null }),
        placeholder,
        content,
      );

      // act
      const container = control.onAdd(createMockMap());

      // assert
      expect(container.querySelector("button.sgb-map-legend-toggle")).not.toBeNull();
      expect(container.querySelector(".sgb-map-legend-panel")).not.toBeNull();
      expect(container.querySelector(".sgb-map-legend-header")).toBeNull();
      expect(container.querySelector(".sgb-map-legend-body")?.textContent).toContain("Legend content");
    });
  });

  describe("onRemove", () => {
    it("should restore content to its placeholder host", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      placeholder.appendChild(content);
      const control = new LegendControl(createDefaultLegendOptions(), placeholder, content);
      control.onAdd(createMockMap());

      // act
      control.onRemove();

      // assert
      expect(placeholder.contains(content)).toBe(true);
      expect(content.hidden).toBe(true);
    });
  });

  describe("update", () => {
    it("should rebuild the shell when title and collapsible structure changes", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      placeholder.appendChild(content);
      const control = new LegendControl(createDefaultLegendOptions(), placeholder, content);
      const container = control.onAdd(createMockMap());

      // act
      control.update(
        createDefaultLegendOptions({
          title: null,
          collapsible: false,
        }),
      );

      // assert
      expect(container.querySelector(".sgb-map-legend-title")).toBeNull();
      expect(container.querySelector(".sgb-map-legend-toggle")).toBeNull();
      expect(container.querySelector(".sgb-map-legend-panel")).toBeNull();
      expect(container.classList.contains("sgb-map-legend-collapsible")).toBe(false);
      expect(container.classList.contains("sgb-map-ctrl-group")).toBe(false);
    });

    it("should preserve content when rebuilding structure", () => {
      // arrange
      const placeholder = document.createElement("div");
      const content = document.createElement("div");
      content.textContent = "Legend content";
      placeholder.appendChild(content);
      const control = new LegendControl(
        createDefaultLegendOptions({ title: null, collapsible: false }),
        placeholder,
        content,
      );
      const container = control.onAdd(createMockMap());

      // act
      control.update(createDefaultLegendOptions({ title: "Legend", collapsible: true }));

      // assert
      expect(container.querySelector(".sgb-map-legend-title")?.textContent).toBe("Legend");
      expect(container.querySelector("button.sgb-map-legend-toggle")).not.toBeNull();
      expect(container.querySelector(".sgb-map-legend-panel .sgb-map-legend-body")?.textContent).toContain(
        "Legend content",
      );
    });
  });
});
