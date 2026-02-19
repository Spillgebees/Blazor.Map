import type { CircleMarker, Map as LeafletMap, Marker, Point, Polyline } from "leaflet";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MockCircleMarker, MockLatLng, MockMap, MockMarker, MockPolyline } from "../../test/leafletMock";
import { resetWindowGlobals } from "../../test/windowSetup";

vi.mock("leaflet", async () => {
  const { createLeafletMock } = await import("../../test/leafletMock");
  return createLeafletMock();
});

import type {
  ISpillgebeesCircleMarker,
  ISpillgebeesFitBoundsOptions,
  ISpillgebeesMarker,
  ISpillgebeesPolyline,
} from "../interfaces/map";
import type { LayerStorage, LayerTuple } from "../types/layers";
import { fitBounds, fitBoundsForMap } from "./fitBoundsForMap";

// biome-ignore lint/security/noSecrets: false positive
describe("fitBoundsForMap", () => {
  let mockMap: MockMap;

  beforeEach(() => {
    mockMap = new MockMap();
  });

  it("should call map.fitBounds with merged bounds of matched layers", () => {
    // arrange
    const marker = new MockMarker(new MockLatLng(51.5, -0.1), {
      title: "London",
    });
    const layerStorage: LayerStorage = {
      byId: new Map([
        [
          "marker-1",
          {
            model: { id: "marker-1" } as unknown as ISpillgebeesMarker,
            leaflet: marker as unknown as Marker,
          } as LayerTuple,
        ],
      ]),
      byLeaflet: new Map(),
    };
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: ["marker-1"],
    };

    // act
    fitBoundsForMap(mockMap as unknown as LeafletMap, layerStorage, options);

    // assert
    expect(mockMap.fitBounds).toHaveBeenCalledOnce();
  });

  it("should handle a mix of Marker and Polyline layers", () => {
    // arrange
    const marker = new MockMarker(new MockLatLng(51.5, -0.1));
    const polyline = new MockPolyline([new MockLatLng(48.8, 2.3), new MockLatLng(50.0, 3.0)]);
    const layerStorage: LayerStorage = {
      byId: new Map([
        [
          "marker-1",
          {
            model: { id: "marker-1" } as unknown as ISpillgebeesMarker,
            leaflet: marker as unknown as Marker,
          } as LayerTuple,
        ],
        [
          "polyline-1",
          {
            model: { id: "polyline-1" } as unknown as ISpillgebeesPolyline,
            leaflet: polyline as unknown as Polyline,
          } as LayerTuple,
        ],
      ]),
      byLeaflet: new Map(),
    };
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: ["marker-1", "polyline-1"],
    };

    // act
    fitBoundsForMap(mockMap as unknown as LeafletMap, layerStorage, options);

    // assert
    expect(mockMap.fitBounds).toHaveBeenCalledOnce();
    // The first layer sets bounds, subsequent layers extend
    // For the polyline, getBounds is used (not getLatLng)
    expect(polyline.getBounds).toHaveBeenCalled();
  });

  it("should handle CircleMarker (uses getLatLng)", () => {
    // arrange
    const circleMarker = new MockCircleMarker(new MockLatLng(40.7, -74.0));
    const layerStorage: LayerStorage = {
      byId: new Map([
        [
          "cm-1",
          {
            model: { id: "cm-1" } as unknown as ISpillgebeesCircleMarker,
            leaflet: circleMarker as unknown as CircleMarker,
          } as LayerTuple,
        ],
      ]),
      byLeaflet: new Map(),
    };
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: ["cm-1"],
    };

    // act
    fitBoundsForMap(mockMap as unknown as LeafletMap, layerStorage, options);

    // assert
    expect(circleMarker.getLatLng).toHaveBeenCalled();
    expect(mockMap.fitBounds).toHaveBeenCalledOnce();
  });

  it("should skip unmatched layer IDs", () => {
    // arrange
    const marker = new MockMarker(new MockLatLng(51.5, -0.1));
    const layerStorage: LayerStorage = {
      byId: new Map([
        [
          "marker-1",
          {
            model: { id: "marker-1" } as unknown as ISpillgebeesMarker,
            leaflet: marker as unknown as Marker,
          } as LayerTuple,
        ],
      ]),
      byLeaflet: new Map(),
    };
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: ["marker-1", "nonexistent-id"],
    };

    // act
    fitBoundsForMap(mockMap as unknown as LeafletMap, layerStorage, options);

    // assert
    expect(mockMap.fitBounds).toHaveBeenCalledOnce();
  });

  it("should NOT call fitBounds when no layers match", () => {
    // arrange
    const layerStorage: LayerStorage = {
      byId: new Map(),
      byLeaflet: new Map(),
    };
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: ["nonexistent-id"],
    };

    // act
    fitBoundsForMap(mockMap as unknown as LeafletMap, layerStorage, options);

    // assert
    expect(mockMap.fitBounds).not.toHaveBeenCalled();
  });

  it("should handle empty layerIds array", () => {
    // arrange
    const layerStorage: LayerStorage = {
      byId: new Map(),
      byLeaflet: new Map(),
    };
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: [],
    };

    // act
    fitBoundsForMap(mockMap as unknown as LeafletMap, layerStorage, options);

    // assert
    expect(mockMap.fitBounds).not.toHaveBeenCalled();
  });

  it("should pass padding options to fitBounds", () => {
    // arrange
    const marker = new MockMarker(new MockLatLng(51.5, -0.1));
    const layerStorage: LayerStorage = {
      byId: new Map([
        [
          "marker-1",
          {
            model: { id: "marker-1" } as unknown as ISpillgebeesMarker,
            leaflet: marker as unknown as Marker,
          } as LayerTuple,
        ],
      ]),
      byLeaflet: new Map(),
    };
    const padding = { x: 20, y: 20 } as unknown as Point;
    const topLeftPadding = { x: 10, y: 10 } as unknown as Point;
    const bottomRightPadding = { x: 30, y: 30 } as unknown as Point;
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: ["marker-1"],
      padding,
      topLeftPadding,
      bottomRightPadding,
    };

    // act
    fitBoundsForMap(mockMap as unknown as LeafletMap, layerStorage, options);

    // assert
    expect(mockMap.fitBounds).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({
        padding,
        paddingTopLeft: topLeftPadding,
        paddingBottomRight: bottomRightPadding,
      }),
    );
  });
});

