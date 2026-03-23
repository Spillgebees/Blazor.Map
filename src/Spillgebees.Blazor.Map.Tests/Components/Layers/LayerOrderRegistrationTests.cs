using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components.Layers;

namespace Spillgebees.Blazor.Map.Tests.Components.Layers;

public class LayerOrderRegistrationTests
{
    [Test]
    public void Should_inherit_source_stack_metadata_when_layer_does_not_override_it()
    {
        // arrange
        var sourceOrder = new MapLayerOrderOptions("stations", null, null);
        var layerOrder = new MapLayerOrderOptions(null, null, "trains");

        // act
        var registration = LayerOrderRegistration.Create(layerOrder, sourceOrder, declarationOrder: 7);

        // assert
        registration.DeclarationOrder.Should().Be(7);
        registration.Stack.Should().Be("stations");
        registration.BeforeStack.Should().BeNull();
        registration.AfterStack.Should().Be("trains");
    }

    [Test]
    public void Should_prefer_explicit_layer_stack_metadata_over_inherited_source_values()
    {
        // arrange
        var sourceOrder = new MapLayerOrderOptions("stations", "base", null);
        var layerOrder = new MapLayerOrderOptions("labels", null, "trains");

        // act
        var registration = LayerOrderRegistration.Create(layerOrder, sourceOrder, declarationOrder: 11);

        // assert
        registration.Stack.Should().Be("labels");
        registration.BeforeStack.Should().Be("base");
        registration.AfterStack.Should().Be("trains");
    }
}
