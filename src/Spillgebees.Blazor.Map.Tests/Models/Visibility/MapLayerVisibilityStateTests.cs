using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models.Visibility;

namespace Spillgebees.Blazor.Map.Tests.Models.Visibility;

public class MapLayerVisibilityStateTests
{
    [Test]
    public void Should_validate_group_ids()
    {
        var act = () => new MapLayerVisibilityGroup("", [MapLayerVisibilityTarget.Layer("layer")]);

        act.Should().Throw<ArgumentException>().WithMessage("*non-empty*");
    }

    [Test]
    public void Should_validate_duplicate_group_ids()
    {
        var act = () =>
            new MapLayerVisibilityState([
                new MapLayerVisibilityGroup("group", [MapLayerVisibilityTarget.Layer("layer-a")]),
                new MapLayerVisibilityGroup("group", [MapLayerVisibilityTarget.Layer("layer-b")]),
            ]);

        act.Should().Throw<ArgumentException>().WithMessage("*unique*group*");
    }

    [Test]
    public void Should_validate_targets()
    {
        var emptyLayers = () => MapLayerVisibilityTarget.Layer();
        var emptyStyle = () => MapLayerVisibilityTarget.Style("", "layer");
        var runtimeStyle = () =>
            new MapLayerVisibilityTarget(MapLayerVisibilityTargetKind.RuntimeLayer, ["layer"], "style");

        emptyLayers.Should().Throw<ArgumentException>().WithMessage("*at least one layer ID*");
        emptyStyle.Should().Throw<ArgumentException>().WithMessage("*non-empty*");
        runtimeStyle.Should().Throw<ArgumentException>().WithMessage("*must not declare a style ID*");
    }

    [Test]
    public void Should_snapshot_input_lists()
    {
        var layerIds = new List<string> { "layer-a" };
        var targets = new List<MapLayerVisibilityTarget> { new(MapLayerVisibilityTargetKind.RuntimeLayer, layerIds) };
        var group = new MapLayerVisibilityGroup("group", targets);
        var state = new MapLayerVisibilityState([group]);

        layerIds.Add("layer-b");
        targets.Add(MapLayerVisibilityTarget.Layer("layer-c"));

        state.Groups.Single().Targets.Should().HaveCount(1);
        state.Groups.Single().Targets.Single().LayerIds.Should().Equal("layer-a");
    }

    [Test]
    public void Should_raise_one_group_changed_event_when_visibility_changes()
    {
        var state = new MapLayerVisibilityState([
            new MapLayerVisibilityGroup("group", [MapLayerVisibilityTarget.Layer("layer")]),
        ]);
        var events = new List<MapLayerVisibilityChangedEventArgs>();
        state.Changed += (_, args) => events.Add(args);

        state.SetVisible("group", false);
        state.SetVisible("group", false);

        events.Should().ContainSingle();
        events[0].ChangeKind.Should().Be(MapLayerVisibilityChangeKind.GroupChanged);
        events[0].GroupId.Should().Be("group");
        events[0].IsVisible.Should().BeFalse();
    }

    [Test]
    public void Should_toggle_visibility()
    {
        var state = new MapLayerVisibilityState([
            new MapLayerVisibilityGroup("group", [MapLayerVisibilityTarget.Layer("layer")]),
        ]);

        state.Toggle("group");

        state.IsVisible("group").Should().BeFalse();
    }

    [Test]
    public void Should_raise_one_groups_replaced_event()
    {
        var state = new MapLayerVisibilityState([
            new MapLayerVisibilityGroup("old", [MapLayerVisibilityTarget.Layer("old-layer")]),
        ]);
        var events = new List<MapLayerVisibilityChangedEventArgs>();
        state.Changed += (_, args) => events.Add(args);

        state.Replace([new MapLayerVisibilityGroup("new", [MapLayerVisibilityTarget.Layer("new-layer")])]);

        events.Should().ContainSingle();
        events[0].ChangeKind.Should().Be(MapLayerVisibilityChangeKind.GroupsReplaced);
        events[0].GroupId.Should().BeNull();
        state.Contains("old").Should().BeFalse();
        state.Contains("new").Should().BeTrue();
    }
}
