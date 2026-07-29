import { beforeEach, describe, expect, it, vi } from "vitest";
import { getMockMapConstructor, type MockMapInstance, resetMockMapState } from "../../tests/unit/maplibreMock";
import { type ControlsController, createControlsController } from "./controls";
import type { ControlData } from "./ops";

function navigation(overrides?: Partial<ControlData>): ControlData {
  return { kind: "navigation", controlId: "nav", visible: true, position: "top-right", order: 100, ...overrides };
}

function panel(overrides?: Partial<ControlData>): ControlData {
  return {
    kind: "panel",
    controlId: "panel-1",
    visible: true,
    position: "top-right",
    order: 500,
    label: "Layers",
    initiallyOpen: true,
    ...overrides,
  };
}

function fullscreen(overrides?: Partial<ControlData>): ControlData {
  return { kind: "fullscreen", controlId: "fs", visible: true, position: "top-right", order: 200, ...overrides };
}

function center(overrides?: Partial<ControlData>): ControlData {
  return { kind: "center", controlId: "center", visible: true, position: "top-left", order: 100, ...overrides };
}

interface Harness {
  controller: ControlsController;
  map: MockMapInstance;
  container: HTMLElement;
  events: { handlerId: number; payload: unknown }[];
  contentFor(id: string): HTMLElement;
}

function createHarness(): Harness {
  const MockMap = getMockMapConstructor();
  const map = new (MockMap as unknown as new () => MockMapInstance)();
  const container = document.createElement("div");
  document.body.appendChild(container);
  map.getContainer = vi.fn().mockReturnValue(container);
  // behave like real MapLibre: addControl/removeControl drive the IControl lifecycle
  // (the mocked native control classes have no onAdd/onRemove — skip them)
  map.addControl = vi.fn().mockImplementation((control: { onAdd?: (m: unknown) => HTMLElement }) => {
    if (typeof control.onAdd === "function") {
      container.appendChild(control.onAdd(map));
    }
  });
  map.removeControl = vi.fn().mockImplementation((control: { onRemove?: () => void }) => {
    control.onRemove?.();
  });
  const events: Harness["events"] = [];
  const controller = createControlsController(map as never, container, (handlerId, payload) =>
    events.push({ handlerId, payload }),
  );

  return {
    controller,
    map,
    container,
    events,
    contentFor(id) {
      const placeholder = document.createElement("div");
      placeholder.setAttribute("data-sgb-control-placeholder", id);
      const content = document.createElement("div");
      placeholder.appendChild(content);
      container.appendChild(placeholder);
      return content;
    },
  };
}

