using AwesomeAssertions;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map.Tests.Engine;

public class EntityDifferTests
{
    private static EntityInput Input(
        string id,
        double lng = 0,
        double lat = 0,
        float rotation = 0,
        float sortKey = 0,
        int structuralHash = 1
    ) => new(id, lng, lat, rotation, sortKey, structuralHash);

    [Test]
    public void Should_report_new_entities_as_upserts_and_bump_the_epoch()
    {
        // arrange
        var differ = new EntityDiffer();

        // act
        var result = differ.Diff([Input("a"), Input("b")]);

        // assert
        result.Epoch.Should().Be(1u);
        result.HasStructuralChanges.Should().BeTrue();
        result.UpsertInputPositions.Should().Equal(0, 1);
        result.Moved.Should().BeEmpty();
        result.RemovedIndices.Should().BeEmpty();
        differ.IndexOf("a").Should().Be(0u);
        differ.IndexOf("b").Should().Be(1u);
    }

    [Test]
    public void Should_report_position_changes_as_motion_without_bumping_the_epoch()
    {
        // arrange
        var differ = new EntityDiffer();
        differ.Diff([Input("a"), Input("b")]);

        // act
        var result = differ.Diff([Input("a", lng: 6.13, lat: 49.61, rotation: 90), Input("b")]);

        // assert
        result.Epoch.Should().Be(1u);
        result.HasStructuralChanges.Should().BeFalse();
        result.UpsertInputPositions.Should().BeEmpty();
        result.Moved.Should().Equal(new EntityMotionRecord(0, 6.13, 49.61, 90f, 0f));
    }

    [Test]
    public void Should_report_nothing_when_nothing_changed()
    {
        // arrange
        var differ = new EntityDiffer();
        differ.Diff([Input("a"), Input("b")]);

        // act
        var result = differ.Diff([Input("a"), Input("b")]);

        // assert
        result.HasStructuralChanges.Should().BeFalse();
        result.UpsertInputPositions.Should().BeEmpty();
        result.Moved.Should().BeEmpty();
        result.RemovedIndices.Should().BeEmpty();
    }

    [Test]
    public void Should_report_structural_hash_changes_as_upserts()
    {
        // arrange
        var differ = new EntityDiffer();
        differ.Diff([Input("a")]);

        // act
        var result = differ.Diff([Input("a", lng: 5, structuralHash: 2)]);

        // assert
        result.Epoch.Should().Be(2u);
        result.UpsertInputPositions.Should().Equal(0);
        result.Moved.Should().BeEmpty("the upsert already carries the new position");
    }

    [Test]
    public void Should_report_missing_entities_as_removes_and_recycle_their_indices()
    {
        // arrange
        var differ = new EntityDiffer();
        differ.Diff([Input("a"), Input("b"), Input("c")]);

        // act
        var removeResult = differ.Diff([Input("a"), Input("c")]);
        var removedIndices = removeResult.RemovedIndices.ToArray();
        var reuseResult = differ.Diff([Input("a"), Input("c"), Input("d")]);

        // assert
        removedIndices.Should().Equal(1u);
        reuseResult.UpsertInputPositions.Should().Equal(2);
        differ.IndexOf("d").Should().Be(1u, "removed indices are recycled");
    }

    [Test]
    public void Should_keep_the_epoch_stable_across_motion_only_updates()
    {
        // arrange
        var differ = new EntityDiffer();
        differ.Diff([Input("a")]);

        // act
        differ.Diff([Input("a", lng: 1)]);
        differ.Diff([Input("a", lng: 2)]);
        var result = differ.Diff([Input("a", lng: 3)]);

        // assert
        result.Epoch.Should().Be(1u);
    }

    [Test]
    public void Should_throw_on_duplicate_entity_ids()
    {
        // arrange
        var differ = new EntityDiffer();
        differ.Diff([Input("a")]);

        // act
        var act = () => differ.Diff([Input("a"), Input("a", lng: 1)]);

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate*'a'*");
    }

    [Test]
    public void Should_handle_full_membership_replacement()
    {
        // arrange
        var differ = new EntityDiffer();
        differ.Diff([Input("a"), Input("b")]);

        // act
        var result = differ.Diff([Input("c"), Input("d")]);

        // assert
        result.UpsertInputPositions.Should().Equal(0, 1);
        result.RemovedIndices.Should().BeEquivalentTo([0u, 1u]);
        result.Epoch.Should().Be(2u);
    }
}
