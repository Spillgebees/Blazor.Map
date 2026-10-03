using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map.Tests.Engine;

/// <summary>
/// Display changes (toggles, style-default reports) re-run the sync, so only items that
/// actually changed may cross the wire.
/// </summary>
public class MapDisplayCoordinatorTests
{
    private const string ApplyOpsIdentifier = "Spillgebees.Engine.applyOps";

    [Test]
    public async Task Should_send_only_the_items_that_changed()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        var state = new MapDisplayState([
            new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")]),
            new MapDisplayItem("roads", [MapDisplayTarget.Layers("roads")]),
        ]);
        coordinator.Sync(state);
        await SettleAsync(js);
        js.Clear();

        // act
        state.SetOn("tram", true);
        coordinator.Sync(state);
        await SettleAsync(js);

        // assert
        AppliedOps(js).Select(op => op["id"]!.GetValue<string>()).Should().Equal("tram");
    }

    [Test]
    public async Task Should_send_nothing_when_only_style_defaults_changed()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        var state = new MapDisplayState([new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")])]);
        coordinator.Sync(state);
        await SettleAsync(js);
        js.Clear();

        // act
        state.SetStyleDefaults(new object(), new Dictionary<string, bool> { ["tram"] = false });
        coordinator.Sync(state);
        await SettleAsync(js);

        // assert
        AppliedOps(js).Should().BeEmpty();
    }

    [Test]
    public async Task Should_release_items_removed_from_the_state()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        coordinator.Sync(new MapDisplayState([new MapDisplayItem("roads", [MapDisplayTarget.Layers("roads")])]));
        await SettleAsync(js);
        js.Clear();

        // act
        coordinator.Sync(new MapDisplayState([]));
        await SettleAsync(js);

        // assert
        AppliedOps(js).Select(op => op["op"]!.GetValue<string>()).Should().Equal("visibility.remove");
    }

    [Test]
    public async Task Should_serialize_unset_items_and_target_vocabulary()
    {
        // arrange
        var (coordinator, js) = await CreateReadyCoordinatorAsync();
        var state = new MapDisplayState([
            new MapDisplayItem(
                "mixed",
                [
                    MapDisplayTarget.Layers("trains"),
                    MapDisplayTarget.StyleLayers("railway", "tracks").Where(new object[] { "has", "ref" }),
                    MapDisplayTarget.StyleTags("railway", "tram"),
                    MapDisplayTarget.Style("railway"),
                ]
            ),
        ]);

        // act
        coordinator.Sync(state);
        await SettleAsync(js);

        // assert
        var op = AppliedOps(js).Should().ContainSingle().Subject;
        op.ToJsonString()
            .Should()
            .Be(
                """{"op":"visibility.set","id":"mixed","targets":[{"kind":"layers","ids":["trains"]},{"kind":"styleLayers","styleId":"railway","layerIds":["tracks"],"filter":["has","ref"]},{"kind":"styleTags","styleId":"railway","tags":["tram"]},{"kind":"style","styleId":"railway"}]}"""
            );
    }

    // Queue defers its flush with Task.Yield; outside Blazor's renderer that continuation runs
    // on the thread pool, so wait for the channel to go quiet instead of flushing alongside it.
    private static async Task SettleAsync(RecordingJsRuntime js)
    {
        var stablePolls = 0;
        var previousCount = -1;
        for (var attempt = 0; attempt < 500 && stablePolls < 5; attempt++)
        {
            var count = js.Snapshot().Count;
            stablePolls = count == previousCount ? stablePolls + 1 : 0;
            previousCount = count;
            await Task.Delay(1);
        }
    }

    private static async Task<(MapDisplayCoordinator, RecordingJsRuntime)> CreateReadyCoordinatorAsync()
    {
        var js = new RecordingJsRuntime();
        var channel = new MapEngineChannel(js);
        channel.Attach(default);
        await channel.MarkReadyAsync();
        return (new MapDisplayCoordinator(channel), js);
    }

    private static IReadOnlyList<JsonObject> AppliedOps(RecordingJsRuntime js) =>
        js.Snapshot()
            .Where(call => call.Identifier == ApplyOpsIdentifier)
            .SelectMany(call => JsonNode.Parse((string)call.Args[1]!)!.AsArray())
            .Select(node => node!.AsObject())
            .ToArray();

    private sealed record Invocation(string Identifier, object?[] Args);

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        private readonly Lock _lock = new();
        private readonly List<Invocation> _invocations = [];

        public IReadOnlyList<Invocation> Snapshot()
        {
            lock (_lock)
            {
                return _invocations.ToArray();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _invocations.Clear();
            }
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            lock (_lock)
            {
                _invocations.Add(new Invocation(identifier, args ?? []));
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args
        ) => InvokeAsync<TValue>(identifier, args);
    }
}
