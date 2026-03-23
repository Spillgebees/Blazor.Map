using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models.Expressions;

namespace Spillgebees.Blazor.Map.Tests.Models.Expressions;

public class FeatureStateKeyTests
{
    [Test]
    public void Should_create_bool_key_with_correct_name()
    {
        // arrange & act
        var key = FeatureState.Bool("hover");

        // assert
        key.Name.Should().Be("hover");
    }

    [Test]
    public void Should_create_number_key_with_correct_name()
    {
        // arrange & act
        var key = FeatureState.Number("opacity");

        // assert
        key.Name.Should().Be("opacity");
    }

    [Test]
    public void Should_create_string_key_with_correct_name()
    {
        // arrange & act
        var key = FeatureState.String("label");

        // assert
        key.Name.Should().Be("label");
    }

    [Test]
    public void Should_produce_key_value_pair_from_set()
    {
        // arrange
        var key = FeatureState.Bool("hover");

        // act
        var pair = key.Set(true);

        // assert
        pair.Key.Should().Be("hover");
        pair.Value.Should().Be(true);
    }

    [Test]
    public void Should_produce_key_value_pair_with_false_value()
    {
        // arrange
        var key = FeatureState.Bool("hover");

        // act
        var pair = key.Set(false);

        // assert
        pair.Key.Should().Be("hover");
        pair.Value.Should().Be(false);
    }

    [Test]
    public void Should_produce_key_value_pair_with_numeric_value()
    {
        // arrange
        var key = FeatureState.Number("opacity");

        // act
        var pair = key.Set(0.75);

        // assert
        pair.Key.Should().Be("opacity");
        pair.Value.Should().Be(0.75);
    }

    [Test]
    public void Should_produce_key_value_pair_with_string_value()
    {
        // arrange
        var key = FeatureState.String("label");

        // act
        var pair = key.Set("active");

        // assert
        pair.Key.Should().Be("label");
        pair.Value.Should().Be("active");
    }

    [Test]
    public void Should_produce_case_expression_from_when()
    {
        // arrange
        var key = FeatureState.Bool("hover");

        // act
        var expr = key.When(trueValue: 1.2, falseValue: 1.0);

        // assert
        expr.Should().HaveCount(4);
        expr[0].Should().Be("case");
        expr[2].Should().Be(1.2);
        expr[3].Should().Be(1.0);
    }

    [Test]
    public void Should_produce_boolean_feature_state_reader_in_when_condition()
    {
        // arrange
        var key = FeatureState.Bool("hover");

        // act
        var expr = key.When(trueValue: "#ff0000", falseValue: "#000000");

        // assert
        var condition = expr[1] as object[];
        condition.Should().NotBeNull();
        condition![0].Should().Be("boolean");

        var featureStateReader = condition[1] as object[];
        featureStateReader.Should().NotBeNull();
        featureStateReader![0].Should().Be("feature-state");
        featureStateReader[1].Should().Be("hover");

        condition[2].Should().Be(false);
    }

    [Test]
    public void Should_produce_when_expression_with_double_values()
    {
        // arrange
        var key = FeatureState.Bool("selected");

        // act
        var expr = key.When(trueValue: 2.0, falseValue: 1.0);

        // assert
        expr[0].Should().Be("case");
        expr[2].Should().Be(2.0);
        expr[3].Should().Be(1.0);

        var condition = expr[1] as object[];
        condition.Should().NotBeNull();
        var featureStateReader = condition![1] as object[];
        featureStateReader.Should().NotBeNull();
        featureStateReader![1].Should().Be("selected");
    }

    [Test]
    public void Should_produce_coalesce_expression_for_numeric_reads()
    {
        // arrange
        var key = FeatureState.Number("rotation");

        // act
        var expr = key.Read(0.0);

        // assert
        expr.Should().HaveCount(3);
        expr[0].Should().Be("coalesce");
        ((object[])expr[1])[0].Should().Be("feature-state");
        ((object[])expr[1])[1].Should().Be("rotation");
        expr[2].Should().Be(0.0);
    }
}
