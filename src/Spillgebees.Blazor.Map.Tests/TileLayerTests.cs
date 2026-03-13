using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Tests;

public class TileLayerTests
{
    [Test]
    public void Wms_factory_should_set_wms_properties()
    {
        // act
        var tileLayer = TileLayer.Wms(
            baseUrl: "https://wms.example.com/service",
            attribution: "&copy; Example",
            layers: "basemap,labels",
            format: "image/png",
            transparent: true,
            version: "1.3.0",
            styles: "default",
            tileSize: 512
        );

        // assert
        tileLayer.UrlTemplate.Should().Be("https://wms.example.com/service");
        tileLayer.Attribution.Should().Be("&copy; Example");
        tileLayer.Layers.Should().Be("basemap,labels");
        tileLayer.Format.Should().Be("image/png");
        tileLayer.Transparent.Should().BeTrue();
        tileLayer.Version.Should().Be("1.3.0");
        tileLayer.Styles.Should().Be("default");
        tileLayer.TileSize.Should().Be(512);
    }

    [Test]
    public void Open_data_base_map_wms_should_be_configured_for_luxembourg_open_data()
    {
        // act
        var tileLayer = TileLayer.OpenDataBaseMapWms;

        // assert
        tileLayer.UrlTemplate.Should().Be("https://wms.geoportail.lu/geoserver/opendata/wms");
        tileLayer.Layers.Should().Be("basemap");
        tileLayer.Format.Should().Be("image/png");
        tileLayer.Version.Should().Be("1.3.0");
        tileLayer.Transparent.Should().BeFalse();
    }
}
