using System.Reflection;
using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Docs.Samples;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests.Samples;

public class CustomControlsExampleTests : BunitContext
{
    private const string FlyToIdentifier = "Spillgebees.Map.mapFunctions.flyTo";

    public CustomControlsExampleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(FlyToIdentifier);
    }

    [Test]
    public void Should_render_focus_station_control_without_duplicate_builtin_controls()
    {
        // arrange
        var cut = Render<CustomControlsExample>();

        // act
        var focusButton = cut.Find("button.sgb-map-action-control-button");

        // assert
        cut.FindComponents<MapGeolocateControl>().Should().BeEmpty();
        cut.FindComponents<MapTerrainControl>().Should().BeEmpty();
        cut.Markup.Should().Contain("Focus station");
        cut.Markup.Should().Contain("Central Station");
        focusButton.GetAttribute("aria-label").Should().Be("Focus Central Station");
        cut.FindComponents<MapActionControl>().Should().HaveCount(1);
        focusButton.ParentElement!.ClassList.Should().NotContain("sgb-map-ctrl-group");

        var icon = focusButton.QuerySelector("svg.docs-station-focus-icon");
        icon.Should().NotBeNull();
        icon!.GetAttribute("aria-hidden").Should().Be("true");
        icon.GetAttribute("focusable").Should().Be("false");
        icon.QuerySelector("path")!.GetAttribute("stroke").Should().Be("currentColor");
    }

    [Test]
    public void Should_focus_and_cycle_station_features_from_custom_control()
    {
        // arrange
        var cut = Render<CustomControlsExample>();
        var source = cut.FindComponent<GeoJsonSource>();
        CountFeatures(source.Instance.Data).Should().Be(4);

        // act
        cut.Find("button.sgb-map-action-control-button").Click();

        // assert
        cut.Markup.Should().Contain("Focused Central Station");
        cut.Find("button.sgb-map-action-control-button").GetAttribute("aria-label").Should().Be("Focus North Station");
        CountFeatures(source.Instance.Data).Should().Be(4);
        JSInterop.VerifyInvoke(FlyToIdentifier);

        var flyToInvocation = JSInterop.Invocations[FlyToIdentifier][0];
        flyToInvocation.Arguments[1].Should().Be(new Coordinate(49.6117, 6.1319));
        flyToInvocation.Arguments[2].Should().Be(14);
    }

    private static int CountFeatures(object? data)
    {
        data.Should().NotBeNull();
        var features =
            data!.GetType().GetProperty("features", BindingFlags.Instance | BindingFlags.Public)!.GetValue(data)
            as object[];

        var count = features?.Length;

        count.Should().NotBeNull();
        return count.Value;
    }
}
