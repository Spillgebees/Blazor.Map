using AwesomeAssertions;
using Spillgebees.Blazor.Map;

namespace Spillgebees.Blazor.Map.Tests.Models.Clustering;

public class ClusterOptionsTests
{
    [Test]
    public void Should_provide_disabled_none_preset()
    {
        // arrange
        var options = ClusterOptions.None;

        // act
        var enabled = options.Enabled;

        // assert
        enabled.Should().BeFalse();
        options.Radius.Should().Be(ClusterOptions.DefaultRadius);
        options.MaxZoom.Should().BeNull();
        options.MinPoints.Should().BeNull();
        options.Properties.Should().BeNull();
    }

    [Test]
    public void Should_provide_enabled_default_preset()
    {
        // arrange
        var options = ClusterOptions.Default;

        // act
        var enabled = options.Enabled;

        // assert
        enabled.Should().BeTrue();
        options.Radius.Should().Be(ClusterOptions.DefaultRadius);
        options.MaxZoom.Should().BeNull();
        options.MinPoints.Should().BeNull();
        options.Properties.Should().BeNull();
    }

    [Test]
    public void Should_create_enabled_options_with_source_semantics()
    {
        // arrange
        var properties = new Dictionary<string, object>
        {
            ["total"] = new object[] { "+", new object[] { "get", "count" } },
        };

        // act
        var options = ClusterOptions.Create(radius: 64, maxZoom: 12, minPoints: 3, properties: properties);

        // assert
        options.Enabled.Should().BeTrue();
        options.Radius.Should().Be(64);
        options.MaxZoom.Should().Be(12);
        options.MinPoints.Should().Be(3);
        options.Properties.Should().BeSameAs(properties);
    }

    [Test]
    public void Should_reject_invalid_source_options()
    {
        // arrange
        var act = () => ClusterOptions.Create(radius: 0);

        // act
        var assertion = act.Should().Throw<ArgumentOutOfRangeException>();

        // assert
        assertion.Which.ParamName.Should().Be("radius");
    }

    [Test]
    public void Should_reject_negative_max_zoom()
    {
        // arrange
        var act = () => ClusterOptions.Create(maxZoom: -1);

        // act
        var assertion = act.Should().Throw<ArgumentOutOfRangeException>();

        // assert
        assertion.Which.ParamName.Should().Be("maxZoom");
    }

    [Test]
    public void Should_reject_non_positive_min_points()
    {
        // arrange
        var invalidMinPoints = new[] { 0, -1 };

        // act
        var assertions = invalidMinPoints.Select(minPoints =>
            new Action(() => ClusterOptions.Create(minPoints: minPoints)).Should().Throw<ArgumentOutOfRangeException>()
        );

        // assert
        assertions.Should().AllSatisfy(assertion => assertion.Which.ParamName.Should().Be("minPoints"));
    }
}
