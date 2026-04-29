using AwesomeAssertions;
using Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Tests.Samples.TrainTracking;

public class TrainSampleSimulationTests
{
    [Test]
    public void Should_create_initialized_train_states_from_catalog()
    {
        // arrange
        var expectedTrainCount = TrainSampleCatalog.Definitions.Count;

        // act
        var trains = TrainSampleSimulation.CreateStates();

        // assert
        trains.Should().HaveCount(expectedTrainCount);
        trains.Should().OnlyContain(train => train.Waypoints.Count >= 2);
        trains.Should().OnlyContain(train => train.NextPosition != new Coordinate(0, 0));
        trains.Should().OnlyContain(train => train.CurrentPosition != train.NextPosition);
    }

    [Test]
    public void Should_wrap_to_first_waypoint_when_advancing_past_last_segment()
    {
        // arrange
        var train = new TrainSampleState(
            "test-train",
            "T 1",
            "Alpha > Beta",
            "TestRail",
            "#123456",
            0.1,
            [new Coordinate(49.0, 6.0), new Coordinate(50.0, 7.0), new Coordinate(51.0, 8.0)]
        )
        {
            WaypointIndex = 1,
            Progress = 0.95,
            CurrentPosition = new Coordinate(50.95, 7.95),
            NextPosition = new Coordinate(51.0, 8.0),
        };

        // act
        TrainSampleSimulation.Advance(train);

        // assert
        train.WaypointIndex.Should().Be(0);
        train.Progress.Should().Be(0.0);
        train.NextPosition.Should().Be(new Coordinate(50.0, 7.0));
        train.CurrentPosition.Should().Be(new Coordinate(49.0, 6.0));
    }

    [Test]
    public void Should_mark_only_the_hovered_train_in_geojson()
    {
        // arrange
        var trains = new[]
        {
            new TrainSampleState(
                "hovered",
                "RE 11",
                "Luxembourg > Ettelbruck",
                "CFL",
                "#2563eb",
                0.03,
                [new Coordinate(49.6, 6.1), new Coordinate(49.7, 6.2)]
            )
            {
                CurrentPosition = new Coordinate(49.6, 6.1),
                NextPosition = new Coordinate(49.7, 6.2),
            },
            new TrainSampleState(
                "idle",
                "RB 10",
                "Ettelbruck > Diekirch",
                "CFL",
                "#2563eb",
                0.04,
                [new Coordinate(49.8, 6.3), new Coordinate(49.9, 6.4)]
            )
            {
                CurrentPosition = new Coordinate(49.8, 6.3),
                NextPosition = new Coordinate(49.9, 6.4),
            },
        };

        // act
        var geoJson = TrainSampleSimulation.BuildGeoJson(trains, "hovered");

        // assert
        geoJson.Features.Should().HaveCount(2);
        geoJson.Features.Single(feature => feature.Id == "hovered").Properties.Hovered.Should().BeTrue();
        geoJson.Features.Single(feature => feature.Id == "idle").Properties.Hovered.Should().BeFalse();
        geoJson.Features.Should().OnlyContain(feature => feature.Properties.Icon == "train-2563eb");
    }

    [Test]
    public void Should_throw_when_train_definition_has_insufficient_waypoints()
    {
        // arrange
        var invalidTrain = new TrainSampleState(
            "invalid",
            "T 2",
            "Nowhere",
            "TestRail",
            "#654321",
            0.1,
            [new Coordinate(49.0, 6.0)]
        );

        // act
        var act = () => TrainSampleSimulation.BuildGeoJson([invalidTrain], null);

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least two waypoints*");
    }

    [Test]
    public void Should_build_tracked_entities_with_primary_symbol_and_companion_decorations()
    {
        // arrange
        var train = new TrainSampleState(
            "re11",
            "RE 11",
            "Luxembourg > Ettelbruck",
            "CFL",
            "#2563eb",
            0.03,
            [new Coordinate(49.6, 6.1), new Coordinate(49.7, 6.2)]
        )
        {
            CurrentPosition = new Coordinate(49.6, 6.1),
            NextPosition = new Coordinate(49.7, 6.2),
        };

        // act
        var layer = TrainSampleSimulation.BuildTrackedEntityLayer([train]);
        var trackedEntities = TrackedEntityMaterializer.Materialize(
            layer.Items,
            layer.IdOptions,
            layer.Visual.Symbol,
            layer.Visual.Decorations
        );

        // assert
        trackedEntities.Should().HaveCount(1);
        trackedEntities[0].Id.Should().Be("re11");
        trackedEntities[0].Item.Should().BeSameAs(train);
        trackedEntities[0].Symbol.IconImage.Should().Be("train-2563eb");
        trackedEntities[0].Hover.Should().BeEquivalentTo(new TrackedEntityHoverIntent(1.2, true));
        trackedEntities[0]
            .Decorations.Select(decoration => decoration.Id)
            .Should()
            .BeEquivalentTo(["service", "route", "operator"]);
        trackedEntities[0]
            .Decorations.Single(decoration => decoration.Id == "route")
            .DisplayMode.Should()
            .Be(TrackedEntityDecorationDisplayMode.Hover);
        trackedEntities[0]
            .Decorations.Single(decoration => decoration.Id == "operator")
            .DisplayMode.Should()
            .Be(TrackedEntityDecorationDisplayMode.Selected);

        trackedEntities[0]
            .Decorations.Where(decoration => decoration.Id is "service" or "route")
            .Should()
            .OnlyContain(decoration => decoration.Rotation == trackedEntities[0].Symbol.Rotation);

        trackedEntities[0].Properties.Should().NotBeNull();
        trackedEntities[0].Properties!.Should().ContainKey("internationalPresence");
        trackedEntities[0].Properties!["internationalPresence"].Should().Be(0);
    }

