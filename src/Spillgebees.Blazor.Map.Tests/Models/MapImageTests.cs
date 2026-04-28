using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests.Models;

public class MapImageTests
{
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
        var act = () => _ = new MapImage("train", "https://example.com/train.png", 24, 24, pixelRatio);

        // assert
        act.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("pixelRatio");
    }

    [Test]
    public void Should_accept_positive_finite_pixel_ratio()
    {
        // arrange

        // act
        var definition = new MapImage("train", "https://example.com/train.png", 24, 24, 1.5);

        // assert
        definition.PixelRatio.Should().Be(1.5);
    }
}
