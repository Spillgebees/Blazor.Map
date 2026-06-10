using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Spillgebees.Blazor.Map.Engine;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Event args for tracked entity interactions: hands back the caller's own item.
/// </summary>
public sealed record EntityEventArgs<TItem>(TItem Item, string EntityId, Coordinate Position);

/// <summary>
/// Engine-backed tracked entity layer. Devs hand it their domain items plus selectors;
/// updates are diffed against a per-entity snapshot and cross the interop boundary as
/// binary motion frames (positions/rotations) or small structural upserts — never as a
/// rebuilt feature collection, and never through the Blazor render cycle
/// (docs/plans/map-engine-rewrite.md §3.2, §3.5). Decorations are declared as
/// <see cref="EntityDecoration{TItem}"/> children; hover and selection styling run as
/// JS-local feature-state.
/// </summary>
[CascadingTypeParameter(nameof(TItem))]
public sealed class TrackedEntityLayer<TItem> : ComponentBase, IAsyncDisposable
{
    /// <summary>Owning <see cref="SgbMap"/>, provided as a cascading parameter.</summary>
    [CascadingParameter]
    public SgbMap? Map { get; set; }

    /// <summary>Unique layer id; also prefixes the generated engine source and layer ids.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>Domain items to track; each item becomes an entity on the map.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Extracts a stable unique id per item, used to track entities across updates.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string> ItemId { get; set; } = null!;

    /// <summary>Extracts each item's position, driving where its entity renders on the map.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, Coordinate> Position { get; set; } = null!;

    /// <summary>Extracts each item's rotation in degrees clockwise, driving the entity icon's rotation.</summary>
    [Parameter]
    public Func<TItem, double>? Rotation { get; set; }

    /// <summary>Extracts each item's sort key, driving entity render order (lower values render first).</summary>
    [Parameter]
    public Func<TItem, double>? SortKey { get; set; }

    /// <summary>Per-item icon image id; falls back to <see cref="IconImage"/>.</summary>
    [Parameter]
    public Func<TItem, string>? Icon { get; set; }

    /// <summary>Icon image id used for all entities unless <see cref="Icon"/> overrides per item.</summary>
    [Parameter]
    public string? IconImage { get; set; }

    /// <summary>Icon scale factor relative to the image's native size. Defaults to 1.0.</summary>
    [Parameter]
    public double IconSize { get; set; } = 1.0;

    /// <summary>Extracts a per-item color, exposed to entity and decoration styling as the <c>color</c> property.</summary>
    [Parameter]
    public Func<TItem, string>? Color { get; set; }

    /// <summary>Icon scale factor applied while the entity is hovered.</summary>
    [Parameter]
    public Func<TItem, double>? HoverScale { get; set; }

    /// <summary>
    /// Extra feature properties per item — available to style expressions, cluster
    /// aggregations, and display filters. Changes count as structural updates.
    /// </summary>
    [Parameter]
    public Func<TItem, IReadOnlyDictionary<string, object?>>? Properties { get; set; }

    /// <summary>Position interpolation duration for entity movement.</summary>
    [Parameter]
    public TimeSpan? Animate { get; set; }

    /// <summary>Easing applied to <see cref="Animate"/> interpolation.</summary>
    [Parameter]
    public AnimationEasing AnimateEasing { get; set; } = AnimationEasing.Linear;

    /// <summary>Enables JS-local hover feature-state on the layer.</summary>
    [Parameter]
    public bool Interactive { get; set; }

    /// <summary>
    /// Source-level clustering using the library's <see cref="ClusterOptions"/> model:
    /// radius/zoom/min-points, custom aggregation properties, a styleable
    /// <see cref="ClusterLayerSet"/>, and click behavior. Decorations live in a sibling
    /// source so <c>point_count</c> and custom cluster properties count entities.
    /// </summary>
    [Parameter]
    public ClusterOptions? Cluster { get; set; }

    /// <summary>Entity ids rendered with the <c>selected</c> feature-state.</summary>
    [Parameter]
    public IReadOnlyCollection<string>? SelectedIds { get; set; }

    /// <summary>Fires when an entity is clicked; args carry the item, entity id, and click coordinate.</summary>
    [Parameter]
    public EventCallback<EntityEventArgs<TItem>> OnClick { get; set; }

    /// <summary>Fires when the pointer enters an entity; args carry the item, entity id, and pointer coordinate.</summary>
    [Parameter]
    public EventCallback<EntityEventArgs<TItem>> OnHoverEnter { get; set; }

