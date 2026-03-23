namespace Spillgebees.Blazor.Map.Docs.Samples.TrainTracking;

public sealed class TrainTrackingVisibilityState
{
    private readonly Dictionary<string, bool> _overlayGroupVisibility = TrainTrackingPresentation
        .OverlayLegendDefinition.GetItems()
        .Where(item => item.Targets is { Count: > 0 })
        .ToDictionary(item => item.Id, item => item.IsVisibleByDefault, StringComparer.Ordinal);

    public bool ShowBuildings { get; set; } = true;

    public bool ShowTrains { get; set; } = true;

    public bool IsOverlayGroupVisible(string groupKey) =>
        _overlayGroupVisibility.TryGetValue(groupKey, out var isVisible) && isVisible;

    public void SetOverlayGroupVisibility(string groupKey, bool visible)
    {
        switch (groupKey)
        {
            case "3d-buildings":
                ShowBuildings = visible;
                break;
            case "trains":
                ShowTrains = visible;
                break;
            default:
                _overlayGroupVisibility[groupKey] = visible;
                break;
        }
    }

    public IReadOnlyDictionary<string, bool> GetOverlayGroupVisibility() => _overlayGroupVisibility;
}
