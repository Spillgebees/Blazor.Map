using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Tests;

public class MapCustomControlTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string SetControlsIdentifier = "Spillgebees.Map.mapFunctions.setControls";
    private const string SetControlContentIdentifier = "Spillgebees.Map.mapFunctions.setControlContent";
    private const string RemoveControlContentIdentifier = "Spillgebees.Map.mapFunctions.removeControlContent";

    public MapCustomControlTests()
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
            parameters.AddChildContent<MapCustomControl>(control =>
                control.Add(c => c.Id, "refresh-control").AddChildContent("Refresh")
            )
        );

        // assert
        cut.Markup.Should().Contain("Refresh");
        cut.Markup.Should().Contain("sgb-map-custom-control-placeholder");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_content_control_and_sync_element_references_after_map_ready(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapCustomControl>(control =>
                control
                    .Add(c => c.Id, "refresh-control")
                    .Add(c => c.Position, ControlPosition.TopLeft)
                    .Add(c => c.Order, 10)
                    .Add(c => c.Class, "refresh-shell")
                    .AddChildContent("Refresh")
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
    public async Task Should_remove_content_control_when_disabled(CancellationToken cancellationToken)
    {
        // arrange
        var enabled = true;
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapCustomControl>(control =>
                control.Add(c => c.Id, "refresh-control").Add(c => c.Enabled, enabled).AddChildContent("Refresh")
            )
        );
        cancellationToken.ThrowIfCancellationRequested();
        await cut.Instance.OnMapInitializedAsync();

        // act
        enabled = false;
        cut.Render(parameters =>
            parameters.AddChildContent<MapCustomControl>(control =>
                control.Add(c => c.Id, "refresh-control").Add(c => c.Enabled, enabled).AddChildContent("Refresh")
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
        AddPendingRemovalId(cut.FindComponent<MapCustomControl>().Instance, pendingControlId);

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
                parameters.AddChildContent<MapCustomControl>(control => control.Add(c => c.Id, " "))
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
                        if (ShowControl)
                        {
                            childBuilder.OpenComponent<MapCustomControl>(0);
                            childBuilder.AddAttribute(1, nameof(MapCustomControl.Id), "refresh-control");
                            childBuilder.AddAttribute(
                                2,
                                nameof(MapCustomControl.ChildContent),
                                (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "Refresh"))
                            );
                            childBuilder.CloseComponent();
                        }
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
        var field = component
            .GetType()
            .GetField(
                "_pendingRemovalIds",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
        var pendingRemovalIds = field!.GetValue(component).Should().BeAssignableTo<ICollection<string>>().Subject;
        pendingRemovalIds.Add(controlId);
    }
}
