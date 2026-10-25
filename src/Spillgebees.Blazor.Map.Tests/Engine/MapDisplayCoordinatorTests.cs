using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
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
        var (coordinator, js, dispatcher) = await CreateReadyCoordinatorAsync();
        var state = new MapDisplayState([
            new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")]),
            new MapDisplayItem("roads", [MapDisplayTarget.Layers("roads")]),
        ]);
        await OnRendererAsync(dispatcher, () => coordinator.Sync(state));
        js.Clear();

        // act
        state.SetOn("tram", true);
        await OnRendererAsync(dispatcher, () => coordinator.Sync(state));

        // assert
        AppliedOps(js).Select(op => op["id"]!.GetValue<string>()).Should().Equal("tram");
    }

    [Test]
    public async Task Should_send_nothing_when_only_style_defaults_changed()
    {
        // arrange
        var (coordinator, js, dispatcher) = await CreateReadyCoordinatorAsync();
        var state = new MapDisplayState([new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")])]);
        await OnRendererAsync(dispatcher, () => coordinator.Sync(state));
        js.Clear();

        // act
        state.SetStyleDefaults(new object(), new Dictionary<string, bool> { ["tram"] = false });
        await OnRendererAsync(dispatcher, () => coordinator.Sync(state));

        // assert
        AppliedOps(js).Should().BeEmpty();
    }

    [Test]
    public async Task Should_release_items_removed_from_the_state()
    {
        // arrange
        var (coordinator, js, dispatcher) = await CreateReadyCoordinatorAsync();
        await OnRendererAsync(
            dispatcher,
            () =>
                coordinator.Sync(new MapDisplayState([new MapDisplayItem("roads", [MapDisplayTarget.Layers("roads")])]))
        );
        js.Clear();

        // act
        await OnRendererAsync(dispatcher, () => coordinator.Sync(new MapDisplayState([])));

        // assert
        AppliedOps(js).Select(op => op["op"]!.GetValue<string>()).Should().Equal("visibility.remove");
    }

    [Test]
    public async Task Should_serialize_unset_items_and_target_vocabulary()
    {
        // arrange
        var (coordinator, js, dispatcher) = await CreateReadyCoordinatorAsync();
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
        await OnRendererAsync(dispatcher, () => coordinator.Sync(state));

        // assert
        var op = AppliedOps(js).Should().ContainSingle().Subject;
        op.ToJsonString()
            .Should()
            .Be(
                """{"op":"visibility.set","id":"mixed","targets":[{"kind":"layers","ids":["trains"]},{"kind":"styleLayers","styleId":"railway","layerIds":["tracks"],"filter":["has","ref"]},{"kind":"styleTags","styleId":"railway","tags":["tram"]},{"kind":"style","styleId":"railway"}]}"""
            );
    }

    private static async Task<(MapDisplayCoordinator, RecordingJsRuntime, Dispatcher)> CreateReadyCoordinatorAsync()
    {
        var js = new RecordingJsRuntime();
        var dispatcher = Dispatcher.CreateDefault();
        var channel = new MapEngineChannel(js);
        channel.Attach(default);
        await dispatcher.InvokeAsync(channel.MarkReadyAsync);
        return (new MapDisplayCoordinator(channel), js, dispatcher);
    }

    // the channel isn't thread-safe and relies on the renderer's sync context, so drive it the
    // same way. the second call drains the flush that Queue deferred with Task.Yield.
    private static async Task OnRendererAsync(Dispatcher dispatcher, Action action)
    {
        await dispatcher.InvokeAsync(action);
        await dispatcher.InvokeAsync(() => { });
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
