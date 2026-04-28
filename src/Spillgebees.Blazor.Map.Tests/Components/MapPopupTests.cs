using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class MapPopupTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string SetPopupContentIdentifier = "Spillgebees.Map.mapFunctions.setPopupContent";
    private const string RemovePopupContentIdentifier = "Spillgebees.Map.mapFunctions.removePopupContent";

    public MapPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(SetPopupContentIdentifier);
        JSInterop.SetupVoid(RemovePopupContentIdentifier);
    }

    [Test]
    public async Task Should_register_blazor_content_when_open()
    {
        // arrange
        var component = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );

        // act
        await component.Instance.OnMapInitializedAsync();

        // assert
        component.WaitForAssertion(() => JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1));
        JSInterop.Invocations[SetPopupContentIdentifier][0].Arguments[1].Should().Be("details");
    }

    [Test]
    public async Task Should_pass_close_callback_when_registering_blazor_content()
    {
        // arrange
        var component = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );

        // act
        await component.Instance.OnMapInitializedAsync();

        // assert
        component.WaitForAssertion(() => JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1));
        JSInterop.Invocations[SetPopupContentIdentifier][0].Arguments[6].Should().NotBeNull();
    }

    [Test]
    public async Task Should_defer_blazor_content_registration_until_map_is_ready()
    {
        // arrange
        var component = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );
        var popup = component.FindComponent<MapPopup>().Instance;

        // act & assert
        JSInterop.Invocations[SetPopupContentIdentifier].Should().BeEmpty();
        GetPrivateField<bool>(popup, "_contentRegistered").Should().BeFalse();
        GetPrivateField<string?>(popup, "_registeredId").Should().BeNull();
        GetPrivateField<object?>(popup, "_lastSnapshot").Should().BeNull();

        // act
        await component.Instance.OnMapInitializedAsync();

        // assert
        component.WaitForAssertion(() => JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1));
        GetPrivateField<bool>(popup, "_contentRegistered").Should().BeTrue();
        GetPrivateField<string?>(popup, "_registeredId").Should().Be("details");
        GetPrivateField<object?>(popup, "_lastSnapshot").Should().NotBeNull();
    }

    [Test]
    public async Task Should_register_blazor_content_once_after_duplicate_renders_before_map_is_ready()
    {
        // arrange
        var component = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );

        // act
        component.Render();
        await component.Instance.OnMapInitializedAsync();

        // assert
        component.WaitForAssertion(() => JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1));
    }

    [Test]
    public async Task Should_not_register_blazor_content_when_disposed_before_map_ready()
    {
        // arrange
        var showPopup = true;
        var component = Render<ConditionalPopupHost>(parameters => parameters.Add(p => p.ShowPopup, showPopup));
        var map = component.FindComponent<SgbMap>().Instance;
        JSInterop.Invocations[SetPopupContentIdentifier].Should().BeEmpty();

        // act
        showPopup = false;
        component.Render(parameters => parameters.Add(p => p.ShowPopup, showPopup));
        await map.OnMapInitializedAsync();
        await Task.Delay(100);

        // assert
        JSInterop.Invocations[SetPopupContentIdentifier].Should().BeEmpty();
    }

    [Test]
    public async Task Should_notify_open_changed_when_popup_is_closed_from_javascript()
    {
        // arrange
        var open = true;
        var component = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup
                        .Add(p => p.Id, "details")
                        .Add(p => p.Position, new Coordinate(49.61, 6.13))
                        .Add(p => p.Open, open)
                        .Add(p => p.OpenChanged, value => open = value)
                )
            )
        );
        var popup = component.FindComponent<MapPopup>().Instance;
        await component.Instance.OnMapInitializedAsync();
        component.WaitForAssertion(() => JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1));

        // act
        await popup.OnPopupClosedAsync();

        // assert
        open.Should().BeFalse();
    }

    [Test]
    public async Task Should_permit_reregistering_when_popup_is_closed_from_javascript_without_open_changed()
    {
        // arrange
        var component = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );
        var popup = component.FindComponent<MapPopup>().Instance;
        await component.Instance.OnMapInitializedAsync();
        component.WaitForAssertion(() => JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1));

        // act
        await popup.OnPopupClosedAsync();

        // assert
        JSInterop.Invocations[RemovePopupContentIdentifier].Should().BeEmpty();

        // act
        component.Render();

        // assert
        JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(2);
        JSInterop.Invocations[SetPopupContentIdentifier][1].Arguments[1].Should().Be("details");
    }

    [Test]
    public async Task Should_remove_old_popup_registration_when_id_changes_while_open()
    {
        // arrange
        var component = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );
        await component.Instance.OnMapInitializedAsync();
        component.WaitForAssertion(() => JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1));
        var popup = component.FindComponent<MapPopup>();

        // act
        popup.Render(parameters => parameters.Add(p => p.Id, "details-2"));

        // assert
        JSInterop.Invocations[RemovePopupContentIdentifier].Should().HaveCount(1);
        JSInterop.Invocations[RemovePopupContentIdentifier][0].Arguments[1].Should().Be("details");
        JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(2);
        JSInterop.Invocations[SetPopupContentIdentifier][1].Arguments[1].Should().Be("details-2");
    }

    [Test]
    public async Task Should_not_register_blazor_content_again_when_snapshot_is_unchanged()
    {
        // arrange
        var component = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );
        await component.Instance.OnMapInitializedAsync();
        component.WaitForAssertion(() => JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1));

        // act
        component.Render();

        // assert
        JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1);
    }

    [Test]
    public void Should_not_register_blazor_content_when_closed()
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup
                        .Add(p => p.Id, "details")
                        .Add(p => p.Position, new Coordinate(49.61, 6.13))
                        .Add(p => p.Open, false)
                )
            )
        );

        // assert
        JSInterop.Invocations[SetPopupContentIdentifier].Should().BeEmpty();
    }

    [Test]
    public void Should_throw_when_popup_is_outside_overlays_section()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            );

        // act & assert
        action.Should().Throw<InvalidOperationException>().WithMessage("MapPopup must be placed inside MapOverlays.");
    }

    private static T? GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull();
        return (T?)field!.GetValue(instance);
    }

    public sealed class ConditionalPopupHost : ComponentBase
    {
        [Parameter]
        public bool ShowPopup { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(
                1,
                nameof(SgbMap.ChildContent),
                (RenderFragment)(
                    childBuilder =>
                    {
                        childBuilder.OpenComponent<MapOverlays>(0);
                        childBuilder.AddAttribute(
                            1,
                            nameof(MapOverlays.ChildContent),
                            (RenderFragment)(
                                overlaysBuilder =>
                                {
                                    if (ShowPopup)
                                    {
                                        overlaysBuilder.OpenComponent<MapPopup>(0);
                                        overlaysBuilder.AddAttribute(1, nameof(MapPopup.Id), "details");
                                        overlaysBuilder.AddAttribute(
                                            2,
                                            nameof(MapPopup.Position),
                                            new Coordinate(49.61, 6.13)
                                        );
                                        overlaysBuilder.CloseComponent();
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
}
