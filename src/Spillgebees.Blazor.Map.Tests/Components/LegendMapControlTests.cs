using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class LegendMapControlTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string GetProtocolVersionIdentifier = "Spillgebees.Map.getProtocolVersion";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";
    private const string SetControlContentIdentifier = "Spillgebees.Map.mapFunctions.setControlContent";
    private const string RemoveControlContentIdentifier = "Spillgebees.Map.mapFunctions.removeControlContent";
    private const string HasStyleLayerIdentifier = "Spillgebees.Map.mapFunctions.hasStyleLayer";
    private const string SetStyleLayerVisibilityIdentifier = "Spillgebees.Map.mapFunctions.setStyleLayerVisibility";

    public LegendMapControlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop.Setup<int>(GetProtocolVersionIdentifier).SetResult(13);
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
        JSInterop.SetupVoid(SetControlContentIdentifier);
        JSInterop.SetupVoid(RemoveControlContentIdentifier);
        JSInterop.Setup<bool>(HasStyleLayerIdentifier).SetResult(true);
        JSInterop.SetupVoid(SetStyleLayerVisibilityIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_legend_control_with_the_map_shell(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<ComponentHost>(parameters =>
            parameters.Add(p => p.Controls, [CreateControl("legend-control")])
        );
        var map = cut.FindComponent<SgbMap>().Instance;

        // act
        await map.OnMapInitializedAsync();

        // assert
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke(SetControlContentIdentifier));
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_remove_legend_control_when_disabled(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<ComponentHost>(parameters =>
            parameters.Add(p => p.Controls, [CreateControl("legend-control")])
        );
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();

        // act
        cut.Render(parameters =>
            parameters.Add(
                p => p.Controls,
                [
                    CreateControl(
                        "legend-control",
                        placement: new MapControlPlacement(ControlPosition.TopRight, 500, Enabled: false)
                    ),
                ]
            )
        );

        // assert
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke(RemoveControlContentIdentifier));
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_remove_previous_control_content_when_control_id_changes(
        CancellationToken cancellationToken
    )
    {
        // arrange
        var originalControlId = "legend-control-a";
        var nextControlId = "legend-control-b";
        var cut = Render<ComponentHost>(parameters =>
            parameters.Add(p => p.Controls, [CreateControl(originalControlId)])
        );
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();

        // act
        cut.Render(parameters => parameters.Add(p => p.Controls, [CreateControl(nextControlId)]));

        // assert
        cut.WaitForAssertion(() =>
            JSInterop
                .Invocations[RemoveControlContentIdentifier]
                .Any(invocation =>
                    string.Equals(invocation.Arguments[1]?.ToString(), originalControlId, StringComparison.Ordinal)
                )
                .Should()
                .BeTrue()
        );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_remove_visibility_groups_when_legend_is_disabled(CancellationToken cancellationToken)
    {
        // arrange
        var cut = Render<ComponentHost>(parameters =>
            parameters.Add(p => p.Controls, [CreateControl("legend-control")])
        );
        var map = cut.FindComponent<SgbMap>().Instance;
        await map.OnMapInitializedAsync();
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(0)
        );

        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        // act
        cut.Render(parameters =>
            parameters.Add(
                p => p.Controls,
                [
                    CreateControl(
                        "legend-control",
                        placement: new MapControlPlacement(ControlPosition.TopRight, 500, Enabled: false)
                    ),
                ]
            )
        );

        // assert
        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount)
        );

        var latestBatch = GetLatestSceneMutationBatch();
        latestBatch
            .Mutations.Should()
            .Contain(m =>
                m.Kind == "removeVisibilityGroup"
                && string.Equals(GetMutationProperty<string>(m, "GroupId"), "legend:stations", StringComparison.Ordinal)
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_sync_toggleable_legend_items_as_scene_visibility_groups(
        CancellationToken cancellationToken
    )
    {
        // arrange
        MapLegendVisibilityChangedEventArgs? callbackArgs = null;
        var cut = Render<ComponentHost>(parameters =>
            parameters.Add(
                p => p.Controls,
                [
                    CreateControl(
                        "legend-control",
                        content: CreateContent(
                            CreateDefinition(),
                            onItemVisibilityChanged: EventCallback.Factory.Create<MapLegendVisibilityChangedEventArgs>(
                                this,
                                args => callbackArgs = args
                            )
                        )
                    ),
                ]
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
        callbackArgs.Selected.Should().BeFalse();
        JSInterop.VerifyNotInvoke(SetStyleLayerVisibilityIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_allow_custom_item_templates_to_toggle_item_visibility(CancellationToken cancellationToken)
    {
        // arrange
        MapLegendVisibilityChangedEventArgs? callbackArgs = null;
        var cut = Render<TemplatedComponentHost>(parameters =>
            parameters.Add(
                p => p.Controls,
                [
                    CreateControl(
                        "legend-control",
                        content: CreateContent(
                            CreateDefinition(),
                            itemTemplate: context =>
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
                                            () => context.SetSelectedAsync(!context.Selected)
                                        )
                                    );
                                    builder.AddContent(4, context.Item.Label);
                                    builder.CloseElement();
                                },
                            onItemVisibilityChanged: EventCallback.Factory.Create<MapLegendVisibilityChangedEventArgs>(
                                this,
                                args => callbackArgs = args
                            )
                        )
                    ),
                ]
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
        callbackArgs.Selected.Should().BeFalse();
        JSInterop.Invocations[SetStyleLayerVisibilityIdentifier].Count.Should().Be(0);
    }

    [Test]
    public void Should_render_typed_legend_sections_and_items()
    {
        // arrange & act
        var cut = Render<ComponentHost>(parameters =>
            parameters.Add(p => p.Controls, [CreateControl("legend-control")])
        );

        // assert
        cut.Markup.Should().Contain("sgb-map-legend-content");
        cut.Markup.Should().Contain("Operational layers");
        cut.Markup.Should().Contain("Stations");
        cut.Markup.Should().Contain("Passenger stops and station labels.");
        cut.Markup.Should().Contain("sgb-map-legend-item-switch");
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
            parameters.Add(
                p => p.Controls,
                [
                    CreateControl(
                        "legend-control",
                        content: CreateContent(
                            definition,
                            onItemVisibilityChanged: EventCallback.Factory.Create<MapLegendVisibilityChangedEventArgs>(
                                this,
                                args => callbackArgs = args
                            )
                        )
                    ),
                ]
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
        callbackArgs.Selected.Should().BeFalse();
        JSInterop.Invocations[SetStyleLayerVisibilityIdentifier].Count.Should().Be(0);
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
        var cut = Render<ComponentHost>(parameters =>
            parameters.Add(p => p.Controls, [CreateControl("legend-control", content: CreateContent(definition))])
        );

        // assert
        cut.Markup.Should().Contain("Static label");
        cut.FindAll("input[data-testid='map-legend-toggle-static-item']").Should().BeEmpty();
    }

    private static LegendMapControl CreateControl(
        string controlId,
        MapControlPlacement? placement = null,
        LegendChromeOptions? chrome = null,
        LegendContentOptions? content = null
    ) =>
        new(
            controlId,
            placement ?? new MapControlPlacement(ControlPosition.TopRight, 500, Enabled: true),
            chrome ?? new LegendChromeOptions("Legend", Collapsible: true, InitiallyOpen: true, ClassName: null),
            content ?? CreateContent(CreateDefinition())
        );

    private static LegendContentOptions CreateContent(
        MapLegendDefinition definition,
        RenderFragment<MapLegendItemTemplateContext>? itemTemplate = null,
        EventCallback<MapLegendVisibilityChangedEventArgs> onItemVisibilityChanged = default
    ) => new(definition, itemTemplate, onItemVisibilityChanged);

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
        public IReadOnlyList<MapControl> Controls { get; set; } = [CreateControl("legend-control")];

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // arrange
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(1, nameof(SgbMap.Controls), Controls);
            builder.CloseComponent();

            // act

            // assert
        }
    }

    public sealed class TemplatedComponentHost : ComponentBase
    {
        [Parameter, EditorRequired]
        public IReadOnlyList<MapControl> Controls { get; set; } = [CreateControl("legend-control")];

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // arrange
            builder.OpenComponent<SgbMap>(0);
            builder.AddAttribute(1, nameof(SgbMap.Controls), Controls);
            builder.CloseComponent();

            // act

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
