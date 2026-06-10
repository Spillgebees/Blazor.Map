using AwesomeAssertions;
using Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

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
