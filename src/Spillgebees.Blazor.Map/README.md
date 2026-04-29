# Spillgebees.Blazor.Map

`Spillgebees.Blazor.Map` is a Blazor map component powered by [MapLibre GL JS](https://maplibre.org/).

See the [documentation and demos](https://spillgebees.github.io/Blazor.Map) for guides, examples, and live components.

## Quick example

```razor
<SgbMap MapOptions="@_mapOptions" Height="400px" Width="100%">
    <MapControls>
        <MapNavigationControl />
        <MapScaleControl />
    </MapControls>

    <MapSources>
        <GeoJsonSource Id="railway" Data="@_railwayGeoJson">
            <LineLayer Id="tracks" Color="#475569" Width="2" />
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

    private readonly string _railwayGeoJson = """
        { "type": "FeatureCollection", "features": [] }
        """;

    private readonly PopupOptions _luxembourgPopup = PopupOptions.FromText("Luxembourg City");
}
```
