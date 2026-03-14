using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;

namespace Spillgebees.Blazor.Map.Tests;

public class SgbMapTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string GetProtocolVersionIdentifier = "Spillgebees.Map.getProtocolVersion";

    /// <summary>
    /// Timeout in milliseconds for tests to prevent hanging.
    /// </summary>
    private const int TestTimeoutMs = 5000;

    public SgbMapTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.Setup<int>(GetProtocolVersionIdentifier).SetResult(1);
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_render_with_custom_dimensions(CancellationToken cancellationToken)
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters => parameters.Add(p => p.Width, "800px").Add(p => p.Height, "600px"));

        // assert
        var mapContainer = cut.Find("div.sgb-map-container");
        var style = mapContainer.GetAttribute("style") ?? "";
        style.Should().Contain("width:800px");
        style.Should().Contain("height:600px");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_add_custom_css_to_map_container(CancellationToken cancellationToken)
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters => parameters.Add(p => p.ContainerClass, "my-custom-class"));

        // assert
        var mapContainer = cut.Find("div.sgb-map-container.my-custom-class");
        mapContainer.Should().NotBeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_trigger_map_initialization_after_render(CancellationToken cancellationToken)
    {
        // arrange & act
        Render<SgbMap>();

        // assert
        JSInterop.VerifyInvoke(CreateMapIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_dispose_map_correctly_when_js_initialization_has_finished(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<SgbMap>();

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
        var cut = Render<SgbMap>();

        // act
        await cut.Instance.DisposeAsync();

        // assert
        JSInterop.VerifyInvoke(DisposeMapIdentifier);
    }
}
