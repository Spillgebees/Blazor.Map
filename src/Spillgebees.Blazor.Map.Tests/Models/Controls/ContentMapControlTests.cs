using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Tests.Models.Controls;

public class ContentControlDefinitionTests
{
    [Test]
    public void Should_set_kind_to_content_literal()
    {
        // arrange

        // act
        var control = new ContentControlDefinition("custom-control");

        // assert
        control.Kind.Should().Be("content");
    }
}
