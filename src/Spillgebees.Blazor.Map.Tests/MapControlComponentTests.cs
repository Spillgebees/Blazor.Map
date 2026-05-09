using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Tests;

public class MapControlComponentTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string SetControlsIdentifier = "Spillgebees.Map.mapFunctions.setControls";

    public MapControlComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(SetControlsIdentifier);
    }

    [Test]
    public void Should_render_no_markup_for_builtin_control_components()
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<NavigationMapControl>(control => control.Add(c => c.Id, "navigation-tools"))
            )
        );

        // assert
        cut.Markup.Should().NotContain("navigation-tools");
    }

    [Test]
    public async Task Should_register_builtin_control_before_map_creation()
    {
        // arrange
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<NavigationMapControl>(control =>
                    control
                        .Add(c => c.Id, "navigation-tools")
                        .Add(c => c.Position, ControlPosition.TopLeft)
                        .Add(c => c.Order, 10)
                        .Add(c => c.ShowCompass, false)
                        .Add(c => c.ShowZoom, true)
                )
            )
        );

        // act
        await cut.Instance.OnMapInitializedAsync();
        cut.Render();

        // assert
        JSInterop.VerifyInvoke(CreateMapIdentifier);
    }

    [Test]
    public async Task Should_update_builtin_control_when_parameters_change()
    {
        // arrange
        var showZoom = true;
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<NavigationMapControl>(control =>
                    control.Add(c => c.Id, "navigation-tools").Add(c => c.ShowZoom, showZoom)
                )
            )
        );
        await cut.Instance.OnMapInitializedAsync();

        // act
        showZoom = false;
        cut.Render(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<NavigationMapControl>(control =>
                    control.Add(c => c.Id, "navigation-tools").Add(c => c.ShowZoom, showZoom)
                )
            )
        );

        // assert
        JSInterop.Invocations[SetControlsIdentifier].Count.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Should_unregister_builtin_control_when_disposed()
    {
        // arrange
        var showControl = true;
        var cut = Render<ConditionalControlHost>(parameters => parameters.Add(p => p.ShowControl, showControl));
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        var initialCount = JSInterop.Invocations[SetControlsIdentifier].Count;

        // act
        showControl = false;
        cut.Render(parameters => parameters.Add(p => p.ShowControl, showControl));

        // assert
        JSInterop.Invocations[SetControlsIdentifier].Count.Should().BeGreaterThan(initialCount);

        var setControlsInvocations = JSInterop.Invocations[SetControlsIdentifier];
        var latestInvocation = setControlsInvocations[setControlsInvocations.Count - 1];
        var controlsPayload = latestInvocation.Arguments[1].Should().BeAssignableTo<IEnumerable<object>>().Subject;
        controlsPayload.Select(GetControlId).Should().NotContain("scale-tools");
    }

    [Test]
    public void Should_not_sync_builtin_control_when_disposed_before_map_ready()
    {
        // arrange
        var showControl = true;
        var cut = Render<ConditionalControlHost>(parameters => parameters.Add(p => p.ShowControl, showControl));

        // act
        showControl = false;
        cut.Render(parameters => parameters.Add(p => p.ShowControl, showControl));

        // assert
        JSInterop.Invocations[SetControlsIdentifier].Count.Should().Be(0);
    }

    [Test]
    public void Should_throw_when_registered_control_id_duplicates_existing_control()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters
                    .Add(p => p.Controls, [new NavigationControlDefinition("navigation-tools")])
                    .AddChildContent<MapControls>(controls =>
                        controls.AddChildContent<ScaleMapControl>(control => control.Add(c => c.Id, "navigation-tools"))
                    )
            );

        // act & assert
        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Control IDs must be unique. Duplicate ID: 'navigation-tools'.");
    }

    [Test]
    public void Should_use_default_geolocate_control_values()
    {
        // arrange
        var control = new GeolocateMapControl();

        // act
        var defaults = new
        {
            control.Id,
            control.Position,
            control.Order,
            control.Visible,
            control.TrackUser,
        };

        // assert
        defaults.Id.Should().Be("geolocate");
        defaults.Position.Should().Be(ControlPosition.TopRight);
        defaults.Order.Should().Be(300);
        defaults.Visible.Should().BeTrue();
        defaults.TrackUser.Should().BeFalse();
    }

    [Test]
    public void Should_use_default_scale_control_values()
    {
        // arrange
        var control = new ScaleMapControl();

        // act
        var defaults = new
        {
            control.Id,
            control.Position,
            control.Order,
            control.Visible,
            control.Unit,
        };

        // assert
        defaults.Id.Should().Be("scale");
        defaults.Position.Should().Be(ControlPosition.BottomLeft);
        defaults.Order.Should().Be(100);
        defaults.Visible.Should().BeTrue();
        defaults.Unit.Should().Be(ScaleUnit.Metric);
    }

    [Test]
    public void Should_use_default_terrain_control_source_id()
    {
        // arrange
        var control = new TerrainMapControl();

        // act
        var defaults = new
        {
            control.Id,
            control.Position,
            control.Order,
            control.Visible,
            control.SourceId,
        };

        // assert
        defaults.Id.Should().Be("terrain");
        defaults.Position.Should().Be(ControlPosition.TopRight);
        defaults.Order.Should().Be(400);
        defaults.Visible.Should().BeTrue();
        defaults.SourceId.Should().Be("terrain");
    }

    public sealed class ConditionalControlHost : ComponentBase
    {
        [Parameter]
        public bool ShowControl { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(
                1,
                nameof(SgbMap.ChildContent),
                (RenderFragment)(
                    childBuilder =>
                    {
                        childBuilder.OpenComponent<MapControls>(0);
                        childBuilder.AddAttribute(
                            1,
                            nameof(MapControls.ChildContent),
                            (RenderFragment)(
                                controlsBuilder =>
                                {
                                    if (ShowControl)
                                    {
                                        controlsBuilder.OpenComponent<ScaleMapControl>(0);
                                        controlsBuilder.AddAttribute(1, nameof(ScaleMapControl.Id), "scale-tools");
                                        controlsBuilder.CloseComponent();
                                    }
                                }
                            )
                        );
                        childBuilder.CloseComponent();
                    }
                )
            );
            builder.CloseComponent();
        }
    }

    private static string? GetControlId(object control) =>
        control.GetType().GetProperty("ControlId")?.GetValue(control)?.ToString();
}
