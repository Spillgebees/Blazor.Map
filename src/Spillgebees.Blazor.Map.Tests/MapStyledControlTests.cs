using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Visibility;

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
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<ButtonMapControl>(control =>
                    control
                        .Add(c => c.Id, "refresh-control")
                        .Add(c => c.Position, ControlPosition.TopLeft)
                        .Add(c => c.Label, "Refresh map")
                        .Add(c => c.Text, "Refresh")
                        .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clickCount++))
                )
            )
        );
        cancellationToken.ThrowIfCancellationRequested();

        // act
        await cut.Instance.OnMapInitializedAsync();
        cut.Render();
        cut.Find("button.sgb-map-action-control-button").Click();

        // assert
        JSInterop.VerifyInvoke(SetControlContentIdentifier);
        cut.Find("button.sgb-map-action-control-button").GetAttribute("title").Should().Be("Refresh map");
        clickCount.Should().Be(1);
    }

    [Test]
    public void Should_render_toggle_aria_pressed_and_invoke_is_on_changed()
    {
        // arrange
        bool? changedValue = null;
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<ToggleButtonMapControl>(control =>
                    control
                        .Add(c => c.Id, "layer-toggle")
                        .Add(c => c.Label, "Toggle stations")
                        .Add(c => c.Text, "Stations")
                        .Add(c => c.IsOn, true)
                        .Add(c => c.IsOnChanged, EventCallback.Factory.Create<bool>(this, value => changedValue = value))
                )
            )
        );

        // act
        var button = cut.Find("button.sgb-map-toggle-control-button");
        button.Click();

        // assert
        button.GetAttribute("aria-pressed").Should().Be("true");
        button.GetAttribute("title").Should().Be("Toggle stations");
        changedValue.Should().BeFalse();
    }

    [Test]
    public void Should_render_action_icon_text_layout_class()
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<ButtonMapControl>(control =>
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
            )
        );

        // assert
        cut.Find("button.sgb-map-control-button-with-icon-text").Should().NotBeNull();
        cut.Markup.Should().Contain("sgb-map-control-icon");
        cut.Markup.Should().Contain("sgb-map-control-text");
    }

    [Test]
    public void Should_throw_when_button_label_is_empty()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapControls>(controls =>
                    controls.AddChildContent<ButtonGroupMapControl>(group =>
                        group
                            .Add(p => p.Id, "tools")
                            .Add(p => p.Label, "Tools")
                            .AddChildContent<MapButton>(button => button.Add(p => p.Text, "Refresh"))
                    )
                )
            );

        // act & assert
        action.Should().Throw<InvalidOperationException>().WithMessage("A non-empty Label is required.");
    }

    [Test]
    public void Should_throw_when_button_has_no_visible_icon_or_text()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapControls>(controls =>
                    controls.AddChildContent<ButtonGroupMapControl>(group =>
                        group
                            .Add(p => p.Id, "tools")
                            .Add(p => p.Label, "Tools")
                            .AddChildContent<MapButton>(button => button.Add(p => p.Label, "Refresh"))
                    )
                )
            );

        // act & assert
        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("MapButton requires non-empty Text or Icon.");
    }

    [Test]
    public void Should_throw_when_toggle_button_on_state_has_no_visible_content()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapControls>(controls =>
                    controls.AddChildContent<ButtonGroupMapControl>(group =>
                        group
                            .Add(p => p.Id, "tools")
                            .Add(p => p.Label, "Tools")
                            .AddChildContent<MapToggleButton>(button =>
                                button
                                    .Add(p => p.Label, "Toggle stations")
                                    .Add(p => p.IsOn, true)
                                    .Add(p => p.OffText, "Show")
                            )
                    )
                )
            );

        // act & assert
        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("MapToggleButton requires visible content for the current on state.");
    }

    [Test]
    public void Should_render_toggle_control_without_redundant_wrapper_aria_label()
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<ToggleButtonMapControl>(control =>
                    control
                        .Add(c => c.Id, "layer-toggle")
                        .Add(c => c.Label, "Toggle stations")
                        .Add(c => c.Text, "Stations")
                )
            )
        );

        // assert
        cut.Find(".sgb-map-toggle-control").HasAttribute("aria-label").Should().BeFalse();
        cut.Find("button.sgb-map-toggle-control-button").GetAttribute("aria-label").Should().Be("Toggle stations");
    }

    [Test]
    public void Should_use_explicit_button_title_when_provided()
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<ButtonMapControl>(control =>
                    control
                        .Add(c => c.Id, "refresh-control")
                        .Add(c => c.Label, "Refresh map")
                        .Add(c => c.Title, "Reload visible data")
                        .Add(c => c.Text, "Refresh")
                )
            )
        );

        // assert
        cut.Find("button.sgb-map-action-control-button").GetAttribute("aria-label").Should().Be("Refresh map");
        cut.Find("button.sgb-map-action-control-button").GetAttribute("title").Should().Be("Reload visible data");
    }

    [Test]
    public void Should_throw_when_panel_label_is_empty()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapControls>(controls =>
                    controls.AddChildContent<PanelMapControl>(panel =>
                        panel.Add(p => p.Id, "filters").Add(p => p.Label, " ")
                    )
                )
            );

        // act & assert
        action.Should().Throw<InvalidOperationException>().WithMessage("A non-empty Label is required.");
    }

    [Test]
    public async Task Should_invoke_panel_open_state_callback()
    {
        // arrange
        bool? openState = null;
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<PanelMapControl>(panel =>
                    panel
                        .Add(p => p.Id, "filters")
                        .Add(p => p.Label, "Filters")
                        .Add(p => p.IsOpen, false)
                        .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, value => openState = value))
                )
            )
        );

        // act
        await cut.FindComponent<PanelMapControl>().Instance.OnPanelOpenChangedAsync(true);

        // assert
        openState.Should().BeTrue();
    }

    [Test]
    public void Should_render_layer_visibility_control_and_bind_switches()
    {
        // arrange
        var visibility = new MapLayerVisibilityState(
            [
                new MapLayerVisibilityGroup("routes", [MapLayerVisibilityTarget.Layer("routes-layer")], Label: "Routes"),
                new MapLayerVisibilityGroup("stations", [MapLayerVisibilityTarget.Layer("stations-layer")], Label: "Stations"),
            ]
        );

        var cut = Render<SgbMap>(parameters =>
            parameters
                .Add(map => map.LayerVisibility, visibility)
                .AddChildContent<MapControls>(controls =>
                    controls.AddChildContent<LayerMapControl>(control =>
                        control
                            .Add(c => c.Id, "layers")
                            .Add(c => c.GroupIds, ["stations"])
                    )
                )
        );

        // act
        cut.Find("[data-testid='map-layer-toggle-stations']").Change(false);

        // assert
        cut.Markup.Should().Contain("Stations");
        cut.Markup.Should().NotContain("Routes");
        visibility.TryGetGroup("stations", out var stations).Should().BeTrue();
        stations!.IsVisible.Should().BeFalse();
    }
}
