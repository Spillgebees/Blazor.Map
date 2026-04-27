using AwesomeAssertions;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Legends;

namespace Spillgebees.Blazor.Map.Tests;

public class BaseMapControlsTests
{
    [Test]
    public void Should_return_true_and_control_when_try_get_control_finds_match()
    {
        // arrange
        var map = new TestBaseMap();
        var control = new LegendMapControl(
            "legend-main",
            new MapControlPlacement(ControlPosition.TopRight, 500, Enabled: true),
            new LegendChromeOptions("Legend", Collapsible: true, InitiallyOpen: true, ClassName: null),
            new LegendContentOptions(new MapLegendDefinition([]), ItemTemplate: null, OnItemVisibilityChanged: default)
        );
        map.SetInternalControls([control]);

        // act
        var found = map.TryResolveControl("legend-main", out var resolvedControl);

        // assert
        found.Should().BeTrue();
        resolvedControl.Should().NotBeNull();
        resolvedControl.Should().BeSameAs(control);
    }

    [Test]
    public void Should_return_false_and_null_when_try_get_control_does_not_find_match()
    {
        // arrange
        var map = new TestBaseMap();
        map.SetInternalControls([
            new LegendMapControl(
                "legend-main",
                new MapControlPlacement(ControlPosition.TopRight, 500, Enabled: true),
                new LegendChromeOptions("Legend", Collapsible: true, InitiallyOpen: true, ClassName: null),
                new LegendContentOptions(
                    new MapLegendDefinition([]),
                    ItemTemplate: null,
                    OnItemVisibilityChanged: default
                )
            ),
        ]);

        // act
        var found = map.TryResolveControl("missing", out var resolvedControl);

        // assert
        found.Should().BeFalse();
        resolvedControl.Should().BeNull();
    }

    [Test]
    public void Should_preserve_declaration_order_when_custom_control_id_changes()
    {
        // arrange
        var map = new TestBaseMap();
        var firstOwnerId = "first-owner";
        var secondOwnerId = "second-owner";
        map.RegisterTestCustomControl(firstOwnerId, CreateContentControl("first"));
        map.RegisterTestCustomControl(secondOwnerId, CreateContentControl("second"));

        // act
        map.RegisterTestCustomControl(firstOwnerId, CreateContentControl("renamed-first"));

        // assert
        map.GetDesiredControlIds().Should().Equal("renamed-first", "second");
    }

    [Test]
    public void Should_append_control_when_removed_and_readded_with_new_declaration_owner()
    {
        // arrange
        var map = new TestBaseMap();
        map.RegisterTestCustomControl("first-owner", CreateContentControl("first"));
        map.RegisterTestCustomControl("second-owner", CreateContentControl("second"));

        // act
        map.UnregisterTestCustomControlByOwner("first-owner");
        map.RegisterTestCustomControl("third-owner", CreateContentControl("first"));

        // assert
        map.GetDesiredControlIds().Should().Equal("second", "first");
    }

    [Test]
    public void Should_keep_declaration_slot_when_disabled_control_is_reenabled()
    {
        // arrange
        var map = new TestBaseMap();
        var firstOwnerId = "first-owner";
        map.RegisterTestCustomControl(firstOwnerId, CreateContentControl("first"));
        map.RegisterTestCustomControl("second-owner", CreateContentControl("second"));

        // act
        map.RegisterTestCustomControl(firstOwnerId, CreateContentControl("first", enabled: false));
        map.RegisterTestCustomControl(firstOwnerId, CreateContentControl("first", enabled: true));

        // assert
        map.GetDesiredControlIds().Should().Equal("first", "second");
    }

    [Test]
    public void Should_remove_custom_control_when_owner_is_disposed()
    {
        // arrange
        var map = new TestBaseMap();
        map.RegisterTestCustomControl("first-owner", CreateContentControl("first"));
        map.RegisterTestCustomControl("second-owner", CreateContentControl("second"));

        // act
        map.UnregisterTestCustomControlByOwner("first-owner");

        // assert
        map.GetDesiredControlIds().Should().Equal("second");
    }

    private static ContentMapControl CreateContentControl(string controlId, bool enabled = true) =>
        new(controlId, enabled, ControlPosition.TopRight, 500);

    private sealed class TestBaseMap : BaseMap
    {
        public TestBaseMap()
        {
            Controls = [];
        }

        public void SetInternalControls(IReadOnlyList<MapControl> controls)
        {
            // arrange

            // act
            InternalControls = [.. controls];

            // assert
        }

        public bool TryResolveControl(string controlId, out MapControl? control)
        {
            // arrange

            // act
            var found = TryGetControl(controlId, out control);

            // assert
            return found;
        }

        public void RegisterTestCustomControl(string ownerId, ContentMapControl control)
        {
            // arrange

            // act
            base.RegisterControl(ownerId, control);

            // assert
        }

        public void UnregisterTestCustomControlByOwner(string ownerId)
        {
            // arrange

            // act
            base.UnregisterControlByOwner(ownerId);

            // assert
        }

        public IReadOnlyList<string> GetDesiredControlIds()
        {
            // arrange

            // act
            var controls = GetDesiredControls();

            // assert
            return [.. controls.Select(control => control.ControlId)];
        }
    }
}
