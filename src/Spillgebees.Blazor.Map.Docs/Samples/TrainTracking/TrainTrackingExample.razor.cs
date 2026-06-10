using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public partial class TrainTrackingExample : IAsyncDisposable
{
    private const int SelectionDetailsMinZoom = 13;

    private SgbMap _map = null!;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _simulationTask;
    private string? _selectedTrainId;
    private readonly List<TrainSampleState> _trains = [];
    private readonly List<MapImage> _images;
    private RenderFragment<MapLegendItemTemplateContext>? _overlayLegendItemTemplate;

    [CascadingParameter]
    public MapTheme GlobalTheme { get; set; }

    [Inject]
    private IConfiguration _configuration { get; set; } = null!;

    private IReadOnlyList<MapStyle> _styles = null!;
    private string? _composedGlyphsUrl;
    private readonly MapDisplayState _display = TrainTrackingPresentation.CreateDisplay();
    private IReadOnlyCollection<string>? _selectedTrainIds => _selectedTrainId is null ? null : [_selectedTrainId];

    public TrainTrackingExample()
    {
        _images = BuildTrainImages(TrainSampleSimulation.CreateStates());
    }

    protected override void OnInitialized()
    {
        _styles = TrainTrackingPresentation.BuildStyles(
            _configuration[TrainTrackingPresentation.OverlayStyleUrlConfigurationKey]
        );
        _composedGlyphsUrl = _configuration[TrainTrackingPresentation.ComposedGlyphsUrlConfigurationKey];
        _trains.AddRange(TrainSampleSimulation.CreateStates());
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

    private static Dictionary<string, object?> BuildTrainProperties(TrainSampleState train) =>
        new()
        {
            ["internationalPresence"] = TrainSampleSimulation.IsInternational(train) ? 1 : 0,
        };

    private async Task HandleTrainClick(EntityEventArgs<TrainSampleState> interaction)
    {
        _selectedTrainId = interaction.Item.Id;
        await InvokeAsync(StateHasChanged);

        var targetZoom = await GetSelectionFocusZoomAsync();
        await _map.FlyToAsync(interaction.Position, zoom: targetZoom);
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

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

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
