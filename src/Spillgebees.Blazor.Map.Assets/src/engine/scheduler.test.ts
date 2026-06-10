import { describe, expect, it, vi } from "vitest";
import { createScheduler, type Scheduler } from "./scheduler";

interface FakeFrameLoop {
  scheduler: Scheduler;
  step(now: number): void;
  pendingFrames(): number;
}

function fakeFrameLoop(): FakeFrameLoop {
  let nextHandle = 1;
  const pending = new Map<number, (now: number) => void>();

  const scheduler = createScheduler(
    (callback) => {
      const handle = nextHandle++;
      pending.set(handle, callback);
      return handle;
    },
    (handle) => {
      pending.delete(handle);
    },
  );

  return {
    scheduler,
    step(now) {
      const callbacks = [...pending.values()];
      pending.clear();
      for (const callback of callbacks) {
        callback(now);
      }
    },
    pendingFrames: () => pending.size,
  };
}

describe("scheduler", () => {
  it("coalesces dirty marks per key, latest wins", () => {
    const { scheduler, step } = fakeFrameLoop();
    const first = vi.fn();
    const second = vi.fn();

    scheduler.markDirty("a", first);
    scheduler.markDirty("a", second);
    step(100);

    expect(first).not.toHaveBeenCalled();
    expect(second).toHaveBeenCalledExactlyOnceWith(100);
  });

  it("flushes independent keys in the same frame", () => {
    const { scheduler, step } = fakeFrameLoop();
    const a = vi.fn();
    const b = vi.fn();

    scheduler.markDirty("a", a);
    scheduler.markDirty("b", b);
    step(0);

    expect(a).toHaveBeenCalledOnce();
    expect(b).toHaveBeenCalledOnce();
  });

  it("schedules at most one frame regardless of dirty marks", () => {
    const { scheduler, pendingFrames } = fakeFrameLoop();

    scheduler.markDirty("a", () => {});
    scheduler.markDirty("b", () => {});

    expect(pendingFrames()).toBe(1);
  });

  it("runs animation ticks before flushes and keeps the loop alive until they stop", () => {
    const { scheduler, step, pendingFrames } = fakeFrameLoop();
    const order: string[] = [];
    let ticks = 0;

    scheduler.setAnimation("anim", (now) => {
      order.push(`tick@${now}`);
      ticks++;
      scheduler.markDirty("flush", () => order.push(`flush@${now}`));
      return ticks < 2;
    });

    step(1);
    expect(order).toEqual(["tick@1", "flush@1"]);
    expect(pendingFrames()).toBe(1);

    step(2);
    expect(order).toEqual(["tick@1", "flush@1", "tick@2", "flush@2"]);
    expect(pendingFrames()).toBe(0);
  });

  it("stops scheduling after dispose", () => {
    const { scheduler, step, pendingFrames } = fakeFrameLoop();
    const flush = vi.fn();

    scheduler.markDirty("a", flush);
    scheduler.dispose();
    scheduler.markDirty("b", flush);
    step(0);

    expect(flush).not.toHaveBeenCalled();
    expect(pendingFrames()).toBe(0);
  });

  it("flushNow drains pending work synchronously", () => {
    const { scheduler } = fakeFrameLoop();
    const flush = vi.fn();

    scheduler.markDirty("a", flush);
    scheduler.flushNow(42);

    expect(flush).toHaveBeenCalledExactlyOnceWith(42);
  });
});
