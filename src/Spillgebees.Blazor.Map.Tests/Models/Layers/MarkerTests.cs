using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Tests.Models.Layers;

public class MarkerTests
{
    [Test]
    public void Should_create_marker_with_default_optional_values()
    {
        // arrange & act
        var marker = new Marker("test-id", new Coordinate(49.6, 6.1), "Test Title");

        // assert
        marker.Id.Should().Be("test-id");
        marker.Coordinate.Should().Be(new Coordinate(49.6, 6.1));
        marker.Title.Should().Be("Test Title");
        marker.Icon.Should().BeNull();
        marker.RotationAngle.Should().BeNull();
        marker.RotationOrigin.Should().BeNull();
        marker.Tooltip.Should().BeNull();
    }

    [Test]
    public void Should_create_marker_with_custom_icon()
    {
        // arrange
        var icon = new MarkerIcon("https://example.com/icon.png", IconSize: [25, 41], IconAnchor: [12, 41]);

        // act
        var marker = new Marker("test-id", new Coordinate(49.6, 6.1), "Test Title", Icon: icon);

        // assert
        marker.Icon.Should().NotBeNull();
        marker.Icon!.IconUrl.Should().Be("https://example.com/icon.png");
        marker.Icon.IconSize.Should().BeEquivalentTo([25, 41]);
        marker.Icon.IconAnchor.Should().BeEquivalentTo([12, 41]);
    }

    [Test]
    public void Should_create_marker_with_rotation_properties()
    {
        // arrange & act
        var marker = new Marker(
            "test-id",
            new Coordinate(49.6, 6.1),
            "Test Title",
            RotationAngle: 45.0,
            RotationOrigin: "bottom center"
        );

        // assert
        marker.RotationAngle.Should().Be(45.0);
        marker.RotationOrigin.Should().Be("bottom center");
    }

    [Test]
    public void Should_create_marker_with_all_properties()
    {
        // arrange
        var icon = new MarkerIcon("https://example.com/icon.png");
        var tooltip = new Blazor.Map.Models.Tooltips.TooltipOptions("Hello");

        // act
        var marker = new Marker(
            "test-id",
            new Coordinate(49.6, 6.1),
            "Test Title",
            Icon: icon,
            RotationAngle: 90.0,
            RotationOrigin: "center center",
            Tooltip: tooltip
        );

        // assert
        marker.Id.Should().Be("test-id");
        marker.Icon.Should().NotBeNull();
        marker.RotationAngle.Should().Be(90.0);
        marker.RotationOrigin.Should().Be("center center");
        marker.Tooltip.Should().NotBeNull();
    }

    [Test]
    public void Should_default_tooltip_to_null_when_not_specified()
    {
        // arrange & act — positional construction with only required + icon
        var marker = new Marker("id", new Coordinate(0, 0), null, Icon: new MarkerIcon("/icon.png"));

        // assert
        marker.Tooltip.Should().BeNull();
    }

    [Test]
    public void Should_create_marker_with_default_z_index_properties()
    {
        // arrange & act
        var marker = new Marker("test-id", new Coordinate(49.6, 6.1), "Test Title");

        // assert
        marker.ZIndexOffset.Should().BeNull();
        marker.RiseOnHover.Should().BeNull();
        marker.RiseOffset.Should().BeNull();
    }

    [Test]
    public void Should_create_marker_with_rise_on_hover_properties()
    {
        // arrange & act
        var marker = new Marker(
            "test-id",
            new Coordinate(49.6, 6.1),
            "Test Title",
            ZIndexOffset: 100,
            RiseOnHover: true,
            RiseOffset: 500
        );

        // assert
        marker.ZIndexOffset.Should().Be(100);
        marker.RiseOnHover.Should().BeTrue();
        marker.RiseOffset.Should().Be(500);
    }
}
