using System.Text.Json.Nodes;

namespace Spillgebees.Blazor.Map.Engine;

/// <summary>
/// Tile overlays (<see cref="TileOverlay"/>) → ops. Each overlay is an ordinary raster
/// source + layer pair (so it rides style replay for free); referrer policies register
/// per URL origin for the engine's transformRequest.
/// </summary>
internal sealed class MapTileOverlayCoordinator(MapEngineChannel channel)
{
    private readonly Dictionary<string, TileOverlay> _synced = [];
    private IReadOnlyList<TileOverlay>? _appliedParameter;

    /// <summary>Applies the map's Overlays parameter, reference-diffed.</summary>
    public void SyncParameter(IReadOnlyList<TileOverlay>? overlays)
    {
        if (ReferenceEquals(overlays, _appliedParameter))
        {
            return;
        }

        _appliedParameter = overlays;
        Sync(overlays ?? []);
    }

    private void Sync(IReadOnlyList<TileOverlay> overlays)
    {
        var desired = new Dictionary<string, TileOverlay>();
        foreach (var overlay in overlays)
        {
            desired[overlay.Id] = overlay;
        }

        List<string>? removedIds = null;
        foreach (var id in _synced.Keys)
        {
            if (!desired.ContainsKey(id))
            {
                (removedIds ??= []).Add(id);
            }
        }

        foreach (var id in removedIds ?? [])
        {
            QueueRemove(id, _synced[id]);
            _synced.Remove(id);
        }

        foreach (var (id, overlay) in desired)
        {
            if (_synced.TryGetValue(id, out var synced) && synced == overlay)
            {
                continue;
            }

            if (synced is not null)
            {
                QueueRemove(id, synced);
            }

            _synced[id] = overlay;
            QueueAdd(overlay);
        }
    }

    private static string SourceId(string overlayId) => $"sgb-overlay-{overlayId}";

    private void QueueAdd(TileOverlay overlay)
    {
        if (overlay.ReferrerPolicy is { } policy && TryGetOrigin(overlay.UrlTemplate, out var origin))
        {
            channel.Queue(new MapRequestPolicyOp(origin, EnumJsonName.Get(policy)));
        }

        var sourceId = SourceId(overlay.Id);
        channel.Queue(
            new SourceAddOp(
                sourceId,
                new JsonObject
                {
                    ["type"] = "raster",
                    ["tiles"] = new JsonArray(overlay.UrlTemplate),
                    ["tileSize"] = overlay.TileSize,
                    ["attribution"] = overlay.Attribution,
                }
            )
        );
        channel.Queue(
            new LayerAddOp(
                sourceId,
                new JsonObject
                {
                    ["id"] = sourceId,
                    ["type"] = "raster",
                    ["source"] = sourceId,
                    ["paint"] = new JsonObject { ["raster-opacity"] = overlay.Opacity },
                }
            )
        );
    }

    private void QueueRemove(string overlayId, TileOverlay overlay)
    {
        var sourceId = SourceId(overlayId);
        channel.Queue(new LayerRemoveOp(sourceId));
        channel.Queue(new SourceRemoveOp(sourceId));
        if (overlay.ReferrerPolicy is not null && TryGetOrigin(overlay.UrlTemplate, out var origin))
        {
            channel.Queue(new MapRequestPolicyOp(origin, Policy: null));
        }
    }

    private static bool TryGetOrigin(string urlTemplate, out string origin)
    {
        if (Uri.TryCreate(urlTemplate, UriKind.Absolute, out var uri))
        {
            origin = uri.GetLeftPart(UriPartial.Authority);
            return true;
        }

        origin = string.Empty;
        return false;
    }
}
