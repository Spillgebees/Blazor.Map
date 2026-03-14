import { beforeEach, describe, expect, it } from "vitest";
import { resetWindowGlobals } from "../test/windowSetup";
import { bootstrap, PROTOCOL_VERSION } from "./map";

describe("bootstrap", () => {
  beforeEach(() => {
    resetWindowGlobals();
  });

  it("should initialize the namespace when none exists", () => {
    // arrange & act
    bootstrap();

    // assert
    expect(window.Spillgebees).toBeDefined();
    expect(window.Spillgebees.Map).toBeDefined();
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
  });

  it("should register all map functions", () => {
    // arrange & act
    bootstrap();

    // assert
    const { mapFunctions } = window.Spillgebees.Map;
    expect(mapFunctions.createMap).toBeTypeOf("function");
    expect(mapFunctions.syncFeatures).toBeTypeOf("function");
    expect(mapFunctions.setOverlays).toBeTypeOf("function");
    expect(mapFunctions.setControls).toBeTypeOf("function");
    expect(mapFunctions.setMapOptions).toBeTypeOf("function");
    expect(mapFunctions.setTheme).toBeTypeOf("function");
    expect(mapFunctions.fitBounds).toBeTypeOf("function");
    expect(mapFunctions.flyTo).toBeTypeOf("function");
    expect(mapFunctions.resize).toBeTypeOf("function");
    expect(mapFunctions.disposeMap).toBeTypeOf("function");
  });

  it("should initialize empty stores", () => {
    // arrange & act
    bootstrap();

    // assert
    expect(window.Spillgebees.Map.maps).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.maps.size).toBe(0);
    expect(window.Spillgebees.Map.features).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.features.size).toBe(0);
    expect(window.Spillgebees.Map.overlays).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.overlays.size).toBe(0);
    expect(window.Spillgebees.Map.controls).toBeInstanceOf(Map);
    expect(window.Spillgebees.Map.controls.size).toBe(0);
  });

  it("should be a no-op when the protocol version already matches", () => {
    // arrange
    bootstrap();
    const originalMapFunctions = window.Spillgebees.Map.mapFunctions;
    const originalMaps = window.Spillgebees.Map.maps;

    // act
    bootstrap();

    // assert — same object references, not replaced
    expect(window.Spillgebees.Map.mapFunctions).toBe(originalMapFunctions);
    expect(window.Spillgebees.Map.maps).toBe(originalMaps);
  });

  it("should force-reinitialize when the protocol version mismatches", () => {
    // arrange — simulate a stale namespace from an older version
    window.Spillgebees = {
      Map: {
        getProtocolVersion: () => PROTOCOL_VERSION - 1,
        mapFunctions: { staleFunction: () => {} } as never,
        maps: new Map(),
        features: new Map(),
        overlays: new Map(),
        controls: new Map(),
      },
    };
    const staleMapFunctions = window.Spillgebees.Map.mapFunctions;

    // act
    bootstrap();

    // assert — namespace was replaced, not preserved
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
    expect(window.Spillgebees.Map.mapFunctions).not.toBe(staleMapFunctions);
    expect(window.Spillgebees.Map.mapFunctions.createMap).toBeTypeOf("function");
    expect((window.Spillgebees.Map.mapFunctions as Record<string, unknown>).staleFunction).toBeUndefined();
  });

  it("should reinitialize when getProtocolVersion is missing", () => {
    // arrange — simulate a corrupted namespace with no version function
    window.Spillgebees = {
      Map: {
        mapFunctions: {},
        maps: new Map(),
        features: new Map(),
        overlays: new Map(),
        controls: new Map(),
      } as never,
    };

    // act
    bootstrap();

    // assert
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
    expect(window.Spillgebees.Map.mapFunctions.createMap).toBeTypeOf("function");
  });

  it("should reinitialize when getProtocolVersion throws", () => {
    // arrange — simulate a namespace where getProtocolVersion is corrupted
    window.Spillgebees = {
      Map: {
        getProtocolVersion: () => {
          throw new Error("corrupted");
        },
        mapFunctions: {},
        maps: new Map(),
        features: new Map(),
        overlays: new Map(),
        controls: new Map(),
      } as never,
    };

    // act
    bootstrap();

    // assert
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
  });

  it("should preserve other Spillgebees namespace properties", () => {
    // arrange — simulate other libraries using the Spillgebees namespace
    window.Spillgebees = {
      OtherLibrary: { foo: "bar" },
    } as never;

    // act
    bootstrap();

    // assert — Map is initialized but OtherLibrary is preserved
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
    expect((window.Spillgebees as Record<string, unknown>).OtherLibrary).toEqual({ foo: "bar" });
  });
});

describe("index lifecycle hooks", () => {
  beforeEach(() => {
    resetWindowGlobals();
  });

  it("should initialize on first beforeStart call", async () => {
    // arrange
    const { beforeStart } = await import("./index");

    // act
    beforeStart(undefined);

    // assert
    expect(window.hasBeforeStartBeenCalledForSpillgebeesMap).toBe(true);
    expect(window.Spillgebees.Map.getProtocolVersion()).toBe(PROTOCOL_VERSION);
  });

  it("should not re-bootstrap on duplicate lifecycle hook calls", async () => {
    // arrange
    const { beforeStart, beforeWebStart } = await import("./index");
    beforeStart(undefined);
    const originalMaps = window.Spillgebees.Map.maps;

    // act — simulate duplicate hook call from a different render mode
    beforeWebStart(undefined);

    // assert — same store reference, no re-initialization
    expect(window.Spillgebees.Map.maps).toBe(originalMaps);
  });
});
