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
    public void Should_define_shared_visibility_groups_for_toggleable_legend_items(CancellationToken cancellationToken)
    {
        var visibility = TrainTrackingPresentation.CreateLayerVisibility();
        var legendGroupIds = TrainTrackingPresentation
            .OverlayLegendDefinition.GetItems()
            .Where(item => item.VisibilityGroupId is not null)
            .Select(item => item.VisibilityGroupId)
            .ToArray();

        legendGroupIds.Should().AllSatisfy(groupId => visibility.Contains(groupId!).Should().BeTrue());
        visibility.IsVisible("tram").Should().BeFalse();
        visibility.IsVisible("infrastructure").Should().BeFalse();
        visibility.IsVisible("tracks").Should().BeTrue();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_pass_layer_visibility_to_map(CancellationToken cancellationToken)
    {
        var cut = Render<TrainTrackingExample>();
        var map = cut.FindComponent<SgbMap>().Instance;

        await map.OnMapInitializedAsync();

        map.LayerVisibility.Should().NotBeNull();
        map.LayerVisibility!.Contains("tracks").Should().BeTrue();
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_render_legend_items_as_visibility_group_bindings(CancellationToken cancellationToken)
    {
        var items = TrainTrackingPresentation.OverlayLegendDefinition.GetItems();

        items.Should().Contain(item => item.Id == "tracks" && item.VisibilityGroupId == "tracks");
        items.Should().Contain(item => item.Id == "trains" && item.VisibilityGroupId == "trains");
        items.Should().OnlyContain(item => item.VisibilityGroupId != null);
    }
}
