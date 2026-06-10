using AwesomeAssertions;
using Spillgebees.Blazor.Map.Docs.Samples;

namespace Spillgebees.Blazor.Map.Tests.Samples;

public class PixelRatioExampleTests : BunitContext
{
    public PixelRatioExampleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<double>("SpillgebeesDocs.getDevicePixelRatio").SetResult(1.25);
    }

    [Test]
    public void Should_render_one_map_with_browser_default_selected()
    {
        // arrange & act
        var cut = Render<PixelRatioExample>();

        // assert
        cut.FindComponents<SgbMap>().Should().HaveCount(1);
        cut.FindComponents<CustomMapControl>().Should().HaveCount(1);
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("1.25x");
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("2x");
        cut.Find("button.active").TextContent.Should().Contain("Browser default");

        var map = cut.FindComponent<SgbMap>().Instance;
        map.PixelRatioMode.Should().Be(MapPixelRatioMode.BrowserDefault);
        map.PixelRatio.Should().BeNull();
    }

    [Test]
    public void Should_switch_to_rounded_device_pixel_ratio()
    {
        // arrange
        var cut = Render<PixelRatioExample>();

        // act
        cut.Find("button[data-test='pixel-ratio-rounded-dpr']").Click();

        // assert
        var map = cut.FindComponent<SgbMap>().Instance;
        map.PixelRatioMode.Should().Be(MapPixelRatioMode.RoundedUpDevicePixelRatio);
        map.PixelRatio.Should().BeNull();
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("2x");
    }

    [Test]
    public void Should_switch_to_explicit_pixel_ratio()
    {
        // arrange
        var cut = Render<PixelRatioExample>();

        // act
        cut.Find("button[data-test='pixel-ratio-explicit']").Click();

        // assert
        var map = cut.FindComponent<SgbMap>().Instance;
        map.PixelRatio.Should().Be(1.25);
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("1.25x");
    }

    [Test]
    public void Should_switch_back_to_browser_default()
    {
        // arrange
        var cut = Render<PixelRatioExample>();

        // act
        cut.Find("button[data-test='pixel-ratio-rounded-dpr']").Click();
        cut.Find("button[data-test='pixel-ratio-browser-default']").Click();

        // assert
        var map = cut.FindComponent<SgbMap>().Instance;
        map.PixelRatioMode.Should().Be(MapPixelRatioMode.BrowserDefault);
        map.PixelRatio.Should().BeNull();
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("1.25x");
    }
}
