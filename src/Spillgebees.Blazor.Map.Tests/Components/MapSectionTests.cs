using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class MapSectionTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string AddMapSourceIdentifier = "Spillgebees.Map.mapFunctions.addMapSource";

    public MapSectionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(AddMapSourceIdentifier);
    }

    [Test]
    public void Should_render_sources_inside_map_sources_section()
    {
        // arrange & act
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapSources>(sources =>
                sources.AddChildContent<GeoJsonSource>(source =>
                    source.Add(s => s.Id, "stations").Add(s => s.Data, new { })
                )
            )
        );

        // assert
        cut.FindComponent<GeoJsonSource>().Instance.Id.Should().Be("stations");
    }

    [Test]
    public void Should_throw_when_source_is_outside_map_sources_section()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<GeoJsonSource>(source =>
                    source.Add(s => s.Id, "stations").Add(s => s.Data, new { })
                )
            );

        // act & assert
        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("GeoJsonSource must be placed inside MapSources.");
    }

    [Test]
    public void Should_throw_when_control_is_outside_map_controls_section()
    {
        // arrange
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapNavigationControl>(control => control.Add(c => c.Id, "navigation"))
            );

        // act & assert
        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("MapNavigationControl must be placed inside MapControls.");
    }

    [Test]
    public void Should_throw_when_section_is_outside_map()
    {
        // arrange
        var action = () => Render<MapOverlays>();

        // act & assert
        action.Should().Throw<InvalidOperationException>().WithMessage("MapOverlays must be placed inside SgbMap.");
    }

    [Test]
    public void Should_render_tracked_entity_layer_inside_map_overlays_section()
    {
        // arrange
        var layer = TrackedEntityTestData.CreateLayer();

        // act
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapOverlays>(overlays =>
                overlays.AddChildContent<TrackedEntityLayer<TrackedEntityTestData.Vehicle>>(tracked =>
                    tracked.Add(t => t.Layer, layer)
                )
            )
        );

        // assert
        cut.FindComponent<TrackedEntityLayer<TrackedEntityTestData.Vehicle>>().Instance.Layer.Should().Be(layer);
    }

    [Test]
    public void Should_throw_when_tracked_entity_layer_is_outside_map_overlays_section()
    {
        // arrange
        var layer = TrackedEntityTestData.CreateLayer();
        var action = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<TrackedEntityLayer<TrackedEntityTestData.Vehicle>>(tracked =>
                    tracked.Add(t => t.Layer, layer)
                )
            );

        // act & assert
        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("TrackedEntityLayer must be placed inside MapOverlays.");
    }
}
