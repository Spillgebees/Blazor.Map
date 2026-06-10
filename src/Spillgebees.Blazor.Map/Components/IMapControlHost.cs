using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// The host surface map control components talk to (via
/// <see cref="MapControlRegistryContext"/>), implemented by <see cref="SgbMap"/>.
/// </summary>
internal interface IMapControlHost
{
    bool RegisterControl(string ownerId, MapControlDefinition control);

    bool UnregisterControl(string controlId);

    bool UnregisterControlByOwner(string ownerId);

    bool RuntimeIsReady { get; }

    Task<bool> WhenReadyAsync();

    ValueTask SyncControlsAsync();

    ValueTask SetControlContentAsync(
        string controlId,
        string kind,
        ElementReference placeholderReference,
        ElementReference contentReference,
        Func<bool, Task>? onPanelOpenChangedAsync = null
    );

    ValueTask RemoveControlContentAsync(string controlId);
}

/// <summary>
/// The host surface <see cref="MapPopup"/> binds to, implemented by both maps.
/// </summary>
internal interface IMapInteropHost
{
    bool RuntimeIsReady { get; }

    Task<bool> WhenReadyAsync();

    ValueTask SetPopupContentAsync(
        string popupId,
        Coordinate position,
        PopupOptions options,
        ElementReference placeholderReference,
        ElementReference contentReference,
        Func<Task> onClosedAsync
    );

    ValueTask RemovePopupContentAsync(string popupId);
}

/// <summary>
/// The host surface marker/shape components bind to, implemented by both maps.
/// </summary>
internal interface IMapFeatureHost
{
    ValueTask SetOverlayMarkersAsync(string ownerId, IReadOnlyList<Marker> markers);

    ValueTask SetOverlayCirclesAsync(string ownerId, IReadOnlyList<Circle> circles);

    ValueTask SetOverlayPolylinesAsync(string ownerId, IReadOnlyList<Polyline> polylines);

    ValueTask RemoveOverlayFeaturesAsync(string ownerId);
}

/// <summary>
/// The host surface <see cref="OverlayMapControl"/> binds to, implemented by both maps.
/// </summary>
internal interface IMapOverlayHost
{
    event EventHandler<MapOverlayChangedEventArgs>? OverlayChanged;

    IReadOnlyList<MapOverlayItem> GetOverlayItems();

    void SetOverlayVisible(string overlayId, bool visible);

    void SetOverlayPartVisible(string overlayId, string partId, bool visible);
}
