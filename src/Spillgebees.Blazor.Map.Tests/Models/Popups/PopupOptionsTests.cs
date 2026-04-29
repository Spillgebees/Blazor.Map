using System.Text.Json;
using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models.Popups;

namespace Spillgebees.Blazor.Map.Tests.Models.Popups;

public class PopupOptionsTests
{
    [Test]
    public void Should_create_text_popup_options()
    {
        // arrange & act
        var options = PopupOptions.FromText("<strong>safe</strong>");

        // assert
        options.Content.Should().Be("<strong>safe</strong>");
        options.ContentMode.Should().Be(PopupContentMode.Text);
    }

    [Test]
    public void Should_create_raw_html_popup_options()
    {
        // arrange & act
        var options = PopupOptions.FromRawHtml("<strong>raw</strong>");

        // assert
        options.Content.Should().Be("<strong>raw</strong>");
        options.ContentMode.Should().Be(PopupContentMode.RawHtml);
    }

    [Test]
    public void Should_serialize_text_content_mode_for_javascript()
    {
        // arrange
        var options = PopupOptions.FromText("<strong>safe</strong>");

        // act
        var json = JsonSerializer.Serialize(options, JsonSerializerOptions.Web);

        // assert
        json.Should().Contain("\"contentMode\":\"text\"");
    }

    [Test]
    public void Should_serialize_raw_html_content_mode_for_javascript()
    {
        // arrange
        var options = PopupOptions.FromRawHtml("<strong>raw</strong>");

        // act
        var json = JsonSerializer.Serialize(options, JsonSerializerOptions.Web);

        // assert
        json.Should().Contain("\"contentMode\":\"rawHtml\"");
    }

    [Test]
    public void Should_serialize_popup_trigger_with_attribute_converter()
    {
        // arrange
        var popup = new PopupOptions("stop", PopupContentMode.Text, Trigger: PopupTrigger.Hover);

        // act
        var json = JsonSerializer.Serialize(popup, JsonSerializerOptions.Web);

        // assert
        json.Should().Contain("\"trigger\":\"hover\"");
    }
}
