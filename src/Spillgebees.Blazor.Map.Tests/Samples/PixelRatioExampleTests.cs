using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Docs.Samples;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;

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
        cut.FindComponents<MapCustomControl>().Should().HaveCount(1);
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("1.25x");
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("2x");
        cut.Find("button.active").TextContent.Should().Contain("Browser default");

        var mapOptions = cut.FindComponent<SgbMap>().Instance.MapOptions;
        mapOptions.PixelRatioMode.Should().Be(MapPixelRatioMode.BrowserDefault);
        mapOptions.PixelRatio.Should().BeNull();
    }

    [Test]
    public void Should_switch_to_rounded_device_pixel_ratio()
    {
        // arrange
        var cut = Render<PixelRatioExample>();

        // act
        cut.FindAll("button.sample-btn").Single(button => button.TextContent.Contains("Rounded DPR")).Click();

        // assert
        var mapOptions = cut.FindComponent<SgbMap>().Instance.MapOptions;
        mapOptions.PixelRatioMode.Should().Be(MapPixelRatioMode.RoundedUpDevicePixelRatio);
        mapOptions.PixelRatio.Should().BeNull();
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("2x");
    }

    [Test]
    public void Should_switch_to_explicit_pixel_ratio()
    {
        // arrange
        var cut = Render<PixelRatioExample>();

        // act
        cut.FindAll("button.sample-btn")[2].Click();

        // assert
        var mapOptions = cut.FindComponent<SgbMap>().Instance.MapOptions;
        mapOptions.PixelRatio.Should().Be(1.25);
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("1.25x");
    }

    [Test]
    public void Should_switch_back_to_browser_default()
    {
        // arrange
        var cut = Render<PixelRatioExample>();

        // act
        cut.FindAll("button.sample-btn").Single(button => button.TextContent.Contains("Rounded DPR")).Click();
        cut.FindAll("button.sample-btn").Single(button => button.TextContent.Contains("Browser default")).Click();

        // assert
        var mapOptions = cut.FindComponent<SgbMap>().Instance.MapOptions;
        mapOptions.PixelRatioMode.Should().Be(MapPixelRatioMode.BrowserDefault);
        mapOptions.PixelRatio.Should().BeNull();
        cut.Find(".docs-pixel-ratio-control").TextContent.Should().Contain("1.25x");
    }
}
