using AwesomeAssertions;
using Spillgebees.Blazor.Map.Docs.Samples;

namespace Spillgebees.Blazor.Map.Tests.Samples;

public class InteractionMetadataExampleTests : BunitContext
{
    public InteractionMetadataExampleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Test]
    public void Should_render_a_page_level_cooperative_interaction_guide()
    {
        // arrange
        var expectedInteractions = MapInteractionMetadata.GetDefaults(cooperativeGestures: true);

        // act
        var cut = Render<InteractionMetadataExample>();

        // assert
        cut.FindComponent<SgbMap>().Instance.CooperativeGestures.Should().BeTrue();
        cut.FindAll(".interaction-reference-row").Should().HaveCount(expectedInteractions.Count);
        cut.Markup.Should().Contain("Ctrl/Cmd + scroll");
        cut.Markup.Should().Contain("Two-finger drag");
        cut.Markup.Should().Contain("Three-finger vertical drag");
        cut.Markup.Should().NotContain("One-finger drag");
    }

    [Test]
    public void Should_wire_an_accessible_native_popover_outside_the_map()
    {
        // arrange
        var cut = Render<InteractionMetadataExample>();

        // act
        var trigger = cut.Find(".interaction-help-trigger");
        var popover = cut.Find("#map-interaction-reference");

        // assert
        trigger.GetAttribute("popovertarget").Should().Be("map-interaction-reference");
        trigger.GetAttribute("aria-haspopup").Should().Be("dialog");
        popover.GetAttribute("popover").Should().Be("auto");
        popover.GetAttribute("role").Should().Be("dialog");
        popover.GetAttribute("aria-labelledby").Should().Be("interaction-reference-title");
        popover.QuerySelector("[popovertargetaction='hide']").Should().NotBeNull();
        cut.Find(".sgb-map-root").Contains(popover).Should().BeFalse();
    }

    [Test]
    public void Should_render_a_visual_for_every_documented_input()
    {
        // arrange
        var cut = Render<InteractionMetadataExample>();

        // act
        var visuals = cut.FindAll(".interaction-gesture-visual");

        // assert
        visuals.Should().HaveCount(MapInteractionMetadata.GetDefaults(cooperativeGestures: true).Count);
        visuals.Should().OnlyContain(visual => visual.GetAttribute("aria-hidden") == "true");
    }
}
