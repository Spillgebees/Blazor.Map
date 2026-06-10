// Binary motion frame decoder (docs/plans/map-engine-protocol.md §3.4).
//
// Layout, little-endian, columnar:
//   header (16 bytes): u32 magic+version, u32 epoch, u32 count, u32 column bitmask
//   sections in bitmask order, tightly packed:
//     u32[count]   indices
//     f64[2*count] coords (lng, lat interleaved) — always present
//     f32[count]   rotation (bit 2)
//     f32[count]   sortKey  (bit 4)

export const MOTION_MAGIC_V1 = 0x53474201;

export const MotionColumns = {
  coords: 1,
  rotation: 2,
  sortKey: 4,
} as const;

const HEADER_BYTES = 16;

export interface MotionFrame {
  epoch: number;
  count: number;
  indices: Uint32Array;
  /** lng, lat interleaved; length 2 * count. */
  coords: Float64Array;
  rotations: Float32Array | null;
  sortKeys: Float32Array | null;
}

export function decodeMotionFrame(bytes: Uint8Array): MotionFrame {
  if (bytes.byteLength < HEADER_BYTES) {
    throw new Error(`Motion frame too small: ${bytes.byteLength} bytes`);
  }

  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  const magic = view.getUint32(0, true);
  if (magic !== MOTION_MAGIC_V1) {
    throw new Error(`Unknown motion frame magic 0x${magic.toString(16)}`);
  }

  const epoch = view.getUint32(4, true);
  const count = view.getUint32(8, true);
  const columns = view.getUint32(12, true);
  if ((columns & MotionColumns.coords) === 0) {
    throw new Error("Motion frame is missing the coords column");
  }

  const hasRotation = (columns & MotionColumns.rotation) !== 0;
  const hasSortKey = (columns & MotionColumns.sortKey) !== 0;
  const expectedBytes =
    HEADER_BYTES + count * 4 + count * 16 + (hasRotation ? count * 4 : 0) + (hasSortKey ? count * 4 : 0);
  if (bytes.byteLength !== expectedBytes) {
    throw new Error(`Motion frame size mismatch: expected ${expectedBytes} bytes, got ${bytes.byteLength}`);
  }

  // The payload byte offset is not guaranteed to be 8-aligned, so sections are read
  // through the DataView instead of typed-array views over the source buffer.
  let offset = HEADER_BYTES;

  const indices = new Uint32Array(count);
  for (let i = 0; i < count; i++, offset += 4) {
    indices[i] = view.getUint32(offset, true);
  }

  const coords = new Float64Array(count * 2);
  for (let i = 0; i < count * 2; i++, offset += 8) {
    coords[i] = view.getFloat64(offset, true);
  }

  let rotations: Float32Array | null = null;
  if (hasRotation) {
    rotations = new Float32Array(count);
    for (let i = 0; i < count; i++, offset += 4) {
      rotations[i] = view.getFloat32(offset, true);
    }
  }

  let sortKeys: Float32Array | null = null;
  if (hasSortKey) {
    sortKeys = new Float32Array(count);
    for (let i = 0; i < count; i++, offset += 4) {
      sortKeys[i] = view.getFloat32(offset, true);
    }
  }

  return { epoch, count, indices, coords, rotations, sortKeys };
}

/** Test/tooling helper: encodes a frame in the wire layout. The production encoder lives in C#. */
export function encodeMotionFrame(frame: MotionFrame): Uint8Array {
  const hasRotation = frame.rotations !== null;
  const hasSortKey = frame.sortKeys !== null;
  const columns =
    MotionColumns.coords | (hasRotation ? MotionColumns.rotation : 0) | (hasSortKey ? MotionColumns.sortKey : 0);
  const byteLength =
    HEADER_BYTES +
    frame.count * 4 +
    frame.count * 16 +
    (hasRotation ? frame.count * 4 : 0) +
    (hasSortKey ? frame.count * 4 : 0);

  const bytes = new Uint8Array(byteLength);
  const view = new DataView(bytes.buffer);
  view.setUint32(0, MOTION_MAGIC_V1, true);
  view.setUint32(4, frame.epoch, true);
  view.setUint32(8, frame.count, true);
  view.setUint32(12, columns, true);

  let offset = HEADER_BYTES;
  for (let i = 0; i < frame.count; i++, offset += 4) {
    view.setUint32(offset, frame.indices[i], true);
  }
  for (let i = 0; i < frame.count * 2; i++, offset += 8) {
    view.setFloat64(offset, frame.coords[i], true);
  }
  if (frame.rotations) {
    for (let i = 0; i < frame.count; i++, offset += 4) {
      view.setFloat32(offset, frame.rotations[i], true);
    }
  }
  if (frame.sortKeys) {
    for (let i = 0; i < frame.count; i++, offset += 4) {
      view.setFloat32(offset, frame.sortKeys[i], true);
    }
  }

  return bytes;
}
