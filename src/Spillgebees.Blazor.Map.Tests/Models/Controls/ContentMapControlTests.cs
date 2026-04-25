using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models.Controls;

namespace Spillgebees.Blazor.Map.Tests.Models.Controls;

public class ContentMapControlTests
{
    [Test]
    public void Should_set_kind_to_content_literal()
    {
        // arrange

        // act
        var control = new ContentMapControl("custom-control");

        // assert
        control.Kind.Should().Be("content");
    }
}
