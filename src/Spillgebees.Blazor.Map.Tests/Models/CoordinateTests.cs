using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests.Models;

public class CoordinateTests
{
    [Test]
    public void Should_create_coordinate_from_latitude_longitude_order()
    {
        // arrange
        var latitude = 49.6117;
        var longitude = 6.1319;

        // act
        var coordinate = Coordinate.FromLatLng(latitude, longitude);

        // assert
        coordinate.Should().Be(new Coordinate(latitude, longitude));
    }

    [Test]
    public void Should_create_coordinate_from_longitude_latitude_order()
    {
        // arrange
        var longitude = 6.1319;
        var latitude = 49.6117;

        // act
        var coordinate = Coordinate.FromLngLat(longitude, latitude);

        // assert
        coordinate.Should().Be(new Coordinate(latitude, longitude));
    }
}
