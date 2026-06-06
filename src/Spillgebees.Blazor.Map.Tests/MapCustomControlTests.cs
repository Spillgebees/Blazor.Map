using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Tests;

public class CustomMapControlTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string SetControlsIdentifier = "Spillgebees.Map.mapFunctions.setControls";
    private const string SetControlContentIdentifier = "Spillgebees.Map.mapFunctions.setControlContent";
    private const string RemoveControlContentIdentifier = "Spillgebees.Map.mapFunctions.removeControlContent";

    public CustomMapControlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(SetControlsIdentifier);
        JSInterop.SetupVoid(SetControlContentIdentifier);
        JSInterop.SetupVoid(RemoveControlContentIdentifier);
    }

    [Test]
    public void Should_render_child_content_in_hidden_blazor_placeholder()
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<CustomMapControl>(control =>
                    control.Add(c => c.Id, "refresh-control").AddChildContent("Refresh")
                )
            )
        );

        // assert
        cut.Markup.Should().Contain("Refresh");
        cut.Markup.Should().Contain("sgb-map-custom-control-placeholder");
    }

    [Test]
    public void Should_expose_button_group_label_with_group_semantics()
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<ButtonGroupMapControl>(control =>
                    control.Add(c => c.Id, "station-tools").Add(c => c.Label, "Station tools").AddChildContent("Focus")
                )
            )
        );

        // assert
        var group = cut.Find(".sgb-map-control-button-group");
        group.GetAttribute("role").Should().Be("group");
        group.GetAttribute("aria-label").Should().Be("Station tools");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_content_control_and_sync_element_references_after_map_ready(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<CustomMapControl>(control =>
                    control
                        .Add(c => c.Id, "refresh-control")
                        .Add(c => c.Position, ControlPosition.TopLeft)
                        .Add(c => c.Order, 10)
                        .Add(c => c.Class, "refresh-shell")
                        .AddChildContent("Refresh")
                )
            )
        );
        cancellationToken.ThrowIfCancellationRequested();

        // act
        await cut.Instance.OnMapInitializedAsync();
        cut.Render();

        // assert
        JSInterop.VerifyInvoke(SetControlContentIdentifier);
        JSInterop.VerifyInvoke(CreateMapIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_remove_content_control_when_hidden(CancellationToken cancellationToken)
    {
        // arrange
        var visible = true;
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<CustomMapControl>(control =>
                    control.Add(c => c.Id, "refresh-control").Add(c => c.Visible, visible).AddChildContent("Refresh")
                )
            )
        );
        cancellationToken.ThrowIfCancellationRequested();
        await cut.Instance.OnMapInitializedAsync();

        // act
        visible = false;
        cut.Render(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<CustomMapControl>(control =>
                    control.Add(c => c.Id, "refresh-control").Add(c => c.Visible, visible).AddChildContent("Refresh")
                )
            )
        );

        // assert
        JSInterop.VerifyInvoke(RemoveControlContentIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_remove_pending_content_control_when_disposed(CancellationToken cancellationToken)
    {
        // arrange
        var showControl = true;
        var pendingControlId = "previous-refresh-control";
        var cut = Render<ConditionalCustomControlHost>(parameters => parameters.Add(p => p.ShowControl, showControl));
        var map = cut.FindComponent<SgbMap>().Instance;
        cancellationToken.ThrowIfCancellationRequested();
        await map.OnMapInitializedAsync();
        AddPendingRemovalId(cut.FindComponent<CustomMapControl>().Instance, pendingControlId);

        // act
        showControl = false;
        cut.Render(parameters => parameters.Add(p => p.ShowControl, showControl));

        // assert
        cut.WaitForAssertion(() =>
            JSInterop
                .Invocations[RemoveControlContentIdentifier]
                .Any(invocation =>
                    string.Equals(invocation.Arguments[1]?.ToString(), pendingControlId, StringComparison.Ordinal)
                )
                .Should()
                .BeTrue()
        );
    }

    [Test]
    public void Should_throw_when_id_is_empty()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapControls>(controls =>
                    controls.AddChildContent<CustomMapControl>(control => control.Add(c => c.Id, " "))
                )
            );

        // act & assert
        action.Should().Throw<InvalidOperationException>().WithMessage("A non-empty Id is required.");
    }

    public sealed class ConditionalCustomControlHost : ComponentBase
    {
        [Parameter]
        public bool ShowControl { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // arrange
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
                                        controlsBuilder.OpenComponent<CustomMapControl>(0);
                                        controlsBuilder.AddAttribute(1, nameof(CustomMapControl.Id), "refresh-control");
                                        controlsBuilder.AddAttribute(
                                            2,
                                            nameof(CustomMapControl.ChildContent),
                                            (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "Refresh"))
                                        );
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

            // act

            // assert
        }
    }

    private static void AddPendingRemovalId(object component, string controlId)
    {
        var registrationField = component
            .GetType()
            .GetField(
                "_registration",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
        var registration = registrationField!.GetValue(component);
        var pendingRemovalIdsField = registration!
            .GetType()
            .GetField(
                "_pendingRemovalIds",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
        var pendingRemovalIds = pendingRemovalIdsField!
            .GetValue(registration)
            .Should()
            .BeAssignableTo<ICollection<string>>()
            .Subject;
        pendingRemovalIds.Add(controlId);
    }
}
