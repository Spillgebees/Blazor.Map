using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class LegendMapControlTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";
    private const string SetControlContentIdentifier = "Spillgebees.Map.mapFunctions.setControlContent";
    private const string RemoveControlContentIdentifier = "Spillgebees.Map.mapFunctions.removeControlContent";

    public LegendMapControlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
        JSInterop.SetupVoid(SetControlContentIdentifier);
        JSInterop.SetupVoid(RemoveControlContentIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_initial_display_without_legend(CancellationToken cancellationToken)
    {
        var display = CreateDisplay();
        var cut = Render<SgbMap>(parameters => parameters.Add(map => map.Display, display));

        await cut.Instance.OnMapInitializedAsync();

        cut.WaitForAssertion(() =>
            GetSceneMutations()
                .Should()
                .Contain(mutation =>
                    mutation.Kind == "setVisibilityGroup"
                    && mutation.GroupId!.Contains("stations")
                    && mutation.GroupVisible == false
                )
        );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_update_changed_display_item(CancellationToken cancellationToken)
    {
        var display = CreateDisplay();
        var cut = Render<SgbMap>(parameters => parameters.Add(map => map.Display, display));
        await cut.Instance.OnMapInitializedAsync();
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        display.SetOn("stations", true);

        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount)
        );
        GetLatestSceneMutationBatch()
            .Mutations.Should()
            .ContainSingle(mutation =>
                mutation.Kind == "setVisibilityGroup"
                && mutation.GroupId!.Contains("stations")
                && mutation.GroupVisible == true
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_unregister_removed_groups_when_state_is_replaced(CancellationToken cancellationToken)
    {
        var display = CreateDisplay();
        var cut = Render<SgbMap>(parameters => parameters.Add(map => map.Display, display));
        await cut.Instance.OnMapInitializedAsync();
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        display.Replace([new MapDisplayItem("routes", [MapDisplayTarget.RuntimeLayers("routes-layer")])]);

        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount)
        );
        GetSceneMutations()
            .Should()
            .Contain(mutation => mutation.Kind == "removeVisibilityGroup" && mutation.GroupId!.Contains("stations"));
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_namespace_display_groups(CancellationToken cancellationToken)
    {
        // arrange
        var display = new MapDisplayState([
            new MapDisplayItem("stations", [MapDisplayTarget.RuntimeLayers("display-stations-layer")]),
        ]);
        var cut = Render<SgbMap>(parameters => parameters.Add(map => map.Display, display));

        // act
        await cut.Instance.OnMapInitializedAsync();

        // assert
        GetSceneMutations()
            .Where(mutation => mutation.Kind == "setVisibilityGroup")
            .Select(mutation => mutation.GroupId)
            .Should()
            .ContainSingle(groupId => groupId != "stations" && groupId!.Contains("stations"));
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_toggle_shared_display_from_legend(CancellationToken cancellationToken)
    {
        var display = CreateDisplay();
        var definition = CreateLegend();
        var cut = Render<SgbMap>(parameters =>
            parameters
                .Add(map => map.Display, display)
                .AddChildContent<MapControls>(controls =>
                    controls.AddChildContent<LegendMapControl>(control => control.Add(c => c.Definition, definition))
                )
        );
        await cut.Instance.OnMapInitializedAsync();

        await cut.Find("input[data-testid='map-legend-toggle-stations']").ChangeAsync(true);

        display.IsOn("stations").Should().BeTrue();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_render_static_legend_items_without_toggle(CancellationToken cancellationToken)
    {
        var definition = new MapLegend([new MapLegendSection("Static", [new MapLegendItem("static", "Static item")])]);
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<LegendMapControl>(control => control.Add(c => c.Definition, definition))
            )
        );

        await cut.Instance.OnMapInitializedAsync();

        cut.Markup.Should().Contain("Static item");
        cut.FindAll("input[data-testid='map-legend-toggle-static']").Should().BeEmpty();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_sanitize_icon_symbol_css_class(CancellationToken cancellationToken)
    {
        var definition = new MapLegend([
            new MapLegendSection(
                "Static",
                [new MapLegendItem("icon", "Icon item", Symbol: MapLegendSymbol.Icon("valid_class invalid<script>"))]
            ),
        ]);
        var cut = Render<SgbMap>(parameters =>
            parameters.AddChildContent<MapControls>(controls =>
                controls.AddChildContent<LegendMapControl>(control => control.Add(c => c.Definition, definition))
            )
        );

        await cut.Instance.OnMapInitializedAsync();

        var symbol = cut.Find(".sgb-map-legend-symbol[data-symbol='icon']");
        symbol.GetAttribute("class").Should().Be("sgb-map-legend-symbol valid_class invalidscript");
    }

    [Test]
    public void Should_keep_class_name_as_fifth_positional_legend_item_argument()
    {
        var item = new MapLegendItem("item", "Item", null, null, "item-class");

        item.ClassName.Should().Be("item-class");
        item.Symbol.Should().BeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_keep_two_legends_in_sync(CancellationToken cancellationToken)
    {
        var display = CreateDisplay();
        var definition = CreateLegend();
        var cut = Render<SgbMap>(parameters =>
            parameters
                .Add(map => map.Display, display)
                .AddChildContent<MapControls>(controls =>
                    controls.AddChildContent(builder =>
                    {
                        builder.OpenComponent<LegendMapControl>(0);
                        builder.AddAttribute(1, nameof(LegendMapControl.Id), "legend-a");
                        builder.AddAttribute(2, nameof(LegendMapControl.Definition), definition);
                        builder.CloseComponent();
                        builder.OpenComponent<LegendMapControl>(3);
                        builder.AddAttribute(4, nameof(LegendMapControl.Id), "legend-b");
                        builder.AddAttribute(5, nameof(LegendMapControl.Definition), definition);
                        builder.CloseComponent();
                    })
                )
        );
        await cut.Instance.OnMapInitializedAsync();

        display.SetOn("stations", true);

        cut.WaitForAssertion(() =>
            cut.FindAll("input[data-testid='map-legend-toggle-stations']")
                .Should()
                .AllSatisfy(input => input.HasAttribute("checked").Should().BeTrue())
        );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_pass_display_context_to_templates(CancellationToken cancellationToken)
    {
        var display = CreateDisplay();
        MapLegendItemTemplateContext? templateContext = null;
        RenderFragment<MapLegendItemTemplateContext> template = context =>
        {
            templateContext = context;
            return builder => builder.AddContent(0, context.Item.Label);
        };

        var cut = Render<SgbMap>(parameters =>
            parameters
                .Add(map => map.Display, display)
                .AddChildContent<MapControls>(controls =>
                    controls.AddChildContent<LegendMapControl>(control =>
                        control.Add(c => c.Definition, CreateLegend()).Add(c => c.ItemTemplate, template)
                    )
                )
        );

        await cut.Instance.OnMapInitializedAsync();

        templateContext.Should().NotBeNull();
        templateContext!.IsToggleable.Should().BeTrue();
        templateContext.IsOn.Should().BeFalse();
        templateContext.DisplayItem!.Id.Should().Be("stations");
    }

    [Test, Timeout(TestTimeoutMs)]
    public void Should_fail_when_toggleable_legend_item_references_missing_display_item(
        CancellationToken cancellationToken
    )
    {
        var definition = CreateLegend();
        var act = () =>
            Render<SgbMap>(parameters =>
                parameters.AddChildContent<MapControls>(controls =>
                    controls.AddChildContent<LegendMapControl>(control => control.Add(c => c.Definition, definition))
                )
            );

        act.Should().Throw<InvalidOperationException>().WithMessage("*stations*");
    }

    [Test]
    public async Task Should_throw_descriptive_error_for_invalid_legend_toggle_value()
    {
        // arrange
        var display = CreateDisplay();
        var binder = new MapLegendDisplayBinder(() => Task.CompletedTask);
        binder.UpdateDisplaySubscription(display);
        var item = new MapLegendItem("stations", "Stations", DisplayItemId: "stations");

        // act
        var act = () => binder.ToggleItemAsync(item, new ChangeEventArgs { Value = 42 });

        // assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Legend display toggle expected a bool or parseable string value.");
    }

    private IReadOnlyList<MapSceneMutation> GetSceneMutations() =>
        JSInterop
            .Invocations[ApplySceneMutationsIdentifier]
            .Select(invocation => invocation.Arguments[1])
            .OfType<MapSceneMutationBatch>()
            .SelectMany(batch => batch.Mutations)
            .ToArray();

    private MapSceneMutationBatch GetLatestSceneMutationBatch() =>
        JSInterop
            .Invocations[ApplySceneMutationsIdentifier][^1]
            .Arguments[1]
            .Should()
            .BeOfType<MapSceneMutationBatch>()
            .Subject;

    private static MapDisplayState CreateDisplay() =>
        new([
            new MapDisplayItem(
                "stations",
                [MapDisplayTarget.StyleLayers("overlay-style", "stations-circle", "stations-label")],
                IsOn: false
            ),
        ]);

    private static MapLegend CreateLegend() =>
        new([new MapLegendSection("Layers", [new MapLegendItem("stations", "Stations", DisplayItemId: "stations")])]);
}
