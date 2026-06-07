using AwesomeAssertions;

namespace Spillgebees.Blazor.Map.Tests.Models.Display;

public sealed class MapDisplayStateTests
{
    [Test]
    public void Should_toggle_display_item_and_raise_changed_event()
    {
        // arrange
        var state = new MapDisplayState([
            new MapDisplayItem("transit", [MapDisplayTarget.StyleLayers("base", "rail")]),
        ]);
        MapDisplayChangedEventArgs? changed = null;
        state.Changed += (_, args) => changed = args;

        // act
        state.Toggle("transit");

        // assert
        state.IsOn("transit").Should().BeFalse();
        changed.Should().NotBeNull();
        changed!.ItemId.Should().Be("transit");
        changed.Item!.IsOn.Should().BeFalse();
    }

    [Test]
    public void Should_reject_feature_display_target_without_filter()
    {
        // arrange
        var act = () => new MapDisplayTarget(MapDisplayTargetKind.StyleLayerFeatures, StyleId: "base");

        // act
        var exception = act.Should().Throw<ArgumentException>();

        // assert
        exception.Which.ParamName.Should().Be("Filter");
    }

    [Test]
    public void Should_reject_duplicate_display_item_ids()
    {
        // arrange
        var item = new MapDisplayItem("roads", [MapDisplayTarget.RuntimeLayers("road-layer")]);
        var act = () => new MapDisplayState([item, item]);

        // act
        var exception = act.Should().Throw<ArgumentException>();

        // assert
        exception.Which.Message.Should().Contain("Duplicate ID: 'roads'");
    }

    [Test]
    public void Should_defensively_copy_display_targets()
    {
        // arrange
        var targets = new List<MapDisplayTarget> { MapDisplayTarget.RuntimeLayers("roads") };

        // act
        var item = new MapDisplayItem("roads", targets);
        targets.Clear();

        // assert
        item.Targets.Should().ContainSingle(target => target.LayerIds.Contains("roads"));
    }
}
