using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class MapPopupTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string SetPopupContentIdentifier = "Spillgebees.Map.mapFunctions.setPopupContent";
    private const string RemovePopupContentIdentifier = "Spillgebees.Map.mapFunctions.removePopupContent";

    public MapPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(SetPopupContentIdentifier);
        JSInterop.SetupVoid(RemovePopupContentIdentifier);
    }

    [Test]
    public void Should_register_blazor_content_when_open()
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );

        // assert
        JSInterop.Invocations[SetPopupContentIdentifier].Should().HaveCount(1);
        JSInterop.Invocations[SetPopupContentIdentifier][0].Arguments[1].Should().Be("details");
    }

    [Test]
    public void Should_pass_close_callback_when_registering_blazor_content()
    {
        // arrange & act
        Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );

        // assert
        JSInterop.Invocations[SetPopupContentIdentifier][0].Arguments[6].Should().NotBeNull();
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
    public void Should_remove_old_popup_registration_when_id_changes_while_open()
    {
        // arrange
        var component = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<MapPopup>(popup =>
                    popup.Add(p => p.Id, "details").Add(p => p.Position, new Coordinate(49.61, 6.13))
                )
            )
        );
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
    public void Should_not_register_blazor_content_again_when_snapshot_is_unchanged()
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
}
