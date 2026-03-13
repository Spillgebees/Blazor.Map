using System.Collections.Immutable;
using AwesomeAssertions;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Layers;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Tests.Utilities;

public class LayerDifferTests
{
    private static readonly Func<Marker, string> _idSelector = m => m.Id;

    private static readonly Marker _markerA = new("a", new Coordinate(49.6, 6.1), "A");
    private static readonly Marker _markerB = new("b", new Coordinate(49.7, 6.2), "B");
    private static readonly Marker _markerC = new("c", new Coordinate(49.8, 6.3), "C");
    private static readonly Marker _markerD = new("d", new Coordinate(49.9, 6.4), "D");

    private static readonly Marker _markerAUpdated = _markerA with { Coordinate = new Coordinate(50.0, 7.0) };

    [Test]
    public void Should_return_no_changes_when_lists_are_reference_equal()
    {
        // arrange
        var layers = new List<Marker> { _markerA, _markerB };

        // act
        var result = LayerDiffer.Diff(layers, layers, _idSelector);

        // assert
        result.Added.Should().BeEmpty();
        result.Removed.Should().BeEmpty();
        result.Updated.Should().BeEmpty();
    }

    [Test]
    public void Should_return_no_changes_when_both_lists_are_empty()
    {
        // arrange
        var oldLayers = new List<Marker>();
        var newLayers = new List<Marker>();

        // act
        var result = LayerDiffer.Diff(oldLayers, newLayers, _idSelector);

        // assert
        result.Added.Should().BeEmpty();
        result.Removed.Should().BeEmpty();
        result.Updated.Should().BeEmpty();
    }

    [Test]
    public void Should_return_all_added_when_old_is_empty()
    {
        // arrange
        var oldLayers = new List<Marker>();
        var newLayers = new List<Marker> { _markerA, _markerB, _markerC };

        // act
        var result = LayerDiffer.Diff(oldLayers, newLayers, _idSelector);

        // assert
        result.Added.Should().BeEquivalentTo([_markerA, _markerB, _markerC]);
        result.Removed.Should().BeEmpty();
        result.Updated.Should().BeEmpty();
    }

    [Test]
    public void Should_return_all_removed_when_new_is_empty()
    {
        // arrange
        var oldLayers = new List<Marker> { _markerA, _markerB, _markerC };
        var newLayers = new List<Marker>();

        // act
        var result = LayerDiffer.Diff(oldLayers, newLayers, _idSelector);

        // assert
        result.Added.Should().BeEmpty();
        result.Removed.Should().BeEquivalentTo(["a", "b", "c"]);
        result.Updated.Should().BeEmpty();
    }

    [Test]
    public void Should_detect_added_layers()
    {
        // arrange
        var oldLayers = new List<Marker> { _markerA, _markerB };
        var newLayers = new List<Marker> { _markerA, _markerB, _markerC };

        // act
        var result = LayerDiffer.Diff(oldLayers, newLayers, _idSelector);

        // assert
        result.Added.Should().BeEquivalentTo([_markerC]);
        result.Removed.Should().BeEmpty();
        result.Updated.Should().BeEmpty();
    }

    [Test]
    public void Should_detect_removed_layers()
    {
        // arrange
        var oldLayers = new List<Marker> { _markerA, _markerB, _markerC };
        var newLayers = new List<Marker> { _markerA, _markerB };

        // act
        var result = LayerDiffer.Diff(oldLayers, newLayers, _idSelector);

        // assert
        result.Added.Should().BeEmpty();
        result.Removed.Should().BeEquivalentTo(["c"]);
        result.Updated.Should().BeEmpty();
    }

    [Test]
    public void Should_detect_updated_layers()
    {
        // arrange
        var oldLayers = new List<Marker> { _markerA };
        var newLayers = new List<Marker> { _markerAUpdated };

        // act
        var result = LayerDiffer.Diff(oldLayers, newLayers, _idSelector);

        // assert
        result.Added.Should().BeEmpty();
        result.Removed.Should().BeEmpty();
        result.Updated.Should().BeEquivalentTo([_markerAUpdated]);
    }

    [Test]
    public void Should_detect_no_changes_when_values_are_equal()
    {
        // arrange
        var oldLayers = new List<Marker> { _markerA };
        var markerACopy = new Marker("a", new Coordinate(49.6, 6.1), "A");
        var newLayers = new List<Marker> { markerACopy };

        // act
        var result = LayerDiffer.Diff(oldLayers, newLayers, _idSelector);

        // assert
        result.Added.Should().BeEmpty();
        result.Removed.Should().BeEmpty();
        result.Updated.Should().BeEmpty();
    }

    [Test]
    public void Should_handle_mixed_add_remove_update()
    {
        // arrange
        var oldLayers = new List<Marker> { _markerA, _markerB, _markerC };
        var newLayers = new List<Marker> { _markerAUpdated, _markerD };

        // act
        var result = LayerDiffer.Diff(oldLayers, newLayers, _idSelector);

        // assert
        result.Added.Should().BeEquivalentTo([_markerD]);
        result.Removed.Should().BeEquivalentTo(["b", "c"]);
        result.Updated.Should().BeEquivalentTo([_markerAUpdated]);
    }

    [Test]
    public void Should_return_has_changes_false_when_no_diff()
    {
        // arrange
        var layers = new List<Marker> { _markerA, _markerB };
        var layersCopy = new List<Marker>
        {
            new("a", new Coordinate(49.6, 6.1), "A"),
            new("b", new Coordinate(49.7, 6.2), "B"),
        };

        // act
        var result = LayerDiffer.Diff(layers, layersCopy, _idSelector);

        // assert
        result.HasChanges.Should().BeFalse();
    }

    [Test]
    public void Should_return_has_changes_true_when_diff_exists()
    {
        // arrange
        var oldLayers = new List<Marker> { _markerA };
        var newLayers = new List<Marker> { _markerA, _markerB };

        // act
        var result = LayerDiffer.Diff(oldLayers, newLayers, _idSelector);

        // assert
        result.HasChanges.Should().BeTrue();
    }
}
