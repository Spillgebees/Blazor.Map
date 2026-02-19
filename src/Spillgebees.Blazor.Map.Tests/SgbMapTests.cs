using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models.Layers;

namespace Spillgebees.Blazor.Map.Tests;

public class SgbMapTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string InvalidateSizeIdentifier = "Spillgebees.Map.mapFunctions.invalidateSize";

    /// <summary>
    /// Timeout in milliseconds for tests to prevent hanging.
    /// </summary>
    private const int TestTimeoutMs = 5000;

    public SgbMapTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(InvalidateSizeIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_render_with_custom_dimensions(CancellationToken cancellationToken)
    {
        // arrange
        var tileLayers = new List<TileLayer> { TileLayer.OpenStreetMap };

        // act
        var cut = Render<SgbMap>(parameters =>
            parameters.Add(p => p.TileLayers, tileLayers).Add(p => p.Width, "800px").Add(p => p.Height, "600px")
        );

        // assert
        var mapContainer = cut.Find("div.sgb-map-container");
        var style = mapContainer.GetAttribute("style") ?? "";
        style.Should().Contain("width:800px");
        style.Should().Contain("height:600px");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_add_custom_css_to_map_container(CancellationToken cancellationToken)
    {
        // arrange
        var tileLayers = new List<TileLayer> { TileLayer.OpenStreetMap };

        // act
        var cut = Render<SgbMap>(parameters =>
            parameters.Add(p => p.TileLayers, tileLayers).Add(p => p.MapContainerClass, "my-custom-class")
        );

        // assert
        var mapContainer = cut.Find("div.sgb-map-container.my-custom-class");
        mapContainer.Should().NotBeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_trigger_map_initialization_after_render(CancellationToken cancellationToken)
    {
        // arrange
        var tileLayers = new List<TileLayer> { TileLayer.OpenStreetMap };

        // act
        Render<SgbMap>(parameters => parameters.Add(p => p.TileLayers, tileLayers));

        // assert
        JSInterop.VerifyInvoke(CreateMapIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_dispose_map_correctly_when_js_initialization_has_finished(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var tileLayers = new List<TileLayer> { TileLayer.OpenStreetMap };
        var cut = Render<SgbMap>(parameters => parameters.Add(p => p.TileLayers, tileLayers));

        // act
        // simulate map initialization completion
        await cut.Instance.OnMapInitializedAsync();
        await cut.Instance.DisposeAsync();

        // assert
        JSInterop.VerifyInvoke(DisposeMapIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_dispose_map_correctly_when_js_initialization_has_not_finished(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var tileLayers = new List<TileLayer> { TileLayer.OpenStreetMap };
        var cut = Render<SgbMap>(parameters => parameters.Add(p => p.TileLayers, tileLayers));

        // act
        await cut.Instance.DisposeAsync();

        // assert
        JSInterop.VerifyInvoke(DisposeMapIdentifier);
    }
}
