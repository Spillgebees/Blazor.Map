`Spillgebees.Blazor.Map` is a Blazor map component powered by [MapLibre GL JS](https://maplibre.org/).

### Registering the component

This component comes with a [JS initializer](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-10.0#javascript-initializers), as such it is bootstrapped when `Blazor` launches.

The only thing you need to do is to add this package's CSS file for styling.

Include it in the `head` tag:

```html
<link href="_content/Spillgebees.Blazor.Map/Spillgebees.Blazor.Map.lib.module.css"
      rel="stylesheet" />
```

### Usage

You can take a look at the demo pages for a few general usage examples: [net10.0](https://spillgebees.github.io/Blazor.Map/main/net10.0/)

Use `SgbMap` with structured child sections. Put controls in `MapControls`, MapLibre sources and layers in `MapSources`, and application overlays in `MapOverlays`:

```razor
<SgbMap MapOptions="@_mapOptions" Height="400px" Width="100%">
    <MapControls>
        <MapNavigationControl />
        <MapScaleControl />
    </MapControls>

    <MapSources>
        <GeoJsonSource Id="stations" Data="@_stationGeoJson">
            <CircleLayer Id="station-circles" Radius="6" Color="#2563eb" />
        </GeoJsonSource>
    </MapSources>

    <MapOverlays>
        <MapMarker Id="luxembourg-city"
                   Position="@(new Coordinate(49.6117, 6.1319))"
                   Title="Luxembourg City"
                   Popup="@_luxembourgPopup" />
    </MapOverlays>
</SgbMap>

@code {
    private readonly MapOptions _mapOptions = new(
        Center: new Coordinate(49.6117, 6.1319),
        Zoom: 12,
        Style: MapStyle.OpenFreeMap.Liberty);

    private readonly string _stationGeoJson = """
        { "type": "FeatureCollection", "features": [] }
        """;

    private readonly PopupOptions _luxembourgPopup = PopupOptions.FromText("Luxembourg City");
}
```

WMS raster tiles are configured through `MapStyle`:

```csharp
var mapOptions = new MapOptions(
    Center: new Coordinate(49.6117, 6.1319),
    Zoom: 9,
    Style: MapStyle.FromWmsUrl(
        baseUrl: "https://wmts1.geoportail.lu/opendata/service",
        layers: "basemap_3d_hd",
        attribution: "&copy; OpenData Luxembourg",
        format: "image/png",
        transparent: false,
        version: "1.3.0"));
```
