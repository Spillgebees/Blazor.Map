using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Controls;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Models.TrackedData;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public partial class TrainTrackingExample : IAsyncDisposable
{
    private const int SelectionDetailsMinZoom = 13;

    private SgbMap _map = null!;
    private TrackedDataSource<TrainSampleState> _trainSource = null!;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _simulationTask;
    private string? _hoveredTrainId;
    private string? _selectedTrainId;
    private readonly List<TrainSampleState> _trains = [];
    private List<MapImageDefinition> _images = [];

    [CascadingParameter]
    public MapTheme GlobalTheme { get; set; }

    [Inject]
    private IConfiguration Configuration { get; set; } = null!;

    private MapOptions _mapOptions = null!;
    private readonly MapControlOptions _mapControlOptions = TrainTrackingPresentation.MapControlOptions;
    private readonly AnimationOptions _trainAnimation = TrainTrackingPresentation.TrainAnimation;
    private readonly TrackedDataClusterOptions _trainClusterOptions =
        TrainTrackingPresentation.TrackedTrainClusterOptions;
    private readonly object[] _trainIconOpacityExpr = TrainTrackingPresentation.TrainIconOpacityExpression;
    private readonly TrainTrackingVisibilityState _visibility = new();
    private readonly TrackedDataIdentityOptions<TrainSampleState> _trainIdentity = new(train => train.Id);
    private readonly TrackedDataSymbolOptions<TrainSampleState> _trainSymbol = new(
        train => train.CurrentPosition,
        train => $"train-{train.Color.TrimStart('#')}",
        SizeSelector: _ => 1.0,
        RotationSelector: train => TrainTrackingPresentation.GetBearing(train),
        ColorSelector: train => train.Color,
        HoverSelector: _ => new TrackedEntityHoverIntent(1.2, true),
        RenderOrderSelector: _ => 100,
        PropertiesSelector: train => new Dictionary<string, object?>
        {
            ["internationalPresence"] = TrainSampleSimulation.IsInternational(train) ? 1 : 0,
        }
    );
    private static IReadOnlyList<TrackedDataDecorationOptions<TrainSampleState>> _trainDecorations =>
        [
            new(
                "cluster-sentinel",
                IconImageSelector: train => $"train-{train.Color.TrimStart('#')}",
                DisplayMode: TrackedEntityDecorationDisplayMode.Always,
                IconSizeSelector: _ => 0.0,
                RenderOrderSelector: _ => -1
            ),
            new(
                "service",
                TextSelector: train => train.ServiceNumber,
                Offset: new Point(13.3, -3.3),
                Anchor: "left",
                DisplayMode: TrackedEntityDecorationDisplayMode.Always,
                ColorSelector: _ => "#0f172a",
                TextSizeSelector: _ => 12,
                RenderOrderSelector: _ => 110,
                HaloColorSelector: _ => "#ffffff",
                HaloWidthSelector: _ => 1.5,
                TextFont: ["DM Sans", "Noto Sans Regular"]
            ),
            new(
                "route",
                TextSelector: train => train.Route.Replace(">", "\u203A"),
                Offset: new Point(20, 7.5),
                Anchor: "left",
                DisplayMode: TrackedEntityDecorationDisplayMode.HoverOrSelected,
                ColorSelector: _ => "#94a3b8",
                TextSizeSelector: _ => 8,
                RenderOrderSelector: _ => 105,
                HaloColorSelector: _ => "#ffffff",
                HaloWidthSelector: _ => 1.0,
                TextFont: ["DM Sans", "Noto Sans Regular"]
            ),
            new(
                "operator",
                TextSelector: train => train.Operator,
                Offset: new Point(-16, -4),
                Anchor: "right",
                DisplayMode: TrackedEntityDecorationDisplayMode.HoverOrSelected,
                ColorSelector: train => train.Color,
                TextSizeSelector: _ => 10,
                RenderOrderSelector: _ => 108,
                HaloColorSelector: _ => "#ffffff",
                HaloWidthSelector: _ => 1.5,
                TextFont: ["Martian Mono", "Noto Sans Regular"]
            ),
        ];

    private TrackedDataInteractionOptions<TrainSampleState> _trainInteraction =>
        new(IsHovered: train => train.Id == _hoveredTrainId, IsSelected: train => train.Id == _selectedTrainId);

    private static MapLegendDefinition OverlayLegendDefinition => TrainTrackingPresentation.OverlayLegendDefinition;

    private static LegendControlOptions OverlayLegendControlOptions => TrainTrackingPresentation.LegendControlOptions;

    protected override void OnInitialized()
    {
        _mapOptions = TrainTrackingPresentation.BuildMapOptions(
            Configuration[TrainTrackingPresentation.OverlayStyleUrlConfigurationKey],
            Configuration[TrainTrackingPresentation.ComposedGlyphsUrlConfigurationKey]
        );
        _trains.AddRange(TrainSampleSimulation.CreateStates());
        _images = BuildTrainImages(_trains);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(2000));

        try
        {
            await Task.Delay(500, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _simulationTask = RunSimulationLoopAsync(_cts.Token);
    }

    private async Task RunSimulationLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(cancellationToken))
            {
                foreach (var train in _trains)
                {
                    TrainSampleSimulation.Advance(train);
                }

                await InvokeAsync(StateHasChanged);
                await InvokeAsync(RefreshMapFocusForSelectionAsync);
            }
        }
        catch (OperationCanceledException) { }
    }

    private static List<MapImageDefinition> BuildTrainImages(IEnumerable<TrainSampleState> trains)
    {
        return
        [
            .. trains
                .Select(train => train.Color)
                .Distinct()
                .Select(color =>
                {
                    var iconName = $"train-{color.TrimStart('#')}";
                    var svg = TrainSampleSimulation.BuildIconSvg(color);
                    var dataUri = $"data:image/svg+xml,{Uri.EscapeDataString(svg)}";
                    return new MapImageDefinition(iconName, dataUri, 28, 28);
                }),
        ];
    }

    private async Task HandleTrainClick(TrackedEntityInteractionEventArgs<TrainSampleState> interaction)
    {
        if (interaction.Entity.Metadata is not { } train)
        {
            return;
        }

        _selectedTrainId = train.Id;
        await InvokeAsync(StateHasChanged);

        var targetZoom = await GetSelectionFocusZoomAsync();
        await _map.FlyToAsync(interaction.Entity.Position, zoom: targetZoom);
        await _map.ClosePopupAsync();
    }

    private Task HandleTrainHover(TrackedEntityInteractionEventArgs<TrainSampleState> interaction)
    {
        if (interaction.Entity.Metadata is not { } train)
        {
            return Task.CompletedTask;
        }

        if (_hoveredTrainId == train.Id)
        {
            return Task.CompletedTask;
        }

        _hoveredTrainId = train.Id;
        return InvokeAsync(StateHasChanged);
    }

    private Task HandleTrainLeave()
    {
        if (_hoveredTrainId is null)
        {
            return Task.CompletedTask;
        }

        _hoveredTrainId = null;
        return InvokeAsync(StateHasChanged);
    }

    private async Task ClearSelectionAsync()
    {
        if (_selectedTrainId is null)
        {
            return;
        }

        var selectedTrainId = _selectedTrainId;
        _selectedTrainId = null;

        if (_hoveredTrainId == selectedTrainId)
        {
            _hoveredTrainId = null;
        }

        await InvokeAsync(StateHasChanged);
        await _map.ClosePopupAsync();
    }

    private async Task RefreshMapFocusForSelectionAsync()
    {
        if (_selectedTrainId is null || _trains.FirstOrDefault(t => t.Id == _selectedTrainId) is not { } selectedTrain)
        {
            return;
        }

        var targetZoom = await GetSelectionFocusZoomAsync();
        await _map.FlyToAsync(selectedTrain.CurrentPosition, zoom: targetZoom);
    }

    private async Task<int?> GetSelectionFocusZoomAsync()
    {
        var currentZoom = await _map.GetZoomAsync();

        return GetSelectionFocusZoom(currentZoom);
    }

    private static int? GetSelectionFocusZoom(double? currentZoom)
    {
        if (currentZoom is null)
        {
            return SelectionDetailsMinZoom;
        }

        return currentZoom.Value >= SelectionDetailsMinZoom ? null : SelectionDetailsMinZoom;
    }

    private Task HandleLegendItemVisibilityChangedAsync(MapLegendVisibilityChangedEventArgs args)
    {
        _visibility.SetOverlayGroupVisibility(args.Item.Id, args.Visible);

        return Task.CompletedTask;
    }

    private static bool ResolveLegendToggleValue(ChangeEventArgs args) =>
        args.Value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => false,
        };

    private static string GetOverlayLegendSwatchClassName(MapLegendItemDefinition item) =>
        $"train-overlay-swatch train-overlay-swatch-{item.Id}";

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        if (_simulationTask is not null)
        {
            try
            {
                await _simulationTask;
            }
            catch (OperationCanceledException) { }
        }

        _timer?.Dispose();
    }
}
