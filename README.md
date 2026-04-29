# Spillgebees.Blazor.Map

<p align="center">
    <a href="https://www.nuget.org/packages/Spillgebees.Blazor.Map"><img alt="Nuget (with prereleases)" src="https://img.shields.io/nuget/vpre/Spillgebees.Blazor.Map?logo=nuget&style=for-the-badge"></a>
    <img alt="GitHub Workflow Status (with branch)" src="https://img.shields.io/github/actions/workflow/status/spillgebees/Blazor.Map/build-and-test.yml?branch=main&label=build%20%26%20test&style=for-the-badge" />
</p>

`Spillgebees.Blazor.Map` is a Blazor map component powered by [MapLibre GL JS](https://maplibre.org/).

More details in the [documentation](./src/Spillgebees.Blazor.Map/README.md).

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

Live demo: [net10.0](https://spillgebees.github.io/Blazor.Map/main/net10.0/)