describe("fitBounds (wrapper)", () => {
  let mockMap: MockMap;
  let mapContainer: HTMLElement;

  beforeEach(() => {
    resetWindowGlobals();
    mockMap = new MockMap();
    mapContainer = document.createElement("div");

    // Set up window globals
    window.Spillgebees = {
      Map: {
        mapFunctions: {} as unknown as typeof window.Spillgebees.Map.mapFunctions,
        maps: new Map([[mapContainer, mockMap]]) as unknown as typeof window.Spillgebees.Map.maps,
        layers: new Map() as unknown as typeof window.Spillgebees.Map.layers,
        tileLayers: new Map() as unknown as typeof window.Spillgebees.Map.tileLayers,
        controls: new Map() as unknown as typeof window.Spillgebees.Map.controls,
      },
    };
  });

  it("should early-return if map not found", () => {
    // arrange
    const unknownContainer = document.createElement("div");
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: ["marker-1"],
    };

    // act
    fitBounds(unknownContainer, options);

    // assert
    expect(mockMap.fitBounds).not.toHaveBeenCalled();
  });

  it("should early-return if layerStorage not found", () => {
    // arrange
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: ["marker-1"],
    };

    // act
    fitBounds(mapContainer, options);

    // assert
    expect(mockMap.fitBounds).not.toHaveBeenCalled();
  });

  it("should delegate to fitBoundsForMap when both map and layerStorage exist", () => {
    // arrange
    const marker = new MockMarker(new MockLatLng(51.5, -0.1));
    const layerStorage: LayerStorage = {
      byId: new Map([
        [
          "marker-1",
          {
            model: { id: "marker-1" } as unknown as ISpillgebeesMarker,
            leaflet: marker as unknown as Marker,
          } as LayerTuple,
        ],
      ]),
      byLeaflet: new Map(),
    };
    (window.Spillgebees.Map.layers as Map<unknown, LayerStorage>).set(mockMap, layerStorage);
    const options: ISpillgebeesFitBoundsOptions = {
      layerIds: ["marker-1"],
    };

    // act
    fitBounds(mapContainer, options);

    // assert
    expect(mockMap.fitBounds).toHaveBeenCalledOnce();
  });
});
