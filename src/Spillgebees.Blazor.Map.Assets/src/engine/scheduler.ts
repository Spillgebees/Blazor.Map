// Per-map write scheduler.
//
// All map writes funnel through one requestAnimationFrame callback: dirty flushes are
// latest-wins per key (a slow frame drops intermediate states instead of queueing them),
// and animation ticks run first so the flushes they mark dirty land in the same frame.

export type FrameRequest = (callback: (now: number) => void) => number;
export type FrameCancel = (handle: number) => void;

export interface Scheduler {
  /** Schedules `flush` for the next frame; a newer flush under the same key replaces it. */
  markDirty(key: string, flush: (now: number) => void): void;
  /** Registers a per-frame tick that keeps the frame loop alive while it returns true. */
  setAnimation(key: string, tick: ((now: number) => boolean) | null): void;
  /** Runs pending animations + flushes immediately (teardown, tests). */
  flushNow(now: number): void;
  dispose(): void;
}

export function createScheduler(
  requestFrame: FrameRequest = (callback) => requestAnimationFrame(callback),
  cancelFrame: FrameCancel = (handle) => cancelAnimationFrame(handle),
): Scheduler {
  const flushes = new Map<string, (now: number) => void>();
  const animations = new Map<string, (now: number) => boolean>();
  let frameHandle: number | null = null;
  let disposed = false;

  function schedule(): void {
    if (frameHandle !== null || disposed) {
      return;
    }

    // frameHandle stays set while the frame runs so markDirty/setAnimation calls made
    // inside ticks don't schedule a redundant extra frame.
    frameHandle = requestFrame((now) => {
      runFrame(now);
      frameHandle = null;
      if (animations.size > 0 || flushes.size > 0) {
        schedule();
      }
    });
  }

  function runFrame(now: number): void {
    for (const [key, tick] of animations) {
      if (!tick(now)) {
        animations.delete(key);
      }
    }

    const pending = [...flushes.values()];
    flushes.clear();
    for (const flush of pending) {
      flush(now);
    }
  }

  return {
    markDirty(key, flush) {
      if (disposed) {
        return;
      }

      flushes.set(key, flush);
      schedule();
    },
    setAnimation(key, tick) {
      if (disposed) {
        return;
      }

      if (tick === null) {
        animations.delete(key);
        return;
      }

      animations.set(key, tick);
      schedule();
    },
    flushNow(now) {
      runFrame(now);
    },
    dispose() {
      disposed = true;
      if (frameHandle !== null) {
        cancelFrame(frameHandle);
        frameHandle = null;
      }
      flushes.clear();
      animations.clear();
    },
  };
}
