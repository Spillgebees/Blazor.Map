using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spillgebees.Blazor.Map.Components;
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
    public void Should_define_layer_visibility_groups_for_train_and_buildings_controls(
        CancellationToken cancellationToken
    )
    {
        var visibility = TrainTrackingPresentation.CreateLayerVisibility();

        visibility.Contains("trains").Should().BeTrue();
        visibility.Contains("3d-buildings").Should().BeTrue();
        visibility.Groups.Should().HaveCount(2);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_pass_layer_visibility_to_map(CancellationToken cancellationToken)
    {
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;

        await map.OnMapInitializedAsync();

        map.LayerVisibility.Should().NotBeNull();
        map.LayerVisibility!.Contains("trains").Should().BeTrue();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_overlay_parts_and_allow_whole_overlay_toggle(CancellationToken cancellationToken)
    {
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;

        await map.OnMapInitializedAsync();
        var overlay = map.GetOverlayItems().Single(item => item.Id == TrainTrackingPresentation.RailwayOverlayId);

        overlay.IsVisible.Should().BeTrue();
        overlay.Parts.Select(part => part.Id).Should().BeEquivalentTo(
            ["tracks", "tram", "stations", "platforms", "routes", "lifecycle", "infrastructure"]
        );
        overlay.Parts.Single(part => part.Id == "tram").IsVisible.Should().BeFalse();
        overlay.Parts.Single(part => part.Id == "infrastructure").IsVisible.Should().BeFalse();

        map.SetOverlayVisible(TrainTrackingPresentation.RailwayOverlayId, false);
        map.GetOverlayItems()
            .Single(item => item.Id == TrainTrackingPresentation.RailwayOverlayId)
            .IsVisible.Should()
            .BeFalse();

        map.SetOverlayVisible(TrainTrackingPresentation.RailwayOverlayId, true);
        map.GetOverlayItems().Single(item => item.Id == TrainTrackingPresentation.RailwayOverlayId).IsVisible.Should().BeTrue();
    }
}
