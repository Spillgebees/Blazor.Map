using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Spillgebees.Blazor.Map.Models;
using Spillgebees.Blazor.Map.Models.Events;
using Spillgebees.Blazor.Map.Models.Expressions;
using Spillgebees.Blazor.Map.Models.Popups;
using Spillgebees.Blazor.Map.Models.TrackedData;
using Spillgebees.Blazor.Map.Models.TrackedEntities;
using Spillgebees.Blazor.Map.Utilities;

namespace Spillgebees.Blazor.Map.Components.Layers;

/// <summary>
/// High-level declarative tracked data source for rendering moving entities on a map.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public partial class TrackedDataSource<TItem> : ComponentBase, IAsyncDisposable
{
    private static readonly TimeSpan _hoverLeaveDebounce = TimeSpan.FromMilliseconds(300);
    private static readonly object[] _clusterFilter = Expr.Has("point_count");
    private static readonly object[] _primaryFilter = Expr.All(
        Expr.Eq(TrackedEntityFeatureProperties.Kind, TrackedEntityFeatureKind.Primary.ToMapLibreValue()),
        Expr.Not(Expr.Has("point_count"))
    );
    private static readonly object[] _clusterRadius = Expr.Step("point_count", 22, 10, 28, 50, 36, 100, 44);
    private static readonly object[] _clusterHitAreaRadius = Expr.Step("point_count", 30, 10, 36, 50, 44, 100, 52);
    private static readonly object[] _clusterCountTextField = Expr.Get("point_count_abbreviated");
    private static readonly object[] _renderOrderSortKey =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.RenderOrder },
        0,
    ];
    private static readonly object[] _primaryIconSize =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.IconSize },
        1.0,
    ];
    private static readonly object[] _primaryIconRotate =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.IconRotation },
        0.0,
    ];
    private static readonly object[] _primaryIconOffset =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.Offset },
        new object[] { "literal", new[] { 0.0, 0.0 } },
    ];
    private static readonly object[] _primaryIconAnchor =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.Anchor },
        "center",
    ];
    private static readonly object[] _decorationTextSize =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.TextSize },
        14.0,
    ];
    private static readonly object[] _decorationIconSize =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.IconSize },
        1.0,
    ];
    private static readonly object[] _decorationIconRotate =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.IconRotation },
        0.0,
    ];
    private static readonly object[] _decorationIconOffset =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.Offset },
        new object[] { "literal", new[] { 0.0, 0.0 } },
    ];
    private static readonly object[] _decorationColor =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.Color },
        "#0f172a",
    ];
    private static readonly object[] _hoverState =
    [
        "boolean",
        new object[] { "feature-state", TrackedEntityFeatureStates.Hover.Name },
        false,
    ];
    private static readonly object[] _selectedState =
    [
        "boolean",
        new object[] { "feature-state", TrackedEntityFeatureStates.Selected.Name },
        false,
    ];
    private static readonly object[] _decorationDisplayModeOpacity =
    [
        "case",
        Expr.Eq(
            TrackedEntityFeatureProperties.DisplayMode,
            TrackedEntityDecorationDisplayMode.Always.ToMapLibreValue()
        ),
        1.0,
        Expr.Eq(TrackedEntityFeatureProperties.DisplayMode, TrackedEntityDecorationDisplayMode.Hover.ToMapLibreValue()),
        new object[] { "case", _hoverState, 1.0, 0.0 },
        Expr.Eq(
            TrackedEntityFeatureProperties.DisplayMode,
            TrackedEntityDecorationDisplayMode.Selected.ToMapLibreValue()
        ),
        new object[] { "case", _selectedState, 1.0, 0.0 },
        Expr.Eq(
            TrackedEntityFeatureProperties.DisplayMode,
            TrackedEntityDecorationDisplayMode.HoverOrSelected.ToMapLibreValue()
        ),
        new object[] { "case", new object[] { "any", _hoverState, _selectedState }, 1.0, 0.0 },
        1.0,
    ];
    private static readonly object[] _decorationHaloColor =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.HaloColor },
        "rgba(0,0,0,0)",
    ];
    private static readonly object[] _decorationHaloWidth =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.HaloWidth },
        0.0,
    ];
    private static readonly object[] _decorationIconColor =
    [
        "coalesce",
        new object[] { "get", TrackedEntityFeatureProperties.IconColor },
        "rgba(0,0,0,0)",
    ];

    private GeoJsonSource _primarySource = null!;
    private IReadOnlyDictionary<string, object?> _primaryData =
        TrackedEntityGeoJsonBuilder.BuildPrimaryFeatureCollection<object>([]);
    private IReadOnlyDictionary<string, object?> _decorationData =
        TrackedEntityGeoJsonBuilder.BuildDecorationFeatureCollection<object>([]);
    private IReadOnlyDictionary<string, TrackedEntity<TItem>> _entitiesById = new Dictionary<
        string,
        TrackedEntity<TItem>
    >(StringComparer.Ordinal);
    private IReadOnlyList<TrackedPrimaryProjection> _primaryProjection = [];
    private IReadOnlyList<TrackedDecorationProjection> _decorationProjection = [];
    private IReadOnlyList<TrackedEntity<TItem>> _entities = [];
    private IReadOnlyList<TrackedEntity<TItem>> _renderedEntities = [];
    private IReadOnlyDictionary<string, bool> _appliedHoveredStates = new Dictionary<string, bool>(
        StringComparer.Ordinal
    );
    private IReadOnlyDictionary<string, bool> _appliedSelectedStates = new Dictionary<string, bool>(
        StringComparer.Ordinal
    );
    private CancellationTokenSource? _hoverLeaveCancellationTokenSource;
    private int _hoverGeneration;
    private int _popupOperationGeneration;
    private TrackedPopupState? _activePopup;

    [Parameter, EditorRequired]
    public string SourceId { get; set; } = string.Empty;

    [Parameter]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    [Parameter, EditorRequired]
    public TrackedDataIdOptions<TItem> Id { get; set; } = null!;

    [Parameter, EditorRequired]
    public TrackedDataSymbolOptions<TItem> Symbol { get; set; } = null!;

    [Parameter]
    public IReadOnlyList<TrackedDataDecorationOptions<TItem>> Decorations { get; set; } = [];

    [Parameter]
    public TrackedDataClusterOptions Cluster { get; set; } = new();

    [Parameter]
    public TrackedDataInteractionOptions<TItem> Interaction { get; set; } = new();

    [Parameter]
    public AnimationOptions? Animation { get; set; }

    [Parameter]
    public bool Visible { get; set; } = true;

    [Parameter]
    public StyleValue<double>? PrimaryIconOpacity { get; set; }

    [Parameter]
    public EventCallback<TrackedEntityInteractionEventArgs<TItem>> OnItemClick { get; set; }

    [Parameter]
    public EventCallback<TrackedEntityInteractionEventArgs<TItem>> OnItemMouseEnter { get; set; }

    [Parameter]
    public EventCallback OnItemMouseLeave { get; set; }

    [Parameter]
    public int MaxZoom { get; set; } = 18;

    [Parameter]
    public string? Attribution { get; set; }

    [Parameter]
    public string? Stack { get; set; }

    [Parameter]
    public string? BeforeStack { get; set; }

    [Parameter]
    public string? AfterStack { get; set; }

    [CascadingParameter]
    public BaseMap? Map { get; set; }

    internal string ClusterHitAreaLayerId => $"{SourceId}-cluster-hit-area";

    internal string DecorationSourceId => $"{SourceId}-decorations";

    internal string ClusterLayerId => $"{SourceId}-clusters";

    internal string ClusterCountLayerId => $"{SourceId}-cluster-count";

    internal string PrimaryHitAreaLayerId => $"{SourceId}-hit-area";

    internal string PrimaryLayerId => $"{SourceId}-symbols";

    internal string? PrimaryAfterStack => Cluster.Enabled ? ClusterCountLayerId : null;

    private bool HasDecorations => _entities.Any(e => e.Decorations.Count > 0);

    /// <summary>
    /// Prevent decorations for a single entity from self-clustering.
    /// Each entity produces Decorations.Count features at the same coordinate.
    /// Setting minPoints above that threshold ensures only decorations from
    /// 2+ nearby entities cluster together, matching the primary source behavior.
    /// </summary>
    private int DecorationClusterMinPoints => Decorations.Count + 1;

    internal IReadOnlyList<DecorationLayerDefinition> DecorationLayers =>
        Decorations
            .Select(decoration => new DecorationLayerDefinition(
                decoration,
                decoration.Anchor,
                GetDecorationLayerId(decoration, decoration.Anchor),
                decoration.IconTextFit,
                decoration.IconTextFitPadding,
                decoration.TextFont,
                decoration.IconImageSelector is not null
            ))
            .ToArray();

    internal static object ClusterFilter => _clusterFilter;

    internal static StyleValue<string> ClusterCountTextField => _clusterCountTextField;

    internal static StyleValue<double> ClusterRadiusValue => _clusterRadius;

    internal static StyleValue<double> ClusterHitAreaRadiusValue => _clusterHitAreaRadius;

    internal static StyleValue<double> RenderOrderSortKeyValue => _renderOrderSortKey;

    internal static StyleValue<double> PrimaryIconSizeValue => _primaryIconSize;

    internal static StyleValue<double> PrimaryIconRotateValue => _primaryIconRotate;

    internal static StyleValue<double[]> PrimaryIconOffsetValue => _primaryIconOffset;

    internal static StyleValue<string> PrimaryIconAnchorValue => _primaryIconAnchor;

    internal static StyleValue<double> DecorationTextSizeValue => _decorationTextSize;

    internal static StyleValue<double> DecorationIconSizeValue => _decorationIconSize;

    internal static StyleValue<double> DecorationIconRotateValue => _decorationIconRotate;

    internal static StyleValue<double[]> DecorationIconOffsetValue => _decorationIconOffset;

    internal static StyleValue<string> DecorationColorValue => _decorationColor;

    internal static StyleValue<string> DecorationIconColorValue => _decorationIconColor;

    internal static StyleValue<double> DecorationDisplayModeOpacityValue => _decorationDisplayModeOpacity;

    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(SourceId))
        {
            throw new InvalidOperationException("Tracked data source ID must not be empty.");
        }

        ArgumentNullException.ThrowIfNull(Id);
        ArgumentNullException.ThrowIfNull(Symbol);

        _entities = TrackedDataEntityMaterializer.Materialize(Items, Id, Symbol, Decorations);

        var nextPrimaryProjection = BuildPrimaryProjection(_entities);
        var nextDecorationProjection = BuildDecorationProjection(_entities);

        if (FeatureDiffer.Diff(_primaryProjection, nextPrimaryProjection, static f => f.Id).HasChanges)
            _primaryData = TrackedEntityGeoJsonBuilder.BuildPrimaryFeatureCollection(_entities);

        if (FeatureDiffer.Diff(_decorationProjection, nextDecorationProjection, static f => f.Id).HasChanges)
            _decorationData = TrackedEntityGeoJsonBuilder.BuildDecorationFeatureCollection(_entities);

        _primaryProjection = nextPrimaryProjection;
        _decorationProjection = nextDecorationProjection;
        _entitiesById = _entities.ToDictionary(e => e.Id, StringComparer.Ordinal);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Map is null)
        {
            return;
        }

        var mapReady = await Map.WhenReadyAsync();
        if (!mapReady)
        {
            return;
        }

        await ApplyInteractionDiffsAsync();
    }

    internal static object PrimaryFilter => _primaryFilter;

    internal static object GetDecorationFilter(DecorationLayerDefinition decorationLayer) =>
        Expr.All(
            Expr.Not(Expr.Has("point_count")),
            Expr.Eq(TrackedEntityFeatureProperties.Kind, TrackedEntityFeatureKind.Decoration.ToMapLibreValue()),
            Expr.Eq(TrackedEntityFeatureProperties.DecorationId, decorationLayer.Decoration.Id)
        );

    internal string GetDecorationLayerId(TrackedDataDecorationOptions<TItem> decoration, string? anchor) =>
        anchor is null ? $"{SourceId}-{decoration.Id}" : $"{SourceId}-{decoration.Id}-{anchor}";

    internal static string GetResolvedAnchor(string? anchor) => string.IsNullOrWhiteSpace(anchor) ? "center" : anchor;

    internal static StyleValue<double[]> GetDecorationTextOffsetValue(TrackedDataDecorationOptions<TItem> decoration)
    {
        if (decoration.Offset is null)
        {
            return new[] { 0.0, 0.0 };
        }

        return new[] { decoration.Offset.X / 10.0, decoration.Offset.Y / 10.0 };
    }

    internal static StyleValue<double> DecorationVisibilityOpacityValue => _decorationDisplayModeOpacity;

    internal static StyleValue<string> DecorationHaloColorValue => _decorationHaloColor;

    internal static StyleValue<double> DecorationHaloWidthValue => _decorationHaloWidth;

    public ValueTask<double> GetClusterExpansionZoomAsync(int clusterId) =>
        _primarySource.GetClusterExpansionZoomAsync(clusterId);

    public async ValueTask ZoomClusterToDissolveAsync(LayerFeatureEventArgs clusterEvent)
    {
        if (Map is null)
        {
            throw new InvalidOperationException("Tracked data cluster zoom requires a parent map.");
        }

        var clusterId = clusterEvent.Properties?.GetProperty("cluster_id").GetInt32() ?? 0;
        var zoom = await GetClusterExpansionZoomAsync(clusterId);
        await Map.FlyToAsync(clusterEvent.Position, (int)Math.Ceiling(zoom));
    }

    public ValueTask SetFeatureStateAsync(string entityId, IReadOnlyDictionary<string, object> state)
    {
        if (Map is null)
        {
            throw new InvalidOperationException("Tracked entity feature state requires a parent map.");
        }

        return Map.SetTrackedEntityFeatureStateAsync(SourceId, DecorationSourceId, entityId, state);
    }

    public ValueTask SetFeatureStateAsync(string entityId, KeyValuePair<string, object> state)
    {
        return SetFeatureStateAsync(entityId, new Dictionary<string, object> { [state.Key] = state.Value });
    }

    public bool TryGetEntity(string entityId, out TrackedEntity<TItem>? entity)
    {
        if (_entitiesById.TryGetValue(entityId, out var resolvedEntity))
        {
            entity = resolvedEntity;
            return true;
        }

        entity = null;
        return false;
    }

    public bool TryResolveInteraction(
        LayerFeatureEventArgs featureEvent,
        out TrackedEntityInteractionEventArgs<TItem>? interaction
    )
    {
        if (!TryGetEntityId(featureEvent, out var entityId) || !TryGetEntity(entityId, out var entity))
        {
            interaction = null;
            return false;
        }

        interaction = new TrackedEntityInteractionEventArgs<TItem>(
            entity!,
            featureEvent,
            GetDecorationId(featureEvent)
        );
        return true;
    }

    private async Task ApplyInteractionDiffsAsync()
    {
        var entityRefreshDetected = HaveTrackedEntitiesChangedSinceLastRender();
        var desiredHoveredStates = BuildInteractionLookup(Interaction.IsHovered);
        var desiredSelectedStates = BuildInteractionLookup(Interaction.IsSelected);

        await ClosePopupIfNoLongerValidAsync(desiredSelectedStates);

        await ApplyStateDiffAsync(
            _appliedHoveredStates,
            desiredHoveredStates,
            TrackedEntityFeatureStates.Hover,
            replayActiveStates: entityRefreshDetected
        );
        await ApplyStateDiffAsync(
            _appliedSelectedStates,
            desiredSelectedStates,
            TrackedEntityFeatureStates.Selected,
            replayActiveStates: entityRefreshDetected
        );

        _renderedEntities = _entities;
        _appliedHoveredStates = desiredHoveredStates;
        _appliedSelectedStates = desiredSelectedStates;
    }

    private async Task HandleGeneratedClusterClickAsync(LayerFeatureEventArgs featureEvent)
    {
        if (Cluster.ClickBehavior != TrackedEntityClusterClickBehavior.ZoomToDissolve)
        {
            return;
        }

        await ZoomClusterToDissolveAsync(featureEvent);
    }

    private async Task HandleGeneratedItemClickAsync(LayerFeatureEventArgs featureEvent)
    {
        if (!TryResolveInteraction(featureEvent, out var interaction) || interaction is null)
        {
            return;
        }

        await OnItemClick.InvokeAsync(interaction);
        await HandlePopupOnClickAsync(interaction);
    }

    private async Task HandleGeneratedItemMouseEnterAsync(LayerFeatureEventArgs featureEvent)
    {
        Interlocked.Increment(ref _hoverGeneration);
        CancelPendingHoverLeave();

        if (!TryResolveInteraction(featureEvent, out var interaction) || interaction is null)
        {
            return;
        }

        await OnItemMouseEnter.InvokeAsync(interaction);
        await HandlePopupOnMouseEnterAsync(interaction);
    }

    private async Task HandleGeneratedItemMouseLeaveAsync()
    {
        CancelPendingHoverLeave();

        var hoverGeneration = _hoverGeneration;
        var cancellationTokenSource = new CancellationTokenSource();
        _hoverLeaveCancellationTokenSource = cancellationTokenSource;

        try
        {
            await Task.Delay(_hoverLeaveDebounce, cancellationTokenSource.Token);

            if (
                ReferenceEquals(_hoverLeaveCancellationTokenSource, cancellationTokenSource)
                && hoverGeneration == _hoverGeneration
            )
            {
                if (OnItemMouseLeave.HasDelegate)
                {
                    await OnItemMouseLeave.InvokeAsync();
                }

                await HandlePopupOnMouseLeaveAsync();
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested) { }
        finally
        {
            if (ReferenceEquals(_hoverLeaveCancellationTokenSource, cancellationTokenSource))
            {
                _hoverLeaveCancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _popupOperationGeneration);
        CancelPendingHoverLeave();
        await CloseActivePopupAsync();
    }

    private async Task ApplyStateDiffAsync(
        IReadOnlyDictionary<string, bool> previousStates,
        IReadOnlyDictionary<string, bool> desiredStates,
        FeatureStateKey<bool> key,
        bool replayActiveStates
    )
    {
        var orderedIds = previousStates
            .Keys.Concat(desiredStates.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        foreach (var id in orderedIds)
        {
            var previousValue = previousStates.TryGetValue(id, out var resolvedPreviousValue) && resolvedPreviousValue;
            var desiredValue = desiredStates.TryGetValue(id, out var resolvedDesiredValue) && resolvedDesiredValue;

            if (previousValue == desiredValue && !(replayActiveStates && desiredValue))
            {
                continue;
            }

            await SetFeatureStateAsync(id, key.Set(desiredValue));
        }
    }

    private bool HaveTrackedEntitiesChangedSinceLastRender()
    {
        return FeatureDiffer.Diff(_renderedEntities, _entities, static entity => entity.Id).HasChanges;
    }

    private IReadOnlyDictionary<string, bool> BuildInteractionLookup(Func<TItem, bool>? selector)
    {
        if (selector is null)
        {
            return new Dictionary<string, bool>(StringComparer.Ordinal);
        }

        return Items
            .Select(item => new KeyValuePair<string, bool>(Id.GetId(item), selector(item)))
            .Where(pair => pair.Value)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private void CancelPendingHoverLeave()
    {
        if (_hoverLeaveCancellationTokenSource is null)
        {
            return;
        }

        _hoverLeaveCancellationTokenSource.Cancel();
        _hoverLeaveCancellationTokenSource = null;
    }

    private IDictionary<string, object>? GetClusterProperties() => Cluster.Properties?.ToDictionary();

    private async Task HandlePopupOnClickAsync(TrackedEntityInteractionEventArgs<TItem> interaction)
    {
        if (!TryResolvePopup(interaction, PopupTrigger.Click, out var popup) || popup is null)
        {
            return;
        }

        await OpenPopupAsync(interaction.Entity.Id, interaction.Entity.Position, popup, PopupTrigger.Click);
    }

    private async Task HandlePopupOnMouseEnterAsync(TrackedEntityInteractionEventArgs<TItem> interaction)
    {
        if (!TryResolvePopup(interaction, PopupTrigger.Hover, out var popup) || popup is null)
        {
            return;
        }

        await OpenPopupAsync(interaction.Entity.Id, interaction.Entity.Position, popup, PopupTrigger.Hover);
    }

    private async Task HandlePopupOnMouseLeaveAsync()
    {
        if (_activePopup?.Trigger != PopupTrigger.Hover)
        {
            return;
        }

        await CloseActivePopupAsync();
    }

    private bool TryResolvePopup(
        TrackedEntityInteractionEventArgs<TItem> interaction,
        PopupTrigger trigger,
        out PopupOptions? popup
    )
    {
        popup = null;

        if (interaction.Entity.Item is null)
        {
            return false;
        }

        var resolvedPopup = Symbol.GetPopup(interaction.Entity.Item);
        if (resolvedPopup?.Trigger == PopupTrigger.Permanent)
        {
            return false;
        }

        if (resolvedPopup?.Trigger != trigger)
        {
            return false;
        }

        popup = resolvedPopup;
        return true;
    }

    private async Task OpenPopupAsync(string entityId, Coordinate position, PopupOptions popup, PopupTrigger trigger)
    {
        if (Map is null)
        {
            return;
        }

        var popupOperationGeneration = Interlocked.Increment(ref _popupOperationGeneration);
        _activePopup = new TrackedPopupState(entityId, trigger, popupOperationGeneration);

        await OnBeforeShowPopupAsync();

        if (_activePopup?.OperationGeneration != popupOperationGeneration)
        {
            return;
        }

        await Map.ShowPopupAsync(position, popup.Content, popup);
    }

    protected virtual Task OnBeforeShowPopupAsync()
    {
        return Task.CompletedTask;
    }

    private async Task CloseActivePopupAsync()
    {
        if (_activePopup is null)
        {
            return;
        }

        _activePopup = null;

        if (Map is null)
        {
            return;
        }

        await Map.ClosePopupAsync();
    }

    private async Task ClosePopupIfNoLongerValidAsync(IReadOnlyDictionary<string, bool> desiredSelectedStates)
    {
        if (_activePopup is null)
        {
            return;
        }

        if (!_entitiesById.ContainsKey(_activePopup.EntityId))
        {
            await CloseActivePopupAsync();
            return;
        }

        if (
            _activePopup.Trigger == PopupTrigger.Click
            && Interaction.IsSelected is not null
            && !desiredSelectedStates.ContainsKey(_activePopup.EntityId)
        )
        {
            await CloseActivePopupAsync();
        }
    }

    private static bool TryGetEntityId(LayerFeatureEventArgs featureEvent, out string entityId)
    {
        entityId = string.Empty;

        if (featureEvent.Properties is not JsonElement properties)
        {
            return false;
        }

        if (
            !properties.TryGetProperty(TrackedEntityFeatureProperties.EntityId, out var entityIdProperty)
            || entityIdProperty.ValueKind != JsonValueKind.String
        )
        {
            return false;
        }

        entityId = entityIdProperty.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(entityId);
    }

    private static string? GetDecorationId(LayerFeatureEventArgs featureEvent)
    {
        if (featureEvent.Properties is not JsonElement properties)
        {
            return null;
        }

        if (
            !properties.TryGetProperty(TrackedEntityFeatureProperties.DecorationId, out var decorationIdProperty)
            || decorationIdProperty.ValueKind != JsonValueKind.String
        )
        {
            return null;
        }

        return decorationIdProperty.GetString();
    }

    private static IReadOnlyList<TrackedPrimaryProjection> BuildPrimaryProjection(
        IReadOnlyList<TrackedEntity<TItem>> entities
    ) =>
        entities
            .Select(entity => new TrackedPrimaryProjection(
                entity.Id,
                entity.Position,
                entity.Symbol,
                entity.Color,
                entity.Hover,
                entity.RenderOrder,
                entity.Properties
            ))
            .ToArray();

    private static IReadOnlyList<TrackedDecorationProjection> BuildDecorationProjection(
        IReadOnlyList<TrackedEntity<TItem>> entities
    ) =>
        entities
            .SelectMany(entity =>
                entity.Decorations.Select(decoration => new TrackedDecorationProjection(
                    $"{entity.Id}::{decoration.Id}",
                    entity.Id,
                    entity.Position,
                    entity.Color,
                    entity.Hover,
                    entity.RenderOrder,
                    entity.Properties,
                    decoration
                ))
            )
            .ToArray();

    private sealed record TrackedPrimaryProjection(
        string Id,
        Coordinate Position,
        TrackedEntitySymbol Symbol,
        string? Color,
        TrackedEntityHoverIntent? Hover,
        double? RenderOrder,
        IReadOnlyDictionary<string, object?>? Properties
    );

    private sealed record TrackedDecorationProjection(
        string Id,
        string EntityId,
        Coordinate Position,
        string? Color,
        TrackedEntityHoverIntent? Hover,
        double? RenderOrder,
        IReadOnlyDictionary<string, object?>? Properties,
        TrackedEntityDecoration Decoration
    );

    internal sealed record DecorationLayerDefinition(
        TrackedDataDecorationOptions<TItem> Decoration,
        string? Anchor,
        string LayerId,
        string? IconTextFit,
        double[]? IconTextFitPadding,
        string[]? TextFont,
        bool HasIcon
    );

    private sealed record TrackedPopupState(string EntityId, PopupTrigger Trigger, int OperationGeneration);
}
