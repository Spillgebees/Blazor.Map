using AwesomeAssertions;
using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class SgbMapOverlayContentTests : BunitContext
{
    public SgbMapOverlayContentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Test]
    public void Should_render_overlay_content_inside_the_map_container()
    {
        // arrange
        RenderFragment overlay = builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "data-testid", "overlay-action");
            builder.AddContent(2, "Overlay action");
            builder.CloseElement();
        };

        // act
        var cut = Render<SgbMap>(parameters => parameters.Add(component => component.OverlayContent, overlay));

        // assert
        var container = cut.Find(".sgb-map-container");
        var overlayRoot = container.QuerySelector(".sgb-map-overlay-root");
        overlayRoot.Should().NotBeNull();
        overlayRoot!.QuerySelector("[data-testid='overlay-action']")!.TextContent.Should().Be("Overlay action");
        cut.Find(".sgb-map-root > .sgb-map-container > .sgb-map-overlay-root").Should().NotBeNull();
    }

    [Test]
    public void Should_not_render_overlay_root_when_overlay_content_is_not_supplied()
    {
        // arrange

        // act
        var cut = Render<SgbMap>();

        // assert
        cut.FindAll(".sgb-map-overlay-root").Should().BeEmpty();
    }
}
