using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Tests;

public class MapStyledControlTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string SetControlsIdentifier = "Spillgebees.Map.mapFunctions.setControls";
    private const string SetControlContentIdentifier = "Spillgebees.Map.mapFunctions.setControlContent";
    private const string RemoveControlContentIdentifier = "Spillgebees.Map.mapFunctions.removeControlContent";

    public MapStyledControlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(SetControlsIdentifier);
        JSInterop.SetupVoid(SetControlContentIdentifier);
        JSInterop.SetupVoid(RemoveControlContentIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_action_control_content_and_invoke_on_click(CancellationToken cancellationToken)
    {
        // arrange
        var clickCount = 0;
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapActionControl>(control =>
                control
                    .Add(c => c.Id, "refresh-control")
                    .Add(c => c.Position, ControlPosition.TopLeft)
                    .Add(c => c.Label, "Refresh map")
                    .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clickCount++))
            )
        );
        cancellationToken.ThrowIfCancellationRequested();

        // act
        await cut.Instance.OnMapInitializedAsync();
        cut.Render();
        cut.Find("button.sgb-map-action-control-button").Click();

        // assert
        JSInterop.VerifyInvoke(SetControlContentIdentifier);
        clickCount.Should().Be(1);
    }

    [Test]
    public void Should_render_toggle_aria_pressed_and_invoke_pressed_changed()
    {
        // arrange
        bool? changedValue = null;
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapToggleControl>(control =>
                control
                    .Add(c => c.Id, "layer-toggle")
                    .Add(c => c.Label, "Toggle stations")
                    .Add(c => c.Pressed, true)
                    .Add(c => c.PressedChanged, EventCallback.Factory.Create<bool>(this, value => changedValue = value))
            )
        );

        // act
        var button = cut.Find("button.sgb-map-toggle-control-button");
        button.Click();

        // assert
        button.GetAttribute("aria-pressed").Should().Be("true");
        changedValue.Should().BeFalse();
    }

    [Test]
    public void Should_render_action_icon_text_layout_class()
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapActionControl>(control =>
                control
                    .Add(c => c.Id, "focus-control")
                    .Add(c => c.Label, "Focus station")
                    .Add(c => c.Text, "Central Station")
                    .Add(
                        c => c.Icon!,
                        builder =>
                        {
                            builder.OpenElement(0, "svg");
                            builder.CloseElement();
                        }
                    )
            )
        );

        // assert
        cut.Find("button.sgb-map-control-button-with-icon-text").Should().NotBeNull();
        cut.Markup.Should().Contain("sgb-map-control-icon");
        cut.Markup.Should().Contain("sgb-map-control-text");
    }
}
