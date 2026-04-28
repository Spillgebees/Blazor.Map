using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Spillgebees.Blazor.Map.Components;
using Spillgebees.Blazor.Map.Components.Layers;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Models.Legends;
using Spillgebees.Blazor.Map.Models.Options;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public partial class TrainTrackingExample : IAsyncDisposable
{
    private const int SelectionDetailsMinZoom = 13;

    private SgbMap _map = null!;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _simulationTask;
    private string? _hoveredTrainId;
    private string? _selectedTrainId;
    private readonly List<TrainSampleState> _trains = [];
    private readonly List<MapImage> _images;
    private RenderFragment<MapLegendItemTemplateContext>? _overlayLegendItemTemplate;
    private EventCallback<MapLegendVisibilityChangedEventArgs> _legendItemVisibilityChangedCallback;

    [CascadingParameter]
    public MapTheme GlobalTheme { get; set; }

    [Inject]
    private IConfiguration Configuration { get; set; } = null!;

    private MapOptions _mapOptions = null!;
    private readonly AnimationOptions _trainAnimation = TrainTrackingPresentation.TrainAnimation;
    private readonly TrackedEntityClusterOptions _trainClusterOptions =
        TrainTrackingPresentation.TrackedTrainClusterOptions;
    private readonly TrackedEntityBehaviorOptions<TrainSampleState> _trainBehavior;
    private readonly TrackedEntityCallbacks<TrainSampleState> _trainCallbacks;
    private readonly object[] _trainIconOpacityExpr = TrainTrackingPresentation.TrainIconOpacityExpression;
    private readonly TrainTrackingVisibilityState _visibility = new();
    private readonly TrackedEntityIdOptions<TrainSampleState> _trainId = new(train => train.Id);
    private readonly TrackedEntityInteractionOptions<TrainSampleState> _trainInteraction;
    private readonly TrackedEntitySymbolOptions<TrainSampleState> _trainSymbol = new(
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
    private static readonly IReadOnlyList<TrackedEntityDecorationOptions<TrainSampleState>> _trainDecorations =
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
            Anchor: SymbolAnchor.Left,
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
            Anchor: SymbolAnchor.Left,
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
            Anchor: SymbolAnchor.Right,
            DisplayMode: TrackedEntityDecorationDisplayMode.HoverOrSelected,
            ColorSelector: train => train.Color,
            TextSizeSelector: _ => 10,
            RenderOrderSelector: _ => 108,
            HaloColorSelector: _ => "#ffffff",
            HaloWidthSelector: _ => 1.5,
            TextFont: ["Martian Mono", "Noto Sans Regular"]
        ),
    ];

    private TrackedEntityLayerDefinition<TrainSampleState>? _trackedEntityLayer;

    public TrainTrackingExample()
    {
        _images = BuildTrainImages(TrainSampleSimulation.CreateStates());
        _trainInteraction = new(
            IsHovered: train => train.Id == _hoveredTrainId,
            IsSelected: train => train.Id == _selectedTrainId
        );
        _trainBehavior = new(_trainInteraction);
        _trainCallbacks = new(
            OnItemClick: HandleTrainClick,
            OnItemMouseEnter: HandleTrainHover,
            OnItemMouseLeave: HandleTrainLeave,
            OnBeforeShowPopup: null
        );
    }

    protected override void OnInitialized()
    {
        _mapOptions = TrainTrackingPresentation.BuildMapOptions(
            Configuration[TrainTrackingPresentation.OverlayStyleUrlConfigurationKey],
            Configuration[TrainTrackingPresentation.ComposedGlyphsUrlConfigurationKey]
        );
        _trains.AddRange(TrainSampleSimulation.CreateStates());
        RebuildTrackedLayers();
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

                RebuildTrackedLayers();
                await InvokeAsync(StateHasChanged);
                await InvokeAsync(RefreshMapFocusForSelectionAsync);
            }
        }
        catch (OperationCanceledException) { }
    }

    private static List<MapImage> BuildTrainImages(IEnumerable<TrainSampleState> trains)
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
                    return new MapImage(iconName, dataUri, 28, 28);
                }),
        ];
    }

    private async Task HandleTrainClick(TrackedEntityInteractionEventArgs<TrainSampleState> interaction)
    {
        if (interaction.Entity.Item is not { } train)
        {
            return;
        }

        _selectedTrainId = train.Id;
        RebuildTrackedLayers();
        await InvokeAsync(StateHasChanged);

        var targetZoom = await GetSelectionFocusZoomAsync();
        await _map.FlyToAsync(interaction.Entity.Position, zoom: targetZoom);
        await _map.ClosePopupAsync();
    }

    private Task HandleTrainHover(TrackedEntityInteractionEventArgs<TrainSampleState> interaction)
    {
        if (interaction.Entity.Item is not { } train)
        {
            return Task.CompletedTask;
        }

        if (_hoveredTrainId == train.Id)
        {
            return Task.CompletedTask;
        }

        _hoveredTrainId = train.Id;
        RebuildTrackedLayers();
        return InvokeAsync(StateHasChanged);
    }

    private Task HandleTrainLeave()
    {
        if (_hoveredTrainId is null)
        {
            return Task.CompletedTask;
        }

        _hoveredTrainId = null;
        RebuildTrackedLayers();
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

        RebuildTrackedLayers();
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
        _visibility.SetOverlayGroupVisibility(args.Item.Id, args.Selected);
        RebuildTrackedLayers();

        return Task.CompletedTask;
    }

    private void RebuildTrackedLayers()
    {
        _trackedEntityLayer = new TrackedEntityLayerDefinition<TrainSampleState>(
            Id: "train-source",
            Items: _trains,
            IdOptions: _trainId,
            Visual: new TrackedEntityVisualOptions<TrainSampleState>(
                Symbol: _trainSymbol,
                Decorations: _trainDecorations,
                Cluster: _trainClusterOptions,
                Animation: _trainAnimation,
                Visible: _visibility.ShowTrains,
                PrimaryIconOpacity: _trainIconOpacityExpr
            ),
            Behavior: _trainBehavior,
            Callbacks: _trainCallbacks
        );
    }

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
