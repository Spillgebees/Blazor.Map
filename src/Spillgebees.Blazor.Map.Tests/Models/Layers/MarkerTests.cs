using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Models.Popups;

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
        marker.Position.Should().Be(new Coordinate(49.6, 6.1));
        marker.Title.Should().Be("Test Title");
        marker.Icon.Should().BeNull();
        marker.Popup.Should().BeNull();
        marker.Color.Should().BeNull();
        marker.Scale.Should().BeNull();
        marker.Rotation.Should().BeNull();
        marker.Draggable.Should().BeFalse();
        marker.Opacity.Should().BeNull();
        marker.ClassName.Should().BeNull();
    }

    [Test]
    public void Should_create_marker_with_custom_icon()
    {
        // arrange
        var icon = new MarkerIcon("https://example.com/icon.png", Size: new Point(25, 41), Anchor: new Point(12, 41));

        // act
        var marker = new Marker("test-id", new Coordinate(49.6, 6.1), "Test Title", Icon: icon);

        // assert
        marker.Icon.Should().NotBeNull();
        marker.Icon!.Url.Should().Be("https://example.com/icon.png");
        marker.Icon.Size.Should().Be(new Point(25, 41));
        marker.Icon.Anchor.Should().Be(new Point(12, 41));
    }

    [Test]
    public void Should_create_marker_with_rotation()
    {
        // arrange & act
        var marker = new Marker("test-id", new Coordinate(49.6, 6.1), "Test Title", Rotation: 45.0);

        // assert
        marker.Rotation.Should().Be(45.0);
    }

    [Test]
    public void Should_create_marker_with_all_properties()
    {
        // arrange
        var icon = new MarkerIcon("https://example.com/icon.png");
        var popup = new PopupOptions("Hello");

        // act
        var marker = new Marker(
            "test-id",
            new Coordinate(49.6, 6.1),
            "Test Title",
            Popup: popup,
            Icon: icon,
            Color: "#ff0000",
            Scale: 1.5,
            Rotation: 90.0,
            Draggable: true,
            Opacity: 0.8,
            ClassName: "custom-marker"
        );

        // assert
        marker.Id.Should().Be("test-id");
        marker.Icon.Should().NotBeNull();
        marker.Popup.Should().NotBeNull();
        marker.Color.Should().Be("#ff0000");
        marker.Scale.Should().Be(1.5);
        marker.Rotation.Should().Be(90.0);
        marker.Draggable.Should().BeTrue();
        marker.Opacity.Should().Be(0.8);
        marker.ClassName.Should().Be("custom-marker");
    }

    [Test]
    public void Should_default_popup_to_null_when_not_specified()
    {
        // arrange & act — positional construction with only required + icon
        var marker = new Marker("id", new Coordinate(0, 0), null, Icon: new MarkerIcon("/icon.png"));

        // assert
        marker.Popup.Should().BeNull();
    }

    [Test]
    public void Should_create_marker_with_color_and_scale()
    {
        // arrange & act
        var marker = new Marker("test-id", new Coordinate(49.6, 6.1), "Test Title", Color: "#3388ff", Scale: 2.0);

        // assert
        marker.Color.Should().Be("#3388ff");
        marker.Scale.Should().Be(2.0);
    }

    [Test]
    public void Should_create_marker_with_draggable_and_opacity()
    {
        // arrange & act
        var marker = new Marker("test-id", new Coordinate(49.6, 6.1), "Test Title", Draggable: true, Opacity: 0.5);

        // assert
        marker.Draggable.Should().BeTrue();
        marker.Opacity.Should().Be(0.5);
    }
}
