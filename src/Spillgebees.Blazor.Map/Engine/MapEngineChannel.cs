using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// The .NET side of the ops + motion channels (docs/plans/map-engine-protocol.md §1, §3.6).
/// Ops queue and flush as one <c>applyOps</c> call per render batch; everything queued
/// before the map's load event is buffered. Motion frames are latest-wins per layer with
/// a single interop call in flight — if JS is slow, intermediate frames are replaced,
/// never queued. Ops always flush before motion so upserts land ahead of the frames
/// computed against their epoch. Runs on the Blazor sync context; not thread-safe.
/// </summary>
internal sealed class MapEngineChannel(IJSRuntime jsRuntime)
{
    private readonly List<EngineOp> _pendingOps = [];
    private readonly Dictionary<string, byte[]> _pendingMotion = [];
    private readonly Dictionary<string, (string DataJson, int? AnimateMs, string? AnimateEasing)> _pendingSourceData = [];
    private ElementReference _container;
    private bool _isReady;
    private bool _flushScheduled;
    private bool _motionPumpRunning;

    /// <summary>Surfaces channel failures (serialization, interop) to the owning component.</summary>
    public event Action<Exception>? Error;

    public void Attach(ElementReference container) => _container = container;

    public async Task MarkReadyAsync()
    {
        _isReady = true;
        await FlushOpsAsync();
        await PumpMotionAsync();
    }

    public void Queue(EngineOp op)
    {
        _pendingOps.Add(op);
        _ = ScheduleFlushAsync();
    }

    public Task QueueAndFlushAsync(EngineOp op)
    {
        _pendingOps.Add(op);
        return FlushOpsAsync();
    }

    /// <summary>Flushes any pending ops immediately (teardown paths).</summary>
    public Task FlushAsync() => FlushOpsAsync();

    public void PushMotion(string layerId, byte[] frame)
    {
        _pendingMotion[layerId] = frame;
        _ = PumpMotionAsync();
    }

    /// <summary>
    /// Sends raw GeoJSON text without C#-side parsing or ops-channel re-serialization —
    /// JS parses once. Latest-wins per source, ops always flush first.
    /// </summary>
    public void PushSourceData(string sourceId, string dataJson, int? animateMs, string? animateEasing = null)
    {
        _pendingSourceData[sourceId] = (dataJson, animateMs, animateEasing);
        _ = PumpMotionAsync();
    }

    private async Task ScheduleFlushAsync()
    {
        if (_flushScheduled || !_isReady)
        {
            return;
        }

        _flushScheduled = true;
        try
        {
            // collect every op queued during the current render batch into one call.
            await Task.Yield();
        }
        finally
        {
            _flushScheduled = false;
        }

        await FlushOpsAsync();
    }

    private async Task FlushOpsAsync()
    {
        if (!_isReady || _pendingOps.Count == 0)
        {
            return;
        }

        var opsJson = JsonSerializer.Serialize(
            (IReadOnlyList<EngineOp>)[.. _pendingOps],
            MapEngineJsonContext.Default.IReadOnlyListEngineOp
        );
        _pendingOps.Clear();

        try
        {
            await MapEngineJs.ApplyOpsAsync(jsRuntime, _container, opsJson);
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception exception)
        {
            Error?.Invoke(exception);
        }
    }

    private async Task PumpMotionAsync()
    {
        if (_motionPumpRunning || !_isReady)
        {
            return;
        }

        _motionPumpRunning = true;
        try
        {
            while (_pendingMotion.Count > 0 || _pendingSourceData.Count > 0)
            {
                if (_pendingOps.Count > 0)
                {
                    await FlushOpsAsync();
                }

                if (_pendingMotion.Count > 0)
                {
                    var entry = _pendingMotion.First();
                    _pendingMotion.Remove(entry.Key);
                    await MapEngineJs.PushMotionAsync(jsRuntime, _container, entry.Key, entry.Value);
                    continue;
                }

                var dataEntry = _pendingSourceData.First();
                _pendingSourceData.Remove(dataEntry.Key);
                await MapEngineJs.SetSourceDataAsync(
                    jsRuntime,
                    _container,
                    dataEntry.Key,
                    dataEntry.Value.DataJson,
                    dataEntry.Value.AnimateMs,
                    dataEntry.Value.AnimateEasing
                );
            }
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception exception)
        {
            Error?.Invoke(exception);
        }
        finally
        {
            _motionPumpRunning = false;
        }
    }
}
