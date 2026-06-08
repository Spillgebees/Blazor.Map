using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spillgebees.Blazor.Map;
using Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

namespace Spillgebees.Blazor.Map.Tests.Samples.TrainTracking;

public class TrainTrackingExampleTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";
    private const string SetControlContentIdentifier = "Spillgebees.Map.mapFunctions.setControlContent";
    private const string SetImagesIdentifier = "Spillgebees.Map.mapFunctions.setImages";

    public TrainTrackingExampleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
        JSInterop.SetupVoid(SetControlContentIdentifier);
        JSInterop.SetupVoid(SetImagesIdentifier);

        Services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [TrainTrackingPresentation.OverlayStyleUrlConfigurationKey] = "traintracking/style.json",
                    }
                )
                .Build()
        );
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_define_shared_display_items_for_toggleable_legend_items(CancellationToken cancellationToken)
    {
        var display = TrainTrackingPresentation.CreateDisplay();
        var legendItemIds = TrainTrackingPresentation
            .OverlayLegendDefinition.GetItems()
            .Where(item => item.DisplayItemId is not null)
            .Select(item => item.DisplayItemId)
            .ToArray();

        legendItemIds.Should().AllSatisfy(itemId => display.Contains(itemId!).Should().BeTrue());
        display.IsOn("tram").Should().BeFalse();
        display.IsOn("infrastructure").Should().BeFalse();
        display.IsOn("tracks").Should().BeTrue();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_pass_display_to_map(CancellationToken cancellationToken)
    {
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;

        await map.OnMapInitializedAsync();

        map.Display.Should().NotBeNull();
        map.Display!.Contains("tracks").Should().BeTrue();
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_render_legend_items_as_display_bindings(CancellationToken cancellationToken)
    {
        var items = TrainTrackingPresentation.OverlayLegendDefinition.GetItems();

        items.Should().Contain(item => item.Id == "tracks" && item.DisplayItemId == "tracks");
        items.Should().Contain(item => item.Id == "trains" && item.DisplayItemId == "trains");
        items.Should().OnlyContain(item => item.DisplayItemId != null);
    }
}
