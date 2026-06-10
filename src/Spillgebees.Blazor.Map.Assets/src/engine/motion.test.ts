import { describe, expect, it } from "vitest";
import { decodeMotionFrame, encodeMotionFrame, MOTION_MAGIC_V1, type MotionFrame } from "./motion";

function frameOf(partial: Partial<MotionFrame> & { count: number }): MotionFrame {
  return {
    epoch: partial.epoch ?? 1,
    count: partial.count,
    indices: partial.indices ?? new Uint32Array(partial.count),
    coords: partial.coords ?? new Float64Array(partial.count * 2),
    rotations: partial.rotations ?? null,
    sortKeys: partial.sortKeys ?? null,
  };
}

describe("motion frame codec", () => {
  it("round-trips coords-only frames", () => {
    const frame = frameOf({
      epoch: 7,
      count: 2,
      indices: new Uint32Array([3, 11]),
      coords: new Float64Array([6.1319, 49.6117, -73.5673, 45.5017]),
    });

    const decoded = decodeMotionFrame(encodeMotionFrame(frame));

    expect(decoded.epoch).toBe(7);
    expect(decoded.count).toBe(2);
    expect([...decoded.indices]).toEqual([3, 11]);
    expect([...decoded.coords]).toEqual([6.1319, 49.6117, -73.5673, 45.5017]);
    expect(decoded.rotations).toBeNull();
    expect(decoded.sortKeys).toBeNull();
  });

  it("round-trips frames with rotation and sortKey columns", () => {
    const frame = frameOf({
      count: 2,
      indices: new Uint32Array([0, 1]),
      coords: new Float64Array([1, 2, 3, 4]),
      rotations: new Float32Array([90, 180.5]),
      sortKeys: new Float32Array([5, 6]),
    });

    const decoded = decodeMotionFrame(encodeMotionFrame(frame));

    expect([...(decoded.rotations ?? [])]).toEqual([90, 180.5]);
    expect([...(decoded.sortKeys ?? [])]).toEqual([5, 6]);
  });

  it("decodes from an unaligned byte offset", () => {
    const encoded = encodeMotionFrame(
      frameOf({ count: 1, indices: new Uint32Array([42]), coords: new Float64Array([1.5, -2.5]) }),
    );
    const padded = new Uint8Array(encoded.byteLength + 3);
    padded.set(encoded, 3);

    const decoded = decodeMotionFrame(padded.subarray(3));

    expect([...decoded.indices]).toEqual([42]);
    expect([...decoded.coords]).toEqual([1.5, -2.5]);
  });

  it("preserves f64 coordinate precision", () => {
    const lng = 6.131923456789012;
    const decoded = decodeMotionFrame(
      encodeMotionFrame(frameOf({ count: 1, indices: new Uint32Array([0]), coords: new Float64Array([lng, 0]) })),
    );

    expect(decoded.coords[0]).toBe(lng);
  });

  it("decodes the cross-language golden frame", () => {
    // Encoded by the C# MotionFrameEncoder — see MotionFrameEncoderTests
    // ("Should_match_the_cross_language_golden_frame"); both sides must agree.
    // biome-ignore lint/security/noSecrets: cross-language golden test vector, not a secret
    const base64 = "AUJHUyoAAAABAAAABwAAAAMAAACeXinLEIcYQEp7gy9MzkhAAAC0QgAAoEA=";
    const bytes = Uint8Array.from(atob(base64), (char) => char.charCodeAt(0));

    const decoded = decodeMotionFrame(bytes);

    expect(decoded.epoch).toBe(42);
    expect(decoded.count).toBe(1);
    expect([...decoded.indices]).toEqual([3]);
    expect(decoded.coords[0]).toBeCloseTo(6.1319, 10);
    expect(decoded.coords[1]).toBeCloseTo(49.6117, 10);
    expect(decoded.rotations?.[0]).toBe(90);
    expect(decoded.sortKeys?.[0]).toBe(5);
  });

  it("rejects frames with an unknown magic", () => {
    const encoded = encodeMotionFrame(frameOf({ count: 0 }));
    new DataView(encoded.buffer).setUint32(0, MOTION_MAGIC_V1 + 1, true);

    expect(() => decodeMotionFrame(encoded)).toThrow(/magic/);
  });

  it("rejects truncated frames", () => {
    const encoded = encodeMotionFrame(
      frameOf({ count: 2, indices: new Uint32Array([1, 2]), coords: new Float64Array(4) }),
    );

    expect(() => decodeMotionFrame(encoded.subarray(0, encoded.byteLength - 4))).toThrow(/size mismatch/);
    expect(() => decodeMotionFrame(encoded.subarray(0, 8))).toThrow(/too small/);
  });
});
