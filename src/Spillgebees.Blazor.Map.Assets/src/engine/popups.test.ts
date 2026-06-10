import { beforeEach, describe, expect, it, type vi } from "vitest";
import {
  getMockMapConstructor,
  getMockPopupConstructor,
  type MockMapInstance,
  type MockPopupInstance,
  resetMockMapState,
} from "../../tests/unit/maplibreMock";
import type { PopupData } from "./ops";
import { createPopupController, type PopupController } from "./popups";

function popup(overrides?: Partial<PopupData>): PopupData {
  return {
    id: "p1",
    position: { latitude: 49.61, longitude: 6.13 },
    options: { content: "", contentMode: "text", trigger: "click", anchor: "auto", closeButton: true },
    ...overrides,
  };
}

interface Harness {
  controller: PopupController;
  container: HTMLElement;
  events: { handlerId: number; payload: unknown }[];
  contentFor(id: string): { placeholder: HTMLElement; content: HTMLElement };
}

function createHarness(): Harness {
  const MockMap = getMockMapConstructor();
  const map = new (MockMap as unknown as new () => MockMapInstance)();
  const container = document.createElement("div");
  const events: Harness["events"] = [];
  const controller = createPopupController(map as never, container, (handlerId, payload) =>
    events.push({ handlerId, payload }),
  );

  return {
    controller,
    container,
    events,
    contentFor(id) {
      const placeholder = document.createElement("div");
      placeholder.setAttribute("data-sgb-popup-placeholder", id);
      const content = document.createElement("div");
      placeholder.appendChild(content);
      container.appendChild(placeholder);
      return { placeholder, content };
    },
  };
}

function popupInstance(index = 0): MockPopupInstance {
  return getMockPopupConstructor().mock.instances[index] as unknown as MockPopupInstance;
}

function fireClose(index = 0): void {
  const instance = popupInstance(index);
  const close = (instance.on as ReturnType<typeof vi.fn>).mock.calls.find(
    (call: unknown[]) => call[0] === "close",
  )?.[1] as () => void;
  close();
}

describe("engine popup controller", () => {
  beforeEach(() => {
    resetMockMapState();
  });

  it("opens a popup with the component's DOM content", () => {
    const { controller, contentFor } = createHarness();
    const { content } = contentFor("p1");
    controller.set(popup());

    const instance = popupInstance();
    expect(instance.setLngLat).toHaveBeenCalledWith([6.13, 49.61]);
    expect(instance.setDOMContent).toHaveBeenCalledWith(content);
    expect(instance.addTo).toHaveBeenCalled();
    expect(controller.ids()).toEqual(["p1"]);
  });

  it("returns the content to its placeholder and raises the closed event", () => {
    const { controller, contentFor, events } = createHarness();
    const { placeholder, content } = contentFor("p1");
    controller.set(popup({ events: { closed: 11 } }));

    fireClose();

    expect(placeholder.contains(content)).toBe(true);
    expect(events).toEqual([{ handlerId: 11, payload: {} }]);
    expect(controller.ids()).toEqual([]);
  });

  it("suppresses the closed event on programmatic removal", () => {
    const { controller, contentFor, events } = createHarness();
    contentFor("p1");
    controller.set(popup({ events: { closed: 11 } }));

    controller.remove("p1");

    expect(popupInstance().remove).toHaveBeenCalled();
    expect(events).toEqual([]);
    expect(controller.ids()).toEqual([]);
  });

  it("replaces an existing popup for the same id without firing closed", () => {
    const { controller, contentFor, events } = createHarness();
    contentFor("p1");
    controller.set(popup({ events: { closed: 11 } }));
    controller.set(popup({ position: { latitude: 50, longitude: 7 }, events: { closed: 11 } }));

    expect(getMockPopupConstructor()).toHaveBeenCalledTimes(2);
    expect(events).toEqual([]);
    expect(controller.ids()).toEqual(["p1"]);
  });
});
