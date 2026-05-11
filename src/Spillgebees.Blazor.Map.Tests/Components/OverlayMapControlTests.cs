using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map.Tests.Components;

public class OverlayMapControlTests : BunitContext
{
    private const int TestTimeoutMs = 5000;
    private const string CreateMapIdentifier = "Spillgebees.Map.mapFunctions.createMap";
    private const string DisposeMapIdentifier = "Spillgebees.Map.mapFunctions.disposeMap";
    private const string ResizeIdentifier = "Spillgebees.Map.mapFunctions.resize";
    private const string ApplySceneMutationsIdentifier = "Spillgebees.Map.mapFunctions.applySceneMutations";
    private const string SetMapOptionsIdentifier = "Spillgebees.Map.mapFunctions.setMapOptions";
    private const string SetControlContentIdentifier = "Spillgebees.Map.mapFunctions.setControlContent";
    private const string RemoveControlContentIdentifier = "Spillgebees.Map.mapFunctions.removeControlContent";

    public OverlayMapControlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid(CreateMapIdentifier);
        JSInterop.SetupVoid(DisposeMapIdentifier);
        JSInterop.SetupVoid(ResizeIdentifier);
        JSInterop.SetupVoid(ApplySceneMutationsIdentifier);
        JSInterop.SetupVoid(SetMapOptionsIdentifier);
        JSInterop.SetupVoid(SetControlContentIdentifier);
        JSInterop.SetupVoid(RemoveControlContentIdentifier);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_style_overlay_without_parts(CancellationToken cancellationToken)
    {
        var cut = RenderOverlayMap(includeParts: false);

        await cut.Instance.OnMapInitializedAsync();

        GetSceneMutations()
            .Should()
            .Contain(mutation =>
                mutation.Kind == "setOverlay"
                && mutation.OverlayId == "lux-railway"
                && mutation.Visible == true
                && mutation.OverlayTargets!.Single().StyleId == "lux-railway"
                && mutation.OverlayTargets!.Single().LayerIds.Count == 0
                && mutation.Parts!.Count == 0
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_render_overlay_control_and_toggle_parent(CancellationToken cancellationToken)
    {
        var cut = RenderOverlayMap(includeParts: false);
        await cut.Instance.OnMapInitializedAsync();
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        cut.Find("[data-testid='map-overlay-toggle-lux-railway']").Change(false);

        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount)
        );
        GetLatestSceneMutationBatch()
            .Mutations.Should()
            .ContainSingle(mutation =>
                mutation.Kind == "setOverlay" && mutation.OverlayId == "lux-railway" && mutation.Visible == false
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_register_explicit_parts_and_preserve_part_state_when_parent_toggles(
        CancellationToken cancellationToken
    )
    {
        var cut = RenderOverlayMap(includeParts: true);
        await cut.Instance.OnMapInitializedAsync();
        var initialMutationCount = GetSceneMutations().Count;

        cut.Find("[data-testid='map-overlay-toggle-lux-railway']").Change(false);
        cut.Find("[data-testid='map-overlay-toggle-lux-railway']").Change(true);

        cut.WaitForAssertion(() => GetSceneMutations().Count.Should().BeGreaterThan(initialMutationCount));

        var latestOverlay = GetSceneMutations()
            .Skip(initialMutationCount)
            .Last(mutation => mutation.Kind == "setOverlay" && mutation.OverlayId == "lux-railway");

        latestOverlay.Visible.Should().BeTrue();
        latestOverlay.Parts!.Should().Contain(part => part.PartId == "lifecycle" && part.Visible == false);
        latestOverlay.Parts!.Should().Contain(part => part.PartId == "tracks" && part.Visible == true);
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_toggle_overlay_programmatically_without_overlay_map_control(
        CancellationToken cancellationToken
    )
    {
        var cut = RenderOverlayMap(includeParts: false, includeOverlayControl: false);
        await cut.Instance.OnMapInitializedAsync();
        var initialBatchCount = JSInterop.Invocations[ApplySceneMutationsIdentifier].Count;

        cut.Instance.ToggleOverlay("lux-railway");

        cut.WaitForAssertion(() =>
            JSInterop.Invocations[ApplySceneMutationsIdentifier].Count.Should().BeGreaterThan(initialBatchCount)
        );
        GetLatestSceneMutationBatch()
            .Mutations.Should()
            .ContainSingle(mutation =>
                mutation.Kind == "setOverlay" && mutation.OverlayId == "lux-railway" && mutation.Visible == false
            );
    }

    [Test, Timeout(TestTimeoutMs)]
    public async Task Should_toggle_overlay_part_programmatically_and_preserve_part_state_without_overlay_map_control(
        CancellationToken cancellationToken
    )
    {
        var cut = RenderOverlayMap(includeParts: true, includeOverlayControl: false);
        await cut.Instance.OnMapInitializedAsync();
        var initialMutationCount = GetSceneMutations().Count;

        cut.Instance.ToggleOverlayPart("lux-railway", "tracks");
        cut.Instance.ToggleOverlay("lux-railway");
        cut.Instance.ToggleOverlay("lux-railway");

        cut.WaitForAssertion(() => GetSceneMutations().Count.Should().BeGreaterThan(initialMutationCount));

        var latestOverlay = GetSceneMutations()
            .Skip(initialMutationCount)
            .Last(mutation => mutation.Kind == "setOverlay" && mutation.OverlayId == "lux-railway");

        latestOverlay.Visible.Should().BeTrue();
        latestOverlay.Parts!.Should().Contain(part => part.PartId == "tracks" && part.Visible == false);
        latestOverlay.Parts!.Should().Contain(part => part.PartId == "lifecycle" && part.Visible == false);
    }

    [Test]
    public void Should_throw_for_unknown_programmatic_overlay_and_part_ids()
    {
        var cut = RenderOverlayMap(includeParts: true, includeOverlayControl: false);

        var missingOverlay = () => cut.Instance.ToggleOverlay("missing-overlay");
        var missingPart = () => cut.Instance.ToggleOverlayPart("lux-railway", "missing-part");

        missingOverlay.Should().Throw<KeyNotFoundException>().WithMessage("Overlay 'missing-overlay' was not found.");
        missingPart
            .Should()
            .Throw<KeyNotFoundException>()
            .WithMessage("Overlay part 'missing-part' was not found in overlay 'lux-railway'.");
    }

    [Test]
    public void Should_expose_overlay_items_programmatically_without_overlay_map_control()
    {
        var cut = RenderOverlayMap(includeParts: true, includeOverlayControl: false);

        var items = cut.Instance.GetOverlayItems();

        items.Should().ContainSingle();
        items[0].Id.Should().Be("lux-railway");
        items[0].Parts.Select(part => part.Id).Should().BeEquivalentTo(["tracks", "lifecycle"]);
    }

    private IRenderedComponent<SgbMap> RenderOverlayMap(bool includeParts, bool includeOverlayControl = true) =>
        Render<SgbMap>(parameters =>
            parameters.AddChildContent(
                (RenderFragment)(
                    builder =>
                    {
                        if (includeOverlayControl)
                        {
                            builder.OpenComponent<MapControls>(0);
                            builder.AddAttribute(
                                1,
                                nameof(MapControls.ChildContent),
                                (RenderFragment)(
                                    controls =>
                                    {
                                        controls.OpenComponent<OverlayMapControl>(0);
                                        controls.AddAttribute(1, nameof(OverlayMapControl.Id), "overlays");
                                        controls.CloseComponent();
                                    }
                                )
                            );
                            builder.CloseComponent();
                        }

                        builder.OpenComponent<MapOverlays>(2);
                        builder.AddAttribute(
                            3,
                            nameof(MapOverlays.ChildContent),
                            (RenderFragment)(
                                overlays =>
                                {
                                    overlays.OpenComponent<MapOverlay>(0);
                                    overlays.AddAttribute(1, nameof(MapOverlay.Id), "lux-railway");
                                    overlays.AddAttribute(2, nameof(MapOverlay.Label), "Lux railway infrastructure");
                                    overlays.AddAttribute(
                                        3,
                                        nameof(MapOverlay.ChildContent),
                                        (RenderFragment)(
                                            overlay =>
                                            {
                                                overlay.OpenComponent<StyleOverlay>(0);
                                                overlay.AddAttribute(
                                                    1,
                                                    nameof(StyleOverlay.Style),
                                                    MapStyle.FromUrl("traintracking/style.json").WithId("lux-railway")
                                                );
                                                overlay.CloseComponent();

                                                if (!includeParts)
                                                {
                                                    return;
                                                }

                                                overlay.OpenComponent<MapOverlayPart>(2);
                                                overlay.AddAttribute(3, nameof(MapOverlayPart.Id), "tracks");
                                                overlay.AddAttribute(4, nameof(MapOverlayPart.Label), "Tracks");
                                                overlay.AddAttribute(
                                                    5,
                                                    nameof(MapOverlayPart.LayerIds),
                                                    new[] { "railway-line-rail" }
                                                );
                                                overlay.CloseComponent();

                                                overlay.OpenComponent<MapOverlayPart>(6);
                                                overlay.AddAttribute(7, nameof(MapOverlayPart.Id), "lifecycle");
                                                overlay.AddAttribute(8, nameof(MapOverlayPart.Label), "Lifecycle");
                                                overlay.AddAttribute(
                                                    9,
                                                    nameof(MapOverlayPart.LayerIds),
                                                    new[] { "railway-lifecycle-disused" }
                                                );
                                                overlay.AddAttribute(
                                                    10,
                                                    nameof(MapOverlayPart.InitiallyVisible),
                                                    false
                                                );
                                                overlay.CloseComponent();
                                            }
                                        )
                                    );
                                    overlays.CloseComponent();
                                }
                            )
                        );
                        builder.CloseComponent();
                    }
                )
            )
        );

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
}
