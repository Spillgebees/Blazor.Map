using AwesomeAssertions;

namespace Spillgebees.Blazor.Map.Tests.Models.Controls;

public class ControlIconTests
{
    [Test]
    public void Fullscreen_definition_should_default_its_icons_to_null()
    {
        // arrange / act
        var control = new FullscreenControlDefinition();

        // assert — un-customised controls opt out of icon overrides entirely
        control.EnterIcon.Should().BeNull();
        control.ExitIcon.Should().BeNull();
        control.EnterTitle.Should().BeNull();
        control.ExitTitle.Should().BeNull();
    }

    [Test]
    public void Fullscreen_definition_should_carry_custom_icons()
    {
        // arrange / act
        var control = new FullscreenControlDefinition(
            EnterIcon: "<svg data-enter></svg>",
            ExitIcon: "<svg data-exit></svg>",
            EnterTitle: "Go big",
            ExitTitle: "Go small"
        );

        // assert
        control.EnterIcon.Should().Be("<svg data-enter></svg>");
        control.ExitIcon.Should().Be("<svg data-exit></svg>");
        control.EnterTitle.Should().Be("Go big");
        control.ExitTitle.Should().Be("Go small");
    }

    [Test]
    public void Center_definition_should_carry_a_custom_icon()
    {
        // arrange / act
        var control = new CenterControlDefinition(Icon: "<svg data-center></svg>");

        // assert
        control.Icon.Should().Be("<svg data-center></svg>");
    }
}
