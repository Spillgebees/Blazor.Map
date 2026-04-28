using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Tests.Models.Layers;

public class MarkerIconTests
{
    [Test]
    public void Should_create_marker_icon_with_only_required_url()
    {
        // arrange & act
        var icon = new MarkerIcon("https://example.com/icon.png");

        // assert
        icon.Url.Should().Be("https://example.com/icon.png");
        icon.Size.Should().BeNull();
        icon.Anchor.Should().BeNull();
    }

    [Test]
    public void Should_create_marker_icon_with_all_properties()
    {
        // arrange & act
        var icon = new MarkerIcon(
            Url: "https://example.com/icon.png",
            Size: new PixelPoint(25, 41),
            Anchor: new PixelPoint(12, 41)
        );

        // assert
        icon.Url.Should().Be("https://example.com/icon.png");
        icon.Size.Should().Be(new PixelPoint(25, 41));
        icon.Anchor.Should().Be(new PixelPoint(12, 41));
    }

    [Test]
    public void Should_support_value_equality_for_icons_without_optional_properties()
    {
        // arrange
        var icon1 = new MarkerIcon("https://example.com/icon.png");
        var icon2 = new MarkerIcon("https://example.com/icon.png");

        // act & assert
        icon1.Should().Be(icon2);
    }

    [Test]
    public void Should_support_value_equality_for_icons_with_all_properties()
    {
        // arrange
        var icon1 = new MarkerIcon(
            "https://example.com/icon.png",
            Size: new PixelPoint(25, 41),
            Anchor: new PixelPoint(12, 41)
        );
        var icon2 = new MarkerIcon(
            "https://example.com/icon.png",
            Size: new PixelPoint(25, 41),
            Anchor: new PixelPoint(12, 41)
        );

        // act & assert
        icon1.Should().Be(icon2);
    }

    [Test]
    public void Should_not_be_equal_when_size_values_differ()
    {
        // arrange
        var icon1 = new MarkerIcon("https://example.com/icon.png", Size: new PixelPoint(25, 41));
        var icon2 = new MarkerIcon("https://example.com/icon.png", Size: new PixelPoint(32, 32));

        // act & assert
        icon1.Should().NotBe(icon2);
    }

    [Test]
    public void Should_not_be_equal_when_anchor_values_differ()
    {
        // arrange
        var icon1 = new MarkerIcon("https://example.com/icon.png", Anchor: new PixelPoint(12, 41));
        var icon2 = new MarkerIcon("https://example.com/icon.png", Anchor: new PixelPoint(0, 0));

        // act & assert
        icon1.Should().NotBe(icon2);
    }

    [Test]
    public void Should_not_be_equal_when_url_differs()
    {
        // arrange
        var icon1 = new MarkerIcon("https://example.com/icon1.png");
        var icon2 = new MarkerIcon("https://example.com/icon2.png");

        // act & assert
        icon1.Should().NotBe(icon2);
    }
}