describe("engine controls controller", () => {
  beforeEach(() => {
    resetMockMapState();
  });

  it("creates and attaches native controls", () => {
    const { controller, map } = createHarness();
    controller.set(navigation());

    expect(map.addControl).toHaveBeenCalledTimes(1);
    expect(map.addControl).toHaveBeenCalledWith(expect.anything(), "top-right");
    expect(controller.ids()).toEqual(["nav"]);
  });

  it("orders controls by position bucket then order then declaration", () => {
    const { controller } = createHarness();
    controller.set(navigation({ controlId: "b", order: 200 }));
    controller.set(navigation({ controlId: "a", order: 100 }));

    expect(controller.ids()).toEqual(["a", "b"]);
  });

  it("orders same-position controls when another position is interleaved", () => {
    const { controller } = createHarness();
    controller.set(navigation({ controlId: "last", order: 500 }));
    controller.set(center());
    controller.set(navigation({ controlId: "first", order: 100 }));

    expect(controller.ids()).toEqual(["first", "last", "center"]);
  });

  it("preserves same-position order when delayed custom content is attached", () => {
    const { controller, contentFor } = createHarness();
    contentFor("content-1");
    controller.set({ kind: "content", controlId: "content-1", visible: true, position: "top-right", order: 500 });
    controller.set(center());
    controller.set(navigation());

    controller.setContent("content-1");

    expect(controller.ids()).toEqual(["nav", "content-1", "center"]);
  });

  it("skips invisible controls and removes dropped ones", () => {
    const { controller, map } = createHarness();
    controller.set(navigation());
    controller.set(navigation({ controlId: "hidden", visible: false }));
    expect(controller.ids()).toEqual(["nav"]);

    controller.remove("nav");
    expect(controller.ids()).toEqual([]);
    expect(map.removeControl).toHaveBeenCalled();
  });

  it("reuses native control instances when the definition is unchanged", () => {
    const { controller, map } = createHarness();
    controller.set(navigation());
    const firstInstance = (map.addControl as ReturnType<typeof vi.fn>).mock.calls[0][0];

    controller.set(navigation({ order: 150 }));
    const secondInstance = (map.addControl as ReturnType<typeof vi.fn>).mock.calls.at(-1)?.[0];
    expect(secondInstance).toBe(firstInstance);
  });

  it("attaches custom content shells from the DOM convention", () => {
    const { controller, contentFor } = createHarness();
    const content = contentFor("content-1");
    content.hidden = true;

    controller.set({ kind: "content", controlId: "content-1", visible: true, position: "top-right", order: 500 });
    expect(controller.ids()).toEqual([]); // shell waits for its content binding

    controller.setContent("content-1");
    expect(controller.ids()).toEqual(["content-1"]);
  });

  it("drops the custom shell again on removeContent", () => {
    const { controller, contentFor } = createHarness();
    contentFor("content-1");
    controller.set({ kind: "content", controlId: "content-1", visible: true, position: "top-right", order: 500 });
    controller.setContent("content-1");

    controller.removeContent("content-1");
    expect(controller.ids()).toEqual([]);
  });

  it("mounts the fullscreen control as a first-class IControl with a custom icon", () => {
    const { controller, container } = createHarness();

    controller.set(fullscreen({ enterIcon: '<svg data-testid="fs-enter"></svg>' }));

    expect(controller.ids()).toEqual(["fs"]);
    expect(container.querySelector(".sgb-map-fullscreen-control")).not.toBeNull();
    expect(container.querySelector("[data-testid=fs-enter]")).not.toBeNull();
  });

  it("rebuilds the fullscreen control instance when its icon changes, but reuses it otherwise", () => {
    const { controller, map } = createHarness();
    controller.set(fullscreen({ enterIcon: '<svg data-testid="a"></svg>' }));
    const first = (map.addControl as ReturnType<typeof vi.fn>).mock.calls.at(-1)?.[0];

    controller.set(fullscreen({ enterIcon: '<svg data-testid="b"></svg>' }));
    const second = (map.addControl as ReturnType<typeof vi.fn>).mock.calls.at(-1)?.[0];
    expect(second).not.toBe(first);

    controller.set(fullscreen({ enterIcon: '<svg data-testid="b"></svg>', order: 250 }));
    const third = (map.addControl as ReturnType<typeof vi.fn>).mock.calls.at(-1)?.[0];
    expect(third).toBe(second);
  });

  it("threads a custom icon into the center control", () => {
    const { controller, container } = createHarness();

    controller.set(center({ icon: '<svg data-testid="center-custom"></svg>' }));

    expect(container.querySelector("[data-testid=center-custom]")).not.toBeNull();
  });

  it("routes panel open state through the engine event channel", () => {
    const { controller, contentFor, events } = createHarness();
    contentFor("panel-1");
    controller.set(panel());
    controller.setContent("panel-1", { openChanged: 42 });
    expect(controller.ids()).toEqual(["panel-1"]);

    const toggle = document.querySelector<HTMLButtonElement>(".sgb-map-panel-toggle");
    expect(toggle).not.toBeNull();
    toggle?.click();

    expect(events).toEqual([{ handlerId: 42, payload: { open: false } }]);
  });
});
