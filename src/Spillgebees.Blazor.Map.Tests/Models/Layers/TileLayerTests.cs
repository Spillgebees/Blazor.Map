using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Tests.Models.Layers;

public class TileLayerTests
{
    [Test]
    public void Should_group_wms_properties_in_tile_layer_record()
    {
        // act
        var tileLayer = new TileLayer(
            UrlTemplate: "https://wms.example.com/service",
            Attribution: "&copy; Example",
            Tile: new TileLayerOptions(TileSize: 512),
            Wms: new WmsLayerOptions(
                Layers: "basemap,labels",
                Format: "image/png",
                Transparent: true,
                Version: "1.3.0",
                Styles: "default"
            )
        );

        // assert
        tileLayer.UrlTemplate.Should().Be("https://wms.example.com/service");
        tileLayer.Attribution.Should().Be("&copy; Example");
        tileLayer
            .Wms.Should()
            .BeEquivalentTo(
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
    public void Should_configure_open_data_base_map_wms_for_luxembourg()
    {
        // act
        var tileLayer = TileLayer.OpenDataBaseMapWms;

        // assert
        tileLayer.UrlTemplate.Should().Be("https://wmts1.geoportail.lu/opendata/service");
        tileLayer
            .Wms.Should()
            .BeEquivalentTo(
                new WmsLayerOptions(Layers: "basemap", Format: "image/png", Transparent: false, Version: "1.3.0")
            );
    }

    [Test]
    public void Should_group_tile_specific_options_in_regular_tile_layer()
    {
        // act
        var tileLayer = new TileLayer(
            UrlTemplate: "https://{s}.tile.example.com/{z}/{x}/{y}.png",
            Attribution: "&copy; Example",
            Tile: new TileLayerOptions(DetectRetina: true, TileSize: 512)
        );

        // assert
        tileLayer.Tile.Should().BeEquivalentTo(new TileLayerOptions(DetectRetina: true, TileSize: 512));
        tileLayer.Wms.Should().BeNull();
    }
}
