using System.Reflection;
using AwesomeAssertions;
using Spillgebees.Blazor.Map.Docs.Samples;

namespace Spillgebees.Blazor.Map.Tests.Samples;

public class CustomControlsExampleTests : BunitContext
{
    private const string ApplyOpsIdentifier = "Spillgebees.Engine.applyOps";

    public CustomControlsExampleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(ApplyOpsIdentifier);
    }

    [Test]
    public void Should_render_focus_station_control_without_duplicate_builtin_controls()
    {
        // arrange
        var cut = Render<CustomControlsExample>();

        // act
        var focusButton = cut.Find("button.sgb-map-action-control-button");

        // assert
        cut.FindComponents<GeolocateMapControl>().Should().BeEmpty();
        cut.FindComponents<TerrainMapControl>().Should().BeEmpty();
        cut.Markup.Should().Contain("Focus station");
        cut.Markup.Should().Contain("Central Station");
        focusButton.GetAttribute("aria-label").Should().Be("Focus Central Station");
        cut.FindComponents<ButtonMapControl>().Should().HaveCount(1);
        focusButton.ParentElement!.ClassList.Should().NotContain("sgb-map-ctrl-group");

        var icon = focusButton.QuerySelector("svg.docs-station-focus-icon");
        icon.Should().NotBeNull();
        icon!.GetAttribute("aria-hidden").Should().Be("true");
        icon.GetAttribute("focusable").Should().Be("false");
        icon.QuerySelector("path")!.GetAttribute("stroke").Should().Be("currentColor");
    }

    [Test]
    public async Task Should_focus_and_cycle_station_features_from_custom_control()
    {
        // arrange
        var cut = Render<CustomControlsExample>();
        var source = cut.FindComponent<GeoJsonSource>();
        CountFeatures(source.Instance.Data).Should().Be(4);
        // the channel buffers ops until the map reports load
        await cut.FindComponent<SgbMap>().Instance.Router.OnMapEvent("load", default);

        // act
        cut.Find("button.sgb-map-action-control-button").Click();

        // assert — the focus label re-renders in a continuation after the awaited
        // fly-to interop, so poll instead of asserting on the click's synchronous result
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Focused Central Station");
            cut.Find("button.sgb-map-action-control-button")
                .GetAttribute("aria-label")
                .Should()
                .Be("Focus North Station");
        });
        CountFeatures(source.Instance.Data).Should().Be(4);

        cut.WaitForAssertion(() =>
        {
            string[] opsPayloads = [];
            cut.InvokeAsync(() =>
                    opsPayloads = [
                        .. JSInterop
                            .Invocations[ApplyOpsIdentifier]
                            .Select(invocation => invocation.Arguments[1] as string ?? ""),
                    ]
                )
                .GetAwaiter()
                .GetResult();

            opsPayloads
                .Should()
                .Contain(payload =>
                    payload.Contains("\"op\":\"camera.flyTo\"")
                    && payload.Contains("\"latitude\":49.6117")
                    && payload.Contains("\"zoom\":14")
                );
        });
    }

    private static int CountFeatures(object? data)
    {
        data.Should().NotBeNull();
        var features =
            data!.GetType().GetProperty("features", BindingFlags.Instance | BindingFlags.Public)!.GetValue(data)
            as object[];

        var count = features?.Length;

        count.Should().NotBeNull();
        return count.Value;
    }
}
