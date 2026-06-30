using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class SgbMapOverlayContentTests : BunitContext
{
    public SgbMapOverlayContentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Test]
    public void Should_render_overlay_content_inside_the_map_container()
    {
        // arrange
        RenderFragment overlay = builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "data-testid", "overlay-action");
            builder.AddContent(2, "Overlay action");
            builder.CloseElement();
        };

        // act
        var cut = Render<SgbMap>(parameters => parameters.Add(component => component.OverlayContent, overlay));

        // assert
        var container = cut.Find(".sgb-map-container");
        var overlayRoot = container.QuerySelector(".sgb-map-overlay-root");
        overlayRoot.Should().NotBeNull();
        overlayRoot!.QuerySelector("[data-testid='overlay-action']")!.TextContent.Should().Be("Overlay action");
        cut.Find(".sgb-map-root > .sgb-map-container > .sgb-map-overlay-root").Should().NotBeNull();
    }

    [Test]
    public void Should_not_render_overlay_root_when_overlay_content_is_not_supplied()
    {
        // arrange

        // act
        var cut = Render<SgbMap>();

        // assert
        cut.FindAll(".sgb-map-overlay-root").Should().BeEmpty();
    }

    [Test]
    public async Task Should_skip_control_sync_after_map_is_disposed()
    {
        // arrange
        var map = CreateInitializedMap();
        var host = (IMapControlHost)map;
        await map.DisposeAsync();

        // act
        Func<Task> act = async () => await host.SyncControlsAsync();

        // assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Should_complete_pending_control_sync_when_map_is_disposed()
    {
        // arrange
        var map = CreateInitializedMap();
        var host = (IMapControlHost)map;
        var syncLock = GetControlSyncLock(map);
        await syncLock.WaitAsync();
        var syncTask = host.SyncControlsAsync().AsTask();
        var disposeTask = map.DisposeAsync().AsTask();

        // act
        Func<Task> act = async () =>
        {
            disposeTask.IsCompleted.Should().BeFalse();
            syncLock.Release();
            await syncTask.WaitAsync(TimeSpan.FromSeconds(5));
            await disposeTask.WaitAsync(TimeSpan.FromSeconds(5));
        };

        // assert
        syncTask.IsCompleted.Should().BeFalse();
        await act.Should().NotThrowAsync();
    }

    private TestSgbMap CreateInitializedMap()
    {
        var map = new TestSgbMap();
        typeof(SgbMap)
            .GetProperty("_jsRuntime", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, JSInterop.JSRuntime);
        map.Initialize();
        return map;
    }

    private static SemaphoreSlim GetControlSyncLock(SgbMap map) =>
        (SemaphoreSlim)
            typeof(SgbMap).GetField("_controlSyncLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(map)!;

    private sealed class TestSgbMap : SgbMap
    {
        public void Initialize() => OnInitialized();
    }
}
