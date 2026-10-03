using AwesomeAssertions;

namespace Spillgebees.Blazor.Map.Tests.Models.Display;

public sealed class MapDisplayTargetTests
{
    [Test]
    public void Should_create_targets_with_static_factories()
    {
        // act
        var layers = MapDisplayTarget.Layers("trains", "buildings");
        var styleLayers = MapDisplayTarget.StyleLayers("railway", "tram-line-fill");
        var styleTags = MapDisplayTarget.StyleTags("railway", "tram", "metro");
        var style = MapDisplayTarget.Style("railway");

        // assert
        layers.Kind.Should().Be(MapDisplayTargetKind.Layers);
        layers.Names.Should().Equal("trains", "buildings");
        layers.StyleId.Should().BeNull();
        styleLayers.Kind.Should().Be(MapDisplayTargetKind.StyleLayers);
        styleLayers.StyleId.Should().Be("railway");
        styleLayers.Names.Should().Equal("tram-line-fill");
        styleTags.Kind.Should().Be(MapDisplayTargetKind.StyleTags);
        styleTags.Names.Should().Equal("tram", "metro");
        style.Kind.Should().Be(MapDisplayTargetKind.Style);
        style.Names.Should().BeEmpty();
    }

    [Test]
    public void Should_narrow_a_target_to_features_without_changing_it()
    {
        // arrange
        var target = MapDisplayTarget.StyleLayers("railway", "tracks");
        object[] filter = ["==", new object[] { "get", "status" }, "planned"];

        // act
        var narrowed = target.Where(filter);

        // assert
        narrowed.Filter.Should().BeSameAs(filter);
        narrowed.Names.Should().Equal("tracks");
        target.Filter.Should().BeNull();
    }

    [Test]
    public void Should_require_at_least_one_name()
    {
        // arrange
        var noLayers = () => MapDisplayTarget.Layers();
        var noStyleLayers = () => MapDisplayTarget.StyleLayers("railway");
        var noTags = () => MapDisplayTarget.StyleTags("railway");

        // act + assert
        noLayers.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("ids");
        noStyleLayers.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("layerIds");
        noTags.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("tags");
    }

    [Test]
    public void Should_reject_blank_names_and_style_ids()
    {
        // arrange
        var blankLayer = () => MapDisplayTarget.Layers("trains", " ");
        var blankStyle = () => MapDisplayTarget.Style(" ");

        // act + assert
        blankLayer.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("ids");
        blankStyle.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("styleId");
    }

    [Test]
    public void Should_reject_a_null_feature_filter()
    {
        // arrange
        var act = () => MapDisplayTarget.Layers("trains").Where(null!);

        // act + assert
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("filter");
    }
}
