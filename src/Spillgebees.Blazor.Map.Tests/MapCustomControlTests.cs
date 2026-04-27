using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Tests;

public class MapCustomControlTests : BunitContext
{
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

    [Test]
    public async Task Should_register_content_control_and_sync_element_references_after_map_ready()
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

        // act
        await cut.Instance.OnMapInitializedAsync();
        cut.Render();

        // assert
        JSInterop.VerifyInvoke(SetControlContentIdentifier);
        JSInterop.VerifyInvoke(CreateMapIdentifier);
    }

    [Test]
    public async Task Should_remove_content_control_when_disabled()
    {
        // arrange
        var enabled = true;
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapCustomControl>(control =>
                control.Add(c => c.Id, "refresh-control").Add(c => c.Enabled, enabled).AddChildContent("Refresh")
            )
        );
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
}
