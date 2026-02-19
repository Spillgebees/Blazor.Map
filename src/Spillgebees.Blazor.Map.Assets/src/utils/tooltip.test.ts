import { describe, expect, it, vi } from "vitest";
import { MockTooltip } from "../../test/leafletMock";

vi.mock("leaflet", () => ({
  Tooltip: MockTooltip,
}));

import type { ISpillgebeesTooltip } from "../interfaces/map";
import { convertToLeafletTooltip } from "./tooltip";

describe("convertToLeafletTooltip", () => {
  it("should create a Tooltip with all provided options", () => {
    // arrange
    const input: ISpillgebeesTooltip = {
      content: "Hello",
      offset: { x: 5, y: 10 },
      direction: "top",
      permanent: true,
      sticky: false,
      interactive: true,
      opacity: 0.8,
      className: "custom-tooltip",
    };

    // act
    const result = convertToLeafletTooltip(input);

    // assert
    expect(result).toBeInstanceOf(MockTooltip);
    const options = (result as unknown as MockTooltip)._options;
    expect(options.content).toBe("Hello");
    expect(options.direction).toBe("top");
    expect(options.offset).toEqual([5, 10]);
    expect(options.permanent).toBe(true);
    expect(options.sticky).toBe(false);
    expect(options.interactive).toBe(true);
    expect(options.opacity).toBe(0.8);
    expect(options.className).toBe("custom-tooltip");
  });

  it("should use [0, 0] offset when offset is undefined", () => {
    // arrange
    const input: ISpillgebeesTooltip = {
      content: "No offset",
    };

    // act
    const result = convertToLeafletTooltip(input);

    // assert
    const options = (result as unknown as MockTooltip)._options;
    expect(options.offset).toEqual([0, 0]);
  });

  it("should use [x, y] offset when offset is provided", () => {
    // arrange
    const input: ISpillgebeesTooltip = {
      content: "With offset",
      offset: { x: 15, y: -20 },
    };

    // act
    const result = convertToLeafletTooltip(input);

    // assert
    const options = (result as unknown as MockTooltip)._options;
    expect(options.offset).toEqual([15, -20]);
  });

  it("should pass through all optional fields as undefined when not provided", () => {
    // arrange
    const input: ISpillgebeesTooltip = {
      content: "Minimal",
    };

    // act
    const result = convertToLeafletTooltip(input);

    // assert
    const options = (result as unknown as MockTooltip)._options;
    expect(options.content).toBe("Minimal");
    expect(options.direction).toBeUndefined();
    expect(options.offset).toEqual([0, 0]);
    expect(options.permanent).toBeUndefined();
    expect(options.sticky).toBeUndefined();
    expect(options.interactive).toBeUndefined();
    expect(options.opacity).toBeUndefined();
    expect(options.className).toBeUndefined();
  });
});
