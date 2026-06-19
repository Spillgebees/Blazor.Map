namespace Spillgebees.Blazor.Map.Docs.Samples.CameraFollow;

public partial class CameraFollowExample : IAsyncDisposable
{
    private readonly List<FollowSampleTrain> _trains = FollowSampleTrain.CreateFleet();
    private readonly HashSet<string> _despawned = [];
    private readonly List<MapImage> _images;

    // Stable list passed to the layer; rebuilt only when the despawn set changes, not every render,
    // so the layer keeps a steady Items reference while the trains' positions mutate in place.
    private List<FollowSampleTrain> _visibleTrains;

    private MapFollowOptions? _follow;
    private MapFollowChangeReason? _lastReason;
    private string? _targetId;

    // Live option state; every control mutates one of these and rebuilds the follow request.
    private MapFollowGestureMode _zoomMode = MapFollowGestureMode.Anchored;
    private MapFollowGestureMode _orientationMode = MapFollowGestureMode.Free;
    private PitchChoice _pitch = PitchChoice.Flat;
    private MapFollowBearingSource _bearingSource = MapFollowBearingSource.KeepCurrent;
    private OffsetChoice _offset = OffsetChoice.Centered;
    private AnimationChoice _animation = AnimationChoice.EaseInOut;
    private bool _clearOnPan = true;
    private bool _clearWhenMissing = true;

    private CancellationTokenSource? _cts;
    private Task? _simulationTask;
    private PeriodicTimer? _timer;

    public CameraFollowExample()
    {
        _images =
        [
            .. _trains.Select(train => new MapImage(
                $"follow-train-{train.Id}",
                $"data:image/svg+xml,{Uri.EscapeDataString(BuildIconSvg(train.Color))}",
                28,
                28
            )),
        ];
        _visibleTrains = [.. _trains];
    }

    private void RefreshVisibleTrains() => _visibleTrains = [.. _trains.Where(train => !_despawned.Contains(train.Id))];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));
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
                    train.Advance();
                }

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void SelectTarget(string trainId)
    {
        _targetId = trainId;
        if (_despawned.Remove(trainId))
        {
            RefreshVisibleTrains();
        }

        RebuildFollow();
    }

    private void StopFollowing()
    {
        _targetId = null;
        RebuildFollow();
    }

    // Removing the followed train from the layer demonstrates the missing-feature behaviour:
    // clear-and-notify when ClearWhenFeatureMissing is on, hold otherwise.
    private void DespawnTarget()
    {
        if (_targetId is not null && _despawned.Add(_targetId))
        {
            RefreshVisibleTrains();
        }
    }

    private void RespawnAll()
    {
        if (_despawned.Count == 0)
        {
            return;
        }

        _despawned.Clear();
        RefreshVisibleTrains();
    }

    private void SetZoomMode(MapFollowGestureMode value)
    {
        _zoomMode = value;
        RebuildFollow();
    }

    private void SetOrientationMode(MapFollowGestureMode value)
    {
        _orientationMode = value;
        RebuildFollow();
    }

    private void SetPitch(PitchChoice value)
    {
        _pitch = value;
        RebuildFollow();
    }

    private void SetBearingSource(MapFollowBearingSource value)
    {
        _bearingSource = value;
        RebuildFollow();
    }

    private void SetOffset(OffsetChoice value)
    {
        _offset = value;
        RebuildFollow();
    }

    private void SetAnimation(AnimationChoice value)
    {
        _animation = value;
        RebuildFollow();
    }

    private void ToggleClearOnPan()
    {
        _clearOnPan = !_clearOnPan;
        RebuildFollow();
    }

    private void ToggleClearWhenMissing()
    {
        _clearWhenMissing = !_clearWhenMissing;
        RebuildFollow();
    }

    // Maps the live option state onto a MapFollowOptions. Re-assigning Follow re-applies it; the
    // component diffs by record equality, so an unchanged value is a no-op.
    private void RebuildFollow()
    {
        if (_targetId is null)
        {
            _follow = null;
            return;
        }

        _follow = new MapFollowOptions(
            "trains",
            _targetId,
            Camera: new MapFollowCameraOptions(
                ZoomMode: _zoomMode,
                Zoom: 15,
                OrientationMode: _orientationMode,
                Pitch: _pitch == PitchChoice.MaxTilt ? 60 : null,
                BearingSource: _bearingSource,
                Bearing: _bearingSource == MapFollowBearingSource.Fixed ? 90 : null,
                Offset: _offset == OffsetChoice.Right ? new PixelPoint(140, 0) : null
            ),
            Animation: _animation switch
            {
                AnimationChoice.Instant => new AnimationOptions(0),
                AnimationChoice.Linear => new AnimationOptions(600, AnimationEasing.Linear),
                _ => new AnimationOptions(600, AnimationEasing.EaseInOut),
            },
            Interaction: new MapFollowInteractionOptions(
                ClearOnUserPan: _clearOnPan,
                ClearWhenFeatureMissing: _clearWhenMissing
            )
        );
    }

    private void HandleFollowChanged(MapFollowChangedEventArgs args)
    {
        _lastReason = args.Reason;

        // The engine cleared the follow on its own (user gesture or missing entity); deselect so the
        // controls reflect that we are no longer following.
        if (args.Reason is MapFollowChangeReason.UserInteraction or MapFollowChangeReason.FeatureMissing)
        {
            _targetId = null;
        }

        StateHasChanged();
    }

    // Upward-pointing navigation arrow; rotated by the entity's heading so it points along travel.
    private static string BuildIconSvg(string color) =>
        $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="32" height="32">
              <path d="M16 4 L25 27 L16 22 L7 27 Z" fill="{color}" stroke="white" stroke-width="1.5" stroke-linejoin="round"/>
            </svg>
            """;

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

    private enum PitchChoice
    {
        Flat,
        MaxTilt,
    }

    private enum OffsetChoice
    {
        Centered,
        Right,
    }

    private enum AnimationChoice
    {
        Instant,
        EaseInOut,
        Linear,
    }
}
