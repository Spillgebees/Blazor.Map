using AwesomeAssertions;

namespace Spillgebees.Blazor.Map.Tests.Models.Display;

public sealed class MapDisplayStateTests
{
    private static readonly object _mapA = new();
    private static readonly object _mapB = new();

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
    public void Should_leave_display_items_unset_by_default()
    {
        // arrange
        var item = new MapDisplayItem("roads", [MapDisplayTarget.Layers("road-layer")]);

        // act
        var state = new MapDisplayState([item]);

        // assert
        item.IsOn.Should().BeNull();
        state.IsOn("roads").Should().BeTrue("an unset item counts as on until a map reports its style default");
    }

    [Test]
    public void Should_follow_the_reported_style_default_for_unset_items()
    {
        // arrange
        var state = new MapDisplayState([
            new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")]),
            new MapDisplayItem("explicit", [MapDisplayTarget.StyleTags("railway", "tram")], IsOn: true),
        ]);

        // act
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = false, ["explicit"] = false });

        // assert
        state.IsOn("tram").Should().BeFalse();
        state.IsOn("tram", _mapA).Should().BeFalse();
        state.IsOn("explicit").Should().BeTrue();
    }

    [Test]
    public void Should_toggle_an_unset_item_from_its_style_default()
    {
        // arrange
        var state = new MapDisplayState([new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")])]);
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = false });

        // act
        state.Toggle("tram");

        // assert
        state.TryGetItem("tram", out var item).Should().BeTrue();
        item!.IsOn.Should().BeTrue();
    }

    [Test]
    public void Should_scope_unset_items_to_each_maps_style_and_aggregate_outside_maps()
    {
        // arrange
        var state = new MapDisplayState([new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")])]);

        // act
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = false });
        state.SetStyleDefaults(_mapB, new Dictionary<string, bool> { ["tram"] = true });

        // assert
        state.IsOn("tram", _mapA).Should().BeFalse();
        state.IsOn("tram", _mapB).Should().BeTrue();
        state.IsOn("tram").Should().BeTrue("outside any map an unset item is on when any map shows it");
    }

    [Test]
    public void Should_share_an_explicit_choice_across_maps()
    {
        // arrange
        var state = new MapDisplayState([new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")])]);
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = false });
        state.SetStyleDefaults(_mapB, new Dictionary<string, bool> { ["tram"] = true });

        // act
        state.SetOn("tram", false);

        // assert
        state.IsOn("tram", _mapA).Should().BeFalse();
        state.IsOn("tram", _mapB).Should().BeFalse();
        state.IsOn("tram").Should().BeFalse();
    }

    [Test]
    public void Should_ignore_maps_where_an_unset_item_resolves_to_nothing()
    {
        // arrange
        var state = new MapDisplayState([new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")])]);
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = false });

        // act: the minimap has no railway style, so it reports no default for the item
        state.SetStyleDefaults(_mapB, new Dictionary<string, bool>());

        // assert
        state.IsOn("tram").Should().BeFalse();
        state.IsOn("tram", _mapB).Should().BeFalse("a map without its own default falls back to the other maps");
    }

    [Test]
    public void Should_forget_a_maps_defaults_when_it_is_removed()
    {
        // arrange
        var state = new MapDisplayState([new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")])]);
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = false });
        state.SetStyleDefaults(_mapB, new Dictionary<string, bool> { ["tram"] = true });

        // act
        state.RemoveStyleDefaults(_mapB);

        // assert
        state.IsOn("tram").Should().BeFalse();
    }

    [Test]
    public void Should_report_unset_items_as_pending_until_the_map_reports()
    {
        // arrange
        var state = new MapDisplayState([
            new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")]),
            new MapDisplayItem("roads", [MapDisplayTarget.Layers("roads")], IsOn: true),
        ]);

        // act
        var pendingBefore = state.IsPending("tram", _mapA);
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = false });

        // assert
        pendingBefore.Should().BeTrue();
        state.IsPending("tram", _mapA).Should().BeFalse();
        state.IsPending("roads", _mapB).Should().BeFalse("explicit items never wait for style defaults");
    }

    [Test]
    public void Should_raise_changed_only_when_reported_defaults_change()
    {
        // arrange
        var state = new MapDisplayState([new MapDisplayItem("tram", [MapDisplayTarget.StyleTags("railway", "tram")])]);
        var changes = new List<MapDisplayChangedEventArgs>();
        state.Changed += (_, args) => changes.Add(args);

        // act
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = false });
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = false });
        state.SetStyleDefaults(_mapA, new Dictionary<string, bool> { ["tram"] = true });

        // assert
        changes.Should().HaveCount(2);
        changes.Should().AllSatisfy(args => args.ItemId.Should().BeNull());
    }

    [Test]
    public void Should_reject_duplicate_display_item_ids()
    {
        // arrange
        var item = new MapDisplayItem("roads", [MapDisplayTarget.Layers("road-layer")]);
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
        var targets = new List<MapDisplayTarget> { MapDisplayTarget.Layers("roads") };

        // act
        var item = new MapDisplayItem("roads", targets);
        targets.Clear();

        // assert
        item.Targets.Should().ContainSingle(target => target.Names.Contains("roads"));
    }

    [Test]
    public void Should_preserve_display_item_order_when_upserting_items()
    {
        // arrange
        var state = new MapDisplayState([
            new MapDisplayItem("first", [MapDisplayTarget.Layers("first-layer")]),
            new MapDisplayItem("second", [MapDisplayTarget.Layers("second-layer")]),
        ]);

        // act
        state.Upsert(new MapDisplayItem("first", [MapDisplayTarget.Layers("updated-first-layer")]));
        state.Upsert(new MapDisplayItem("third", [MapDisplayTarget.Layers("third-layer")]));

        // assert
        state.Items.Select(item => item.Id).Should().Equal("first", "second", "third");
    }

    [Test]
    public void Should_lazily_reuse_snapshot_between_display_item_changes()
    {
        // arrange
        var state = new MapDisplayState([new MapDisplayItem("roads", [MapDisplayTarget.Layers("roads")])]);

        // act
        var firstSnapshot = state.Items;
        var secondSnapshot = state.Items;
        state.Upsert(new MapDisplayItem("rail", [MapDisplayTarget.Layers("rail")]));
        var thirdSnapshot = state.Items;

        // assert
        secondSnapshot.Should().BeSameAs(firstSnapshot);
        thirdSnapshot.Should().NotBeSameAs(firstSnapshot);
        thirdSnapshot.Select(item => item.Id).Should().Equal("roads", "rail");
    }

    [Test]
    public void Should_validate_display_control_item_context_arguments()
    {
        // arrange
        var item = new MapDisplayItem("roads", [MapDisplayTarget.Layers("roads")]);

        // act
        var nullItem = () => new MapDisplayControlItemContext(null!, true, Callback);
        var nullCallback = () => new MapDisplayControlItemContext(item, true, null!);

        // assert
        nullItem.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("Item");
        nullCallback.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("SetOnAsync");
        return;

        static Task Callback(bool _) => Task.CompletedTask;
    }

    [Test]
    public void Should_expose_display_control_item_context_as_constructor_initialized_shape()
    {
        // arrange
        var item = new MapDisplayItem("roads", [MapDisplayTarget.Layers("roads")]);
        Func<bool, Task> callback = _ => Task.CompletedTask;

        // act
        var context = new MapDisplayControlItemContext(item, true, callback);
        var propertySetters = typeof(MapDisplayControlItemContext)
            .GetProperties()
            .Where(property =>
                property.Name
                    is nameof(MapDisplayControlItemContext.Item)
                        or nameof(MapDisplayControlItemContext.IsOn)
                        or nameof(MapDisplayControlItemContext.SetOnAsync)
            )
            .Select(property => property.SetMethod)
            .ToArray();

        // assert
        context.Item.Should().BeSameAs(item);
        context.IsOn.Should().BeTrue();
        context.SetOnAsync.Should().BeSameAs(callback);
        propertySetters.Should().AllSatisfy(setter => setter.Should().BeNull());
    }
}
