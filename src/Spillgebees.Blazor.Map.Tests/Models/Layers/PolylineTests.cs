using System.Collections.Immutable;
using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Tests.Models.Layers;

public class PolylineTests
{
    [Test]
    public void Should_be_equal_when_coordinates_are_identical()
    {
        // arrange
        var coordinates = ImmutableList.Create(new Coordinate(49.6, 6.1), new Coordinate(50.0, 7.0));
        var polyline1 = new Polyline("test-id", coordinates);
        var polyline2 = new Polyline("test-id", coordinates);

        // act & assert
        polyline1.Should().Be(polyline2);
    }

    [Test]
    public void Should_not_be_equal_when_coordinates_differ()
    {
        // arrange
        var polyline1 = new Polyline(
            "test-id",
            ImmutableList.Create(new Coordinate(49.6, 6.1), new Coordinate(50.0, 7.0))
        );
        var polyline2 = new Polyline(
            "test-id",
            ImmutableList.Create(new Coordinate(48.0, 5.0), new Coordinate(51.0, 8.0))
        );

        // act & assert
        polyline1.Should().NotBe(polyline2);
    }

    [Test]
    public void Should_not_be_equal_when_coordinates_have_different_order()
    {
        // arrange
        var polyline1 = new Polyline(
            "test-id",
            ImmutableList.Create(new Coordinate(49.6, 6.1), new Coordinate(50.0, 7.0))
        );
        var polyline2 = new Polyline(
            "test-id",
            ImmutableList.Create(new Coordinate(50.0, 7.0), new Coordinate(49.6, 6.1))
        );

        // act & assert
        polyline1.Should().NotBe(polyline2);
    }

    [Test]
    public void Should_be_equal_when_both_have_empty_coordinates()
    {
        // arrange
        var polyline1 = new Polyline("test-id", []);
        var polyline2 = new Polyline("test-id", []);

        // act & assert
        polyline1.Should().Be(polyline2);
    }

    [Test]
    public void Should_have_same_hash_code_when_equal()
    {
        // arrange
        var coordinates = ImmutableList.Create(new Coordinate(49.6, 6.1), new Coordinate(50.0, 7.0));
        var polyline1 = new Polyline("test-id", coordinates);
        var polyline2 = new Polyline("test-id", coordinates);

        // act
        var hashCode1 = polyline1.GetHashCode();
        var hashCode2 = polyline2.GetHashCode();

        // assert
        hashCode1.Should().Be(hashCode2);
    }

    [Test]
    public void Should_have_different_hash_code_when_coordinates_differ()
    {
        // arrange
        var polyline1 = new Polyline(
            "test-id",
            ImmutableList.Create(new Coordinate(49.6, 6.1), new Coordinate(50.0, 7.0))
        );
        var polyline2 = new Polyline(
            "test-id",
            ImmutableList.Create(new Coordinate(48.0, 5.0), new Coordinate(51.0, 8.0))
        );

        // act
        var hashCode1 = polyline1.GetHashCode();
        var hashCode2 = polyline2.GetHashCode();

        // assert
        hashCode1.Should().NotBe(hashCode2);
    }

    [Test]
    public void Should_not_be_equal_when_other_properties_differ()
    {
        // arrange
        var coordinates = ImmutableList.Create(new Coordinate(49.6, 6.1), new Coordinate(50.0, 7.0));
        var polyline1 = new Polyline("test-id", coordinates, StrokeColor: "#ff0000");
        var polyline2 = new Polyline("test-id", coordinates, StrokeColor: "#00ff00");

        // act & assert
        polyline1.Should().NotBe(polyline2);
    }
}
