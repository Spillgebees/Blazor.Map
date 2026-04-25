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

    private sealed class TestBaseMap : BaseMap
    {
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
    }
}
