import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  getMockMarkerConstructor,
  getMockPopupConstructor,
  type MockMarkerInstance,
  type MockPopupInstance,
  resetMockMapState,
} from "../../test/maplibreMock";
import { createMarkerController, type MarkerEventKind } from "./markers";
import type { MarkerData } from "./ops";

interface RecordedEvent {
  kind: MarkerEventKind;
  markerId: string;
  lng: number;
  lat: number;
}

function markerInstance(index = 0): MockMarkerInstance {
  return getMockMarkerConstructor().mock.instances[index] as unknown as MockMarkerInstance;
}

function popupInstance(index = 0): MockPopupInstance {
  return getMockPopupConstructor().mock.instances[index] as unknown as MockPopupInstance;
}

function elementOf(instance: MockMarkerInstance): HTMLElement {
  return (instance.getElement as unknown as () => HTMLElement)();
}

function marker(overrides?: Partial<MarkerData>): MarkerData {
  return {
    id: "m1",
    position: { latitude: 51.5, longitude: -0.09 },
    ...overrides,
  };
}

function createHarness() {
  const events: RecordedEvent[] = [];
  const map = { getCanvas: vi.fn().mockReturnValue({ style: {}, dispatchEvent: vi.fn() }) };
  const controller = createMarkerController(map as never, (kind, markerId, lng, lat) =>
    events.push({ kind, markerId, lng, lat }),
  );
  return { controller, events };
}

describe("engine marker controller", () => {
  beforeEach(() => {
    resetMockMapState();
  });

  it("creates a marker with pin options and position", () => {
    const { controller } = createHarness();
    controller.set(marker({ color: "#dc2626", scale: 0.5, draggable: true, className: "custom" }));

    expect(getMockMarkerConstructor()).toHaveBeenCalledWith(
      expect.objectContaining({ color: "#dc2626", scale: 0.5, draggable: true, className: "custom" }),
    );
    const instance = markerInstance();
    expect(instance.setLngLat).toHaveBeenCalledWith([-0.09, 51.5]);
    expect(instance.addTo).toHaveBeenCalled();
    expect(controller.ids()).toEqual(["m1"]);
  });

  it("applies the title as a browser tooltip", () => {
    const { controller } = createHarness();
    controller.set(marker({ title: "Hello" }));

    const element = elementOf(markerInstance());
    expect(element.title).toBe("Hello");
  });

  it("moves the existing element on a position-only update", () => {
    const { controller } = createHarness();
    controller.set(marker());
    controller.set(marker({ position: { latitude: 52, longitude: 1 } }));

    expect(getMockMarkerConstructor()).toHaveBeenCalledTimes(1);
    const instance = markerInstance();
    expect(instance.setLngLat).toHaveBeenLastCalledWith([1, 52]);
    expect(instance.remove).not.toHaveBeenCalled();
  });

  it("recreates the marker on a structural update", () => {
    const { controller } = createHarness();
    controller.set(marker());
    controller.set(marker({ color: "#16a34a" }));

    expect(getMockMarkerConstructor()).toHaveBeenCalledTimes(2);
    expect(markerInstance().remove).toHaveBeenCalled();
    expect(controller.ids()).toEqual(["m1"]);
  });

  it("removes the marker and its popup", () => {
    const { controller } = createHarness();
    controller.set(
      marker({
        popup: { content: "hi", contentMode: "text", trigger: "click", anchor: "auto", closeButton: true },
      }),
    );
    controller.remove("m1");

    expect(markerInstance().remove).toHaveBeenCalled();
    expect(popupInstance().remove).toHaveBeenCalled();
    expect(controller.ids()).toEqual([]);
  });

  it("raises click events with the marker's live position", () => {
    const { controller, events } = createHarness();
    controller.set(marker());

    const instance = markerInstance();
    instance.getLngLat = vi.fn().mockReturnValue({ lng: -0.09, lat: 51.5 });
    elementOf(instance).dispatchEvent(new MouseEvent("click"));

    expect(events).toEqual([{ kind: "click", markerId: "m1", lng: -0.09, lat: 51.5 }]);
  });

  it("raises dragend events for draggable markers", () => {
    const { controller, events } = createHarness();
    controller.set(marker({ draggable: true }));

    const instance = markerInstance();
    const dragEnd = (instance.on as ReturnType<typeof vi.fn>).mock.calls.find(
      (call: unknown[]) => call[0] === "dragend",
    )?.[1] as () => void;
    instance.getLngLat = vi.fn().mockReturnValue({ lng: 2, lat: 48 });
    dragEnd();

    expect(events).toEqual([{ kind: "dragend", markerId: "m1", lng: 2, lat: 48 }]);
  });

  it("attaches click popups and toggles them on marker click", () => {
    const { controller } = createHarness();
    controller.set(
      marker({
        popup: { content: "<b>hi</b>", contentMode: "rawHtml", trigger: "click", anchor: "auto", closeButton: true },
      }),
    );

    const instance = markerInstance();
    expect(instance.setPopup).toHaveBeenCalled();
    expect(popupInstance().setHTML).toHaveBeenCalledWith("<b>hi</b>");

    elementOf(instance).dispatchEvent(new MouseEvent("click"));
    expect(instance.togglePopup).toHaveBeenCalled();
  });

  it("opens permanent popups immediately", () => {
    const { controller } = createHarness();
    controller.set(
      marker({
        popup: { content: "label", contentMode: "text", trigger: "permanent", anchor: "auto", closeButton: false },
      }),
    );

    const instance = markerInstance();
    expect(instance.togglePopup).toHaveBeenCalled();
  });

  it("disposes every marker", () => {
    const { controller } = createHarness();
    controller.set(marker({ id: "a" }));
    controller.set(marker({ id: "b" }));
    controller.dispose();

    expect(markerInstance().remove).toHaveBeenCalled();
    expect(markerInstance(1).remove).toHaveBeenCalled();
    expect(controller.ids()).toEqual([]);
  });
});
