using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests.Models;

public class MapOptionsTests
{
    [Test]
    public void Should_default_to_browser_pixel_ratio()
    {
        // arrange & act
        var options = MapOptions.Default;

        // assert
        options.PixelRatioMode.Should().Be(MapPixelRatioMode.BrowserDefault);
        options.PixelRatio.Should().BeNull();
    }

    [Test]
    [Arguments(double.NaN)]
    [Arguments(double.PositiveInfinity)]
    [Arguments(double.NegativeInfinity)]
    [Arguments(0d)]
    [Arguments(-1d)]
    public void Should_reject_non_finite_or_non_positive_pixel_ratio(double pixelRatio)
    {
        // arrange

        // act
        var act = () => _ = new MapOptions(new Coordinate(0, 0), PixelRatio: pixelRatio);

        // assert
        act.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("pixelRatio");
    }

    [Test]
    public void Should_accept_positive_finite_pixel_ratio()
    {
        // arrange

        // act
        var options = new MapOptions(new Coordinate(0, 0), PixelRatio: 1.5);

        // assert
        options.PixelRatio.Should().Be(1.5);
    }

    [Test]
    public void Should_reject_invalid_pixel_ratio_when_using_with_expression()
    {
        // arrange
        var options = MapOptions.Default;

        // act
        var act = () => _ = options with { PixelRatio = 0 };

        // assert
        act.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("pixelRatio");
    }
}