    /// <summary>Fires when the pointer leaves the hovered entity.</summary>
    [Parameter]
    public EventCallback OnHoverLeave { get; set; }

    /// <summary><see cref="EntityDecoration{TItem}"/> children.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private EntityDiffer _differ = new();
    private readonly List<EntityInput> _inputs = [];
    private readonly Dictionary<string, TItem> _itemsById = [];
    private readonly List<EntityDecoration<TItem>> _decorations = [];
    private HashSet<string> _appliedSelection = [];
    private bool _isInitialized;
    private int _clickHandlerId;
    private int _hoverEnterHandlerId;
    private int _hoverLeaveHandlerId;
    private string? _appliedStructure;
    private string _appliedEntitiesId = "";
    private readonly List<string> _appliedEventLayerIds = [];
    private readonly List<string> _appliedLayerIds = [];

    private string _symbolLayerId => $"{Id}-symbols";
    private string _hitAreaLayerId => $"{Id}-hit-area";
    private string _decorationSourceId => $"{Id}-decorations";

    private string DecorationLayerId(EntityDecoration<TItem> decoration) => $"{Id}-decoration-{decoration.Id}";

    private string ClusterLayerId(ClusterLayerDefinition definition) => $"{Id}-{definition.IdSuffix}";

    private bool _clusteringEnabled => Cluster is { Enabled: true };

    private IReadOnlyList<ClusterLayerDefinition> _clusterLayerDefinitions =>
        Cluster is { Enabled: true, LayerSet: { Enabled: true } layerSet } ? layerSet.Layers : [];

    // renders normally, like GeoJsonSource: decoration children re-render with the
    // parent (their per-item selectors flow through the structural hash anyway), and the
    // hot path lives in ProcessUpdate, not in Blazor child rendering.

