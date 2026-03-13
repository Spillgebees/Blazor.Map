using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Tests;

public class TileLayerTests
{
    [Test]
    public void Wms_factory_should_set_wms_properties()
    {
        // act
        var tileLayer = TileLayer.CreateWms(
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
        tileLayer.Wms.Should().BeEquivalentTo(
            new WmsLayerOptions(
                Layers: "basemap,labels",
                Format: "image/png",
                Transparent: true,
                Version: "1.3.0",
                Styles: "default"
            )
        );
        tileLayer.Tile.Should().BeEquivalentTo(new TileLayerOptions(TileSize: 512));
    }

    [Test]
    public void Open_data_base_map_wms_should_be_configured_for_luxembourg_open_data()
    {
        // act
        var tileLayer = TileLayer.OpenDataBaseMapWms;

        // assert
        tileLayer.UrlTemplate.Should().Be("https://wms.geoportail.lu/geoserver/opendata/wms");
        tileLayer.Wms.Should().BeEquivalentTo(
            new WmsLayerOptions(
                Layers: "basemap",
                Format: "image/png",
                Transparent: false,
                Version: "1.3.0"
            )
        );
    }

    [Test]
    public void Regular_tile_constructor_should_group_tile_specific_options()
    {
        // act
        var tileLayer = new TileLayer(
            urlTemplate: "https://{s}.tile.example.com/{z}/{x}/{y}.png",
            attribution: "&copy; Example",
            detectRetina: true,
            tileSize: 512
        );

        // assert
        tileLayer.Tile.Should().BeEquivalentTo(new TileLayerOptions(DetectRetina: true, TileSize: 512));
        tileLayer.Wms.Should().BeNull();
    }
}
