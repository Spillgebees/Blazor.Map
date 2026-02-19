import { beforeEach, describe, expect, it, vi } from "vitest";
import { resetWindowGlobals } from "../test/windowSetup";

const { mockBootstrap } = vi.hoisted(() => ({
  mockBootstrap: vi.fn(),
}));

vi.mock("./map", () => ({
  bootstrap: mockBootstrap,
}));

vi.mock("./styles.scss", () => ({}));

import {
  afterServerStarted,
  afterStarted,
  afterWebAssemblyStarted,
  afterWebStarted,
  beforeServerStart,
  beforeStart,
  beforeWebAssemblyStart,
  beforeWebStart,
} from "./index";

describe("index lifecycle hooks", () => {
  beforeEach(() => {
    resetWindowGlobals();
    vi.clearAllMocks();
  });

  describe("beforeStart", () => {
    it("should call bootstrap() and set window flag", () => {
      // act
      beforeStart({});

      // assert
      expect(mockBootstrap).toHaveBeenCalledOnce();
      expect(window.hasBeforeStartBeenCalledForSpillgebeesMap).toBe(true);
    });
  });

  describe("afterStarted", () => {
    it("should set window flag", () => {
      // act
      afterStarted({});

      // assert
      expect(window.hasAfterStartedBeenCalledForSpillgebeesMap).toBe(true);
    });
  });

  describe("beforeWebStart", () => {
    it("should delegate to beforeStart on first call", () => {
      // act
      beforeWebStart({});

      // assert
      expect(mockBootstrap).toHaveBeenCalledOnce();
      expect(window.hasBeforeStartBeenCalledForSpillgebeesMap).toBe(true);
    });

    it("should be idempotent — only execute once", () => {
      // arrange
      beforeWebStart({});

      // act
      beforeWebStart({});

      // assert
      expect(mockBootstrap).toHaveBeenCalledOnce();
    });
  });

  describe("afterWebStarted", () => {
    it("should delegate to afterStarted on first call", () => {
      // act
      afterWebStarted({});

      // assert
      expect(window.hasAfterStartedBeenCalledForSpillgebeesMap).toBe(true);
    });

    it("should be idempotent — only execute once", () => {
      // arrange
      afterWebStarted({});

      // act
      afterWebStarted({});

      // assert — flag is still true, but the second call was a no-op
      expect(window.hasAfterStartedBeenCalledForSpillgebeesMap).toBe(true);
    });
  });

  describe("beforeWebAssemblyStart", () => {
    it("should delegate to beforeStart on first call", () => {
      // act
      beforeWebAssemblyStart({});

      // assert
      expect(mockBootstrap).toHaveBeenCalledOnce();
      expect(window.hasBeforeStartBeenCalledForSpillgebeesMap).toBe(true);
    });

    it("should be idempotent — only execute once", () => {
      // arrange
      beforeWebAssemblyStart({});

      // act
      beforeWebAssemblyStart({});

      // assert
      expect(mockBootstrap).toHaveBeenCalledOnce();
    });
  });

  describe("afterWebAssemblyStarted", () => {
    it("should delegate to afterStarted on first call", () => {
      // act
      afterWebAssemblyStarted({});

      // assert
      expect(window.hasAfterStartedBeenCalledForSpillgebeesMap).toBe(true);
    });

    it("should be idempotent — only execute once", () => {
      // arrange
      afterWebAssemblyStarted({});

      // act
      afterWebAssemblyStarted({});

      // assert
      expect(window.hasAfterStartedBeenCalledForSpillgebeesMap).toBe(true);
    });
  });

  describe("beforeServerStart", () => {
    it("should delegate to beforeStart on first call", () => {
      // act
      beforeServerStart({});

      // assert
      expect(mockBootstrap).toHaveBeenCalledOnce();
      expect(window.hasBeforeStartBeenCalledForSpillgebeesMap).toBe(true);
    });

    it("should be idempotent — only execute once", () => {
      // arrange
      beforeServerStart({});

      // act
      beforeServerStart({});

      // assert
      expect(mockBootstrap).toHaveBeenCalledOnce();
    });
  });

  describe("afterServerStarted", () => {
    it("should delegate to afterStarted on first call", () => {
      // act
      afterServerStarted({});

      // assert
      expect(window.hasAfterStartedBeenCalledForSpillgebeesMap).toBe(true);
    });

    it("should be idempotent — only execute once", () => {
      // arrange
      afterServerStarted({});

      // act
      afterServerStarted({});

      // assert
      expect(window.hasAfterStartedBeenCalledForSpillgebeesMap).toBe(true);
    });
  });
});
