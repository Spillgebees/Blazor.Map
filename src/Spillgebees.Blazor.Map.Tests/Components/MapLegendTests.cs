using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class MapLegendTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string GetProtocolVersionIdentifier = "Spillgebees.Map.getProtocolVersion";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";
    private const string SetCustomControlIdentifier = "Spillgebees.Map.mapFunctions.setCustomControl";
    private const string RemoveCustomControlIdentifier = "Spillgebees.Map.mapFunctions.removeCustomControl";
    private const string HasStyleLayerIdentifier = "Spillgebees.Map.mapFunctions.hasStyleLayer";
    private const string SetStyleLayerVisibilityIdentifier = "Spillgebees.Map.mapFunctions.setStyleLayerVisibility";

    public MapLegendTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.Setup<int>(GetProtocolVersionIdentifier).SetResult(11);
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
        JSInterop.SetupVoid(SetCustomControlIdentifier);
        JSInterop.SetupVoid(RemoveCustomControlIdentifier);
        JSInterop.Setup<bool>(HasStyleLayerIdentifier).SetResult(true);
        JSInterop.SetupVoid(SetStyleLayerVisibilityIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_legend_control_with_the_map_shell(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));
        var map = cut.FindComponent<SgbMap>().Instance;

        // act
        await map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke(SetCustomControlIdentifier));
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_keep_pending_legend_registration_across_rerenders_before_map_ready(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));
        var map = cut.FindComponent<SgbMap>().Instance;

        // act
        cut.Render(parameters => parameters.Add(p => p.Definition, CreateDefinition()));
        await map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => JSInterop.Invocations[SetCustomControlIdentifier].Count.Should().Be(1));
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_remove_legend_control_when_disabled(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();

        // act
        cut.Render(parameters =>
            parameters
                .Add(p => p.Definition, CreateDefinition())
                .Add(p => p.ControlOptions, new LegendControlOptions(Enable: false))
        );

        // assert
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke(RemoveCustomControlIdentifier));
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_sync_toggleable_legend_items_as_scene_visibility_groups(
        CancellationToken cancellationToken
    )
    {
        // arrange
        MapLegendVisibilityChangedEventArgs? callbackArgs = null;
        var definition = CreateDefinition();
        var cut = Render<ComponentHost>(parameters =>
            parameters
                .Add(p => p.Definition, definition)
                .Add(
                    p => p.OnItemVisibilityChanged,
                    EventCallback.Factory.Create<MapLegendVisibilityChangedEventArgs>(this, args => callbackArgs = args)
                )
        );
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );

        var initialBatch = GetLatestSceneMutationBatch();
        var toggle = cut.Find("input[data-testid='map-legend-toggle-stations']");

        // act
        await toggle.ChangeAsync(new ChangeEventArgs { Value = false });

        // assert
        var initialVisibilityMutations = initialBatch
            .Mutations.Where(m =>
                m.Kind == "setVisibilityGroup"
                && GetMutationProperty<string>(m, "GroupId") is not null
                && GetMutationProperty<bool?>(m, "GroupVisible") == true
                && GetMutationProperty<IReadOnlyList<object>?>(m, "VisibilityTargets")?.Count == 1
            )
            .ToArray();
        initialVisibilityMutations.Should().HaveCount(1);

        var updatedBatch = GetLatestSceneMutationBatch();
        var updatedVisibilityMutations = updatedBatch
            .Mutations.Where(m =>
                m.Kind == "setVisibilityGroup"
                && GetMutationProperty<string>(m, "GroupId") is not null
                && GetMutationProperty<bool?>(m, "GroupVisible") == false
                && GetMutationProperty<IReadOnlyList<object>?>(m, "VisibilityTargets")?.Count == 1
            )
            .ToArray();
        updatedVisibilityMutations.Should().HaveCount(1);

        callbackArgs.Should().NotBeNull();
        callbackArgs!.Item.Id.Should().Be("stations");
        callbackArgs.Visible.Should().BeFalse();
        JSInterop.VerifyNotInvoke(SetStyleLayerVisibilityIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_not_reregister_legend_shell_when_only_item_visibility_changes(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => JSInterop.Invocations[SetCustomControlIdentifier].Count.Should().BeGreaterThan(0));
        var toggle = cut.Find("input[data-testid='map-legend-toggle-stations']");
        var initialShellInvocationCount = JSInterop.Invocations[SetCustomControlIdentifier].Count;

        // act
        await toggle.ChangeAsync(new ChangeEventArgs { Value = false });

        // assert
        JSInterop.Invocations[SetCustomControlIdentifier].Count.Should().Be(initialShellInvocationCount);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_allow_custom_item_templates_to_toggle_item_visibility(CancellationToken cancellationToken)
    {
        // arrange
        MapLegendVisibilityChangedEventArgs? callbackArgs = null;
        var cut = Render<TemplatedComponentHost>(parameters =>
            parameters
                .Add(p => p.Definition, CreateDefinition())
                .Add(
                    p => p.OnItemVisibilityChanged,
                    EventCallback.Factory.Create<MapLegendVisibilityChangedEventArgs>(this, args => callbackArgs = args)
                )
        );
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        // act
        await cut.Find("button[data-testid='template-toggle-stations']").ClickAsync(new MouseEventArgs());

        // assert
        JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount);
        callbackArgs.Should().NotBeNull();
        callbackArgs!.Item.Id.Should().Be("stations");
        callbackArgs.Visible.Should().BeFalse();
        JSInterop.Invocations[SetStyleLayerVisibilityIdentifier].Count.Should().Be(0);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_reregister_legend_shell_when_position_changes(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke(SetCustomControlIdentifier));
        var initialRegisterInvocationCount = JSInterop.Invocations[SetCustomControlIdentifier].Count;

        // act
        cut.Render(parameters =>
            parameters
                .Add(p => p.Definition, CreateDefinition())
                .Add(p => p.ControlOptions, new LegendControlOptions(Position: ControlPosition.BottomLeft))
        );

        // assert
        JSInterop.Invocations[SetCustomControlIdentifier].Count.Should().Be(initialRegisterInvocationCount + 1);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_control_options_for_attribution_safe_runtime_layout(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var controlOptions = new LegendControlOptions(
            Position: ControlPosition.BottomRight,
            ClassName: "legend-test-shell"
        );
        var cut = Render<ComponentHost>(parameters =>
            parameters.Add(p => p.Definition, CreateDefinition()).Add(p => p.ControlOptions, controlOptions)
        );
        var map = cut.FindComponent<SgbMap>().Instance;

        // act
        await map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke(SetCustomControlIdentifier));
        var invocation = JSInterop.Invocations[SetCustomControlIdentifier].Single();
        invocation.Arguments[1].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
        invocation.Arguments[2].Should().Be("legend");
        invocation.Arguments[3].Should().Be(controlOptions.Position);
        invocation.Arguments[4].Should().Be(500);
        invocation.Arguments[5].Should().BeEquivalentTo(controlOptions);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_replace_scene_visibility_groups_when_definition_changes(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );

        var nextDefinition = new MapLegendDefinition([
            new MapLegendSectionDefinition(
                "Operational layers",
                [
                    new MapLegendItemDefinition(
                        "depots",
                        "Depots",
                        Targets: [new MapLegendTargetDefinition("overlay-style", ["depots-circle"])],
                        IsToggleable: true
                    ),
                ]
            ),
        ]);

        // act
        cut.Render(parameters => parameters.Add(p => p.Definition, nextDefinition));

        // assert
        var mutationBatches = JSInterop
            .Invocations[ApplySceneMutationsIdentifier]
            .Select(invocation => invocation.Arguments[1])
            .OfType<MapSceneMutationBatch>()
            .ToArray();
        var removedVisibilityGroups = mutationBatches
            .SelectMany(batch => batch.Mutations)
            .Where(m =>
                m.Kind == "removeVisibilityGroup" && GetMutationProperty<string>(m, "GroupId") == "legend:stations"
            )
            .ToArray();
        removedVisibilityGroups.Should().HaveCount(1);

        var addedVisibilityGroups = mutationBatches
            .SelectMany(batch => batch.Mutations)
            .Where(m => m.Kind == "setVisibilityGroup" && GetMutationProperty<string>(m, "GroupId") == "legend:depots")
            .ToArray();
        addedVisibilityGroups.Should().HaveCount(1);
        JSInterop.Invocations[SetStyleLayerVisibilityIdentifier].Count.Should().Be(0);
    }

    [Test]
    public void Should_render_typed_legend_sections_and_items()
    {
        // arrange & act
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));

        // assert
        cut.Markup.Should().Contain("sgb-map-legend-content");
        cut.Markup.Should().Contain("Operational layers");
        cut.Markup.Should().Contain("Stations");
        cut.Markup.Should().Contain("Passenger stops and station labels.");
        cut.Markup.Should().Contain("sgb-map-legend-item-switch");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_treat_item_as_toggleable_when_explicitly_set_without_targets(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var definition = new MapLegendDefinition([
            new MapLegendSectionDefinition(
                "Test section",
                [new MapLegendItemDefinition("toggleable-no-targets", "Toggle me", IsToggleable: true)]
            ),
        ]);
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, definition));
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();

        // act
        var toggle = cut.Find("input[data-testid='map-legend-toggle-toggleable-no-targets']");
        await toggle.ChangeAsync(new ChangeEventArgs { Value = false });

        // assert
        toggle.Should().NotBeNull();
        var checkedAttr = cut.Find("input[data-testid='map-legend-toggle-toggleable-no-targets']")
            .GetAttribute("checked");
        checkedAttr.Should().BeNull();
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_fire_visibility_callback_for_toggleable_items_without_targets(
        CancellationToken cancellationToken
    )
    {
        // arrange
        MapLegendVisibilityChangedEventArgs? callbackArgs = null;
        var definition = new MapLegendDefinition([
            new MapLegendSectionDefinition(
                "Test section",
                [new MapLegendItemDefinition("no-target-toggle", "Toggle me", IsToggleable: true)]
            ),
        ]);
        var cut = Render<ComponentHost>(parameters =>
            parameters
                .Add(p => p.Definition, definition)
                .Add(
                    p => p.OnItemVisibilityChanged,
                    EventCallback.Factory.Create<MapLegendVisibilityChangedEventArgs>(this, args => callbackArgs = args)
                )
        );
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        var toggle = cut.Find("input[data-testid='map-legend-toggle-no-target-toggle']");

        // act
        await toggle.ChangeAsync(new ChangeEventArgs { Value = false });

        // assert
        callbackArgs.Should().NotBeNull();
        callbackArgs!.Item.Id.Should().Be("no-target-toggle");
        callbackArgs.Visible.Should().BeFalse();
        JSInterop.Invocations[SetStyleLayerVisibilityIdentifier].Count.Should().Be(0);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_apply_off_class_when_item_is_toggled_off(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        var toggle = cut.Find("input[data-testid='map-legend-toggle-stations']");

        // act
        await toggle.ChangeAsync(new ChangeEventArgs { Value = false });

        // assert
        var itemContainer = cut.Find(".sgb-map-legend-item-toggleable");
        itemContainer.ClassList.Should().Contain("sgb-map-legend-item-off");
    }

    [Test]
    public void Should_render_toggle_switch_for_toggleable_items()
    {
        // arrange & act
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));

        // assert
        cut.Markup.Should().Contain("sgb-map-legend-item-switch");
        cut.Markup.Should().Contain("sgb-map-legend-item-switch-track");
    }

    [Test]
    public void Should_not_render_toggle_for_non_toggleable_item()
    {
        // arrange & act
        var definition = new MapLegendDefinition([
            new MapLegendSectionDefinition(
                "Test section",
                [new MapLegendItemDefinition("static-item", "Static label")]
            ),
        ]);
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, definition));

        // assert
        cut.Markup.Should().Contain("Static label");
        cut.FindAll("input[data-testid='map-legend-toggle-static-item']").Should().BeEmpty();
    }

    [Test]
    public void Should_render_toggle_input_with_switch_role_and_aria_checked()
    {
        // arrange & act
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));

        // assert
        var toggle = cut.Find("input[data-testid='map-legend-toggle-stations']");
        toggle.GetAttribute("role").Should().Be("switch");
        toggle.GetAttribute("aria-checked").Should().Be("true");
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_update_aria_checked_when_toggle_is_turned_off(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<ComponentHost>(parameters => parameters.Add(p => p.Definition, CreateDefinition()));
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        var toggle = cut.Find("input[data-testid='map-legend-toggle-stations']");

        // act
        await toggle.ChangeAsync(new ChangeEventArgs { Value = false });

        // assert
        var updatedToggle = cut.Find("input[data-testid='map-legend-toggle-stations']");
        updatedToggle.GetAttribute("aria-checked").Should().Be("false");
    }

    private static MapLegendDefinition CreateDefinition() =>
        new(
            Sections:
            [
                new MapLegendSectionDefinition(
                    "Operational layers",
                    [
                        new MapLegendItemDefinition(
                            "stations",
                            "Stations",
                            "Passenger stops and station labels.",
                            [new MapLegendTargetDefinition("overlay-style", ["stations-circle", "stations-label"])],
                            true,
                            IsToggleable: true
                        ),
                    ]
                ),
            ]
        );

    public sealed class ComponentHost : ComponentBase
    {
        [Parameter, EditorRequired]
        public MapLegendDefinition Definition { get; set; } = null!;

        [Parameter]
        public EventCallback<MapLegendVisibilityChangedEventArgs> OnItemVisibilityChanged { get; set; }

        [Parameter]
        public LegendControlOptions ControlOptions { get; set; } = LegendControlOptions.Default;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // arrange
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(
                1,
                nameof(SgbMap.ChildContent),
                (RenderFragment)(
                    childBuilder =>
                    {
                        // act
                        childBuilder.OpenComponent<MapLegend>(0);
                        childBuilder.AddAttribute(1, nameof(MapLegend.Definition), Definition);
                        childBuilder.AddAttribute(
                            2,
                            nameof(MapLegend.OnItemVisibilityChanged),
                            OnItemVisibilityChanged
                        );
                        childBuilder.AddAttribute(3, nameof(MapLegend.ControlOptions), ControlOptions);
                        childBuilder.CloseComponent();
                    }
                )
            );
            builder.CloseComponent();

            // assert
        }
    }

    public sealed class TemplatedComponentHost : ComponentBase
    {
        [Parameter, EditorRequired]
        public MapLegendDefinition Definition { get; set; } = null!;

        [Parameter]
        public EventCallback<MapLegendVisibilityChangedEventArgs> OnItemVisibilityChanged { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // arrange
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(
                1,
                nameof(SgbMap.ChildContent),
                (RenderFragment)(
                    childBuilder =>
                    {
                        // act
                        childBuilder.OpenComponent<MapLegend>(0);
                        childBuilder.AddAttribute(1, nameof(MapLegend.Definition), Definition);
                        childBuilder.AddAttribute(
                            2,
                            nameof(MapLegend.OnItemVisibilityChanged),
                            OnItemVisibilityChanged
                        );
                        childBuilder.AddAttribute(
                            3,
                            nameof(MapLegend.ItemTemplate),
                            (RenderFragment<MapLegendItemTemplateContext>)(
                                context =>
                                    builder =>
                                    {
                                        builder.OpenElement(0, "button");
                                        builder.AddAttribute(1, "type", "button");
                                        builder.AddAttribute(2, "data-testid", $"template-toggle-{context.Item.Id}");
                                        builder.AddAttribute(
                                            3,
                                            "onclick",
                                            EventCallback.Factory.Create(
                                                this,
                                                () => context.SetVisibilityAsync(!context.Visible)
                                            )
                                        );
                                        builder.AddContent(4, context.Item.Label);
                                        builder.CloseElement();
                                    }
                            )
                        );
                        childBuilder.CloseComponent();
                    }
                )
            );
            builder.CloseComponent();

            // assert
        }
    }

    private MapSceneMutationBatch GetLatestSceneMutationBatch()
    {
        var invocationCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;
        var invocation = JSInterop.Invocations[ApplySceneMutationsIdentifier][invocationCount - 1];
        return invocation.Arguments[1].Should().BeOfType<MapSceneMutationBatch>().Subject;
    }

    private static T? GetMutationProperty<T>(MapSceneMutation mutation, string propertyName)
    {
        var property = typeof(MapSceneMutation).GetProperty(propertyName);
        if (property is null)
        {
            return default;
        }

        return (T?)property.GetValue(mutation);
    }
}
