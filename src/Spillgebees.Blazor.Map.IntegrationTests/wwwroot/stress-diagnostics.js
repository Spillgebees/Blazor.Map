// Stress/benchmark diagnostics for the integration test app.
// Tracks main-thread health (frame gaps, long tasks), MapLibre API call counters,
// and C#-reported per-tick update durations. Playwright reads everything through
// MapStressDiagnostics.snapshot(containerId); the floating panel is for manual runs.
window.MapStressDiagnostics = (() => {
    const sessions = new Map();
    const MAX_SAMPLES = 20000;

    function findMap(container) {
        const maps = window.Spillgebees?.Map?.maps;
        if (!maps) return null;

        for (const [element, map] of maps.entries()) {
            if (container.contains(element)) return map;
        }

        return null;
    }

    function percentile(sortedValues, fraction) {
        if (sortedValues.length === 0) return 0;
        const index = Math.min(sortedValues.length - 1, Math.ceil(fraction * sortedValues.length) - 1);
        return sortedValues[Math.max(0, index)];
    }

    function summarize(values) {
        const sorted = [...values].sort((a, b) => a - b);
        return {
            count: sorted.length,
            p50: percentile(sorted, 0.5),
            p95: percentile(sorted, 0.95),
            max: sorted.length === 0 ? 0 : sorted[sorted.length - 1],
            totalMs: sorted.reduce((sum, value) => sum + value, 0),
        };
    }

    function pushSample(samples, value) {
        if (samples.length < MAX_SAMPLES) samples.push(value);
    }

    function start(containerId, outputId) {
        stop(containerId);

        const container = document.getElementById(containerId);
        const output = document.getElementById(outputId);
        if (!container || !output) return;

        const state = {
            rafHandle: 0,
            intervalHandle: 0,
            lookupHandle: 0,
            frameCount: 0,
            sampleMaxFrameGap: 0,
            longFrameCount: 0,
            maxFrameGap: 0,
            lastFrameTime: performance.now(),
            lastSampleTime: performance.now(),
            resetTime: performance.now(),
            fps: 0,
            renderCount: 0,
            lastRenderCount: 0,
            rendersPerSecond: 0,
            lastRenderTime: 0,
            sourceDataCount: 0,
            styleDataCount: 0,
            lastSourceDataCount: 0,
            lastStyleDataCount: 0,
            sourceDataPerSecond: 0,
            styleDataPerSecond: 0,
            frameGaps: [],
            tickDurations: [],
            longTasks: [],
            longTaskObserver: null,
            counters: {
                setData: 0,
                updateData: 0,
                addSource: 0,
                removeSource: 0,
                addLayer: 0,
                removeLayer: 0,
                moveLayer: 0,
                setPaintProperty: 0,
                setLayoutProperty: 0,
                setFilter: 0,
                setFeatureState: 0,
            },
            lastCounters: {},
            originals: [],
            patchedSources: new WeakSet(),
            map: null,
            onRender: null,
            onSourceData: null,
            onStyleData: null,
        };

        try {
            state.longTaskObserver = new PerformanceObserver((list) => {
                for (const entry of list.getEntries()) {
                    pushSample(state.longTasks, entry.duration);
                }
            });
            state.longTaskObserver.observe({ entryTypes: ["longtask"] });
        } catch {
            state.longTaskObserver = null;
        }

        function frame(now) {
            const gap = now - state.lastFrameTime;
            state.lastFrameTime = now;
            state.frameCount++;
            pushSample(state.frameGaps, gap);
            state.sampleMaxFrameGap = Math.max(state.sampleMaxFrameGap, gap);
            state.maxFrameGap = Math.max(state.maxFrameGap, gap);
            if (gap > 50) state.longFrameCount++;
            state.rafHandle = requestAnimationFrame(frame);
        }

        function bindMap() {
            if (state.map) return;

            const map = findMap(container);
            if (!map || typeof map.on !== "function") return;

            state.map = map;
            patchMap(map);
            state.onRender = () => {
                state.renderCount++;
                state.lastRenderTime = performance.now();
            };
            state.onSourceData = () => state.sourceDataCount++;
            state.onStyleData = () => state.styleDataCount++;
            map.on("render", state.onRender);
            map.on("sourcedata", state.onSourceData);
            map.on("styledata", state.onStyleData);
        }

        function replaceMethod(target, methodName, replacementFactory) {
            if (!target || typeof target[methodName] !== "function") return;

            const original = target[methodName];
            target[methodName] = replacementFactory(original);
            state.originals.push(() => {
                target[methodName] = original;
            });
        }

        function patchSource(source) {
            if (!source || typeof source !== "object" || state.patchedSources.has(source)) return source;

            state.patchedSources.add(source);
            replaceMethod(source, "setData", (original) =>
                function (...args) {
                    state.counters.setData++;
                    return original.apply(this, args);
                },
            );
            replaceMethod(source, "updateData", (original) =>
                function (...args) {
                    state.counters.updateData++;
                    return original.apply(this, args);
                },
            );
            return source;
        }

        function patchMap(map) {
            replaceMethod(map, "getSource", (original) =>
                function (...args) {
                    return patchSource(original.apply(this, args));
                },
            );

            for (const methodName of [
                "addSource",
                "removeSource",
                "addLayer",
                "removeLayer",
                "moveLayer",
                "setPaintProperty",
                "setLayoutProperty",
                "setFilter",
                "setFeatureState",
            ]) {
                replaceMethod(map, methodName, (original) =>
                    function (...args) {
                        state.counters[methodName]++;
                        const result = original.apply(this, args);
                        if (methodName === "addSource" && args[0]) patchSource(this.getSource(args[0]));
                        return result;
                    },
                );
            }
        }

        function sample() {
            bindMap();
            const now = performance.now();
            const elapsedSeconds = Math.max(0.001, (now - state.lastSampleTime) / 1000);
            state.fps = state.frameCount / elapsedSeconds;
            state.rendersPerSecond = (state.renderCount - state.lastRenderCount) / elapsedSeconds;
            state.sourceDataPerSecond = (state.sourceDataCount - state.lastSourceDataCount) / elapsedSeconds;
            state.styleDataPerSecond = (state.styleDataCount - state.lastStyleDataCount) / elapsedSeconds;
            const sampledCounters = {};
            for (const [key, value] of Object.entries(state.counters)) {
                sampledCounters[key] = (value - (state.lastCounters[key] ?? 0)) / elapsedSeconds;
                state.lastCounters[key] = value;
            }
            const style = typeof state.map?.getStyle === "function" ? state.map.getStyle() : null;
            const sourceCount = style?.sources ? Object.keys(style.sources).length : 0;
            const layerCount = Array.isArray(style?.layers) ? style.layers.length : 0;
            const elapsedSinceReset = (now - state.resetTime) / 1000;
            state.frameCount = 0;
            state.lastRenderCount = state.renderCount;
            state.lastSourceDataCount = state.sourceDataCount;
            state.lastStyleDataCount = state.styleDataCount;
            state.lastSampleTime = now;
            const lastRenderAge = state.lastRenderTime > 0 ? now - state.lastRenderTime : 0;
            const sampleMaxFrameGap = state.sampleMaxFrameGap;
            state.sampleMaxFrameGap = 0;
            const longTaskTotal = state.longTasks.reduce((sum, value) => sum + value, 0);

            output.innerHTML = `
                <div><span class="stress-heartbeat"></span><strong>UI heartbeat</strong></div>
                <div>Elapsed since reset: ${elapsedSinceReset.toFixed(1)} s</div>
                <div>Style counts: ${sourceCount} sources / ${layerCount} layers</div>
                <div>FPS: ${state.fps.toFixed(1)}</div>
                <div>Sample max frame gap: ${sampleMaxFrameGap.toFixed(1)} ms</div>
                <div>Lifetime max frame gap: ${state.maxFrameGap.toFixed(1)} ms</div>
                <div>Long frames &gt;50ms: ${state.longFrameCount}</div>
                <div>Long tasks: ${state.longTasks.length} (${longTaskTotal.toFixed(0)} ms total)</div>
                <div>Map renders/sec: ${state.rendersPerSecond.toFixed(1)}</div>
                <div>Last render age: ${lastRenderAge.toFixed(0)} ms</div>
                <div>MapLibre sourcedata events/sec: ${state.sourceDataPerSecond.toFixed(1)} (total ${state.sourceDataCount})</div>
                <div>MapLibre styledata events/sec: ${state.styleDataPerSecond.toFixed(1)} (total ${state.styleDataCount})</div>
                <hr style="border-color: rgba(248,250,252,0.25);" />
                <div>setData/sec: ${sampledCounters.setData.toFixed(1)} (total ${state.counters.setData})</div>
                <div>updateData/sec: ${sampledCounters.updateData.toFixed(1)} (total ${state.counters.updateData})</div>
                <div>sources +/− sec: ${sampledCounters.addSource.toFixed(1)}/${sampledCounters.removeSource.toFixed(1)} (total ${state.counters.addSource}/${state.counters.removeSource})</div>
                <div>layers +/−/move sec: ${sampledCounters.addLayer.toFixed(1)}/${sampledCounters.removeLayer.toFixed(1)}/${sampledCounters.moveLayer.toFixed(1)} (total ${state.counters.addLayer}/${state.counters.removeLayer}/${state.counters.moveLayer})</div>
                <div>style ops paint/layout/filter sec: ${sampledCounters.setPaintProperty.toFixed(1)}/${sampledCounters.setLayoutProperty.toFixed(1)}/${sampledCounters.setFilter.toFixed(1)} (total ${state.counters.setPaintProperty}/${state.counters.setLayoutProperty}/${state.counters.setFilter})</div>
                <div>feature-state/sec: ${sampledCounters.setFeatureState.toFixed(1)} (total ${state.counters.setFeatureState})</div>
                <button type="button" onclick="MapStressDiagnostics.reset('${containerId}')">Reset diagnostics</button>
            `;
        }

        state.rafHandle = requestAnimationFrame(frame);
        state.intervalHandle = window.setInterval(sample, 500);
        state.lookupHandle = window.setInterval(bindMap, 250);
        sessions.set(containerId, state);
        sample();
    }

    function recordTick(containerId, durationMs) {
        const state = sessions.get(containerId);
        if (!state || typeof durationMs !== "number") return;

        pushSample(state.tickDurations, durationMs);
    }

    function snapshot(containerId) {
        const state = sessions.get(containerId);
        if (!state) return null;

        let renderedFeatures = -1;
        try {
            // counts vector features actually rendered in the viewport — makes
            // benchmark variants comparable (clustering collapses render load)
            renderedFeatures = state.map?.queryRenderedFeatures()?.length ?? -1;
        } catch {
            renderedFeatures = -1;
        }

        return {
            elapsedMs: performance.now() - state.resetTime,
            renderedFeatures,
            frameGaps: { ...summarize(state.frameGaps), over50: state.longFrameCount },
            tickDurations: summarize(state.tickDurations),
            longTasks: summarize(state.longTasks),
            counters: { ...state.counters },
            mapEvents: {
                renders: state.renderCount,
                sourcedata: state.sourceDataCount,
                styledata: state.styleDataCount,
            },
        };
    }

    function reset(containerId) {
        const state = sessions.get(containerId);
        if (!state) return;

        const now = performance.now();
        state.frameCount = 0;
        state.sampleMaxFrameGap = 0;
        state.longFrameCount = 0;
        state.maxFrameGap = 0;
        state.lastFrameTime = now;
        state.lastSampleTime = now;
        state.resetTime = now;
        state.renderCount = 0;
        state.lastRenderCount = 0;
        state.sourceDataCount = 0;
        state.styleDataCount = 0;
        state.lastSourceDataCount = 0;
        state.lastStyleDataCount = 0;
        state.frameGaps.length = 0;
        state.tickDurations.length = 0;
        state.longTasks.length = 0;
        for (const key of Object.keys(state.counters)) {
            state.counters[key] = 0;
            state.lastCounters[key] = 0;
        }
    }

    function stop(containerId) {
        const state = sessions.get(containerId);
        if (!state) return;

        cancelAnimationFrame(state.rafHandle);
        clearInterval(state.intervalHandle);
        clearInterval(state.lookupHandle);
        state.longTaskObserver?.disconnect();
        if (state.map && state.onRender && typeof state.map.off === "function") {
            state.map.off("render", state.onRender);
        }
        if (state.map && state.onSourceData && typeof state.map.off === "function") {
            state.map.off("sourcedata", state.onSourceData);
        }
        if (state.map && state.onStyleData && typeof state.map.off === "function") {
            state.map.off("styledata", state.onStyleData);
        }
        for (let index = state.originals.length - 1; index >= 0; index--) {
            state.originals[index]();
        }
        sessions.delete(containerId);
    }

    return { start, stop, reset, recordTick, snapshot };
})();
