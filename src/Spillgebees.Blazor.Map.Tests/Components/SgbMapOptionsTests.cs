using System.Text.Json;
using AwesomeAssertions;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class SgbMapOptionsTests : BunitContext
{
    private const string CreateMapIdentifier = "Spillgebees.Engine.createMap";

    public SgbMapOptionsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public void Should_forward_cooperative_gestures_when_creating_the_map(bool cooperativeGestures)
    {
        // arrange
        var cut = Render<SgbMap>(parameters =>
            parameters.Add(component => component.CooperativeGestures, cooperativeGestures)
        );

        // act
        cut.WaitForAssertion(() =>
        {
            JSInterop.Invocations[CreateMapIdentifier].Should().ContainSingle();
        });
        var invocation = JSInterop.Invocations[CreateMapIdentifier][0];
        var optionsJson = invocation.Arguments[1].Should().BeOfType<string>().Subject;

        // assert
        using var options = JsonDocument.Parse(optionsJson);
        options.RootElement.GetProperty("cooperativeGestures").GetBoolean().Should().Be(cooperativeGestures);
    }
}
