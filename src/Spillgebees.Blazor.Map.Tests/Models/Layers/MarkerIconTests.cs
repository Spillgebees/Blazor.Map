using AwesomeAssertions;
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
        icon.IconUrl.Should().Be("https://example.com/icon.png");
        icon.IconSize.Should().BeNull();
        icon.IconAnchor.Should().BeNull();
        icon.PopupAnchor.Should().BeNull();
        icon.TooltipAnchor.Should().BeNull();
        icon.ShadowUrl.Should().BeNull();
        icon.ShadowSize.Should().BeNull();
        icon.ShadowAnchor.Should().BeNull();
        icon.ClassName.Should().BeNull();
    }

    [Test]
    public void Should_create_marker_icon_with_all_properties()
    {
        // arrange & act
        var icon = new MarkerIcon(
            IconUrl: "https://example.com/icon.png",
            IconSize: [25, 41],
            IconAnchor: [12, 41],
            PopupAnchor: [1, -34],
            TooltipAnchor: [16, -28],
            ShadowUrl: "https://example.com/shadow.png",
            ShadowSize: [41, 41],
            ShadowAnchor: [12, 41],
            ClassName: "custom-icon"
        );

        // assert
        icon.IconUrl.Should().Be("https://example.com/icon.png");
        icon.IconSize.Should().BeEquivalentTo([25, 41]);
        icon.IconAnchor.Should().BeEquivalentTo([12, 41]);
        icon.PopupAnchor.Should().BeEquivalentTo([1, -34]);
        icon.TooltipAnchor.Should().BeEquivalentTo([16, -28]);
        icon.ShadowUrl.Should().Be("https://example.com/shadow.png");
        icon.ShadowSize.Should().BeEquivalentTo([41, 41]);
        icon.ShadowAnchor.Should().BeEquivalentTo([12, 41]);
        icon.ClassName.Should().Be("custom-icon");
    }

    [Test]
    public void Should_support_value_equality_for_icons_without_arrays()
    {
        // arrange
        var icon1 = new MarkerIcon("https://example.com/icon.png", ShadowUrl: "shadow.png", ClassName: "custom");
        var icon2 = new MarkerIcon("https://example.com/icon.png", ShadowUrl: "shadow.png", ClassName: "custom");

        // act & assert
        icon1.Should().Be(icon2);
    }

    [Test]
    public void Should_support_value_equality_for_icons_with_arrays()
    {
        // arrange
        var icon1 = new MarkerIcon(
            "https://example.com/icon.png",
            IconSize: [25, 41],
            IconAnchor: [12, 41],
            PopupAnchor: [1, -34],
            TooltipAnchor: [16, -28],
            ShadowUrl: "shadow.png",
            ShadowSize: [41, 41],
            ShadowAnchor: [12, 41],
            ClassName: "custom"
        );
        var icon2 = new MarkerIcon(
            "https://example.com/icon.png",
            IconSize: [25, 41],
            IconAnchor: [12, 41],
            PopupAnchor: [1, -34],
            TooltipAnchor: [16, -28],
            ShadowUrl: "shadow.png",
            ShadowSize: [41, 41],
            ShadowAnchor: [12, 41],
            ClassName: "custom"
        );

        // act & assert
        icon1.Should().Be(icon2);
    }

    [Test]
    public void Should_not_be_equal_when_array_values_differ()
    {
        // arrange
        var icon1 = new MarkerIcon("https://example.com/icon.png", IconSize: [25, 41]);
        var icon2 = new MarkerIcon("https://example.com/icon.png", IconSize: [32, 32]);

        // act & assert
        icon1.Should().NotBe(icon2);
    }
}