    [Test]
    public void Should_mark_cross_border_trains_for_cluster_styling_metadata()
    {
        // arrange
        var train = new TrainSampleState(
            "sncb-ic01",
            "IC 2145",
            "Bruxelles-Midi > Namur",
            "SNCB",
            "#eab308",
            0.025,
            [new Coordinate(50.8353, 4.3360), new Coordinate(50.8100, 4.3700)]
        )
        {
            CurrentPosition = new Coordinate(50.8353, 4.3360),
            NextPosition = new Coordinate(50.8100, 4.3700),
        };

        // act
        var layer = TrainSampleSimulation.BuildTrackedEntityLayer([train]);
        var trackedEntity = TrackedEntityMaterializer
            .Materialize(layer.Items, layer.IdOptions, layer.Visual.Symbol, layer.Visual.Decorations)
            .Single();

        // assert
        trackedEntity.Properties.Should().NotBeNull();
        trackedEntity.Properties!.Should().ContainKey("internationalPresence");
        trackedEntity.Properties!["internationalPresence"].Should().Be(1);
    }

    [Test]
    public void Should_not_assign_rotation_to_operator_selected_banner()
    {
        // arrange
        var train = new TrainSampleState(
            "re11",
            "RE 11",
            "Luxembourg > Ettelbruck",
            "CFL",
            "#2563eb",
            0.03,
            [new Coordinate(49.6, 6.1), new Coordinate(49.7, 6.2)]
        )
        {
            CurrentPosition = new Coordinate(49.6, 6.1),
            NextPosition = new Coordinate(49.7, 6.2),
        };

        // act
        var layer = TrainSampleSimulation.BuildTrackedEntityLayer([train]);
        var trackedEntity = TrackedEntityMaterializer
            .Materialize(layer.Items, layer.IdOptions, layer.Visual.Symbol, layer.Visual.Decorations)
            .Single();

        // assert
        trackedEntity.Decorations.Single(decoration => decoration.Id == "operator").Rotation.Should().BeNull();
    }

    [Test]
    public void Should_preserve_train_and_decoration_identity_across_simulation_rebuilds()
    {
        // arrange
        var train = new TrainSampleState(
            "re11",
            "RE 11",
            "Luxembourg > Ettelbruck",
            "CFL",
            "#2563eb",
            0.03,
            [new Coordinate(49.6, 6.1), new Coordinate(49.7, 6.2)]
        )
        {
            CurrentPosition = new Coordinate(49.6, 6.1),
            NextPosition = new Coordinate(49.7, 6.2),
        };

        var initialLayer = TrainSampleSimulation.BuildTrackedEntityLayer([train]);
        var initialTrackedEntity = TrackedEntityMaterializer
            .Materialize(
                initialLayer.Items,
                initialLayer.IdOptions,
                initialLayer.Visual.Symbol,
                initialLayer.Visual.Decorations
            )
            .Single();

        TrainSampleSimulation.Advance(train);

        // act
        var rebuiltLayer = TrainSampleSimulation.BuildTrackedEntityLayer([train]);
        var rebuiltTrackedEntity = TrackedEntityMaterializer
            .Materialize(
                rebuiltLayer.Items,
                rebuiltLayer.IdOptions,
                rebuiltLayer.Visual.Symbol,
                rebuiltLayer.Visual.Decorations
            )
            .Single();

        // assert
        initialTrackedEntity.Id.Should().Be(train.Id);
        rebuiltTrackedEntity.Id.Should().Be(train.Id);
        rebuiltTrackedEntity
            .Decorations.Select(decoration => decoration.Id)
            .Should()
            .Equal(initialTrackedEntity.Decorations.Select(decoration => decoration.Id));
        rebuiltTrackedEntity
            .Decorations.Select(decoration => $"{rebuiltTrackedEntity.Id}::{decoration.Id}")
            .Should()
            .Equal(
                initialTrackedEntity.Decorations.Select(decoration => $"{initialTrackedEntity.Id}::{decoration.Id}")
            );
        rebuiltTrackedEntity.Item.Should().BeSameAs(train);
        rebuiltTrackedEntity.Position.Should().NotBe(initialTrackedEntity.Position);
    }

    [Test]
    public void Should_generate_clean_train_icon_svg_without_stray_symbol_text()
    {
        // arrange
        const string color = "#2563eb";

        // act
        var svg = TrainSampleSimulation.BuildIconSvg(color);

        // assert
        svg.Should().Contain("<svg");
        svg.Should().MatchRegex($"fill=['\"]{System.Text.RegularExpressions.Regex.Escape(color)}['\"]");
        svg.Should().NotContain("<text");
        svg.Should().NotContain("?");
    }
}
