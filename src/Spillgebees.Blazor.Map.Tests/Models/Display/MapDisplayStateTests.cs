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

    [Test]
    public void Should_preserve_display_item_order_when_upserting_items()
    {
        // arrange
        var state = new MapDisplayState([
            new MapDisplayItem("first", [MapDisplayTarget.RuntimeLayers("first-layer")]),
            new MapDisplayItem("second", [MapDisplayTarget.RuntimeLayers("second-layer")]),
        ]);

        // act
        state.Upsert(new MapDisplayItem("first", [MapDisplayTarget.RuntimeLayers("updated-first-layer")]));
        state.Upsert(new MapDisplayItem("third", [MapDisplayTarget.RuntimeLayers("third-layer")]));

        // assert
        state.Items.Select(item => item.Id).Should().Equal("first", "second", "third");
    }

    [Test]
    public void Should_lazily_reuse_snapshot_between_display_item_changes()
    {
        // arrange
        var state = new MapDisplayState([new MapDisplayItem("roads", [MapDisplayTarget.RuntimeLayers("roads")])]);

        // act
        var firstSnapshot = state.Items;
        var secondSnapshot = state.Items;
        state.Upsert(new MapDisplayItem("rail", [MapDisplayTarget.RuntimeLayers("rail")]));
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
        var item = new MapDisplayItem("roads", [MapDisplayTarget.RuntimeLayers("roads")]);
        Func<bool, Task> callback = _ => Task.CompletedTask;

        // act
        var nullItem = () => new MapDisplayControlItemContext(null!, true, callback);
        var nullCallback = () => new MapDisplayControlItemContext(item, true, null!);

        // assert
        nullItem.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("Item");
        nullCallback.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("SetOnAsync");
    }

    [Test]
    public void Should_expose_display_control_item_context_as_constructor_initialized_shape()
    {
        // arrange
        var item = new MapDisplayItem("roads", [MapDisplayTarget.RuntimeLayers("roads")]);
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

    [Test]
    public void Should_create_style_layer_tag_targets_with_static_factories()
    {
        // arrange
        var expectedTags = new[] { "transit", "roads" };

        // act
        var single = MapDisplayTarget.StyleLayerTag("base", "transit");
        var multiple = MapDisplayTarget.StyleLayerTags("base", expectedTags);

        // assert
        single.Kind.Should().Be(MapDisplayTargetKind.StyleLayerTag);
        single.StyleId.Should().Be("base");
        single.Tags.Should().Equal("transit");
        multiple.Tags.Should().Equal(expectedTags);
    }
}