    /// <summary>Cascades the layer to <see cref="EntityDecoration{TItem}"/> children and renders <see cref="ChildContent"/>.</summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<TrackedEntityLayer<TItem>>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<>.Value), this);
        builder.AddComponentParameter(2, nameof(CascadingValue<>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<>.ChildContent), ChildContent);
        builder.CloseComponent();
    }

    internal void RegisterDecoration(EntityDecoration<TItem> decoration)
    {
        if (_decorations.Any(existing => existing.Id == decoration.Id))
        {
            throw new InvalidOperationException($"Duplicate decoration id '{decoration.Id}' on layer '{Id}'.");
        }

        // post-initialization changes are picked up by the structural check after render
        _decorations.Add(decoration);
    }

    internal void UnregisterDecoration(EntityDecoration<TItem> decoration) => _decorations.Remove(decoration);

    /// <summary>Validates the map cascade and, once initialized, diffs <see cref="Items"/> into motion frames and upserts.</summary>
    protected override void OnParametersSet()
    {
        if (Map is null)
        {
            throw new InvalidOperationException(
                $"{nameof(TrackedEntityLayer<>)} must be nested inside a {nameof(SgbMap)}."
            );
        }

        if (_isInitialized)
        {
            ProcessUpdate();
        }
    }

    /// <summary>On first render, creates the engine layers; afterwards, rebuilds them when structural parameters change.</summary>
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            // initialization happens after the first render so EntityDecoration children
            // have registered themselves.
            Initialize();
            _isInitialized = true;
            ProcessUpdate();
            return;
        }

        // structural parameters (cluster, animation, layer specs, decorations) require
        // the engine layer to be rebuilt — detect changes and reinitialize in place.
        // OnAfterRender runs once the whole subtree settled, so decoration children
        // added or disposed this render are already reflected in _decorations.
        if (ComputeStructuralFingerprint() != _appliedStructure)
        {
            Reinitialize();
        }
    }

    private void Reinitialize()
    {
        QueueTeardownOps();
        _differ = new EntityDiffer();
        _appliedSelection = [];
        Initialize();
        ProcessUpdate();
    }

    /// <summary>
    /// Everything that crosses the wire at creation time: a change means the engine
    /// layer must be torn down and recreated. Selector *outputs* are deliberately
    /// excluded — per-item data changes flow through the normal diff path.
    /// </summary>
    private string ComputeStructuralFingerprint()
    {
        var node = new JsonObject
        {
            ["id"] = Id,
            ["cluster"] = BuildClusterSourceOptions(),
            ["decorationCluster"] = BuildDecorationClusterOptions(),
            ["animateMs"] = Animate?.TotalMilliseconds,
            ["animateEasing"] = (int)AnimateEasing,
            ["interactive"] = Interactive,
            ["hover"] = _hoverIsRelevant,
            ["rotation"] = Rotation is not null,
            ["sortKey"] = SortKey is not null,
            ["click"] = OnClick.HasDelegate,
            ["hoverEnter"] = OnHoverEnter.HasDelegate,
            ["hoverLeave"] = OnHoverLeave.HasDelegate,
            ["symbol"] = BuildSymbolLayerSpec(),
            ["clusterLayers"] = new JsonArray(
                [.. _clusterLayerDefinitions.Select(definition => (JsonNode)BuildClusterLayerSpec(definition))]
            ),
            ["decorations"] = new JsonArray(
                [.. _decorations.Select(decoration => (JsonNode)BuildDecorationLayerSpec(decoration))]
            ),
        };

        return node.ToJsonString();
    }

    private bool _hoverIsRelevant =>
        Interactive
        || HoverScale is not null
        || OnHoverEnter.HasDelegate
        || _decorations.Any(decoration => decoration.DisplayMode != EntityDecorationDisplayMode.Always);

    private bool _interactionIsRelevant =>
        _hoverIsRelevant || OnClick.HasDelegate || OnHoverLeave.HasDelegate;

    /// <summary>
    /// Every layer that participates in hover/click: the invisible hit area, the
    /// primary symbols, and the decorations — hovering a label highlights its entity.
    /// </summary>
    private IEnumerable<string> InteractionLayerIds()
    {
        if (_interactionIsRelevant)
        {
            yield return _hitAreaLayerId;
        }

        yield return _symbolLayerId;
        foreach (var decoration in _decorations)
        {
            yield return DecorationLayerId(decoration);
        }
    }

    private void Initialize()
    {
        var channel = Map!.Channel;

        var clusterZoomLayerIds = _clusterLayerDefinitions
            .Where(definition => definition.Interactive)
            .Select(ClusterLayerId)
            .ToArray();

        channel.Queue(
            new EntitiesCreateOp(
                Id,
                new EngineEntityLayerConfig(
                    Cluster: BuildClusterSourceOptions(),
                    Animation: Animate is { } animate
                        ? new EngineAnimation(
                            (int)animate.TotalMilliseconds,
                            AnimateEasing == AnimationEasing.EaseInOut ? "easeInOut" : "linear"
                        )
                        : null,
                    HoverLayerIds: _hoverIsRelevant ? [.. InteractionLayerIds()] : null,
                    ClusterZoomLayerIds: Cluster is { ClickBehavior: ClusterClickBehavior.ZoomToDissolve }
                    && clusterZoomLayerIds.Length > 0
                        ? clusterZoomLayerIds
                        : null,
                    Decorations: _decorations.Count > 0 ? true : null,
                    DecorationCluster: BuildDecorationClusterOptions()
                )
            )
        );

        _appliedLayerIds.Clear();
        foreach (var definition in _clusterLayerDefinitions)
        {
            channel.Queue(new LayerAddOp(ClusterLayerId(definition), BuildClusterLayerSpec(definition)));
            _appliedLayerIds.Add(ClusterLayerId(definition));
        }

        channel.Queue(new LayerAddOp(_symbolLayerId, BuildSymbolLayerSpec()));
        _appliedLayerIds.Add(_symbolLayerId);

        if (_interactionIsRelevant)
        {
            // invisible enlarged hit target above the symbols, so small icons stay
            // comfortable to hover and click
            channel.Queue(new LayerAddOp(_hitAreaLayerId, BuildHitAreaLayerSpec()));
            _appliedLayerIds.Add(_hitAreaLayerId);
        }

        foreach (var decoration in _decorations)
        {
            channel.Queue(new LayerAddOp(DecorationLayerId(decoration), BuildDecorationLayerSpec(decoration)));
            _appliedLayerIds.Add(DecorationLayerId(decoration));
        }

        _appliedEventLayerIds.Clear();
        RegisterEventHandlers(channel);
        _appliedEntitiesId = Id;
        _appliedStructure = ComputeStructuralFingerprint();
    }

    private void RegisterEventHandlers(MapEngineChannel channel)
    {
        if (OnClick.HasDelegate)
        {
            _clickHandlerId = Map!.Router.Register(payload => HandleEntityEventAsync(payload, OnClick));
        }

        if (OnHoverEnter.HasDelegate)
        {
            _hoverEnterHandlerId = Map!.Router.Register(payload => HandleEntityEventAsync(payload, OnHoverEnter));
        }

        if (OnHoverLeave.HasDelegate)
        {
            _hoverLeaveHandlerId = Map!.Router.Register(_ => OnHoverLeave.InvokeAsync());
        }

        if (_clickHandlerId != 0 || _hoverEnterHandlerId != 0 || _hoverLeaveHandlerId != 0)
        {
            var handlers = new EngineEventHandlers(
                Click: _clickHandlerId == 0 ? null : _clickHandlerId,
                Enter: _hoverEnterHandlerId == 0 ? null : _hoverEnterHandlerId,
                Leave: _hoverLeaveHandlerId == 0 ? null : _hoverLeaveHandlerId
            );
            foreach (var layerId in InteractionLayerIds())
            {
                channel.Queue(new EventsSetOp(layerId, handlers));
                _appliedEventLayerIds.Add(layerId);
            }
        }
    }

    private JsonNode? BuildClusterSourceOptions() => EngineSpec.BuildClusterSourceOptions(Cluster);

    /// <summary>
    /// Decorations cluster in lockstep with primaries so they hide when their entities
    /// cluster; min-points is chosen so one entity's decorations never cluster
    /// alone, two entities' always do).
    /// </summary>
    private JsonObject? BuildDecorationClusterOptions()
    {
        if (!_clusteringEnabled || _decorations.Count == 0)
        {
            return null;
        }

        var options = new JsonObject
        {
            ["cluster"] = true,
            ["clusterRadius"] = Cluster!.Radius,
            ["clusterMinPoints"] = _decorations.Count + 1,
        };

        if (Cluster.MaxZoom is { } maxZoom)
        {
            options["clusterMaxZoom"] = maxZoom;
        }

        return options;
    }

    private JsonObject BuildClusterLayerSpec(ClusterLayerDefinition definition) =>
        EngineSpec.BuildClusterLayerSpec(ClusterLayerId(definition), Id, definition);

    private JsonObject BuildSymbolLayerSpec() =>
        new()
        {
            ["id"] = _symbolLayerId,
            ["type"] = "symbol",
            ["source"] = Id,
            ["filter"] = Expr("!", Expr("has", "point_count")),
            ["layout"] = new JsonObject
            {
                ["icon-image"] = Expr("get", "icon"),
                // icon-size is a layout property and cannot read feature-state; the
                // engine maintains the hoverBoost data property on hover instead.
                ["icon-size"] = Expr(
                    "*",
                    Expr("coalesce", Expr("get", "size"), IconSize),
                    Expr("coalesce", Expr("get", "hoverBoost"), 1)
                ),
                ["icon-rotate"] = Expr("coalesce", Expr("get", "rot"), 0),
                ["icon-allow-overlap"] = true,
                // entities always render: skip the collision index entirely — with
                // thousands of symbols, placement bookkeeping is the dominant
                // main-thread cost and nothing is allowed to collide anyway
                ["icon-ignore-placement"] = true,
                ["icon-rotation-alignment"] = "map",
                ["symbol-sort-key"] = Expr("coalesce", Expr("get", "sortKey"), 0),
            },
        };

    private JsonObject BuildHitAreaLayerSpec() =>
        new()
        {
            ["id"] = _hitAreaLayerId,
            ["type"] = "circle",
            ["source"] = Id,
            ["paint"] = new JsonObject
            {
                ["circle-radius"] = 24,
                ["circle-color"] = "#000000",
                ["circle-opacity"] = 0,
                // paint, not layout — MapLibre rejects it as a layout property
                ["circle-pitch-alignment"] = "viewport",
            },
        };

    private JsonObject BuildDecorationLayerSpec(EntityDecoration<TItem> decoration)
    {
        var anchor = EnumJsonName.Get(decoration.Anchor);
        var offset = decoration.Offset ?? new PixelPoint(0, 0);

        var layout = new JsonObject
        {
            ["text-field"] = Expr("get", "text"),
            ["text-size"] = decoration.TextSize,
            ["text-anchor"] = anchor,
            // text-offset is em-based; decoration offsets are declared in offset units
            // where 10 units = 1 em (so a 16-unit offset clears a default marker icon)
            ["text-offset"] = new JsonArray(offset.X / 10.0, offset.Y / 10.0),
            ["text-allow-overlap"] = true,
            ["text-ignore-placement"] = true,
            ["text-pitch-alignment"] = "viewport",
            ["text-rotation-alignment"] = "viewport",
            ["text-optional"] = true,
            // decorations are single-line annotations: never wrap
            ["text-max-width"] = 999,
        };

        if (decoration.TextFont is { Length: > 0 })
        {
            layout["text-font"] = new JsonArray([.. decoration.TextFont.Select(font => (JsonNode)font)]);
        }

        if (decoration.Icon is not null)
        {
            layout["icon-image"] = Expr("get", "icon");
            layout["icon-size"] = decoration.IconSize;
            layout["icon-anchor"] = anchor;
            layout["icon-offset"] = new JsonArray(offset.X, offset.Y);
            layout["icon-allow-overlap"] = true;
            layout["icon-ignore-placement"] = true;
        }

        var paint = new JsonObject
        {
            ["text-color"] = Expr("coalesce", Expr("get", "color"), "#0f172a"),
        };

        if (decoration.HaloColor is not null)
        {
            paint["text-halo-color"] = decoration.HaloColor;
            paint["text-halo-width"] = decoration.HaloWidth ?? 1.0;
        }

        if (DisplayModeOpacity(decoration.DisplayMode) is { } textOpacity)
        {
            paint["text-opacity"] = textOpacity;
        }

        if (decoration.Icon is not null && DisplayModeOpacity(decoration.DisplayMode) is { } iconOpacity)
        {
            paint["icon-opacity"] = iconOpacity;
        }

        return new JsonObject
        {
            ["id"] = DecorationLayerId(decoration),
            ["type"] = "symbol",
            ["source"] = _decorationSourceId,
            ["filter"] = Expr(
                "all",
                Expr("!", Expr("has", "point_count")),
                Expr("==", Expr("get", "decorationId"), decoration.Id)
            ),
            ["layout"] = layout,
            ["paint"] = paint,
        };
    }

    private static JsonArray? DisplayModeOpacity(EntityDecorationDisplayMode mode) =>
        mode switch
        {
            EntityDecorationDisplayMode.OnHover => Expr("case", HoverState(), 1, 0),
            EntityDecorationDisplayMode.OnHoverOrSelect => Expr("case", Expr("any", HoverState(), SelectedState()), 1, 0),
            _ => null,
        };

    private static JsonArray HoverState() => Expr("boolean", Expr("feature-state", "hover"), false);

    private static JsonArray SelectedState() => Expr("boolean", Expr("feature-state", "selected"), false);

    private void ProcessUpdate()
    {
        _inputs.Clear();
        _itemsById.Clear();

        foreach (var item in Items)
        {
            var id = ItemId(item);
            _itemsById[id] = item;
            var position = Position(item);
            _inputs.Add(
                new EntityInput(
                    id,
                    position.Longitude,
                    position.Latitude,
                    (float)(Rotation?.Invoke(item) ?? 0),
                    (float)(SortKey?.Invoke(item) ?? 0),
                    ComputeStructuralHash(item)
                )
            );
        }

        var diff = _differ.Diff(_inputs);

        if (diff.HasStructuralChanges)
        {
            var upserts = new List<EngineEntityUpsert>(diff.UpsertInputPositions.Count);
            foreach (var inputPosition in diff.UpsertInputPositions)
            {
                upserts.Add(BuildUpsert(_inputs[inputPosition]));
            }

            Map!.Channel.Queue(new EntitiesUpsertOp(Id, diff.Epoch, upserts, [.. diff.RemovedIndices]));
        }

        if (diff.Moved.Count > 0)
        {
            Map!.Channel.PushMotion(
                Id,
                MotionFrameEncoder.Encode(diff.Epoch, diff.Moved, Rotation is not null, SortKey is not null)
            );
        }

        ProcessSelection(diff.HasStructuralChanges);
    }

    private int ComputeStructuralHash(TItem item)
    {
        var hash = new HashCode();
        hash.Add(Icon?.Invoke(item) ?? IconImage);
        hash.Add(Color?.Invoke(item));
        hash.Add(HoverScale?.Invoke(item));
        foreach (var (key, value) in Properties?.Invoke(item) ?? _emptyProperties)
        {
            hash.Add(key);
            hash.Add(value);
        }

        foreach (var decoration in _decorations)
        {
            hash.Add(decoration.Text?.Invoke(item));
            hash.Add(decoration.Icon?.Invoke(item));
            hash.Add(decoration.Color?.Invoke(item));
        }

        return hash.ToHashCode();
    }

    private EngineEntityUpsert BuildUpsert(EntityInput input)
    {
        var item = _itemsById[input.Id];
        List<EngineEntityDecoration>? decorations = null;
        if (_decorations.Count > 0)
        {
            decorations = new List<EngineEntityDecoration>(_decorations.Count);
            for (var slot = 0; slot < _decorations.Count; slot++)
            {
                var decoration = _decorations[slot];
                decorations.Add(
                    new EngineEntityDecoration(
                        slot,
                        decoration.Id,
                        Text: decoration.Text?.Invoke(item),
                        Icon: decoration.Icon?.Invoke(item),
                        Color: decoration.Color?.Invoke(item)
                    )
                );
            }
        }

        return new EngineEntityUpsert(
            _differ.IndexOf(input.Id),
            input.Id,
            input.Lng,
            input.Lat,
            Icon: Icon?.Invoke(item) ?? IconImage,
            Rot: Rotation is null ? null : input.Rotation,
            Color: Color?.Invoke(item),
            SortKey: SortKey is null ? null : input.SortKey,
            Hover: HoverScale is null ? null : new EngineEntityHover(Scale: HoverScale(item)),
            Props: BuildProps(item),
            Decorations: decorations
        );
    }

    private static readonly IReadOnlyDictionary<string, object?> _emptyProperties =
        new Dictionary<string, object?>();

    private JsonObject? BuildProps(TItem item)
    {
        if (Properties?.Invoke(item) is not { Count: > 0 } properties)
        {
            return null;
        }

        var props = new JsonObject();
        foreach (var (key, value) in properties)
        {
            props[key] = JsonSerializer.SerializeToNode(value);
        }

        return props;
    }

    private void ProcessSelection(bool structuralChanged)
    {
        var requested = SelectedIds is null || SelectedIds.Count == 0 ? [] : new HashSet<string>(SelectedIds);
        if (requested.Count == 0 && _appliedSelection.Count == 0)
        {
            return;
        }

        if (!structuralChanged && requested.SetEquals(_appliedSelection))
        {
            return;
        }

        _appliedSelection = requested;
        var indices = new List<uint>(requested.Count);
        foreach (var id in requested)
        {
            if (_differ.TryGetIndex(id, out var index))
            {
                indices.Add(index);
            }
        }

        Map!.Channel.Queue(new EntitiesSelectOp(Id, indices));
    }

    private async Task HandleEntityEventAsync(JsonElement payload, EventCallback<EntityEventArgs<TItem>> callback)
    {
        var entityId = payload.TryGetProperty("entityId", out var idProperty) ? idProperty.GetString() : null;
        if (entityId is null || !_itemsById.TryGetValue(entityId, out var item))
        {
            return;
        }

        var lng = payload.TryGetProperty("lng", out var lngProperty) ? lngProperty.GetDouble() : 0;
        var lat = payload.TryGetProperty("lat", out var latProperty) ? latProperty.GetDouble() : 0;
        await callback.InvokeAsync(new EntityEventArgs<TItem>(item, entityId, new Coordinate(lat, lng)));
    }

    private static JsonArray Expr(params object?[] parts) => EngineSpec.Expr(parts);

    /// <summary>Removes everything <see cref="Initialize"/> added, by its applied ids.</summary>
    private void QueueTeardownOps()
    {
        foreach (var handlerId in (int[])[_clickHandlerId, _hoverEnterHandlerId, _hoverLeaveHandlerId])
        {
            if (handlerId != 0)
            {
                Map!.Router.Unregister(handlerId);
            }
        }

        _clickHandlerId = 0;
        _hoverEnterHandlerId = 0;
        _hoverLeaveHandlerId = 0;

        foreach (var layerId in _appliedEventLayerIds)
        {
            Map!.Channel.Queue(new EventsClearOp(layerId));
        }

        _appliedEventLayerIds.Clear();
        for (var index = _appliedLayerIds.Count - 1; index >= 0; index--)
        {
            Map!.Channel.Queue(new LayerRemoveOp(_appliedLayerIds[index]));
        }

        Map!.Channel.Queue(new EntitiesRemoveOp(_appliedEntitiesId));
    }

    /// <summary>Unregisters event handlers and removes the entity layers and sources from the map.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Map is null || !_isInitialized)
        {
            return;
        }

        QueueTeardownOps();
        await Map.Channel.FlushAsync();
    }
}
